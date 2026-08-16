#!/usr/bin/env node

/**
 * 魔方多租户访问控制矩阵测试
 * ============================
 *
 * 维度：
 *   1. 用户类型：管理员（系统管理员）/ 普通用户（无租户绑定）/ 普通用户（已绑定租户）
 *   2. 租户标识：无 / X-Tenant=t1 / Cookie=0（管理后台）
 *   3. 影子开关：Shadow（兼容观察期）/ Enforce（严格 fail-closed）
 *
 * 判定入口：GET /Auth/Info 返回 code==0 视为"通过"，code!=0 或 HTTP 4xx 视为"拒绝"。
 * 预期值依据代码路径推演（ManagerProviderHelper.ValidateTenant / ChooseTenant / DataScopeMiddleware）。
 *
 * 用法：NODE_TLS_REJECT_UNAUTHORIZED=0 node Doc/tenant-matrix-test.mjs
 */

const BASE = 'https://localhost:7116';
const PASSWORD = 'Test@12345';
const TENANT_CODE = 't1';

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// ============ 工具函数 ============

async function api(method, path, body, token, extraHeaders) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (extraHeaders) Object.assign(headers, extraHeaders);
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

function extractToken(data) {
  return data?.access_token || data?.accessToken;
}

/** 登录并返回令牌 */
async function login(username, password, extraHeaders) {
  const res = await api('POST', '/Auth/Login', { username, password, remember: false }, null, extraHeaders);
  if (res.data?.code !== 0) return null;
  return extractToken(res.data?.data);
}

/** 切换影子开关：mode=0 Shadow / 1 Enforce */
async function setTenantEnforceMode(mode) {
  const token = await login('admin', 'admin');
  if (!token) throw new Error('admin 登录失败，无法切换模式');

  const cfg = await api('GET', '/api/admin/cube', null, token);
  const data = cfg.data?.data;
  if (!data) throw new Error('读取 Cube 配置失败');
  data.tenantEnforceMode = mode;
  const put = await api('PUT', '/api/admin/cube', data, token);
  if (put.data?.code !== 0) throw new Error(`切换 tenantEnforceMode=${mode} 失败: ${put.data?.message}`);

  const after = await api('GET', '/api/admin/cube', null, token);
  return after.data?.data?.tenantEnforceMode;
}

/** 请求 /Auth/Info，返回 code */
async function callInfo(token, tenantHeader) {
  const res = await fetch(`${BASE}/Auth/Info`, {
    headers: { Authorization: `Bearer ${token}`, ...tenantHeader },
  });
  const txt = await res.text();
  try {
    const d = JSON.parse(txt);
    return { status: res.status, code: d.code, message: d.message };
  } catch {
    return { status: res.status, code: -1, message: txt.slice(0, 80) };
  }
}

// ============ 矩阵定义 ============

/** 用户：{ key, name, username, password, desc } */
const USERS = [
  { key: 'admin', name: '管理员', username: 'admin', password: 'admin', desc: '系统管理员(IsSystem)' },
  { key: 'legacy', name: '普通用户', username: 'legacy01', password: PASSWORD, desc: '无 TenantUser 绑定' },
  { key: 'bound', name: '普通用户', username: 'tenant01', password: PASSWORD, desc: '已绑定 t1 租户' },
];

/** 租户标识：{ key, name, header } */
const TENANTS = [
  { key: 'none', name: '无租户标识', header: {} },
  { key: 'xtenant', name: `X-Tenant=${TENANT_CODE}`, header: { 'X-Tenant': TENANT_CODE } },
  { key: 'cookie0', name: 'Cookie=0', header: { Cookie: `TenantId-CubeDemo=0` } },
];

/** 预期值（依据代码推演）：
 *  true=应通过(code==0)  false=应拒绝(code!=0)
 *  键：[用户key][租户key][模式key]
 */
