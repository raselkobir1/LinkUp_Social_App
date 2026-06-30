// Verifies that when the callee declines, the CALLER's call also ends (both sides sync).
import puppeteer from 'puppeteer-core';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const BASE = 'https://localhost:5001/api/v1';
const S = Date.now().toString().slice(-7);

const j = async r => (await r.json());
async function call(m, p, { t, b } = {}) {
  const h = {}; if (t) h.Authorization = 'Bearer ' + t; if (b) h['Content-Type'] = 'application/json';
  return j(await fetch(BASE + p, { method: m, headers: h, body: b ? JSON.stringify(b) : undefined }));
}
async function reg(f, l, h) {
  const r = await call('POST', '/auth/register', { b: { firstName: f, lastName: l, email: h + S + '@x.com', userName: h + S, password: 'Passw0rd!', confirmPassword: 'Passw0rd!' } });
  return { id: r.data.user.id, email: h + S + '@x.com' };
}

const A = await reg('Caller', 'X', 'callerx');
const B = await reg('Callee', 'X', 'calleex');
const aTok = (await call('POST', '/auth/login', { b: { email: A.email, password: 'Passw0rd!' } })).data.accessToken;
await call('POST', '/friends/request', { t: aTok, b: { receiverId: B.id } });
const bTok = (await call('POST', '/auth/login', { b: { email: B.email, password: 'Passw0rd!' } })).data.accessToken;
const req = (await call('GET', '/friends/requests/pending', { t: bTok })).data.items.find(r => r.senderId === A.id);
await call('PUT', '/friends/request/' + req.id + '/accept', { t: bTok });

const launch = () => puppeteer.launch({ executablePath: CHROME, headless: 'new', ignoreHTTPSErrors: true,
  args: ['--ignore-certificate-errors', '--no-sandbox', '--use-fake-ui-for-media-stream', '--use-fake-device-for-media-stream', '--autoplay-policy=no-user-gesture-required'] });
async function login(page, email) {
  await page.goto('http://localhost:4200/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 1200));
  await page.type('input[type=email]', email, { delay: 8 });
  await page.type('input[type=password]', 'Passw0rd!', { delay: 8 });
  (await page.$('button[type=submit]') || await page.$('button')).click();
  await new Promise(r => setTimeout(r, 3500));
}

const callerB = await launch(); const calleeB = await launch();
try {
  const calleePage = await calleeB.newPage();
  await login(calleePage, B.email);
  const callerPage = await callerB.newPage();
  await login(callerPage, A.email);

  // Caller starts the call
  await callerPage.goto('http://localhost:4200/profile/' + B.id, { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 1800));
  await (await callerPage.$('[data-testid=start-video-call]')).click();
  await new Promise(r => setTimeout(r, 3000));

  console.log('caller on video-call screen:', callerPage.url().includes('/video-call'));
  const callerStatusBefore = await callerPage.evaluate(() => window.__videoCall?.status);
  console.log('caller status before decline:', callerStatusBefore);

  // Callee declines
  const declineBtn = await calleePage.$('[data-testid=decline-call]');
  console.log('callee sees Decline button:', !!declineBtn);
  await declineBtn.click();

  // Wait for the decline to propagate to the caller
  await new Promise(r => setTimeout(r, 3000));

  const callerStatusAfter = await callerPage.evaluate(() => window.__videoCall?.status);
  const callerLeft = !callerPage.url().includes('/video-call');
  const calleeBanner = await calleePage.$('[data-testid=incoming-call]');
  console.log('caller status after decline:', callerStatusAfter, '| caller left call screen:', callerLeft);
  console.log('callee banner gone:', !calleeBanner);

  const pass = (callerStatusAfter === 'ended' || callerLeft) && !calleeBanner;
  console.log('\nRESULT:', pass ? 'PASS — decline ends the call on BOTH sides' : 'FAIL');
  process.exit(pass ? 0 : 1);
} catch (e) { console.error('ERROR', e.message); process.exit(2); }
finally { await callerB.close(); await calleeB.close(); }
