const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[tree]', ...a);
const sleep = (ms) => new Promise(r => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1500, height: 950 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 160)); });

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.click('button[type=submit]'),
    ]);
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await sleep(5000);

    // Expand the "Demoprojekt" folder NODE in the left tree (click its expand chevron), so the
    // project rows render inside the tree (not just the right panel).
    const folderRow = page.locator('aside').getByText('Demoprojekt', { exact: false }).first();
    // Click the chevron just left of the folder label (the row's first clickable toggle).
    try {
      const row = folderRow.locator('xpath=ancestor::*[self::div or self::li][1]');
      await row.locator('button, svg').first().click({ timeout: 4000 }).catch(async () => { await folderRow.click({ timeout: 4000 }); });
    } catch (e) { log('expand click1:', e.message.slice(0, 60)); await folderRow.click().catch(() => {}); }
    await sleep(2500);

    // If the project node isn't visible in the tree yet, click the folder again to toggle expand.
    let treeHasProj = await page.locator('aside').getByText('Demoprojekt nordbygg 04', { exact: false }).count();
    if (!treeHasProj) {
      try { await folderRow.click({ timeout: 4000 }); } catch {}
      await sleep(2000);
      treeHasProj = await page.locator('aside').getByText('Demoprojekt nordbygg 04', { exact: false }).count();
    }
    log('project node in tree count:', treeHasProj);

    // Indicators INSIDE the tree (aside)
    const probe = await page.evaluate(() => {
      const aside = document.querySelector('aside') || document.body;
      const svgs = [...aside.querySelectorAll('svg[aria-label="Ändrad sedan du senast öppnade"]')];
      const visible = svgs.filter(s => s.getClientRects().length > 0);
      return { inTree: svgs.length, visible: visible.length };
    });
    log('tree indicator svgs:', JSON.stringify(probe));

    await page.screenshot({ path: `${OUT}/ind20_tree_expanded.png`, fullPage: true });

    // Hover the tree indicator → tooltip
    let tip = '(none)';
    try {
      const ind = page.locator('aside svg[aria-label="Ändrad sedan du senast öppnade"]').first();
      await ind.scrollIntoViewIfNeeded();
      await ind.hover({ timeout: 4000 });
      await sleep(900);
      tip = await page.evaluate(() => {
        const t = document.querySelector('.pm-hover-tooltip, [role="tooltip"]');
        return t ? (t.innerText || t.textContent || '').trim() : '(no tooltip el)';
      });
      await page.screenshot({ path: `${OUT}/ind21_tree_tooltip.png`, fullPage: true });
    } catch (e) { log('hover failed:', e.message.slice(0, 80)); }
    log('TREE TOOLTIP:\n' + tip);

    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/ind29_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
