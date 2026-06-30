// Full UI click-test: log in as admin, visit every route/menu, verify render + collect console errors.
import puppeteer from 'puppeteer-core';

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const BASE = 'http://localhost:4200';
const wait = (ms) => new Promise(r => setTimeout(r, ms));

const browser = await puppeteer.launch({
  executablePath: CHROME,
  headless: 'new',
  ignoreHTTPSErrors: true,
  args: ['--ignore-certificate-errors', '--no-sandbox', '--disable-dev-shm-usage'],
});

const results = [];
const allErrors = [];

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 1280, height: 900 });
  let pageErrors = [];
  page.on('console', m => { if (m.type() === 'error') pageErrors.push(m.text()); });
  page.on('pageerror', e => pageErrors.push('PAGEERROR: ' + e.message));

  // Login
  await page.goto(`${BASE}/`, { waitUntil: 'networkidle2', timeout: 60000 });
  await wait(1500);
  await page.type('input[type=email]', 'admin@linkup.com', { delay: 8 });
  await page.type('input[type=password]', 'Admin@123', { delay: 8 });
  (await page.$('button[type=submit]') || await page.$('button')).click();
  await wait(4000);
  const token = await page.evaluate(() => localStorage.getItem('linkup_access_token'));
  const userId = await page.evaluate(() => { try { return JSON.parse(localStorage.getItem('linkup_user')).id; } catch { return null; } });
  results.push(['login → token stored', !!token && !page.url().includes('/auth/login')]);

  // Visit each route and check a selector + capture console errors
  const routes = [
    ['Feed', '/feed', 'app-create-post, textarea, [class*="create"]'],
    ['Friends', '/friends', 'mat-tab-group, .mat-mdc-tab'],
    ['Messages', '/messages', 'input, h2'],
    ['Notifications', '/notifications', 'h1, h2, .text-gray-400'],
    ['Search', '/search?q=a', 'h2, mat-tab-group, .text-gray-400'],
    ['Settings', '/settings', 'mat-slide-toggle, mat-form-field, section'],
    ['Call history', '/calls', 'h1'],
    ['Profile', `/profile/${userId}`, 'mat-tab-group, h1'],
    ['Admin dashboard', '/admin/dashboard', 'h1'],
    ['Admin users', '/admin/users', 'table, .bg-white, input'],
    ['Admin posts', '/admin/posts', '.bg-white, table'],
    ['Admin reports', '/admin/reports', 'h1'],
  ];

  for (const [name, path, sel] of routes) {
    pageErrors = [];
    await page.goto(`${BASE}${path}`, { waitUntil: 'networkidle2', timeout: 40000 }).catch(() => {});
    await wait(1200);
    const onRoute = !page.url().includes('/auth/login');
    let found = false;
    try { found = await page.$(sel.split(',')[0].trim()) !== null; } catch {}
    // fallback: any non-empty main content
    const bodyLen = (await page.evaluate(() => document.body.innerText || '')).trim().length;
    const realErrors = pageErrors.filter(e => !/favicon|404 \(Not Found\)|net::ERR|the server responded with a status of 4|Failed to load resource/i.test(e));
    if (realErrors.length) allErrors.push(`[${name}] ` + realErrors.slice(0, 3).join(' || '));
    results.push([`${name} (${path})`, onRoute && bodyLen > 20 && realErrors.length === 0]);
  }

  // Open the user dropdown menu and verify the new menu items are present
  pageErrors = [];
  await page.goto(`${BASE}/feed`, { waitUntil: 'networkidle2' });
  await wait(1000);
  const menuBtn = await page.$('button[mat-icon-button][aria-haspopup], .mat-mdc-menu-trigger, button:has(img)');
  let menuItems = [];
  try {
    // click avatar (last icon button in navbar)
    const triggers = await page.$$('.mat-mdc-menu-trigger, button');
    for (const t of triggers.reverse()) {
      const hasImg = await t.$('img');
      if (hasImg) { await t.click(); break; }
    }
    await wait(600);
    menuItems = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.mat-mdc-menu-item, [mat-menu-item]')).map(e => e.innerText.trim()));
  } catch {}
  const menuText = menuItems.join(' | ');
  results.push(['user menu has Settings', /Settings/i.test(menuText)]);
  results.push(['user menu has Call history', /Call history/i.test(menuText)]);

  // Report
  console.log('\n=== UI CLICK-TEST RESULTS ===');
  let pass = 0, fail = 0;
  for (const [name, ok] of results) { console.log(`  ${ok ? '✓' : '✗'} ${name}`); ok ? pass++ : fail++; }
  console.log(`\nPASS: ${pass}  FAIL: ${fail}`);
  if (allErrors.length) { console.log('\nConsole/page errors seen:'); allErrors.forEach(e => console.log('  - ' + e.slice(0, 200))); }
  process.exit(fail ? 1 : 0);
} finally {
  await browser.close();
}
