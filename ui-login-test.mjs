// Real browser test: load the SPA, log in as admin, confirm we reach the feed.
import puppeteer from 'puppeteer-core';

const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const consoleErrors = [];

const browser = await puppeteer.launch({
  executablePath: CHROME,
  headless: 'new',
  ignoreHTTPSErrors: true,
  args: ['--ignore-certificate-errors', '--no-sandbox'],
});

try {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', e => consoleErrors.push('PAGEERROR: ' + e.message));

  // 1. Load app — should redirect to login (guest)
  await page.goto('http://localhost:4200/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 1500));
  const url1 = page.url();
  console.log('after load, url =', url1);
  console.log('login form visible:', !!(await page.$('input[type=email], input[formcontrolname=email], input[name=email]')));

  // 2. Fill login form
  const emailSel = await page.$('input[type=email]') ? 'input[type=email]'
    : (await page.$('input[formcontrolname=email]')) ? 'input[formcontrolname=email]' : 'input';
  const passSel = 'input[type=password]';
  await page.type(emailSel, 'admin@linkup.com', { delay: 10 });
  await page.type(passSel, 'Admin@123', { delay: 10 });

  // 3. Submit
  const btn = await page.$('button[type=submit]') || await page.$('button');
  await btn.click();

  // 4. Wait for navigation away from login
  await new Promise(r => setTimeout(r, 4000));
  const url2 = page.url();
  console.log('after login, url =', url2);

  const token = await page.evaluate(() => localStorage.getItem('linkup_access_token'));
  console.log('access token stored:', token ? `yes (len ${token.length})` : 'NO');

  const bodyText = (await page.evaluate(() => document.body.innerText || '')).slice(0, 200).replace(/\n/g, ' | ');
  console.log('page text sample:', bodyText);

  console.log('\nconsole errors:', consoleErrors.length);
  consoleErrors.slice(0, 8).forEach(e => console.log('  -', e.slice(0, 160)));

  const loggedIn = !!token && !url2.includes('/auth/login');
  console.log('\nRESULT:', loggedIn ? 'PASS — logged in via UI, token stored, left login page' : 'FAIL');
  process.exit(loggedIn ? 0 : 1);
} finally {
  await browser.close();
}
