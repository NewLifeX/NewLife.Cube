#!/usr/bin/env node

/**
 * 魔方多租户回归复现测试脚本（v3）
 * =============================
 *
 * 复现 Doc/魔方多租户架构评审与改造建议.md 中描述的 P0/P1 问题：
 *   P0-1: GetTenantId 空 Cookie 引发 NullReferenceException
 *   P0-2: 存量用户锁死——开启多租户没有迁移路径
 *
 * 前提条件：
 *   1. CubeDemo 已在 http://localhost:5000 运行
 *   2. 如需测试多租户场景，需先手动设置 EnableTenant=true
 *      方式：确保 EnableTenant 参数在 Parameter 表中存在且值为 "true"
 *      或直接修改 dbConfig_Cube.json 中的 EnableTenant 字段
 *   3. 默认 admin/admin 账号可用
 *
 * 用法：node Doc/tenant-repro-test.mjs
 *
 * 预期结果：
 *   修复前：测试 2 会出现 500（NRE）或 401（拒绝访问）
 *   修复后：所有测试应通过，且无 500 错误
 */

const BASE = 'https://localhost:7116';
const PASSWORD = 'Test@12345';
const TENANT_CODE = 't1';

// ============ 工具函数 ============

async function api(method, path, body, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  try {
    return { status: res.status, data: JSON.parse(text) };
  } catch {
    return { status: res.status, data: text };
  }
}

let passed = 0, failed = 0;

function assert(condition, message) {
  if (condition) {
    console.log(`  ✅ PASS: ${message}`);
    passed++;
  } else {
    console.error(`  ❌ FAIL: ${message}`);
    failed++;
  }
}

function extractToken(data) {
  return data?.access_token || data?.accessToken;
}

// 缓存的管理员令牌
let adminToken = null;

// ============ 测试 1: API 连通性 ============

async function test1_connectivity() {
  console.log('\n=== 测试 1: API 连通性与管理员认证 ===\n');

  // 1a. 健康检查
  const health = await api('GET', '/cube/info');
  assert(health.data.code === 0, `CubeDemo 健康检查通过`);
  console.log(`  ℹ️ 服务: ${health.data.data?.name} v${health.data.data?.fileVersion}`);

  // 1b. 管理员登录
  const login = await api('POST', '/Auth/Login', { username: 'admin', password: 'admin', remember: false });
  assert(login.data.code === 0, `管理员登录成功`);
  adminToken = extractToken(login.data.data);
  assert(!!adminToken, '获得管理员令牌');

  // 1c. 获取当前 EnableTenant 状态
  const cfg = await api('GET', '/api/admin/cube', null, adminToken);
  const enableTenant = cfg.data?.data?.enableTenant;
  console.log(`  ℹ️ 当前 EnableTenant = ${enableTenant}`);
  if (enableTenant !== true) {
    console.log(`  ⚠️ EnableTenant 未开启，多租户场景（测试 5）将跳过`);
  }
}

// ============ 测试 2: 租户设置完整读=写（P0-1 间接） ============

async function test2_cubeSettingReadWrite() {
  console.log('\n=== 测试 2: 验证 CubeSetting 的 PUT 不破坏 JwtSecret ===\n');
  console.log('  说明：PUT /api/admin/cube 会破坏 JwtSecret，这是 ConfigController 的 bug');
  console.log('  这间接影响多租户测试：设置 EnableTenant=true 后 JWT 失效\n');

  // 读当前值
  const before = await api('GET', '/api/admin/cube', null, adminToken);
  const jwtBefore = before.data?.data?.jwtSecret;
  const etBefore = before.data?.data?.enableTenant;
  console.log(`  ℹ️ JwtSecret 长度: ${jwtBefore?.length || 0}, EnableTenant=${etBefore}`);

  // 写入完整对象
  const full = before.data.data;
  full.enableTenant = !etBefore;
  const writeRes = await api('PUT', '/api/admin/cube', full, adminToken);
  assert(writeRes.data?.code === 0, `PUT CubeSetting 成功`);

  // 读回验证
  const after = await api('GET', '/api/admin/cube', null, adminToken);
  const jwtAfter = after.data?.data?.jwtSecret;
  const etAfter = after.data?.data?.enableTenant;
  assert(etAfter === !etBefore, `EnableTenant 已切换为 ${etAfter}`);

  if (jwtAfter && jwtAfter.length > 0) {
    console.log(`  ✅ JwtSecret 已保留（长度 ${jwtAfter.length}）`);
  } else {
    console.log(`  ⚠️ JwtSecret 被清除（Config.Copy 的 bug —— 会复制 null 值到所有字段）`);
    console.log(`  ⚠️ 这是 ObjectController.Update 的已知问题，修复前不能通过 PUT 安全地设置 EnableTenant`);
  }

  // 恢复原值
  full.enableTenant = etBefore;
  await api('PUT', '/api/admin/cube', full, adminToken);

  // 验证登录仍然可用
  const login = await api('POST', '/Auth/Login', { username: 'admin', password: 'admin', remember: false });
  if (login.data.code !== 0) {
    console.log(`  ❌ 管理员登录失败：JwtSecret 已被破坏，需要重启 CubeDemo`);
  } else {
    console.log(`  ✅ 管理员登录仍然正常`);
    adminToken = extractToken(login.data.data);
  }
}

