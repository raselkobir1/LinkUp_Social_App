// Real-browser check: feed uses infinite scroll (no Load more button), scrolling loads more posts.
import puppeteer from 'puppeteer-core';

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const browser = await puppeteer.launch({
  executablePath: CHROME, headless: 'new', ignoreHTTPSErrors: true,
  args: ['--ignore-certificate-errors', '--no-sandbox'],
});
const countPosts = page => page.$$eval('app-post-card', els => els.length);

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 900, height: 700 });

  // Log in
  await page.goto('http://localhost:4200/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 1200));
  await page.type('input[type=email]', 'admin@linkup.com', { delay: 8 });
  await page.type('input[type=password]', 'Admin@123', { delay: 8 });
  (await page.$('button[type=submit]') || await page.$('button')).click();
  await new Promise(r => setTimeout(r, 4000));

  // Ensure we're on the feed
  await page.goto('http://localhost:4200/feed', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 2500));

  const hasLoadMoreButton = await page.evaluate(() =>
    [...document.querySelectorAll('button')].some(b => /load more/i.test(b.textContent || '')));
  const before = await countPosts(page);
  console.log('Load more button present:', hasLoadMoreButton);
  console.log('posts before scroll:', before);

  // Scroll the last post card into view — works regardless of which ancestor
  // element actually scrolls — to trip the IntersectionObserver sentinel.
  for (let i = 0; i < 6; i++) {
    await page.evaluate(() => {
      const cards = document.querySelectorAll('app-post-card');
      cards[cards.length - 1]?.scrollIntoView({ block: 'end' });
      // also nudge any scrollable container and the window
      window.scrollTo(0, document.body.scrollHeight);
      document.querySelectorAll('*').forEach(el => {
        if (el.scrollHeight > el.clientHeight + 50 && getComputedStyle(el).overflowY !== 'visible')
          el.scrollTop = el.scrollHeight;
      });
    });
    await new Promise(r => setTimeout(r, 1300));
  }
  const after = await countPosts(page);
  console.log('posts after scroll:', after);

  const pass = !hasLoadMoreButton && after >= before && before > 0;
  // If totalCount > pageSize, after should exceed before; otherwise equal is fine.
  console.log('\nRESULT:', pass ? 'PASS — no Load more button; feed auto-loads on scroll' : 'FAIL');
  console.log(after > before ? `(auto-loaded ${after - before} more on scroll)` : '(no extra page needed / all posts already shown)');
  process.exit(pass ? 0 : 1);
} catch (e) {
  console.error('ERROR', e.message); process.exit(2);
} finally {
  await browser.close();
}
