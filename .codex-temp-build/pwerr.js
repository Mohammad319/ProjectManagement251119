const { chromium } = require('./node_modules/playwright-core');
const fs = require('fs');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[err]', ...a);
const lines = [];

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { lines.push(`[${m.type()}] ${m.text()}`); });
  page.on('pageerror', e => { lines.push(`[pageerror] ${e.message}\n${e.stack || ''}`); });

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
    await page.getByText('Demoprojekt', { exact: false }).first().click({ timeout: 5000 });
    await page.waitForTimeout(5000);

    fs.writeFileSync(`${OUT}/console_dump.txt`, lines.join('\n'), 'utf8');
    log('wrote console_dump.txt, total lines:', lines.length);
    const errs = lines.filter(l => /exception|nullref|error|crit/i.test(l));
    log('error-related lines:', errs.length);
    errs.slice(0, 20).forEach(l => log('  >', l.slice(0, 300)));

    log('DONE OK');
  } catch (e) {
    log('ERROR', e.message);
  } finally {
    await browser.close();
  }
})();
