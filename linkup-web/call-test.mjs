// E2E: start calls from the CHAT header (the discoverable entry point), both video and audio.
// A (admin) calls B from their conversation; B accepts; verify the peer connection establishes.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import puppeteer from 'puppeteer-core';

const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const API = 'https://localhost:5001/api/v1';
const WEB = 'http://localhost:4200';

async function api(method, path, body, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(`${API}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  return res.json();
}

async function setAuth(page, auth) {
  await page.goto(`${WEB}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(a => {
    localStorage.setItem('linkup_access_token', a.accessToken);
    localStorage.setItem('linkup_refresh_token', a.refreshToken);
    localStorage.setItem('linkup_user', JSON.stringify(a.user));
  }, auth);
}

// Prime the EXACT constraint shape the call will use — headless fake-media stalls
// on the first getUserMedia of each shape.
const warmUp = (page, video) => page.evaluate(async v => {
  try { (await navigator.mediaDevices.getUserMedia({ video: v, audio: true })).getTracks().forEach(t => t.stop()); } catch {}
}, video);

const log = (...a) => console.log(...a);

(async () => {
  const admin = (await api('POST', '/auth/login', { email: 'admin@linkup.com', password: 'Admin@123' })).data;
  const stamp = Date.now();
  const b = (await api('POST', '/auth/register', {
    firstName: 'Chat', lastName: 'Callee', email: `cc${stamp}@test.com`,
    userName: `cc${stamp}`, password: 'Passw0rd!', confirmPassword: 'Passw0rd!'
  })).data;
  // Create a direct chat so it shows in both users' chat lists.
  await api('POST', '/chats/direct', { targetUserId: b.user.id }, admin.accessToken);
  log('admin & callee ready; direct chat created. callee id:', b.user.id);

  const browser = await puppeteer.launch({
    executablePath: CHROME, headless: 'new',
    args: ['--use-fake-device-for-media-stream', '--use-fake-ui-for-media-stream',
      '--autoplay-policy=no-user-gesture-required', '--disable-features=WebRtcHideLocalIpsWithMdns',
      '--ignore-certificate-errors', '--no-sandbox'],
  });

  const results = [];

  async function scenario(mode, btnTestId) {
    const ctxA = await browser.createBrowserContext();
    const ctxB = await browser.createBrowserContext();
    const A = await ctxA.newPage();
    const B = await ctxB.newPage();
    try {
      await setAuth(A, admin);
      await setAuth(B, b);
      await A.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
      await B.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
      await A.waitForFunction(() => window.__callHubConnected === true, { timeout: 20000 });
      await B.waitForFunction(() => window.__callHubConnected === true, { timeout: 20000 });
      await warmUp(A, mode === 'video'); await warmUp(B, mode === 'video');

      // A opens the conversation with B and starts the call from the header.
      await A.goto(`${WEB}/messages`, { waitUntil: 'networkidle2' });
      await A.waitForSelector(`[data-testid=chat-item-${b.user.id}]`, { timeout: 15000 });
      await A.click(`[data-testid=chat-item-${b.user.id}]`);
      await A.waitForSelector(`[data-testid=${btnTestId}]`, { timeout: 10000 });
      await A.click(`[data-testid=${btnTestId}]`);

      // B answers.
      await B.waitForSelector('[data-testid=accept-call]', { timeout: 15000 });
      await B.click('[data-testid=accept-call]');

      const connected = () => {
        const v = window.__videoCall;
        return v && (v.pcState === 'connected' || v.iceState === 'connected' || v.iceState === 'completed');
      };
      try {
        await Promise.all([
          A.waitForFunction(connected, { timeout: 30000 }),
          B.waitForFunction(connected, { timeout: 30000 }),
        ]);
      } catch { /* fall through to dump */ }

      const sa = await A.evaluate(() => window.__videoCall);
      const sb = await B.evaluate(() => window.__videoCall);
      const connOk = s => s && ['connected', 'completed'].includes(s.pcState === 'connected' ? 'connected' : s.iceState);
      const mediaOk = mode === 'video' ? (sa.hasRemote && sb.hasRemote) : true;
      const modeOk = (sa.video === (mode === 'video')) && (sb.video === (mode === 'video'));
      const ok = connOk(sa) && connOk(sb) && mediaOk && modeOk && sa.role === 'caller' && sb.role === 'callee';
      log(`[${mode}] A:`, JSON.stringify(sa));
      log(`[${mode}] B:`, JSON.stringify(sb));
      results.push({ mode, ok });
    } finally {
      await ctxA.close(); await ctxB.close();
    }
  }

  try {
    await scenario('video', 'chat-video-call');
    await scenario('audio', 'chat-voice-call');
  } finally {
    await browser.close();
  }

  results.forEach(r => log(`RESULT ${r.mode}: ${r.ok ? 'PASS' : 'FAIL'}`));
  const allOk = results.length === 2 && results.every(r => r.ok);
  log(allOk ? '\nALL PASS — video + voice calls connect via the chat header' : '\nFAIL');
  process.exit(allOk ? 0 : 1);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
