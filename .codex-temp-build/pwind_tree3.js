const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[t3]', ...a);
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

    // Expand "Demoprojekt" folder node: click the chevron button at the far left of its row.
    const res = await page.evaluate(() => {
      const label = [...document.querySelectorAll('span,div,a')].find(el =>
        el.childElementCount === 0 && /^Demoprojekt$/.test((el.textContent || '').trim()));
      if (!label) return 'no-label';
      const lb = label.getBoundingClientRect();
      // Find the nearest button whose center is left of the label (the chevron toggle on the same row).
      const btns = [...document.querySelectorAll('button')].filter(b => {
        const r = b.getBoundingClientRect();
        return Math.abs((r.top + r.bottom) / 2 - (lb.top + lb.bottom) / 2) < 16 && r.left < lb.left;
      });
      if (btns.length) { btns[0].click(); return 'chevron-clicked'; }
      return 'no-chevron';
    });
    log('expand:', res);
    await sleep(2500);

    let cnt = await page.getByText('Demoprojekt nordbygg 04', { exact: false }).count();
    log('"04" occurrences (tree+panel):', cnt);

    // Indicators in the LEFT tree only (x < 210)
    const probe = await page.evaluate(() => {
      const svgs = [...document.querySelectorAll('svg[aria-label="Ändrad sedan du senast öppnade"]')];
      const info = svgs.map(s => { const r = s.getBoundingClientRect(); return { x: Math.round(r.left), y: Math.round(r.top), vis: r.width > 0 }; });
      return { total: svgs.length, tree: info.filter(i => i.x < 210 && i.vis), all: info };
    });
    log('indicators:', JSON.stringify(probe));
    await page.screenshot({ path: `${OUT}/ind24_tree.png`, fullPage: true });

    // Hover the tree indicator (leftmost one)
    let tip = '(none)';
    const treeSvgs = page.locator('svg[aria-label="Ändrad sedan du senast öppnade"]');
    const n = await treeSvgs.count();
    for (let i = 0; i < n; i++) {
      const bb = await treeSvgs.nth(i).boundingBox();
      if (bb && bb.x < 210) {
        await treeSvgs.nth(i).hover({ timeout: 4000 }).catch(() => {});
        await sleep(900);
        tip = await page.evaluate(() => {
          const t = document.querySelector('.pm-hover-tooltip, [role="tooltip"]');
          return t ? (t.innerText || t.textContent || '').trim() : '(no tooltip)';
        });
        await page.screenshot({ path: `${OUT}/ind25_tree_tooltip.png`, fullPage: true });
        break;
      }
    }
    log('TREE TOOLTIP:\n' + tip);
    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/ind26_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally { await browser.close(); }
})();
