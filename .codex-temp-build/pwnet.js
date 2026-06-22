const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const log = (...a) => console.log('[net]', ...a);
(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
  const failed = [];
  page.on('requestfailed', r => failed.push(`FAIL ${r.failure()?.errorText} ${r.url()}`));
  page.on('response', r => { if (r.status() >= 400) failed.push(`HTTP ${r.status()} ${r.url()}`); });
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }).catch(()=>{}), page.click('button[type=submit]')]);
    await page.goto(`${BASE}/control`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(4000);
    const blazor = await page.evaluate(() => ({
      hasBlazor: typeof window.Blazor !== 'undefined',
      ws: (performance.getEntriesByType('resource')||[]).filter(e=>/_blazor|negotiate|hub/i.test(e.name)).map(e=>e.name).slice(0,5),
      scriptModules: Array.from(document.querySelectorAll('script[type=module],script[src]')).map(s=>s.src).filter(Boolean).slice(0,15),
    }));
    log('hasBlazor:', blazor.hasBlazor);
    log('blazor-ish resources:', JSON.stringify(blazor.ws));
    log('script srcs:', JSON.stringify(blazor.scriptModules, null, 1));
    log('FAILED/4xx requests:'); failed.slice(0,30).forEach(f=>log('  '+f));
    // interactivity probe: click Open all users, see if body text changes
    const before = (await page.evaluate(()=>document.body.innerText)).includes('Open all users');
    await page.getByRole('button',{name:'Open all users'}).first().click().catch(e=>log('click err',e.message));
    await page.waitForTimeout(2500);
    const afterText = await page.evaluate(()=>document.body.innerText.replace(/\s+/g,' ').slice(0,200));
    log('after-click body:', afterText);
  } catch (e) { log('ERR', e.message); }
  finally { await browser.close(); }
})();
