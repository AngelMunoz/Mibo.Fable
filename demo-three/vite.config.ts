import { defineConfig } from 'vite';

export default defineConfig(async () => {
    return {
        base: '/',
        plugins: [],
        build: {
            sourcemap: true,
        },
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
