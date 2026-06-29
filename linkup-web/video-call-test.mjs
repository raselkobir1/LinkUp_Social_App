// End-to-end video call test: two isolated browser contexts with fake media.
// A (admin) calls B from B's profile; B accepts; verify the RTCPeerConnection connects.
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

const stamp = Date.now();
const calleeReg = {
  firstName: 'Callee', lastName: 'User', email: `callee${stamp}@test.com`,
  userName: `callee${stamp}`, password: 'Passw0rd!', confirmPassword: 'Passw0rd!'
};

async function setAuth(page, auth) {
  await page.goto(`${WEB}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(a => {
    localStorage.setItem('linkup_access_token', a.accessToken);
    localStorage.setItem('linkup_refresh_token', a.refreshToken);
    localStorage.setItem('linkup_user', JSON.stringify(a.user));
  }, auth);
}

const log = (...a) => console.log(...a);

(async () => {
  // 1. Set up two accounts via the API.
  const admin = (await api('POST', '/auth/login', { email: 'admin@linkup.com', password: 'Admin@123' })).data;
  const callee = (await api('POST', '/auth/register', calleeReg)).data;
  log('admin id:', admin.user.id, '| callee id:', callee.user.id);

  const browser = await puppeteer.launch({
    executablePath: CHROME,
    headless: 'new',
    args: [
      '--use-fake-device-for-media-stream',
      '--use-fake-ui-for-media-stream',
      '--autoplay-policy=no-user-gesture-required',
      // Expose real loopback ICE candidates so two local contexts can connect headless.
      '--disable-features=WebRtcHideLocalIpsWithMdns',
      '--ignore-certificate-errors',
      '--no-sandbox',
    ],
  });

  const errsA = [], errsB = [];
  try {
    // 2. Two isolated contexts so each has its own auth/localStorage.
    const ctxA = await browser.createBrowserContext();
    const ctxB = await browser.createBrowserContext();
    const pageA = await ctxA.newPage();
    const pageB = await ctxB.newPage();
    pageA.on('pageerror', e => errsA.push(e.message));
    pageB.on('pageerror', e => errsB.push(e.message));

    // 3. Authenticate both and land on the feed (shell connects the call hub).
    await setAuth(pageA, admin);
    await setAuth(pageB, callee);
    await pageA.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
    await pageB.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });

    // 4. Wait for both call hubs to be connected.
    await pageA.waitForFunction(() => window.__callHubConnected === true, { timeout: 20000 });
    await pageB.waitForFunction(() => window.__callHubConnected === true, { timeout: 20000 });
    log('both call hubs connected');

    // Diagnostic: does getUserMedia work in this headless context?
    const media = await pageA.evaluate(async () => {
      try {
        if (!navigator.mediaDevices) return 'NO navigator.mediaDevices';
        const s = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        return 'ok tracks=' + s.getTracks().map(t => t.kind).join(',');
      } catch (e) { return 'ERR ' + e.name + ': ' + e.message; }
    });
    log('getUserMedia probe (A):', media);
    pageA.on('console', m => { if (m.type() === 'error') errsA.push('console: ' + m.text()); });
    pageB.on('console', m => { if (m.type() === 'error') errsB.push('console: ' + m.text()); });

    // 5. A opens B's profile and starts the call.
    await pageA.goto(`${WEB}/profile/${callee.user.id}`, { waitUntil: 'networkidle2' });
    await pageA.waitForSelector('[data-testid=start-video-call]', { timeout: 15000 });
    await pageA.click('[data-testid=start-video-call]');
    log('A clicked Video call');

    // 6. B sees the incoming banner and accepts.
    await pageB.waitForSelector('[data-testid=incoming-call]', { timeout: 15000 });
    log('B sees incoming call banner');
    await pageB.waitForSelector('[data-testid=accept-call]', { timeout: 5000 });
    await pageB.click('[data-testid=accept-call]');
    log('B accepted');

    // 7. Wait for the peer connection to establish on both sides.
    const connected = () => {
      const v = window.__videoCall;
      return v && (v.pcState === 'connected' || v.iceState === 'connected' || v.iceState === 'completed');
    };
    try {
      await Promise.all([
        pageA.waitForFunction(connected, { timeout: 30000 }),
        pageB.waitForFunction(connected, { timeout: 30000 }),
      ]);
    } catch {
      log('timed out waiting for connection — dumping state');
    }

    const stateA = await pageA.evaluate(() => window.__videoCall);
    const stateB = await pageB.evaluate(() => window.__videoCall);
    log('A state:', JSON.stringify(stateA));
    log('B state:', JSON.stringify(stateB));

    const ok =
      ['connected', 'completed'].includes(stateA.pcState === 'connected' ? 'connected' : stateA.iceState) &&
      ['connected', 'completed'].includes(stateB.pcState === 'connected' ? 'connected' : stateB.iceState) &&
      stateA.hasRemote && stateB.hasRemote &&
      stateA.role === 'caller' && stateB.role === 'callee';

    log('\npageA errors:', errsA.length, errsA.slice(0, 3));
    log('pageB errors:', errsB.length, errsB.slice(0, 3));
    log('\nRESULT:', ok ? 'PASS — video call connected end-to-end (both peers, remote media present)' : 'FAIL');
    process.exit(ok ? 0 : 1);
  } finally {
    await browser.close();
  }
})().catch(e => { console.error('FATAL', e); process.exit(2); });
