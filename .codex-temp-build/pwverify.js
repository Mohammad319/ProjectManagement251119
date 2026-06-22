const { chromium } = require('./node_modules/playwright-core');

const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[verify]', ...a);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1000, height: 800 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text()); });

  try {
    // 1) Login
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.click('button[type=submit]'),
    ]);
    log('after login url:', page.url());

    // 2) Control page
    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500); // let Blazor circuit connect

    // 3) Department tab is active by default; click "Open all users" (UI is en-US here).
    const openAll = page.getByRole('button', { name: 'Open all users' });
    await openAll.first().click({ timeout: 15000 });

    // 4) Wait for the MhdTable rendered by UsersIndex
    await page.waitForSelector('th#hselect', { timeout: 20000 });
    await page.waitForTimeout(1200); // engine init + frozen sync (rAF + 40ms timers)
    await page.screenshot({ path: `${OUT}/01_table_initial.png` });

    // 5) Frozen DOM evidence
    const frozen = await page.evaluate(() => {
      const info = id => {
        const el = document.getElementById(id);
        if (!el) return null;
        const cs = getComputedStyle(el);
        return { id, dataFrozen: el.getAttribute('data-pm-frozen'), position: cs.position, left: cs.left, zIndex: cs.zIndex };
      };
      return { select: info('hselect'), actions: info('hactions'), email: info('hemail'), lastLogin: info('hlastLogin') };
    });
    log('FROZEN DOM:', JSON.stringify(frozen, null, 0));

    // 6) Resize evidence: drag the email column resizer +180px
    const widthOf = id => page.evaluate(i => document.getElementById(i)?.getBoundingClientRect().width, id);
    const emailBefore = await widthOf('hemail');
    const resizer = page.locator('th#hemail .resizer');
    const rb = await resizer.boundingBox();
    if (!rb) throw new Error('no resizer on th#hemail');
    await page.mouse.move(rb.x + rb.width / 2, rb.y + rb.height / 2);
    await page.mouse.down();
    await page.mouse.move(rb.x + 180, rb.y + rb.height / 2, { steps: 12 });
    await page.mouse.up();
    await page.waitForTimeout(600);
    const emailAfter = await widthOf('hemail');
    log(`RESIZE email width: ${Math.round(emailBefore)} -> ${Math.round(emailAfter)} (delta ${Math.round(emailAfter - emailBefore)})`);
    await page.screenshot({ path: `${OUT}/02_after_resize.png` });

    // 7) Frozen-on-scroll evidence: scroll the table container right
    const rectsBefore = await page.evaluate(() => ({
      sel: document.getElementById('hselect').getBoundingClientRect().left,
      act: document.getElementById('hactions').getBoundingClientRect().left,
      last: document.getElementById('hlastLogin').getBoundingClientRect().left,
    }));
    const scrolled = await page.evaluate(() => {
      const c = document.querySelector('.department-users-table');
      if (!c) return -1;
      c.scrollLeft = 500;
      return c.scrollLeft;
    });
    await page.waitForTimeout(700);
    const rectsAfter = await page.evaluate(() => ({
      sel: document.getElementById('hselect').getBoundingClientRect().left,
      act: document.getElementById('hactions').getBoundingClientRect().left,
      last: document.getElementById('hlastLogin').getBoundingClientRect().left,
    }));
    log('scrollLeft applied:', scrolled);
    log('LEFT before scroll:', JSON.stringify(rectsBefore));
    log('LEFT after  scroll:', JSON.stringify(rectsAfter));
    log(`select moved: ${Math.round(rectsAfter.sel - rectsBefore.sel)}px | lastLogin moved: ${Math.round(rectsAfter.last - rectsBefore.last)}px`);
    await page.screenshot({ path: `${OUT}/03_after_hscroll.png` });

    log('DONE OK');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/99_error.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
