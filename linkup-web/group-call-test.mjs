// E2E: 3-way group call + call history.
// A(admin) calls B (1:1) → A adds C from the in-call invite panel → all three connect (mesh).
// Then verify the call is recorded in A's call history.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import puppeteer from 'puppeteer-core';

const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const API = 'https://localhost:5001/api/v1';
const WEB = 'http://localhost:4200';
const log = (...a) => console.log(...a);

async function api(method, path, body, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(`${API}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  return { status: res.status, json: await res.json().catch(() => null) };
}

async function register(tag) {
  const s = `${tag}${Date.now()}${Math.floor(performance.now())}`;
  const r = await api('POST', '/auth/register', {
    firstName: tag, lastName: 'User', email: `${s}@test.com`, userName: s, password: 'Passw0rd!', confirmPassword: 'Passw0rd!'
  });
  return r.json.data; // { accessToken, refreshToken, user }
}

// Make `a` and `b` friends so they show in each other's friend list.
async function makeFriends(a, b) {
  await api('POST', '/friends/request', { receiverId: b.user.id }, a.accessToken);
  const pending = await api('GET', '/friends/requests/pending', null, b.accessToken);
  const req = pending.json.data.items.find(x => x.senderId === a.user.id) || pending.json.data.items[0];
  await api('PUT', `/friends/request/${req.id}/accept`, {}, b.accessToken);
}

async function setAuth(page, auth) {
  await page.goto(`${WEB}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(a => {
    localStorage.setItem('linkup_access_token', a.accessToken);
    localStorage.setItem('linkup_refresh_token', a.refreshToken);
    localStorage.setItem('linkup_user', JSON.stringify(a.user));
  }, auth);
}
const warm = page => page.evaluate(async () => {
  try { (await navigator.mediaDevices.getUserMedia({ video: true, audio: true })).getTracks().forEach(t => t.stop()); } catch {}
});

(async () => {
  const admin = (await api('POST', '/auth/login', { email: 'admin@linkup.com', password: 'Admin@123' })).json.data;
  const B = await register('Bob');
  const C = await register('Carol');
  await makeFriends(admin, B);
  await makeFriends(admin, C);
  log('setup done. admin friends with B & C. B:', B.user.id, 'C:', C.user.id);

  const browser = await puppeteer.launch({
    executablePath: CHROME, headless: 'new',
    args: ['--use-fake-device-for-media-stream', '--use-fake-ui-for-media-stream',
      '--autoplay-policy=no-user-gesture-required', '--disable-features=WebRtcHideLocalIpsWithMdns',
      '--ignore-certificate-errors', '--no-sandbox'],
  });

  try {
    const ctxA = await browser.createBrowserContext();
    const ctxB = await browser.createBrowserContext();
    const ctxC = await browser.createBrowserContext();
    const A = await ctxA.newPage(), Bp = await ctxB.newPage(), Cp = await ctxC.newPage();
    const errs = { A: [], B: [], C: [] };
    A.on('pageerror', e => errs.A.push(e.message)); A.on('console', m => m.type() === 'error' && errs.A.push('c:' + m.text()));
    Bp.on('pageerror', e => errs.B.push(e.message)); Bp.on('console', m => m.type() === 'error' && errs.B.push('c:' + m.text()));
    Cp.on('pageerror', e => errs.C.push(e.message)); Cp.on('console', m => m.type() === 'error' && errs.C.push('c:' + m.text()));

    await setAuth(A, admin); await setAuth(Bp, B); await setAuth(Cp, C);
    await A.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
    await Bp.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
    await Cp.goto(`${WEB}/feed`, { waitUntil: 'networkidle2' });
    await Promise.all([A, Bp, Cp].map(p => p.waitForFunction(() => window.__callHubConnected === true, { timeout: 20000 })));
    await Promise.all([warm(A), warm(Bp), warm(Cp)]);
    log('all hubs connected');

    // A calls B from B's profile.
    await A.goto(`${WEB}/profile/${B.user.id}`, { waitUntil: 'networkidle2' });
    await A.waitForSelector('[data-testid=start-video-call]', { timeout: 15000 });
    await A.click('[data-testid=start-video-call]');
    await Bp.waitForSelector('[data-testid=accept-call]', { timeout: 15000 });
    await Bp.click('[data-testid=accept-call]');
    log('A↔B call started; B accepted');

    const waitConn = (page, n) => page.waitForFunction(
      min => (window.__videoCall?.connectedCount ?? 0) >= min, { timeout: 30000 }, n);
    try {
      await Promise.all([waitConn(A, 1), waitConn(Bp, 1)]);
      log('A↔B connected (1:1)');
    } catch {
      log('A↔B did NOT connect. State A:', JSON.stringify(await A.evaluate(() => window.__videoCall)));
      log('State B:', JSON.stringify(await Bp.evaluate(() => window.__videoCall)));
      log('A url:', A.url(), '| B url:', Bp.url());
      log('errs A:', errs.A.slice(0, 4)); log('errs B:', errs.B.slice(0, 4));
      throw new Error('1:1 failed');
    }

    // A invites C via the in-call panel.
    await A.waitForSelector('[data-testid=add-person]', { timeout: 10000 });
    await A.click('[data-testid=add-person]');
    await A.waitForSelector(`[data-testid=invite-${C.user.id}]`, { timeout: 10000 });
    await A.click(`[data-testid=invite-${C.user.id}]`);
    log('A invited C');
    await Cp.waitForSelector('[data-testid=accept-call]', { timeout: 15000 });
    await Cp.click('[data-testid=accept-call]');
    log('C accepted');

    // Now everyone should be connected to the other two.
    await Promise.all([waitConn(A, 2), waitConn(Bp, 2), waitConn(Cp, 2)])
      .catch(() => log('timeout waiting for 3-way'));

    const sA = await A.evaluate(() => window.__videoCall);
    const sB = await Bp.evaluate(() => window.__videoCall);
    const sC = await Cp.evaluate(() => window.__videoCall);
    log('A:', JSON.stringify(sA));
    log('B:', JSON.stringify(sB));
    log('C:', JSON.stringify(sC));
    const meshOk = sA.connectedCount === 2 && sB.connectedCount === 2 && sC.connectedCount === 2;

    // Verify call history was recorded for admin.
    const hist = await api('GET', '/video-calls/history', null, admin.accessToken);
    const calls = hist.json?.data?.items ?? [];
    const groupCall = calls.find(c => c.participants?.length >= 3) || calls[0];
    log('history calls:', calls.length, '| top call participants:', groupCall?.participants?.length, '| type:', groupCall?.type, '| status:', groupCall?.status);
    const historyOk = calls.length > 0 && (groupCall?.participants?.length ?? 0) >= 3;

    log(`\nRESULT group mesh: ${meshOk ? 'PASS' : 'FAIL'}`);
    log(`RESULT call history: ${historyOk ? 'PASS' : 'FAIL'}`);
    log(meshOk && historyOk ? '\nALL PASS — 3-way group call connected + recorded in history' : '\nFAIL');
    process.exit(meshOk && historyOk ? 0 : 1);
  } finally {
    await browser.close();
  }
})().catch(e => { console.error('FATAL', e); process.exit(2); });
