const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[diag]', ...a);
(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text()); });
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(()=>{}), page.click('button[type=submit]')]);
    log('after login url:', page.url());
    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    log('control url:', page.url(), 'title:', await page.title());
    await page.screenshot({ path: `${OUT}/diag_control.png`, fullPage: true });
    const texts = await page.evaluate(() => {
      const t = el => (el.innerText || el.textContent || '').trim().replace(/\s+/g,' ').slice(0,40);
      const grab = sel => Array.from(document.querySelectorAll(sel)).map(t).filter(Boolean);
      return {
        buttons: grab('button').slice(0,40),
        tabs: grab('[role=tab]').slice(0,20),
        links: grab('a').slice(0,40),
        bodyStart: (document.body.innerText||'').replace(/\s+/g,' ').slice(0,300),
      };
    });
    log('TABS:', JSON.stringify(texts.tabs));
    log('BUTTONS:', JSON.stringify(texts.buttons));
    log('LINKS:', JSON.stringify(texts.links));
    log('BODY:', texts.bodyStart);
  } catch (e) { log('ERR', e.message); await page.screenshot({ path: `${OUT}/diag_err.png` }).catch(()=>{}); }
  finally { await browser.close(); }
})();
