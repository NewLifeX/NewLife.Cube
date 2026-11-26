import { expect, test } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';

test('真实登录页：记住选择重载恢复、关闭配置隐藏（接口桩）', async ({ page }) => {
  const evidence = '../../.temp/remember-browser';
  await mkdir(evidence, { recursive: true });
  const diagnostics: string[] = [];
  page.on('pageerror', error => diagnostics.push(`pageerror: ${error.stack}`));
  page.on('console', message => { if (message.type() === 'error') diagnostics.push(message.text()); });
  const responses: { url: string; status: number; body: unknown }[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('cube-language', 'zh-CN');
    (window as unknown as { _CUBE_CONFIG_: unknown })._CUBE_CONFIG_ = {
      request: { baseUrl: window.location.origin },
      auth: { tokenRefresh: { enabled: sessionStorage.getItem('disable-refresh') !== 'true' } },
    };
  });
  await page.route(url => /^\/(Auth|Cube)\//i.test(url.pathname), async route => {
    const url = route.request().url();
    const data = /LoginConfig/i.test(url)
      ? { login: { password: true }, security: { passwordComplexity: false }, oauth: [] }
      : /Site/i.test(url) ? { name: '令牌刷新浏览器验证' } : {};
    const body = { code: 0, data };
    responses.push({ url, status: 200, body });
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
  await page.goto('/login');
  const remember = page.getByRole('checkbox', { name: '记住我' });
  const rememberLabel = page.locator('label').filter({ has: remember });
  await expect(rememberLabel).toBeVisible();
  await page.screenshot({ path: `${evidence}/initial-page.png`, fullPage: true });
  await writeFile(`${evidence}/diagnostics.json`, JSON.stringify({ diagnostics, html: await page.locator('body').innerText(), responses }, null, 2));
  await rememberLabel.click();
  await expect(remember).toBeChecked();
  console.log('checkbox-style', await page.locator('.el-checkbox__inner').evaluate(element => ({
    background: getComputedStyle(element).backgroundColor,
    check: getComputedStyle(element, '::after').content,
    transform: getComputedStyle(element, '::after').transform,
  })));
  await page.screenshot({ path: `${evidence}/remember-checked.png`, fullPage: true, animations: 'disabled' });
  await page.reload();
  await expect(remember).toBeChecked();
  await expect(page.locator('input[type="password"]')).toHaveValue('');
  await page.screenshot({ path: `${evidence}/remember-reloaded.png`, fullPage: true, animations: 'disabled' });
  await page.evaluate(() => sessionStorage.setItem('disable-refresh', 'true'));
  await page.reload();
  await expect(remember).toHaveCount(0);
  await page.screenshot({ path: `${evidence}/remember-disabled.png`, fullPage: true });
  await writeFile(`${evidence}/responses.json`, JSON.stringify(responses, null, 2));
});
