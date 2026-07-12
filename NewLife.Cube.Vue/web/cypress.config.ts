import { defineConfig } from "cypress";

/**
 * 对 Chrome（及 chromium 家族）浏览器在 launch 时追加沙箱/资源相关参数，
 * 以便 Cypress 在受限环境（沙箱 / 容器 / CI）中以 headless 方式正常启动。
 */
const addChromeLaunchArgs = (browser: any, launchOptions: any) => {
  // chrome / chromium / electron 在受限环境（沙箱 / 容器 / CI）下需禁用沙箱与崩溃上报进程
  if (
    browser.name === "chrome" ||
    browser.family === "chromium" ||
    browser.name === "electron"
  ) {
    launchOptions.args.push(
      "--no-sandbox",
      "--disable-gpu",
      "--disable-dev-shm-usage",
      "--disable-crash-reporter",
      "--disable-breakpad"
    );
  }
  return launchOptions;
};

export default defineConfig({
  // 环境变量：cy.login() 通过 Cypress.env('apiUrl') 获取后端地址
  env: {
    apiUrl: "https://localhost:7116",
  },
  e2e: {
    specPattern: "cypress/e2e/**/*.{cy,spec}.{js,jsx,ts,tsx}",
    // 前端 dev 地址（API 请求由 VITE_API_URL 驱动直连后端，不走 Vite 代理）
    baseUrl: "http://localhost:5187",
    // 关闭 Chrome 跨域安全策略，允许 cy.request 直连 HTTPS 后端（自签名证书）
    chromeWebSecurity: false,
    setupNodeEvents(on, config) {
      on("before:browser:launch", addChromeLaunchArgs);
      return config;
    },
  },

  component: {
    devServer: {
      framework: "vue",
      bundler: "vite",
    },
    // 组件测试同样使用系统 Chrome headless，需相同的启动参数
    setupNodeEvents(on, config) {
      on("before:browser:launch", addChromeLaunchArgs);
      return config;
    },
  },
});
