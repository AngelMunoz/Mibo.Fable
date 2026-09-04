// Drives the QUnit browser harness (tests/index.html) in a real Chromium.
// Node-only suites run with `pnpm test`; this lane covers what needs the
// DOM and a GPU context.
import { chromium } from 'playwright';
import { createServer } from 'vite';

const server = await createServer({
    root: new URL('.', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'),
    logLevel: 'error',
    server: { port: 0 },
});
await server.listen();
const url = server.resolvedUrls.local[0];

let browser;
const launchAttempts = [
    {},
    { channel: 'msedge' },
    { channel: 'chrome' },
];
for (const options of launchAttempts) {
    try {
        browser = await chromium.launch(options);
        break;
    } catch {
        // Try the next browser channel.
    }
}
if (!browser) {
    console.error('No browser available. Run `npx playwright install chromium`.');
    await server.close();
    process.exit(2);
}

const page = await browser.newPage();
page.on('console', (msg) => {
    const url = msg.location() ? msg.location().url : '';
    if (msg.type() === 'error' && !url.includes('favicon')) {
        console.log('  [page:error]', msg.text());
    }
});
page.on('pageerror', (err) => console.log('  [pageerror]', err.message));
await page.goto(url);
try {
    await page.waitForFunction(() => window.__runEnd !== undefined, null, { timeout: 60000 });
} catch {
    console.error('The test page never reported results.');
    await browser.close();
    await server.close();
    process.exit(2);
}

const run = await page.evaluate(() => window.__runEnd);
console.log(`Browser tests: ${run.counts.total} tests, ${run.counts.failed} failed, ${run.counts.skipped} skipped (${run.runtime} ms)`);
for (const failure of run.failures) {
    console.log(`FAIL ${failure.name}`);
    for (const message of failure.messages) {
        console.log(`  ${message}`);
    }
}

await browser.close();
await server.close();
process.exit(run.status === 'passed' ? 0 : 1);
