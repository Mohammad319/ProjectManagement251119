const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[colview2]', ...a);

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

    // 2) Home + WASM boot
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(6000);

    // 3) Click "Hela avdelningen" (whole department) to load the project list
    const clicked = await page.evaluate(() => {
      const els = [...document.querySelectorAll('button, a, [role=button], div, span')];
      const target = els.find(e => /Hela avdelningen/i.test((e.textContent || '').trim()) && (e.textContent || '').trim().length < 40);
      if (target) { target.click(); return true; }
      return false;
    });
    log('clicked Hela avdelningen:', clicked);
    await page.waitForTimeout(4000);
    await page.screenshot({ path: `${OUT}/cv02_dept.png`, fullPage: true });

    // 4) Probe toolbar for the "Vy" button
    const probe1 = await page.evaluate(() => {
      const btns = [...document.querySelectorAll('button')].map(b => (b.textContent || '').trim());
      return { vy: btns.filter(t => /^Vy$/.test(t)), sample: btns.filter(Boolean).slice(0, 30) };
    });
    log('toolbar (post-folder):', JSON.stringify(probe1));

    // 5) Open the "Vy" dropdown and look for the column-views section (Kolumnvyer / Standard / Välj kolumner)
    const openedVy = await page.evaluate(() => {
      const b = [...document.querySelectorAll('button')].find(x => /^Vy$/.test((x.textContent || '').trim()));
      if (b) { b.click(); return true; }
      return false;
    });
    log('opened Vy:', openedVy);
    await page.waitForTimeout(1500);
    await page.screenshot({ path: `${OUT}/cv03_vy_open.png`, fullPage: true });

    const menu = await page.evaluate(() => {
      const all = document.body.innerText;
      return {
        kolumnvyer: /Kolumnvyer/i.test(all),
        standard: /Standard/i.test(all),
        valjKolumner: /Välj kolumner/i.test(all),
        sparaVy: /Spara.*vy|Spara nuvarande/i.test(all),
      };
    });
    log('column-view menu markers:', JSON.stringify(menu));

    log('DONE OK');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/cv99_error2.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
