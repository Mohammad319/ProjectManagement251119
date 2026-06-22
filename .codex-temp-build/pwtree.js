const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[tree]', ...a);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 }, ignoreHTTPSErrors: true });
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
    await page.waitForTimeout(6000);

    // Dump the left sidebar DOM tree around folder nodes
    const dump = await page.evaluate(() => {
      const aside = document.querySelector('aside') || document.body;
      const out = [];
      aside.querySelectorAll('*').forEach(el => {
        const t = (el.childElementCount === 0 ? (el.textContent || '').trim() : '');
        if (/Demoprojekt|Hela avdelningen/i.test((el.textContent || ''))) {
          out.push({ tag: el.tagName, cls: (el.className || '').toString().slice(0, 60), txt: (el.textContent || '').trim().slice(0, 40), kids: el.childElementCount });
        }
      });
      return out.slice(0, 25);
    });
    log('folder nodes:', JSON.stringify(dump, null, 1));

    // Try clicking Demoprojekt by Playwright text locator (real event)
    try {
      await page.getByText('Demoprojekt', { exact: false }).first().click({ timeout: 5000 });
      log('clicked Demoprojekt via locator');
    } catch (e) { log('Demoprojekt click failed:', e.message.slice(0, 80)); }
    await page.waitForTimeout(4000);
    await page.screenshot({ path: `${OUT}/cv04_demoproj.png`, fullPage: true });

    const probe = await page.evaluate(() => {
      const btns = [...document.querySelectorAll('button')].map(b => (b.textContent || '').trim()).filter(Boolean);
      return { vy: btns.filter(t => /^Vy$/.test(t)), filter: btns.filter(t => /Filter/i.test(t)), sample: btns.slice(0, 30) };
    });
    log('toolbar after Demoprojekt:', JSON.stringify(probe));

    log('DONE OK');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/cv99_tree_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
