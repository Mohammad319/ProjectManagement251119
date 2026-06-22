const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[diag3]', ...a);

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
  const failed = [];
  page.on('requestfailed', r => failed.push(`${r.failure()?.errorText} ${r.url()}`));
  page.on('response', r => { if (r.status() >= 400) failed.push(`HTTP ${r.status()} ${r.url()}`); });
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text()); });
  let wsOpened = 0; page.on('websocket', () => wsOpened++);

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(()=>{}), page.click('button[type=submit]')]);
    log('after login url:', page.url());

    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    log('websockets opened:', wsOpened);
    const markers = await page.evaluate(() => {
      let n = 0; const it = document.createNodeIterator(document, NodeFilter.SHOW_COMMENT);
      let c; while ((c = it.nextNode())) if (/Blazor:/.test(c.nodeValue)) n++;
      return n;
    });
    log('Blazor interactive markers:', markers);

    const hasBtn = await page.getByRole('button', { name: 'Open all users' }).count();
    log('Open all users button count:', hasBtn);
    if (hasBtn) {
      await page.getByRole('button', { name: 'Open all users' }).first().click().catch(e=>log('click err', e.message));
      await page.waitForTimeout(3000);
    }
    const hasTable = await page.locator('th#hselect').count();
    const bodyTxt = await page.evaluate(() => document.body.innerText.replace(/\s+/g,' ').slice(0, 220));
    log('th#hselect count after click:', hasTable);
    log('body text:', bodyTxt);
    await page.screenshot({ path: `${OUT}/diag3_after_click.png` });
  } catch (e) { log('ERR', e.message); }
  finally {
    log('--- FAILED REQUESTS (' + failed.length + ') ---');
    failed.slice(0, 25).forEach(f => log(f));
    await browser.close();
  }
})();
