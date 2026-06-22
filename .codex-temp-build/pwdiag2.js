const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[diag2]', ...a);
(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(()=>{}), page.click('button[type=submit]')]);
    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    await page.getByRole('button', { name: 'Open all users' }).first().click();
    await page.waitForTimeout(4000);
    await page.screenshot({ path: `${OUT}/diag2_users.png`, fullPage: true });
    const info = await page.evaluate(() => {
      const tables = Array.from(document.querySelectorAll('table'));
      return {
        tableCount: tables.length,
        thIdsPerTable: tables.map(t => Array.from(t.querySelectorAll('thead th')).map(th => th.id || th.getAttribute('class')?.slice(0,18) || '(no-id)')),
        anyResizer: document.querySelectorAll('.resizer').length,
        anyFrozenAttr: document.querySelectorAll('[data-pm-frozen]').length,
        rows: tables.map(t => t.querySelectorAll('tbody tr').length),
        bodyText: (document.body.innerText||'').replace(/\s+/g,' ').slice(0,400),
      };
    });
    log(JSON.stringify(info, null, 2));
  } catch (e) { log('ERR', e.message); await page.screenshot({ path: `${OUT}/diag2_err.png` }).catch(()=>{}); }
  finally { await browser.close(); }
})();