const EXPECT = {
  admin: { // 系统管理员：任何租户标识、任何模式均通过
    none: { Shadow: true, Enforce: true },
    xtenant: { Shadow: true, Enforce: true },
    cookie0: { Shadow: true, Enforce: true },
  },
  legacy: { // 无绑定普通用户：仅 Shadow+无租户标识 兼容放行，其余拒绝
    none: { Shadow: true, Enforce: false },
    xtenant: { Shadow: false, Enforce: false },
    cookie0: { Shadow: false, Enforce: false },
  },
  bound: { // 已绑定租户普通用户：无租户时进入第一个绑定租户；X-Tenant 匹配绑定；Cookie=0 时 ChooseTenant 落第一个租户
    none: { Shadow: true, Enforce: true },
    xtenant: { Shadow: true, Enforce: true },
    cookie0: { Shadow: true, Enforce: true },
  },
};

// ============ 执行 ============

async function runMatrix(modeName, modeValue) {
  console.log(`\n========== 影子开关模式: ${modeName} (${modeValue}) ==========\n`);
  console.log(`| 用户 | 租户标识 | 预期 | 实际 | 判定 | 说明 |`);
  console.log(`|---|---|---|---|---|---|`);

  let pass = 0, fail = 0, diff = [];
  for (const u of USERS) {
    for (const t of TENANTS) {
      // 登录不带租户标识（模拟普通登录），Cookie=0 场景登录时也不带
      const token = await login(u.username, u.password);
      if (!token) {
        console.log(`| ${u.name}(${u.username}) | ${t.name} | - | 登录失败 | ❌ | 无法获得令牌 |`);
        fail++;
        continue;
      }
      const r = await callInfo(token, t.header);
      const expected = EXPECT[u.key][t.key][modeName];
      const actual = r.code === 0;
      const ok = expected === actual;
      if (ok) pass++; else { fail++; diff.push({ u: u.username, t: t.name, expected, actual, code: r.code }); }

      const expStr = expected ? '✅通过' : '❌拒绝';
      const actStr = actual ? `通过(code=${r.code})` : `拒绝(code=${r.code})`;
      console.log(`| ${u.name}(${u.username}) | ${t.name} | ${expStr} | ${actStr} | ${ok ? '✅' : '❌'} | ${u.desc} |`);
    }
  }
  console.log(`\n${modeName} 结果: ${pass} 通过, ${fail} 失败`);
  if (diff.length > 0) {
    console.log(`\n差异明细:`);
    for (const d of diff) {
      console.log(`  - 用户[${d.u}] 租户[${d.t}] 预期[${d.expected ? '通过' : '拒绝'}] 实际[${d.actual ? '通过' : '拒绝'}](code=${d.code})`);
    }
  }
  return { pass, fail, diff };
}

async function main() {
  console.log('============================================');
  console.log('  魔方多租户访问控制矩阵测试');
  console.log(`  目标: ${BASE}`);
  console.log('============================================');

  // 先跑 Enforce（当前状态），再切 Shadow 跑一遍
  const enforceMode = await setTenantEnforceMode(1);
  console.log(`ℹ️ 已切换 tenantEnforceMode=${enforceMode} (Enforce)`);
  const r1 = await runMatrix('Enforce', enforceMode);

  const shadowMode = await setTenantEnforceMode(0);
  console.log(`ℹ️ 已切换 tenantEnforceMode=${shadowMode} (Shadow)`);
  const r0 = await runMatrix('Shadow', shadowMode);

  // 恢复 Enforce
  await setTenantEnforceMode(1);
  console.log('\nℹ️ 测试结束，已恢复 tenantEnforceMode=1 (Enforce)');

  console.log('\n========== 汇总 ==========');
  console.log(`| 模式 | 通过 | 失败 |`);
  console.log(`|---|---|---|`);
  console.log(`| Enforce | ${r1.pass} | ${r1.fail} |`);
  console.log(`| Shadow | ${r0.pass} | ${r0.fail} |`);
  console.log(`| 合计 | ${r1.pass + r0.pass} | ${r1.fail + r0.fail} |`);

  process.exitCode = (r1.fail + r0.fail) > 0 ? 1 : 0;
}

main();
