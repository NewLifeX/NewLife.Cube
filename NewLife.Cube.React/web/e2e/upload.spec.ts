/**
 * 上传组件 E2E（CMP-2 Uploader）
 *
 * 以 /Admin/User 新增表单的"头像"图片字段为例，验证：
 * 选择文件 → 调用 UploadFile API → 显示图片预览 + 成功反馈 → 可清除恢复上传按钮。
 * 不保存记录，避免污染用户数据。
 */
import { expect, test } from '@playwright/test';

// 1x1 红色 PNG
const PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
  'base64',
);

test.describe('上传组件（CMP-2 /Cube/App 图标）', () => {
  /** 打开新增表单并返回上传文件 input（7081 一体站点 antd 按钮可能被遮挡，用原生 click） */
  async function openAddForm(page: import('@playwright/test').Page) {
    await page.goto('/Cube/App');
    // 全量并发时，home.spec 登出用例的 admin 重新登录可能在单设备模式下踢掉共享 storageState token
    // → 跳登录后自动重登，保证上传用例可独立稳定运行
    if (page.url().includes('/login')) {
      await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill(process.env.ADMIN_USER || 'admin');
      await page.getByPlaceholder('请输入密码').fill(process.env.ADMIN_PASS || 'admin');
      await page.getByRole('button', { name: '登 录' }).click();
    }
    // 登录成功跳回 /Cube/App 后出现工具栏"新增"按钮（若仍在登录页则超时，暴露登录失败）
    // 注意：不要用 waitForURL('**/Cube/App*')，其 glob 会误匹配 /login?r=%2FCube%2FApp
    await expect(page.locator('button:has-text("新增")').first()).toBeVisible({ timeout: 15000 });
    await page.evaluate(() => {
      const b = Array.from(document.querySelectorAll('button')).find((x) => x.textContent?.includes('新增'));
      (b as HTMLButtonElement | null)?.click();
    });
    const input = page.locator('.ant-upload input[type=file]').first();
    await expect(input).toBeAttached({ timeout: 10000 });
    return input;
  }

  test('选择图片后上传成功并显示预览', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    const input = await openAddForm(page);
    await expect(page.locator('button:has-text("上传图片")').first()).toBeVisible({ timeout: 10000 });

    await input.setInputFiles({ name: 'avatar-test.png', mimeType: 'image/png', buffer: PNG });

    // 上传成功：antd Image 预览出现，该字段"上传图片"按钮消失
    await expect(page.locator('.ant-image').first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('button:has-text("上传图片")').first()).toBeHidden({ timeout: 10000 });

    // 无 JS 控制台错误（favicon 404 忽略）
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('上传后可清除图片恢复上传按钮', async ({ page }) => {
    const input = await openAddForm(page);
    await input.setInputFiles({ name: 'avatar-test.png', mimeType: 'image/png', buffer: PNG });
    await expect(page.locator('.ant-image').first()).toBeVisible({ timeout: 15000 });

    // 删除按钮（danger）清除预览——必须限定在弹窗内，避免误点页面其它 danger 按钮（如用户菜单登出）
    const del = page.locator('.ant-modal-content button.ant-btn-dangerous').first();
    await expect(del).toBeVisible({ timeout: 10000 });
    await page.evaluate(() => {
      const b = document.querySelector('.ant-modal-content button.ant-btn-dangerous');
      (b as HTMLButtonElement | null)?.click();
    });

    // 弹窗保持打开，回到上传按钮状态
    await expect(page.locator('.ant-modal-content')).toBeVisible({ timeout: 8000 });
    await expect(page.locator('.ant-modal-content button:has-text("上传图片")').first()).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.ant-image').first()).toBeHidden({ timeout: 8000 });
  });
});
