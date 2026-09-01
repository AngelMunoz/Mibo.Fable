import { defineConfig } from 'vite';
import { execSync } from 'node:child_process';

// Try to determine the base URL from the git repository
// If this doesn't work for you, don't hesite to remove this function and hardcode the base URL
// in the `defineConfig` function below
function findBaseUrlFromRemoteUrl() {
    try {
        const remoteUrl = execSync("git remote get-url origin", { stdio: [] }).toString();

        const findRemoteBaseUrl = /git@github\.com:.*\/(.*).git/gm;

        const match = findRemoteBaseUrl.exec(remoteUrl);

        if (match) {
            return `/${match[1]}/`;
        }
    } catch {
        // Not a git repository (or no origin remote) — serve from the root.
    }

    return '/';
}

// https://vitejs.dev/config/
export default defineConfig(async ({ command, mode }) => {
    // Try to determine the base URL
    let baseUrl = '/';

    if (command === 'build') {
        baseUrl = findBaseUrlFromRemoteUrl();
    }

    return {
        base: baseUrl,
        plugins: [],
        server: {
            watch: {
                ignored: [
                    "**/*.fs"
                ]
            }
        },
        clearScreen: false,
    }
})
