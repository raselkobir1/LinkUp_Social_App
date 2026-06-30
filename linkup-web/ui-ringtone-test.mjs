// Verifies the callee's PC plays a ringtone when a friend calls.
// Caller (A) clicks Video call on callee (B)'s profile; B should ring + show banner.
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

// 1) Two users, made friends via API
const A = await reg('Caller', 'One', 'caller');
const B = await reg('Callee', 'Two', 'callee');
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

const callerB = await launch();
const calleeB = await launch();
try {
  // Callee: instrument AudioContext to flag when the ringtone oscillator starts
  const calleePage = await calleeB.newPage();
  await calleePage.evaluateOnNewDocument(() => {
    const Real = window.AudioContext || window.webkitAudioContext;
    if (Real) window.AudioContext = class extends Real {
      createOscillator() { window.__ringStarted = true; return super.createOscillator(); }
    };
  });
  await login(calleePage, B.email);

  const callerPage = await callerB.newPage();
  await login(callerPage, A.email);

  // Caller opens callee's profile and clicks Video call
  await callerPage.goto('http://localhost:4200/profile/' + B.id, { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 2000));
  const vc = await callerPage.$('[data-testid=start-video-call]');
  console.log('caller sees Video call button:', !!vc);
  if (vc) await vc.click();

  // Give the ring time to arrive
  await new Promise(r => setTimeout(r, 4000));

  const rang = await calleePage.evaluate(() => !!window.__ringStarted);
  const banner = await calleePage.$('[data-testid=incoming-call]');
  console.log('callee ringtone played (AudioContext oscillator started):', rang);
  console.log('callee incoming-call banner shown:', !!banner);

  const pass = rang && !!banner;
  console.log('\nRESULT:', pass ? 'PASS — friend\'s PC rings on incoming call' : 'FAIL');
  process.exit(pass ? 0 : 1);
} catch (e) { console.error('ERROR', e.message); process.exit(2); }
finally { await callerB.close(); await calleeB.close(); }
