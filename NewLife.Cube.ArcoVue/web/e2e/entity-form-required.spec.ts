import { expect, test } from '@playwright/test';

/**
 * 实体表单必填收敛（OSC-260925 审计）。
 *
 * 背景：旧必填矩阵把所有数据库非空列（布尔开关、数值、枚举、系统列）都标为表单必填，
 * 部门添加表单实测 10 项「X不可以为空！」误拦，只填名称无法保存；流程定义编辑表单
 * 启用/已发布/版本 三项误带红星。收敛后仅非空字符串字段必填。
 *
 * 断言原则：只读页面元数据渲染结果（DOM），不点行坐标；部门用例真实保存并清理数据。
 */

/** 必填字段标签（读取红星标记所在表单项的 label 文本） */
const requiredLabels = (root: import('@playwright/test').Locator) =>
  root
    .locator('.arco-form-item')
    .evaluateAll((els) =>
      els
        .filter((el) => !!el.querySelector('.arco-form-item-label-required-symbol'))
        .map((el) => (el.querySelector('label')?.textContent ?? el.textContent ?? '').trim()),
    );

test('部门添加：仅名称必填，只填名称即可保存', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/Admin/Department');
  await page.waitForTimeout(1500);
  await page.getByRole('button', { name: /添加记录/ }).click();
  const drawer = page.locator('.arco-drawer').last();
  await expect(drawer).toBeVisible({ timeout: 10_000 });

  // 必填只剩「名称」：上级/层级/排序/启用/可见/管理者/扩展1-3 等不再误标
  await expect.poll(() => requiredLabels(drawer)).toEqual(['名称']);

  const name = `E2E必填校验${Date.now() % 100000}`;
  await drawer.locator('.arco-form-item').filter({ hasText: '名称' }).locator('input').first().fill(name);
  await drawer.getByRole('button', { name: '保存' }).click();
  await expect(page.locator('.arco-message').filter({ hasText: '成功' }).first()).toBeVisible({ timeout: 10_000 });

  // 清理：按名称查编号并删除
  const cleanup = await page.evaluate(async (deptName: string) => {
    const res = await fetch('/api/Admin/Department?pageIndex=0&pageSize=200', { headers: { accept: 'application/json' } });
    const json = await res.json();
    const row = (json.data as { id?: number; name?: string }[]).find((r) => r.name === deptName);
    if (row?.id == null) return 'not-found';
    const del = await fetch(`/api/Admin/Department?id=${row.id}&doubleDelete=false`, { method: 'DELETE' });
    return `${del.status}`;
  }, name);
  expect(cleanup, 'E2E 测试数据应清理成功').toBe('200');
});

test('流程定义添加：仅名称/实体路径必填', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 800 });
  await page.goto('/Cube/WorkflowDefinition');
  await page.waitForTimeout(2000);
  await page.getByRole('button', { name: /添加记录/ }).click();
  const drawer = page.locator('.arco-drawer').last();
  await expect(drawer).toBeVisible({ timeout: 10_000 });

  // 名称/实体路径为业务必填；启用/版本/锁策略/发起条件/图 JSON 等由默认值与设计器维护，不在添加表单/不标必填
  await expect.poll(() => requiredLabels(drawer)).toEqual(['名称', '实体路径']);

  await drawer.getByRole('button', { name: '取消' }).click();
  await expect(drawer).toBeHidden({ timeout: 5_000 });
});
