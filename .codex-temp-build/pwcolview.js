const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[colview]', ...a);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 200)); });

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

    // 2) Project list (FolderIndex hosts ProjectsUI) at "/"
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(5000); // WASM download + boot
    await page.screenshot({ path: `${OUT}/cv01_home.png`, fullPage: true });

    // What folders are available? Dump clickable folder-tree text.
    const tree = await page.evaluate(() => {
      const txt = [];
      document.querySelectorAll('[class*="folder"], aside button, nav button').forEach(el => {
        const t = (el.textContent || '').trim().replace(/\s+/g, ' ');
        if (t && t.length < 60) txt.push(t);
      });
      return [...new Set(txt)].slice(0, 40);
    });
    log('TREE/buttons sample:', JSON.stringify(tree));

    // Is the ProjectsUI toolbar present yet (before folder select)?
    const hasVy = await page.evaluate(() => {
      const btns = [...document.querySelectorAll('button')].map(b => (b.textContent || '').trim());
      return {
        vy: btns.filter(t => /^Vy$|^View$/.test(t)).length,
        filter: btns.filter(t => /^Filter|^Filtrera/.test(t)).length,
        totalButtons: btns.length,
      };
    });
    log('toolbar probe (pre-folder):', JSON.stringify(hasVy));

    log('DONE OK');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/cv99_error.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
