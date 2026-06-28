const { chromium } = require('./node_modules/playwright-core');
const EXE = 'C:/Users/Administrator/AppData/Local/ms-playwright/chromium-1228/chrome-win64/chrome.exe';
const BASE = 'http://localhost:5286';
const OUT = 'C:/Users/Administrator/Desktop/PM/b07/ProjectManagement251119/.codex-temp-build';
const log = (...a) => console.log('[ind]', ...a);
const sleep = (ms) => new Promise(r => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1500, height: 950 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') log('PAGE-ERR', m.text().slice(0, 160)); });

  try {
    // 1) Login
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('#email', 'nordbygg.admin@test.local');
    await page.fill('#floatingPassword', 'Demo!Pass123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.click('button[type=submit]'),
    ]);
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await sleep(5000);

    // 2) Find a folder with projects + pick project[0] (via authenticated fetch)
    const pick = await page.evaluate(async (b) => {
      const f = await fetch(b + '/api/v1/folders/accessible?includeArchived=true', { credentials: 'include' });
      if (!f.ok) return { err: 'folders ' + f.status };
      const folders = await f.json();
      for (const fl of folders) {
        const r = await fetch(b + '/api/v1/projects/gpmyg/' + fl.id + '?includeArchived=true', { credentials: 'include' });
        if (!r.ok) continue;
        const projects = await r.json();
        if (projects && projects.length) {
          return { folderId: fl.id, folderName: fl.name, project: { id: projects[0].id, name: projects[0].name } };
        }
      }
      return { err: 'no folder with projects' };
    }, BASE);
    log('pick:', JSON.stringify(pick));
    if (pick.err) throw new Error(pick.err);
    const projName = pick.project.name;
    const projId = pick.project.id;

    // 3) Expand the folder in the tree and OPEN (select) the project → records "senast öppnad"
    try { await page.getByText(pick.folderName, { exact: false }).first().click({ timeout: 5000 }); } catch (e) { log('folder click:', e.message.slice(0, 60)); }
    await sleep(1500);
    try {
      await page.getByText(projName, { exact: false }).first().click({ timeout: 6000 });
      log('opened project in tree:', projName);
    } catch (e) { log('project click failed:', e.message.slice(0, 80)); }
    await sleep(2500);

    // 4) Bump the project: GET its post DTO and PUT it back → new UpdatedAt + change-log "Ändrade projektuppgifter"
    const edit = await page.evaluate(async (args) => {
      const [b, id] = args;
      const g = await fetch(b + '/api/v1/projects/gpp/' + id, { credentials: 'include' });
      if (!g.ok) return { err: 'gpp ' + g.status };
      const dto = await g.json();
      const p = await fetch(b + '/api/v1/projects/' + id, {
        method: 'PUT', credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto),
      });
      return { put: p.status };
    }, [BASE, projId]);
    log('edit via API:', JSON.stringify(edit));

    await sleep(1500);

    // 5) Reload → tree loads senast-öppnad (old) and the project's new UpdatedAt → indicator should show
    await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
    await sleep(5000);
    try { await page.getByText(pick.folderName, { exact: false }).first().click({ timeout: 5000 }); } catch {}
    await sleep(2500);

    // 6) Count three-line indicator svgs and capture tree screenshot
    const probe = await page.evaluate(() => {
      const svgs = [...document.querySelectorAll('svg[aria-label="Ändrad sedan du senast öppnade"]')];
      const visible = svgs.filter(s => s.getClientRects().length > 0);
      return { total: svgs.length, visible: visible.length };
    });
    log('change-indicator svgs in DOM:', JSON.stringify(probe));
    await page.screenshot({ path: `${OUT}/ind10_tree_indicator.png`, fullPage: true });

    // 7) Hover the first visible indicator → capture tooltip text + screenshot
    let tip = '(none)';
    try {
      const ind = page.locator('svg[aria-label="Ändrad sedan du senast öppnade"]').first();
      await ind.scrollIntoViewIfNeeded();
      await ind.hover({ timeout: 4000 });
      await sleep(900);
      tip = await page.evaluate(() => {
        const t = document.querySelector('.pm-hover-tooltip, [role="tooltip"]');
        return t ? (t.innerText || t.textContent || '').trim() : '(no tooltip el)';
      });
      await page.screenshot({ path: `${OUT}/ind11_tooltip.png`, fullPage: true });
    } catch (e) { log('hover failed:', e.message.slice(0, 80)); }
    log('TOOLTIP TEXT:\n' + tip);

    log('DONE');
  } catch (e) {
    log('ERROR', e.message);
    await page.screenshot({ path: `${OUT}/ind99_err.png` }).catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
