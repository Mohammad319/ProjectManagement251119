import { chromium } from 'playwright-core';

const OUT = 'C:/Users/ADMINI~1/AppData/Local/Temp/2/claude/C--Users-Administrator-Desktop-PM-b07-ProjectManagement251119/fa38dd24-ec02-42f2-8818-1c1858b48697/scratchpad';
const UDATA = `${OUT}/udata`;
const BASE = 'http://localhost:5286';
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

const ctx = await chromium.launchPersistentContext(UDATA, {
  headless: true, executablePath: CHROME, viewport: { width: 1500, height: 950 },
});
const page = ctx.pages()[0] || await ctx.newPage();
const errs = [];
page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
const sleep = ms => new Promise(r => setTimeout(r, ms));
async function shot(name) { await page.screenshot({ path: `${OUT}/${name}.png` }); console.log('shot', name); }
async function info(label) {
  const d = await page.evaluate(() => ({
    url: location.href,
    errVisible: !!document.querySelector('#blazor-error-ui') &&
                getComputedStyle(document.querySelector('#blazor-error-ui')).display !== 'none',
    text: (document.body.innerText || '').replace(/\s+/g, ' ').slice(0, 600),
    buttons: [...document.querySelectorAll('button')].map(b => (b.innerText||'').trim()).filter(Boolean).slice(0, 40),
  }));
  console.log(`--- ${label} ---`);
  console.log('url:', d.url, '| blazor-error visible:', d.errVisible);
  console.log('text:', d.text);
  console.log('buttons:', JSON.stringify(d.buttons));
  return d;
}

try {
  await page.goto(BASE, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await sleep(2000);
  if (page.url().includes('/Account/Login')) {
    await page.fill('input[type=email], input[id*=Email i]', 'Admin@at.com');
    await page.fill('input[type=password], input[id*=Password i]', 'Admin@at.com');
    await page.locator('button[type=submit], button:has-text("Login")').first().click();
    await page.waitForLoadState('domcontentloaded').catch(()=>{});
    await sleep(4000);
  }
  // Poll up to 75s for the interactive folder/project view to render.
  let booted = false;
  for (let i = 0; i < 15; i++) {
    await sleep(5000);
    const len = await page.evaluate(() => (document.body.innerText || '').replace(/\s+/g,'').length);
    const main = await page.evaluate(() => {
      const m = document.querySelector('main, .mhd-splitter, [class*=splitter]') || document.body;
      return (m.innerHTML || '').length;
    });
    console.log(`poll ${i+1}: textLen=${len} mainHtml=${main}`);
    if (len > 60) { booted = true; break; }
  }
  await shot('20-home');
  await info('HOME');
  console.log('booted:', booted, 'CONSOLE-ERRS:', JSON.stringify([...new Set(errs)].slice(0, 10)));
} catch (e) {
  console.log('ERROR:', e.message);
  await shot('98-error');
} finally {
  await ctx.close();
}
