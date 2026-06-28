const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[t2]', ...a);
const sleep = (ms) => new Promise(r => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1500, height: 950 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 140)); });

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([ page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button[type=submit]') ]);
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await sleep(5000);

    // Find the chevron button at the very left of the "Demoprojekt" row and click it.
    const clicked = await page.evaluate(() => {
      const aside = document.querySelector('aside');
      if (!aside) return 'no-aside';
      // The folder label span
      const label = [...aside.querySelectorAll('*')].find(el =>
        el.childElementCount === 0 && /^Demoprojekt$/.test((el.textContent || '').trim()));
      if (!label) return 'no-label';
      // Walk up to the row container, then find the first button (chevron toggle).
      let row = label;
      for (let i = 0; i < 6 && row && row.parentElement; i++) { row = row.parentElement; if (row.querySelector('button')) break; }
      const btn = row.querySelector('button');
      if (btn) { btn.click(); return 'clicked-button'; }
      // fallback: click first svg in the row
      const svg = row.querySelector('svg');
      if (svg) { svg.dispatchEvent(new MouseEvent('click', { bubbles: true })); return 'clicked-svg'; }
      return 'no-toggle';
    });
    log('expand result:', clicked);
    await sleep(2500);

    let cnt = await page.locator('aside').getByText('Demoprojekt nordbygg 04', { exact: false }).count();
    log('project node in tree:', cnt);

    // If still collapsed, click the chevron by coordinates (left of the row).
    if (!cnt) {
      const box = await page.locator('aside').getByText('Demoprojekt', { exact: false }).first().boundingBox();
      if (box) { await page.mouse.click(box.x - 18, box.y + box.height / 2); log('coord-click chevron'); await sleep(2500); }
      cnt = await page.locator('aside').getByText('Demoprojekt nordbygg 04', { exact: false }).count();
      log('project node in tree (after coord):', cnt);
    }

    const probe = await page.evaluate(() => {
      const aside = document.querySelector('aside') || document.body;
      const svgs = [...aside.querySelectorAll('svg[aria-label="Ändrad sedan du senast öppnade"]')];
      return { inTree: svgs.length, visible: svgs.filter(s => s.getClientRects().length > 0).length };
    });
    log('tree indicator svgs:', JSON.stringify(probe));
    await page.screenshot({ path: `${OUT}/ind22_tree.png`, fullPage: true });

    let tip = '(none)';
    try {
      const ind = page.locator('aside svg[aria-label="Ändrad sedan du senast öppnade"]').first();
      await ind.hover({ timeout: 4000 });
      await sleep(900);
      tip = await page.evaluate(() => {
        const t = document.querySelector('.pm-hover-tooltip, [role="tooltip"]');
        return t ? (t.innerText || t.textContent || '').trim() : '(no tooltip)';
      });
      await page.screenshot({ path: `${OUT}/ind23_tree_tooltip.png`, fullPage: true });
    } catch (e) { log('hover:', e.message.slice(0, 70)); }
    log('TREE TOOLTIP:\n' + tip);
    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/ind28_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally { await browser.close(); }
})();
