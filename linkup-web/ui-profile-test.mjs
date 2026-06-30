// Real-browser check: opening another user's profile shows full info (name, friendship button).
import puppeteer from 'puppeteer-core';
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const BASE_API = 'https://localhost:5001/api/v1';

// Find a friend's id via the API
const t = (await (await fetch(BASE_API + '/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: 'admin@linkup.com', password: 'Admin@123' }) })).json()).data.accessToken;
const users = (await (await fetch(BASE_API + '/admin/users?page=1&pageSize=60', { headers: { Authorization: 'Bearer ' + t } })).json()).data.items;
const liam = users.find(u => /liam/i.test(u.email));

const browser = await puppeteer.launch({ executablePath: CHROME, headless: 'new', ignoreHTTPSErrors: true, args: ['--ignore-certificate-errors', '--no-sandbox'] });
try {
  const page = await browser.newPage();
  await page.goto('http://localhost:4200/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 1200));
  await page.type('input[type=email]', 'admin@linkup.com', { delay: 8 });
  await page.type('input[type=password]', 'Admin@123', { delay: 8 });
  (await page.$('button[type=submit]') || await page.$('button')).click();
  await new Promise(r => setTimeout(r, 3500));

  await page.goto('http://localhost:4200/profile/' + liam.id, { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 2500));

  const name = await page.$eval('h1', el => el.textContent.trim()).catch(() => '');
  const friendsBtn = await page.evaluate(() => [...document.querySelectorAll('button')].some(b => /friends|message/i.test(b.textContent || '')));
  const bodyHasFriendsCount = await page.evaluate(() => /\d+ friends/i.test(document.body.innerText));
  console.log('profile name h1:', JSON.stringify(name));
  console.log('shows Friends/Message button:', friendsBtn);
  console.log('shows "N friends":', bodyHasFriendsCount);

  const pass = name === 'Liam Nguyen' && friendsBtn;
  console.log('\nRESULT:', pass ? 'PASS — other profile shows full name + correct friendship action' : 'FAIL');
  process.exit(pass ? 0 : 1);
} catch (e) { console.error('ERROR', e.message); process.exit(2); }
finally { await browser.close(); }
