import { chromium } from 'playwright-core';
const OUT = 'C:/Users/ADMINI~1/AppData/Local/Temp/2/claude/C--Users-Administrator-Desktop-PM-b07-ProjectManagement251119/fa38dd24-ec02-42f2-8818-1c1858b48697/scratchpad';
const ctx = await chromium.launchPersistentContext(`${OUT}/udata`, { headless: true, executablePath: 'C\:\\Program Files\\Google\\Chrome\\Application\\chrome.exe', viewport: { width: 1500, height: 950 } });
const page = ctx.pages()[0] || await ctx.newPage();
const log = [];
page.on('console', m => log.push(`[console.${m.type()}] ${m.text()}`.slice(0,200)));
page.on('requestfailed', r => log.push(`[reqfail] ${r.failure()?.errorText} ${r.url()}`.slice(0,200)));
page.on('response', r => { if (r.status() >= 400) log.push(`[http ${r.status()}] ${r.url()}`.slice(0,200)); });
page.on('framenavigated', f => { if (f === page.mainFrame()) log.push(`[nav] ${f.url()}`); });
const sleep = ms => new Promise(r=>setTimeout(r,ms));
try {
  await page.goto('http://localhost:5286/', { waitUntil:'domcontentloaded', timeout:60000 });
  await sleep(2000);
  if (page.url().includes('/Account/Login')) {
    await page.fill('input[type=email]','Admin@at.com'); await page.fill('input[type=password]','Admin@at.com');
    await page.locator('button[type=submit]').first().click(); await sleep(4000);
    log.length = 0; // ignore login noise
    await page.goto('http://localhost:5286/', { waitUntil:'domcontentloaded', timeout:60000 });
  }
  await sleep(18000);
} catch(e){ log.push('ERR '+e.message); }
finally {
  console.log(log.join('\n'));
  await ctx.close();
}
