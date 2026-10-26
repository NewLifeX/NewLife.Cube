import { test, expect } from '@playwright/test';

/**
 * 动态页回归：这些页面没有手写视图，列表列、表头、表单字段全部来自后端 `/meta`。
 *
 * 断言只用结构选择器（表格行、表头位置、缩进、Tab、表单控件），不依赖界面文案——
 * 界面语言随 `navigator.language` 变化，用文案断言会让用例在非中文环境随机失败。
 */
test.describe('动态页（/meta 驱动）', () => {
  test('部门列表按元数据渲染', async ({ page }) => {
    await page.goto('/Admin/Department');
    await expect(page.locator('h1')).toBeVisible();

    // 表头数量 >= 5 且存在数据行，说明列由后端元数据下发
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__body tr').first()).toBeVisible();
  });

  test('菜单列表按树渲染', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });

    // 树形渲染的证据：存在缩进单元与展开图标
    expect(await page.locator('.el-table__indent').count()).toBeGreaterThan(0);
    expect(await page.locator('.el-table__expand-icon').count()).toBeGreaterThan(0);
  });

  test('角色页保留菜单权限入口并加载权限树', async ({ page }) => {
    await page.goto('/Admin/Role');
    const row = page.locator('.el-table__body tr').first();
    await expect(row).toBeVisible({ timeout: 15000 });

    // 行内除内置查看/编辑/删除外还有一个自定义动作，文案随语言变化，按位置取最后一个
    const actions = row.locator('.el-table__cell:last-child button');
    expect(await actions.count()).toBeGreaterThan(1);
    await actions.last().click();

    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible({ timeout: 15000 });
    await expect(dialog.locator('.el-tree-node').first()).toBeVisible({ timeout: 20000 });
    expect(await dialog.locator('.el-checkbox').count()).toBeGreaterThan(0);
  });

  test('参数页与用户统计页同样由元数据渲染', async ({ page }) => {
    // 参数页：标准增删改查列表（枚举列按后端字典显示）
    await page.goto('/Admin/Parameter');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible();

    // 用户统计页：只读列表，写入口由后端 setting.isReadOnly 收掉，图表走 charts 端点，
    // 后端 `stat` 汇总实体让表格底部出现「合计」行（对齐 MVC `_List_Data_Stat`）
    await page.goto('/Admin/UserStat');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    const toolbarTexts = await page.locator('.cube-table button').allInnerTexts();
    expect(toolbarTexts.some((text) => text.trim().length > 0)).toBe(true);
    await expect(page.locator('.el-table__footer')).toBeVisible({ timeout: 15000 });
  });

  test('列表支持批量禁用/启用（后端 enableSelect/disableSelect）', async ({ page }) => {
    const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';
    // 名称必须全局唯一：用例可被 --repeat-each 并行执行，仅用时间戳会在同一毫秒内撞名（唯一索引冲突）
    const name = `E2E-BATCH-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

    /** 轮询后端直到 Enable 变为期望值：批量动作后的列表刷新与实体缓存都可能滞后，单次 GET 会偶发读到旧值 */
    const waitForEnable = async (expected: boolean) => {
      for (let i = 0; i < 24; i++) {
        const res = await page.request.get(
          `${apiBase}/api/Admin/Parameter?q=${encodeURIComponent(name)}`,
          { ignoreHTTPSErrors: true },
        );
        const rows = (await res.json())?.data ?? [];
        if (rows[0]?.enable === expected) return;
        await page.waitForTimeout(300);
      }
      throw new Error(`参数[${name}]的 Enable 未在预期时间内变为 ${expected}`);
    };

    // 播种一条带 Enable 字段的参数，避免动到库里既有数据
    const created = await page.request.post(`${apiBase}/api/Admin/Parameter`, {
      data: { Name: name, Value: '1', Category: 'E2E', Kind: 0, Enable: true },
      ignoreHTTPSErrors: true,
    });
    const createdBody = await created.json();
    const paramId = createdBody?.data?.id ?? createdBody?.data?.ID;
    expect(createdBody?.code, JSON.stringify(createdBody).slice(0, 300)).toBe(0);
    expect(paramId).toBeTruthy();

    try {
      await page.goto('/Admin/Parameter');
      const keyword = page.locator('.cube-table input:not([type="hidden"]):visible').first();
      await keyword.fill(name);
      await keyword.press('Enter');

      const row = page.locator('.el-table__body tr', { hasText: name }).first();
      await expect(row).toBeVisible({ timeout: 20000 });

      // 勾选后批量禁用，确认走的是后端 enableSelect/disableSelect 端点
      await row.locator('.el-checkbox').first().click();
      await page.locator('[data-role="batch-disable"]').click();
      const [disableResp] = await Promise.all([
        page.waitForResponse((r) => r.url().includes('/disableSelect') && r.request().method() === 'POST'),
        page.locator('.el-message-box__btns .el-button--primary').click(),
      ]);
      expect(disableResp.ok()).toBeTruthy();
      await waitForEnable(false);

      // 再批量启用，恢复原状
      await page.locator('.el-table__body tr', { hasText: name }).first().locator('.el-checkbox').first().click();
      await page.locator('[data-role="batch-enable"]').click();
      const [enableResp] = await Promise.all([
        page.waitForResponse((r) => r.url().includes('/enableSelect') && r.request().method() === 'POST'),
        page.locator('.el-message-box__btns .el-button--primary').click(),
      ]);
      expect(enableResp.ok()).toBeTruthy();
      await waitForEnable(true);
    } finally {
      if (paramId) await page.request.delete(`${apiBase}/api/Admin/Parameter?ids=${paramId}`, { ignoreHTTPSErrors: true });
    }
  });

  test('接口型控制器显示说明而不是空表格', async ({ page }) => {
    // vTest1 下是框架的纯 API 控制器（无实体元数据），不应渲染出点了就报错的工具栏。
    // 两条路径都要覆盖：菜单路由命中（AutoPage）与 catch-all 兜底（DefaultEntity → 引擎 metaMissing）
    for (const path of ['/vTest1/Mfa', '/vTest1/Cube']) {
      await page.goto(path);
      await expect(page.locator('.ct-unsupported, .autopage-unsupported')).toBeVisible({ timeout: 20000 });
      expect(await page.locator('.cube-table .el-table').count()).toBe(0);
    }
  });

  test('设置中心混排单对象配置与列表型配置，各页独立路由', async ({ page }) => {
    await page.goto('/Admin/Cube');
    const tabs = page.locator('.settings-tabs .el-tabs__item');
    await expect(tabs.first()).toBeVisible({ timeout: 15000 });

    // 五个单对象配置页 + 四个列表型配置实体（短信/邮件/OAuth/访问规则）平铺在同一导航里
    expect(await tabs.count()).toBeGreaterThanOrEqual(9);
    // 魔方设置有多个 Category，表单卡片头部应为分类 Tab，而不是「基本信息」
    const groupTabs = page.locator('.fmc-group-tabs .el-tabs__item');
    await expect(groupTabs.first()).toBeVisible({ timeout: 15000 });
    expect(await groupTabs.count()).toBeGreaterThanOrEqual(2);
    // 字段说明以 Label 旁问号 Tooltip 呈现
    await expect(page.locator('.fmc-help').first()).toBeVisible();
    // 首屏配置表单由 /meta 驱动
    await expect(page.locator('.fmc-field').first()).toBeVisible({ timeout: 15000 });

    // 切换 Tab 走独立路由，而不是 ?tab= 查询参数
    await tabs.nth(0).click();
    await expect(page).toHaveURL(/\/Admin\/Core$/);
    await expect(page.locator('.fmc-field').first()).toBeVisible({ timeout: 15000 });

    // 列表型配置页同样带设置导航，并以元数据渲染表格
    await page.locator('.settings-tabs .el-tabs__item').nth(6).click();
    await expect(page).toHaveURL(/\/Admin\/MailConfig$/);
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });
  });

  test('旧设置中心 ?tab= 深链重定向到独立路由', async ({ page }) => {
    await page.goto('/Admin/Cube?tab=Sys');
    await expect(page).toHaveURL(/\/Admin\/Sys$/);
    await expect(page.locator('.settings-tabs .el-tabs__item.is-active').first()).toBeVisible({ timeout: 15000 });
  });

  test('/Cube 区域列表同样按元数据渲染', async ({ page }) => {
    await page.goto('/Cube/CronJob');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible();

    await page.goto('/Cube/Area');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    expect(await page.locator('.el-table__body tr').count()).toBeGreaterThan(5);
  });

  test('无菜单行的控制器页面回退动态页而不是 404', async ({ page }) => {
    // 未开启多租户时，租户/租户成员菜单会被服务端过滤掉，但 MVC 侧控制器路由仍可直接访问。
    // 前端 catch-all 探测 /meta 后按动态页渲染，页头标题取实体显示名。
    for (const path of ['/Admin/Tenant', '/Admin/TenantUser']) {
      await page.goto(path);
      const header = page.locator('.list-page-header h1');
      await expect(header).toBeVisible({ timeout: 20000 });
      await expect(header).not.toContainText(/404/);
      await expect(page.locator('.cube-table')).toBeVisible({ timeout: 20000 });
      expect(await page.locator('.el-table').count()).toBeGreaterThan(0);
    }

    // 真正不存在的路径仍走 404 页
    await page.goto('/Admin/NotExists');
    await expect(page.getByText(/404/).first()).toBeVisible({ timeout: 20000 });
    expect(await page.locator('.cube-table').count()).toBe(0);
  });

  test('值集管理页可渲染（配置弹窗不再抛 TDZ 异常）', async ({ page }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', (e) => pageErrors.push(String(e)));

    await page.goto('/Admin/Lov');
    await expect(page.locator('.lov-page')).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.lov-page .el-table')).toBeVisible({ timeout: 15000 });

    // 新增弹窗挂载了值集配置组件（含配置弹窗），能打开即说明 setup 阶段无异常
    await page.locator('.lov-page .page-header button').first().click();
    await expect(page.locator('.el-dialog').first()).toBeVisible({ timeout: 15000 });

    expect(pageErrors.join('\n')).not.toContain('before initialization');
  });

  test('系统首页解析接口业务体而不是原样渲染响应信封', async ({ page }) => {
    await page.goto('/Admin/Index');
    await expect(page.locator('h1')).toBeVisible({ timeout: 20000 });

    // 系统信息卡片应展开为字段行，而不是把 {code,data,traceId} 当字段打印
    await expect(page.locator('.el-descriptions').first()).toBeVisible({ timeout: 20000 });
    const descriptions = await page.locator('.el-descriptions').first().innerText();
    expect(descriptions).toContain('System');
    expect(descriptions.toLowerCase()).not.toContain('traceid');
    expect(descriptions.length).toBeGreaterThan(10);

    // 进程/程序集/菜单树等接口全部带必填查询参数，页面加载后不应出现错误提示
    await expect(page.locator('.el-message--error')).toHaveCount(0);
  });

  test('只读页保留查看详情入口', async ({ page }) => {
    // 只读控制器（ReadOnlyEntityController）在后端没有写入端点，但详情读取与 MVC 一致：
    // 操作列只给「查看」，编辑/删除收掉。
    for (const path of ['/Admin/Log', '/Admin/UserStat']) {
      await page.goto(path);
      await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });

      // 操作列单元格带 .ctg-actions 类；等按钮渲染完成再点
      const action = page.locator('.el-table__body tr .ctg-actions button').first();
      await expect(action).toBeVisible({ timeout: 20000 });
      await action.click();

      const dialog = page.locator('.el-dialog');
      await expect(dialog).toBeVisible({ timeout: 20000 });
      await expect(dialog.locator('.fmc-field').first()).toBeVisible({ timeout: 20000 });

      // 查看态底部只有「关闭」，点它收口（Esc 在弹窗内不一定生效）
      await dialog.locator('.el-dialog__footer button').last().click();
      await expect(dialog).toBeHidden({ timeout: 10000 });
    }
  });

  test('菜单页渲染后端声明的批量刷新工具栏按钮', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });

    // 按钮文案由后端按 Accept-Language 本地化，这里只断言按钮存在（按位置取首个）
    const actions = page.locator('.cube-table button');
    expect(await actions.count()).toBeGreaterThan(3);
  });

  test('通知记录（隐藏菜单）与应用插件页按元数据渲染', async ({ page }) => {
    // 通知记录：隐藏菜单页面，直连 URL 也必须有路由（只读列表 + 行内标记已读列）
    await page.goto('/Admin/NotificationRecord');
    await expect(page.locator('.list-page-header h1')).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.el-table__header th').nth(5)).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.cube-table')).toBeVisible();

    // 应用插件：魔方管理可见菜单，列表由首次查询扫描本地插件得到
    await page.goto('/Cube/AppModule');
    await expect(page.locator('.list-page-header h1')).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible();
  });
});