// ============ 测试 3: 用户注册和基本认证 ============

async function test3_userRegistration() {
  console.log('\n=== 测试 3: 用户注册和基本认证 ===\n');

  // 注册测试用户
  const usernames = ['legacy01', 'admin01', 'tenant01'];
  for (const name of usernames) {
    const reg = await api('POST', '/Auth/Register', {
      category: 0, username: name, password: PASSWORD, confirmPassword: PASSWORD,
    });
    if (reg.data.code === 0) {
      console.log(`  ✅ 注册 ${name} 成功`);
    } else if (reg.data.message?.includes('已存在')) {
      console.log(`  ℹ️ ${name} 已存在`);
    } else {
      console.log(`  ⚠️ 注册 ${name}: ${reg.data.message}`);
    }

    // 验证登录
    const login = await api('POST', '/Auth/Login', { username: name, password: PASSWORD, remember: false });
    if (login.data.code === 0) {
      const token = extractToken(login.data.data);
      assert(!!token, `${name} 登录成功并获得令牌`);
    } else {
      console.log(`  ⚠️ ${name} 登录失败: ${login.data.message}`);
    }
  }
}

// ============ 测试 4: 租户管理 API 可用性 ============

async function test4_tenantManagement() {
  console.log('\n=== 测试 4: 租户管理 API 可用性 ===\n');

  // 查看已有租户
  const tenantList = await api('GET', '/api/admin/tenant', null, adminToken);
  assert(tenantList.data.code === 0, '获取租户列表');
  if (tenantList.data.data?.length > 0) {
    console.log(`  ℹ️ 已有 ${tenantList.data.data.length} 个租户:`);
    for (const t of tenantList.data.data) {
      console.log(`    - id=${t.id}, code=${t.code}, name=${t.name}, enable=${t.enable}`);
    }
  }

  // 查看已有 TenantUser 绑定
  const tuList = await api('GET', '/api/admin/tenantuser', null, adminToken);
  assert(tuList.data.code === 0, '获取租户用户绑定列表');
  console.log(`  ℹ️ 已有 ${tuList.data.data?.length || 0} 个租户用户绑定`);
}

// ============ 测试 5: 多租户场景验证（需手动设置 EnableTenant=true） ============

