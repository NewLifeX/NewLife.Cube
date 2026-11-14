import { test, expect } from '@playwright/test';

const GALLERY = process.env.CT_BASE_URL || 'http://127.0.0.1:5190/';

// 复现缺陷（三因子叠加：:options="[]" 字面量 × element-plus watch(options) 重建 store ×
// 守卫「发起即 mark」）：展开面板后必须经根层北京市→五区真渲染。
// 缺陷态：lazy 灌入数据被 store 重建清空 → 重触发 lazyLoad 被 canLoad 短路 → 面板「暂无数据」。
test('LazyRoot 展开面板五区真渲染(不应暂无数据)', async ({ page }) => {
  await page.goto(`${GALLERY}?story=${encodeURIComponent('AreaSelect/LazyRoot')}`);
  await page.waitForSelector('.area-select');
  await page.click('.area-select');
  await page.waitForSelector('.el-cascader-panel', { timeout: 5000 });

  // 根层必须先渲染出北京市（缺陷态下根菜单直接「暂无数据」→ 本断言红）
  const panel = page.locator('.el-cascader-panel');
  await expect(panel).toContainText('北京市', { timeout: 5000 });
  await expect(panel).not.toContainText('暂无数据');

  // 点开北京市 → 第二层五区真渲染（东城/西城/朝阳/丰台/海淀五区须全部出现）
  await page.click('.el-cascader-node:has-text("北京市")');
  await page.waitForSelector('.el-cascader-menu:nth-child(2) .el-cascader-node', { timeout: 5000 });
  await expect(panel).toContainText('东城区');
  await expect(panel).toContainText('西城区');
  await expect(panel).toContainText('朝阳区');
  await expect(panel).toContainText('丰台区');
  await expect(panel).toContainText('海淀区');
  await expect(panel).not.toContainText('暂无数据');

  await page.waitForTimeout(300); // 等面板动画稳定
  await expect(page.locator('.el-cascader__dropdown')).toHaveScreenshot();
});

// 回显链：modelValue=110101 → /Cube/AreaParents 桩链写入 selected → 面板交互链可达东城区 label。
// 机制注记（element-plus 2.14.8 lazy cascader）：初始 modelValue 时面板 store 尚无节点，
// input 不解析 label（既不显示文字也不裸数字，显示占位）——挂载即回显 label 不在本用例范围，
// 本用例验证「syncPath 命中桩链 + 展开面板逐级到东城区 → input 显示 label 不裸数字」可达性。
// 挂载即回显增强（面板展开前显示 label）留作后续增强。
test('Echo 面板交互链可达东城区 label(不应裸数字)', async ({ page }) => {
  await page.goto(`${GALLERY}?story=${encodeURIComponent('AreaSelect/Echo')}`);
  await page.waitForSelector('.area-select');
  // 挂载态：syncPath 已命中 /Cube/AreaParents 桩链（selected=[110000,110100,110101]）；
  // 机制上 input 此刻显示占位而非 label——断言不裸数字即可
  const input = page.locator('.area-select input');
  await expect(page.locator('.area-select')).not.toContainText('110101', { timeout: 5000 });
  // 展开面板 → 北京市 → 东城区：label 链与 onChange 通路可达
  await page.click('.area-select');
  await page.waitForSelector('.el-cascader-panel', { timeout: 5000 });
  await page.click('.el-cascader-node:has-text("北京市")');
  await page.waitForSelector('.el-cascader-menu:nth-child(2) .el-cascader-node', { timeout: 5000 });
  await page.click('.el-cascader-node:has-text("东城区")');
  await expect(input).toHaveValue(/东城区/, { timeout: 5000 });
  await expect(page.locator('.area-select')).not.toContainText('110101');
  await page.waitForTimeout(300); // 等面板动画稳定
  await expect(page.locator('.area-select')).toHaveScreenshot();
});
