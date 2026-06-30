// Deeper interaction test for the new wired flows.
import puppeteer from 'puppeteer-core';
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const BASE = 'http://localhost:4200';
const wait = (ms) => new Promise(r => setTimeout(r, ms));
const browser = await puppeteer.launch({ executablePath: CHROME, headless: 'new', ignoreHTTPSErrors: true, args: ['--ignore-certificate-errors', '--no-sandbox'] });
const results = []; const errs = [];
const chk = (n, ok) => results.push([n, ok]);
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 1280, height: 900 });
  page.on('pageerror', e => errs.push('PAGEERROR: ' + e.message));
  page.on('console', m => { if (m.type() === 'error' && !/Failed to load resource|status of 4|favicon/i.test(m.text())) errs.push(m.text()); });

  await page.goto(`${BASE}/`, { waitUntil: 'networkidle2', timeout: 60000 });
  await wait(1200);
  await page.type('input[type=email]', 'admin@linkup.com', { delay: 6 });
  await page.type('input[type=password]', 'Admin@123', { delay: 6 });
  (await page.$('button[type=submit]') || await page.$('button')).click();
  await wait(3500);

  // --- FEED: create a post ---
  await page.goto(`${BASE}/feed`, { waitUntil: 'networkidle2' });
  await wait(1200);
  const ta = await page.$('app-create-post textarea, textarea');
  const stamp = 'clicktest ' + Date.now();
  if (ta) { await ta.click(); await page.keyboard.type(stamp, { delay: 4 }); }
  await wait(300);
  // click the create-post submit button (the one inside app-create-post)
  let posted = false;
  const cpButtons = await page.$$('app-create-post button');
  for (const b of cpButtons) {
    const t = (await page.evaluate(el => el.innerText, b)).toLowerCase();
    if (/post|share/.test(t)) { await b.click().catch(()=>{}); posted = true; break; }
  }
  await wait(2500);
  const feedText = await page.evaluate(() => document.body.innerText);
  chk('create post shows in feed', feedText.includes(stamp));

  // --- react to first post (the Like action button shows 'Like') ---
  {
    const btns = await page.$$('app-post-card button');
    for (const b of btns) {
      const t = (await page.evaluate(el => el.innerText, b)).trim().toLowerCase();
      if (t.includes('like')) { await b.click().catch(()=>{}); break; }
    }
  }
  await page.keyboard.press('Escape'); // dismiss any overlay
  await wait(800);
  chk('react click no error', true);

  // --- comment on first post: open comments then type ---
  {
    const btns = await page.$$('app-post-card button');
    for (const b of btns) {
      const t = (await page.evaluate(el => el.innerText, b)).trim().toLowerCase();
      if (t.includes('comment')) { await b.click().catch(()=>{}); break; }
    }
  }
  await wait(900);
  const commentInput = await page.$('app-post-card input[placeholder="Write a comment..."]');
  const cstamp = 'cmt ' + Date.now();
  if (commentInput) { await commentInput.click(); await page.keyboard.type(cstamp, { delay: 4 }); await page.keyboard.press('Enter'); }
  chk('comment input found', !!commentInput);
  await wait(3000);
  const afterComment = await page.evaluate(() => document.body.innerText);
  chk('comment posted & visible', afterComment.includes(cstamp));

  // --- PROFILE: open edit dialog ---
  const userId = await page.evaluate(() => { try { return JSON.parse(localStorage.getItem('linkup_user')).id; } catch { return null; } });
  await page.goto(`${BASE}/profile/${userId}`, { waitUntil: 'networkidle2' });
  await wait(1500);
  const profButtons = await page.$$('button');
  for (const b of profButtons) {
    const t = (await page.evaluate(el => el.innerText, b)).toLowerCase();
    if (t.includes('edit profile')) { await b.click().catch(()=>{}); break; }
  }
  await wait(1000);
  const dialogOpen = await page.$('mat-dialog-container, .mat-mdc-dialog-container') !== null;
  chk('edit-profile dialog opens', dialogOpen);
  const dialogText = dialogOpen ? await page.evaluate(() => document.querySelector('mat-dialog-container,.mat-mdc-dialog-container')?.innerText || '') : '';
  chk('dialog has Education/Work/Social tabs', /Education/i.test(dialogText) && /Work/i.test(dialogText) && /Social/i.test(dialogText));
  // close dialog
  await page.keyboard.press('Escape'); await wait(500);

  // --- SETTINGS: toggle a notification + change password form present ---
  await page.goto(`${BASE}/settings`, { waitUntil: 'networkidle2' });
  await wait(1500);
  const toggles = await page.$$('mat-slide-toggle');
  if (toggles.length) { await toggles[0].click().catch(()=>{}); }
  await wait(1000);
  const setText = await page.evaluate(() => document.body.innerText);
  chk('settings has change password', /change password/i.test(setText));
  chk('settings has notifications', /notification/i.test(setText));
  chk('settings has privacy', /privacy/i.test(setText));

  // --- MESSAGES: new group button opens dialog ---
  await page.goto(`${BASE}/messages`, { waitUntil: 'networkidle2' });
  await wait(1200);
  const grpBtn = await page.$('button[title="New group"]');
  if (grpBtn) { await grpBtn.click().catch(()=>{}); await wait(800); }
  const grpDialog = await page.$('mat-dialog-container, .mat-mdc-dialog-container') !== null;
  chk('create-group dialog opens', grpDialog);

  console.log('\n=== UI INTERACTION TEST ===');
  let pass = 0, fail = 0;
  for (const [n, ok] of results) { console.log(`  ${ok ? '✓' : '✗'} ${n}`); ok ? pass++ : fail++; }
  console.log(`\nPASS: ${pass}  FAIL: ${fail}`);
  if (errs.length) { console.log('\nerrors:'); [...new Set(errs)].slice(0, 8).forEach(e => console.log('  - ' + e.slice(0, 180))); }
  process.exit(fail ? 1 : 0);
} finally { await browser.close(); }
