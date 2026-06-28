const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[disc]', ...a);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1500, height: 950 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 200)); });

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.click('button[type=submit]'),
    ]);
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(6000);

    // API: list folders + projects (authenticated via shared cookies)
    const folders = await page.evaluate(async (b) => {
      const r = await fetch(b + '/api/v1/folders/', { credentials: 'include' });
      return r.ok ? await r.json() : { err: r.status };
    }, BASE);
    log('folders:', JSON.stringify(folders).slice(0, 600));

    await page.screenshot({ path: `${OUT}/ind00_home.png`, fullPage: true });

    // Expand the first folder/department in the tree to reveal projects
    const treeText = await page.evaluate(() => {
      const aside = document.querySelector('aside') || document.body;
      return (aside.innerText || '').slice(0, 1200);
    });
    log('TREE TEXT:\n' + treeText);

    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/ind99_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
