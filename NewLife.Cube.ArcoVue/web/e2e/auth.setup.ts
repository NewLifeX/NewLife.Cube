import { expect, test as setup } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { solveCaptchaDataUrl } from './helpers/captcha';

/**
 * 登录并保存会话状态（OSC-2608139feb；OSC-260922201a 增加验证码识别）。
 * 账号默认 admin/admin，可用环境变量 E2E_USER / E2E_PASSWORD 覆盖。
 * 验证码为算术题图片，通过本地模板匹配（e2e/helpers/captcha.ts）自动识别，识别失败自动重试。
 */
const USER = process.env.E2E_USER || 'admin';
const PASSWORD = process.env.E2E_PASSWORD || 'admin';

setup('authenticate', async ({ page }) => {
  await page.goto('/login');
  // 登录页首个输入框是「租户 Code」，必须按 placeholder 定位
  await page.getByPlaceholder('请输入用户名').fill(USER);
  await page.getByPlaceholder('请输入密码').fill(PASSWORD);

  const loginBtn = page.getByRole('button', { name: '登录', exact: true }).first();
  const layout = page.locator('.arco-layout').first();
  const captchaImg = page.locator('.captcha-img:visible img').first();
  const captchaInput = page.locator('input[placeholder="计算结果"]:visible').first();

  // 验证码可能已随页面加载获取；没有则先点一次登录触发
  if (!(await captchaImg.isVisible({ timeout: 3_000 }).catch(() => false))) {
    await loginBtn.click();
    await captchaImg.waitFor({ state: 'visible', timeout: 10_000 }).catch(() => undefined);
  }

  // 识别 + 登录，最多重试 5 次（识别错误时后端会要求刷新验证码）
  let ok = false;
  for (let i = 0; i < 5 && !ok; i++) {
    const src = await captchaImg.getAttribute('src').catch(() => null);
    const answer = src ? solveCaptchaDataUrl(src) : null;

    if (answer == null) {
      // 识别失败：点验证码刷新再来
      await captchaImg.click({ force: true }).catch(() => undefined);
      await page.waitForTimeout(500);
      continue;
    }

    await captchaInput.fill(answer);
    await loginBtn.click();

    ok = await layout.isVisible({ timeout: 8_000 }).catch(() => false);
    if (!ok) {
      // 登录失败：等待验证码刷新（handleLoginResult 会 refreshCaptcha）
      await page.waitForTimeout(800);
      if (!(await captchaImg.isVisible().catch(() => false))) break;
    }
  }

  // 登录成功进入布局（首页或菜单可见）
  await expect(layout).toBeVisible({ timeout: 10_000 });

  mkdirSync(join(dirname(fileURLToPath(import.meta.url)), '..', 'playwright', '.auth'), { recursive: true });
  await page.context().storageState({ path: 'playwright/.auth/user.json' });
});
