const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const log = (...a) => console.log('[int]', ...a);
(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
  let wsOpened = 0; const wsUrls = [];
  page.on('websocket', ws => { wsOpened++; wsUrls.push(ws.url()); });
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(()=>{}), page.click('button[type=submit]')]);
    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    log('websockets opened:', wsOpened, JSON.stringify(wsUrls));
    // count Blazor interactive component boundary comments in the whole document
    const markers = await page.evaluate(() => {
      let n = 0; const it = document.createNodeIterator(document, NodeFilter.SHOW_COMMENT);
      let c; const samples = [];
      while ((c = it.nextNode())) { if (/Blazor:/.test(c.nodeValue)) { n++; if (samples.length<3) samples.push(c.nodeValue.slice(0,80)); } }
      return { n, samples };
    });
    log('Blazor interactive markers:', markers.n, JSON.stringify(markers.samples));
    // Is the "Open all users" button inside an interactive region? check nearest preceding Blazor comment
    // Interactivity test: click a control tab "Resource type" and see if active content changes
    const beforeTab = await page.evaluate(()=>document.body.innerText.includes('Showing 3 of 3'));
    await page.getByRole('tab', { name: 'Resource type' }).click().catch(e=>log('tab click err', e.message));
    await page.waitForTimeout(2000);
    const afterTab = await page.evaluate(()=>document.body.innerText.replace(/\s+/g,' ').slice(0,160));
    log('was on departments(before tab):', beforeTab);
    log('after clicking Resource type tab:', afterTab);
  } catch (e) { log('ERR', e.message); }
  finally { await browser.close(); }
})();
