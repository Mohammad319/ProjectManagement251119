const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[save]', ...a);
const sleep = (ms) => new Promise(r => setTimeout(r, ms));
const PROJ = 'Demoprojekt nordbygg 01';

const rowText = (page) => page.evaluate((name) => {
  const tr = [...document.querySelectorAll('tr')].find(r => (r.innerText || '').includes(name));
  if (!tr) return null;
  const cells = [...tr.querySelectorAll('td')].map(td => (td.innerText || '').trim());
  return cells;
}, PROJ);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1500, height: 950 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 130)); });

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([ page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}), page.click('button[type=submit]') ]);
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await sleep(5000);

    // Open the Demoprojekt folder → right-panel project list
    await page.getByText('Demoprojekt', { exact: false }).first().click().catch(() => {});
    await sleep(3000);

    const before = await rowText(page);
    log('BEFORE cells:', JSON.stringify(before));

    // Right-click the project row → context menu → "Ändra projektuppgifter"
    const row = page.locator('tr', { hasText: PROJ }).first();
    await row.click({ button: 'right' });
    await sleep(1200);
    await page.screenshot({ path: `${OUT}/sv01_ctxmenu.png` });
    const edit = page.getByText('Ändra projektuppgifter', { exact: false }).first();
    await edit.click({ timeout: 6000 });
    log('clicked edit');
    await sleep(3500);
    await page.screenshot({ path: `${OUT}/sv02_form.png` });

    // Click Save (Spara) in the dialog footer
    let saved = false;
    for (const sel of ['Spara', 'Save']) {
      const b = page.getByRole('button', { name: new RegExp('^' + sel + '$', 'i') }).first();
      if (await b.count()) { await b.click({ timeout: 5000 }).catch(() => {}); saved = true; log('clicked', sel); break; }
    }
    if (!saved) {
      const b = page.locator('button:has-text("Spara"), button:has-text("Save")').first();
      if (await b.count()) { await b.click().catch(() => {}); saved = true; log('clicked save (fallback)'); }
    }
    await sleep(5000); // wait for re-fetch + list update

    const after = await rowText(page);
    log('AFTER cells:', JSON.stringify(after));
    await page.screenshot({ path: `${OUT}/sv03_after.png`, fullPage: true });

    // Heuristic check: the row should NOT have lost previously non-empty cells (no new "-" where text was)
    let regressed = [];
    if (before && after && before.length === after.length) {
      for (let i = 0; i < before.length; i++) {
        const b = before[i], a = after[i];
        if (b && b !== '-' && (a === '-' || a === '')) regressed.push({ col: i, before: b, after: a });
      }
    }
    log('REGRESSED (blanked) cells:', JSON.stringify(regressed));
    log(regressed.length === 0 ? 'PASS: no columns blanked after save' : 'FAIL: some columns blanked');
    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/sv99_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally { await browser.close(); }
})();
