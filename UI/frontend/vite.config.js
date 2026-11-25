import { defineConfig } from "vite";

export default defineConfig(({ command }) => ({
    base: command === "serve" ? "/" : "/react/",
    esbuild: { jsx: "automatic" },
    build: { outDir: "../wwwroot/react", emptyOutDir: true },
    server: {
        proxy: { "/api": "http://localhost:8080" },
        fs: { allow: [".."] },
    },
}));