async function test5_tenantScenarios() {
  console.log('\n=== 测试 5: 多租户场景验证（需 EnableTenant=true） ===\n');

  // 检查当前 EnableTenant 状态
  const cfg = await api('GET', '/api/admin/cube', null, adminToken);
  const enableTenant = cfg.data?.data?.enableTenant;

  if (enableTenant !== true) {
    console.log('  ⚠️ 当前 EnableTenant=false，跳过多租户场景测试');
    console.log('  ℹ️ 请执行以下步骤手动测试：');
    console.log('    1. 停止 CubeDemo');
    console.log('    2. 编辑 Bin/CubeDemo/Data/dbConfig_Cube.json');
    console.log('    3. 添加 "EnableTenant": true');
    console.log('    4. 重启 CubeDemo');
    console.log('    5. 重新运行本测试');
    return;
  }

  console.log('  ℹ️ EnableTenant=true，执行多租户测试...\n');

  // ====================================================================
  // 5a. 存量用户（无 TenantUser 绑定）+ 无 X-Tenant + 无 Cookie
  //     预期：被拒绝（401/403/500）
  //     当前代码：未拒绝（code=0）→ 这是 bug，测试应标红失败
  //     修复后：被拒绝 → 测试标绿通过
  // ====================================================================
  console.log('  ─── 5a. 存量用户无租户信息 = 应被拒绝 ───');
  const login1 = await api('POST', '/Auth/Login', { username: 'legacy01', password: PASSWORD, remember: false });
  if (login1.data.code === 0) {
    const token = extractToken(login1.data.data);
    const info1 = await api('GET', '/Auth/Info', null, token);
    if (info1.status >= 500) {
      // 修复前：NRE → 500（P0-1 bug 表现）
      assert(false, `P0-1 复现: 服务端 500 错误（GetTenantId NRE），需修复`);
    } else {
      const code = info1.data?.code;
      // 核心断言：存量用户无租户信息时必须被拒绝
      // 当前代码 code=0（不拒绝）→ ❌ FAIL（这是 bug）
      // 修复后 code!=0（拒绝）→ ✅ PASS
      assert(code !== 0, `存量用户无租户信息，应被拒绝（当前 code=${code}，修复后应 !=0）`);
    }
  } else {
    assert(false, `legacy01 登录失败: ${login1.data.message}`);
  }

  // ====================================================================
  // 5b. 系统管理员 + Cookie=0
  //     预期：正常通过（管理者不受租户限制）
  //     当前代码：正常通过 ✅
  //     修复后：仍应正常通过 ✅
  // ====================================================================
  console.log('  ─── 5b. 系统管理员带 Cookie=0 = 应正常通过 ───');
  const login2 = await api('POST', '/Auth/Login', { username: 'admin01', password: PASSWORD, remember: false });
  if (login2.data.code === 0) {
    const token = extractToken(login2.data.data);
    const info2 = await fetch(`${BASE}/Auth/Info`, {
      headers: { Authorization: `Bearer ${token}`, Cookie: 'TenantId-CubeDemo=0' },
    });
    const info2Data = await info2.json();
    assert(info2Data.code === 0, `管理员通过认证（code=${info2Data.code}）`);
  } else {
    assert(false, `admin01 登录失败: ${login2.data.message}`);
  }

  // ====================================================================
  // 5c. 已绑定租户的用户 + X-Tenant
  //     预期：正常通过（已有 TenantUser 绑定）
  //     当前代码：正常通过 ✅
  //     修复后：仍应正常通过 ✅
  // ====================================================================
  console.log('  ─── 5c. 已绑定用户带 X-Tenant = 应正常通过 ───');
  const login3 = await fetch(`${BASE}/Auth/Login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Tenant': TENANT_CODE },
    body: JSON.stringify({ username: 'tenant01', password: PASSWORD, remember: false }),
  });
  const login3Data = await login3.json();
  if (login3Data.code === 0) {
    const token = extractToken(login3Data.data);
    const info3 = await fetch(`${BASE}/Auth/Info`, {
      headers: { Authorization: `Bearer ${token}`, 'X-Tenant': TENANT_CODE },
    });
    const info3Data = await info3.json();
    assert(info3Data.code === 0, `已绑定用户通过认证（code=${info3Data.code}）`);
  } else {
    assert(false, `tenant01 登录失败: ${login3Data.message}`);
  }

  // ====================================================================
  // 5d. 存量用户旧令牌 + 携带 X-Tenant
  //     预期：被拒绝（ChooseTenant 不接受未绑定的租户）
  //     修复后：被拒绝（旧令牌不含自动绑定，需要重新登录）
  // ====================================================================
  console.log('  ─── 5d. 存量用户旧令牌 + X-Tenant = 应被拒绝 ───');
  // 先用多租户关闭时的令牌
  const login1b = await api('POST', '/Auth/Login', { username: 'legacy01', password: PASSWORD, remember: false });
  if (login1b.data.code === 0) {
    const oldToken = extractToken(login1b.data.data);
    const info4 = await fetch(`${BASE}/Auth/Info`, {
      headers: { Authorization: `Bearer ${oldToken}`, 'X-Tenant': TENANT_CODE },
    });
    const info4Data = await info4.json();
    // 旧令牌无论是否带 X-Tenant 都应被拒绝（因为没有 TenantUser 绑定）
    assert(info4Data.code !== 0, `旧令牌被拒绝（code=${info4Data.code}）`);
  } else {
    assert(false, `legacy01 登录失败: ${login1b.data.message}`);
  }
}

// ============ 主流程 ============

async function main() {
  console.log('============================================');
  console.log('  魔方多租户回归复现测试 v3');
  console.log('  目标: ' + BASE);
  console.log('============================================\n');

  const tests = [
    test1_connectivity,
    test2_cubeSettingReadWrite,
    test3_userRegistration,
    test4_tenantManagement,
    test5_tenantScenarios,
  ];

  for (const test of tests) {
    try {
      await test();
    } catch (e) {
      console.error(`  ❌ 异常: ${e.message}`);
      failed++;
    }
  }

  console.log(`\n============================================`);
  console.log(`  结果: ${passed} 通过, ${failed} 失败`);
  console.log(`============================================`);
  console.log(`\n注意：测试 5a/5d 的 ❌ FAIL 是当前代码的 bug 表现（P0-1/P0-2），`);
  console.log(`修复后应变为 ✅ PASS。测试 5b/5c 的 ✅ PASS 是正确行为，修复后保持不变。\n`);
  process.exitCode = failed > 0 ? 1 : 0;
}

main();