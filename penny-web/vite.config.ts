import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// https://vite.dev/config/
const apiProxy = {
  target: "https://localhost:7242",
  changeOrigin: true,
  secure: false,
};

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      "/auth": apiProxy,
      "/account": apiProxy,
      "/movement": apiProxy,
      "/category": apiProxy,
    },
  },
});
