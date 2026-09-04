import { defineConfig, type Plugin } from 'vite';
import { globSync } from 'node:fs';

// Serves the QUnit module list to the test harness page, the way Perla's
// dev server does: the page asks the server for the files, so a new test
// module never needs a manual listing anywhere. The include and exclude
// globs are the same shape the qunit CLI takes on the Node side.
function qunitTestFiles(options: { include: string[]; exclude?: string[] }): Plugin {
    return {
        name: 'mibo-qunit-test-files',
        configureServer(server) {
            server.middlewares.use('/__qunit-tests__/files', (_req, res) => {
                const files = globSync(options.include, {
                    cwd: server.config.root,
                    exclude: options.exclude,
                    nodir: true,
                }).map((file) => `./${file}`);
                res.setHeader('Content-Type', 'application/json');
                res.end(JSON.stringify(files));
            });
        },
    };
}

export default defineConfig({
    plugins: [
        qunitTestFiles({
            include: ['*Tests.fs.js'],
        }),
    ],
    server: {
        watch: {
            ignored: ['**/*.fs'],
        },
    },
});
