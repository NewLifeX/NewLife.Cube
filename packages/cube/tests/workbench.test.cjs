// 工作台视图模型与监控曲线单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  isSuccessCode,
  formatCell,
  extractLink,
  isExternalLink,
  normalizeRoles,
  asRecord,
  normalizeKpiColor,
  normalizeCols,
  toMonitorPoint,
  appendMonitorPoint,
  buildMonitorChart,
  buildWorkbenchView,
  createWorkbenchLoader,
} = require('../dist/index.cjs');

test('isSuccessCode: 兼容 0 与 200', () => {
  assert.equal(isSuccessCode(0), true);
  assert.equal(isSuccessCode(200), true);
  assert.equal(isSuccessCode(1), false);
  assert.equal(isSuccessCode('0'), false);
  assert.equal(isSuccessCode(undefined), false);
});

test('formatCell: 空值、HTML 剥离、对象与布尔', () => {
  assert.equal(formatCell(null), '-');
  assert.equal(formatCell(''), '-');
  assert.equal(formatCell('  '), '-');
  assert.equal(formatCell('<b>在线</b>&nbsp;用户'), '在线 用户');
  assert.equal(formatCell(0), '0');
  assert.equal(formatCell(false), 'false');
  assert.equal(formatCell({ a: 1 }), '{"a":1}');
});

test('extractLink: 解析锚点，非链接返回 null', () => {
  assert.deepEqual(extractLink('<a href="/Admin/Index/Main">查看完整服务器信息 »</a>'), {
    href: '/Admin/Index/Main',
    text: '查看完整服务器信息 »',
  });
  assert.equal(extractLink('普通文本'), null);
  assert.equal(extractLink(123), null);
  assert.equal(extractLink('<a href="">空</a>'), null);
});

test('isExternalLink: 站外地址识别', () => {
  assert.equal(isExternalLink('https://newlifex.com'), true);
  assert.equal(isExternalLink('//cdn.example.com/a.js'), true);
  assert.equal(isExternalLink('/Admin/User'), false);
});

test('normalizeRoles: 数组与逗号分隔字符串', () => {
  assert.deepEqual(normalizeRoles(['管理员', '']), ['管理员']);
  assert.deepEqual(normalizeRoles('管理员, 运维 ,'), ['管理员', '运维']);
  assert.deepEqual(normalizeRoles(undefined), []);
  assert.deepEqual(normalizeRoles(''), []);
});

test('asRecord: 仅对象通过', () => {
  assert.deepEqual(asRecord({ a: 1 }), { a: 1 });
  assert.deepEqual(asRecord([1, 2]), {});
  assert.deepEqual(asRecord('x'), {});
  assert.deepEqual(asRecord(null), {});
});

test('normalizeKpiColor / normalizeCols: 收敛非法值', () => {
  assert.equal(normalizeKpiColor(' Green '), 'green');
  assert.equal(normalizeKpiColor('pink'), 'blue');
  assert.equal(normalizeKpiColor(undefined), 'blue');
  assert.equal(normalizeCols(4), 4);
  assert.equal(normalizeCols(0), 1);
  assert.equal(normalizeCols(99), 12);
  assert.equal(normalizeCols(undefined), 6);
  assert.equal(normalizeCols(3.6), 4);
});

test('toMonitorPoint: 合法快照、缺失与越界收敛', () => {
  const point = toMonitorPoint({
    xs: ['10:00:00'],
    series: [
      [23.4],
      [61.2],
    ],
    memUsed: 1024,
    memTotal: 4096,
  });
  assert.deepEqual(point, {
    label: '10:00:00',
    cpu: 23.4,
    memory: 61.2,
    memUsed: 1024,
    memTotal: 4096,
  });

  assert.equal(toMonitorPoint(null), null);
  assert.equal(toMonitorPoint({}), null);
  assert.equal(toMonitorPoint({ series: [[1]] }), null);

  const clamped = toMonitorPoint({ series: [[-5], [130]] });
  assert.equal(clamped.cpu, 0);
  assert.equal(clamped.memory, 100);
  assert.ok(clamped.label);
});

test('appendMonitorPoint: 跳过空值、保留上限、不修改入参', () => {
  const origin = [{ label: 'a', cpu: 1, memory: 2 }];
  assert.equal(appendMonitorPoint(origin, null), origin);

  const next = appendMonitorPoint(origin, { label: 'b', cpu: 3, memory: 4 }, 2);
  assert.deepEqual(
    next.map((p) => p.label),
    ['a', 'b'],
  );

  const capped = appendMonitorPoint(origin, { label: 'c', cpu: 3, memory: 4 }, 1);
  assert.deepEqual(
    capped.map((p) => p.label),
    ['c'],
  );
  assert.deepEqual(
    origin.map((p) => p.label),
    ['a'],
  );
});

test('buildMonitorChart: 空数据、单点与多点映射', () => {
  const empty = buildMonitorChart([], 100, 50);
  assert.equal(empty.cpuPoints, '');
  assert.equal(empty.memPoints, '');
  assert.equal(empty.width, 100);

  const single = buildMonitorChart([{ label: 'a', cpu: 50, memory: 100 }], 100, 50);
  assert.equal(single.cpuPoints, '0,25');
  assert.equal(single.memPoints, '0,0');

  const multi = buildMonitorChart(
    [
      { label: 'a', cpu: 0, memory: 100 },
      { label: 'b', cpu: 100, memory: 0 },
    ],
    100,
    100,
  );
  assert.equal(multi.cpuPoints, '0,100 100,0');
  assert.equal(multi.memPoints, '0,0 100,100');
});

test('buildWorkbenchView: KPI、快捷入口与系统信息', () => {
  const view = buildWorkbenchView({
    user: { name: 'admin', displayName: '管理员', roles: ['系统管理员', '运维'] },
    kpis: [
      { name: 'UserCount', label: '用户总数', value: 12, color: 'pink' },
      { name: 'Online', label: '在线', value: '-', trend: '+2', url: '/Admin/UserOnline' },
    ],
    widgets: [],
    quickLinks: [
      { name: '用户管理', url: '/Admin/User' },
      { name: '无地址', url: '' },
    ],
    sysInfo: {
      Runtime: '.NET 10',
      More: '<a href="/Admin/Index/Main">更多</a>',
    },
  });

  assert.equal(view.displayName, '管理员');
  assert.deepEqual(view.roles, ['系统管理员', '运维']);
  assert.equal(view.roleText, '系统管理员 / 运维');
  assert.equal(view.kpis.length, 2);
  assert.equal(view.kpis[0].value, '12');
  assert.equal(view.kpis[0].color, 'blue');
  assert.equal(view.kpis[1].trend, '+2');
  assert.equal(view.kpis[1].url, '/Admin/UserOnline');
  assert.deepEqual(
    view.quickLinks.map((l) => l.name),
    ['用户管理'],
  );
  assert.deepEqual(
    view.sysInfoRows.map((r) => r.key),
    ['Runtime', 'More'],
  );
  assert.deepEqual(view.sysInfoRows[1].link, { href: '/Admin/Index/Main', text: '更多', external: false });
  assert.equal(view.hasContent, true);
  assert.equal(view.hasChartWidget, false);
});

test('buildWorkbenchView: Profile 卡回退聚合响应', () => {
  const view = buildWorkbenchView({
    user: { displayName: '张三', roles: '管理员,运维' },
    kpis: [],
    widgets: [{ name: 'Profile', title: '个人信息', widgetType: 'Content', data: null }],
    profile: { name: 'zhangsan', displayName: '张三', online: true, logins: 8, lastLogin: '2026-10-26' },
  });

  const widget = view.widgets[0];
  assert.equal(widget.kind, 'profile');
  assert.equal(widget.profile.displayName, '张三');
  assert.equal(widget.profile.initial, '张');
  assert.equal(widget.profile.online, true);
  assert.deepEqual(widget.profile.roles, ['管理员', '运维']);
  assert.equal(widget.profile.rows.length, 4);
  assert.equal(widget.profile.rows[0].text, '8');
});

test('buildWorkbenchView: 内容卡按名称与类型分类', () => {
  const view = buildWorkbenchView({
    kpis: [],
    widgets: [
      { name: 'QuickLink', title: '快捷入口', widgetType: 'Content', data: { links: [{ name: '用户', url: '/Admin/User' }] } },
      {
        name: 'LoginLog',
        title: '登录日志',
        widgetType: 'Content',
        data: {
          logins: [{ userName: 'admin', action: '登录', createTime: '2026-10-26' }],
          onlines: [{ name: 'admin', oAuthProvider: '本地', createTime: '2026-10-26' }],
        },
      },
      { name: 'SysInfo', title: '系统信息', widgetType: 'Kv', data: { Runtime: '.NET 10' } },
      { name: 'Notice', title: '公告', widgetType: 'Html', data: '<b>欢迎</b>' },
      { name: 'Empty', title: '空卡', widgetType: 'Content', data: null },
      { name: 'Monitor', title: '监控', widgetType: 'Chart', data: null, cols: 0 },
    ],
  });

  assert.deepEqual(
    view.widgets.map((w) => w.kind),
    ['quickLinks', 'loginLog', 'kv', 'text', 'empty', 'monitor'],
  );
  assert.equal(view.widgets[0].links[0].name, '用户');
  assert.deepEqual(view.widgets[1].logins[0], { title: 'admin', subtitle: '登录', time: '2026-10-26' });
  assert.deepEqual(view.widgets[1].onlines[0], { title: 'admin', subtitle: '本地', time: '2026-10-26' });
  assert.equal(view.widgets[2].rows[0].key, 'Runtime');
  assert.equal(view.widgets[3].text, '欢迎');
  assert.equal(view.widgets[5].cols, 1);
  assert.equal(view.hasChartWidget, true);
});

test('buildWorkbenchView: 空数据返回空视图', () => {
  const view = buildWorkbenchView(null);
  assert.equal(view.displayName, '-');
  assert.deepEqual(view.kpis, []);
  assert.deepEqual(view.widgets, []);
  assert.deepEqual(view.sysInfoRows, []);
  assert.equal(view.hasContent, false);
  assert.equal(view.hasChartWidget, false);
});

test('createWorkbenchLoader: 成功、业务失败与监控快照', async () => {
  const responses = {
    Dashboard: { code: 0, data: { kpis: [{ name: 'UserCount', label: '用户', value: 1 }], widgets: [] } },
    MonitorData: { code: 0, data: { xs: ['t'], series: [[10], [20]] } },
    'Fail/Action': { code: 7, message: '无权访问' },
  };
  const calls = [];
  const api = {
    page: {
      getAction: async (type, action) => {
        calls.push(`${type}/${action}`);
        if (type === 'Fail') return responses['Fail/Action'];
        return responses[action];
      },
    },
  };

  const loader = createWorkbenchLoader(api);
  const ok = await loader.load();
  assert.equal(ok.data.kpis[0].name, 'UserCount');
  assert.equal(ok.error, undefined);

  const point = await loader.pollMonitor();
  assert.equal(point.cpu, 10);
  assert.equal(point.memory, 20);

  assert.deepEqual(calls, ['Admin/Index/Dashboard', 'Admin/Index/MonitorData']);

  const failed = createWorkbenchLoader({ page: { getAction: async () => ({ code: 7, message: '无权访问' }) } });
  const result = await failed.load();
  assert.equal(result.error, '无权访问');
  assert.equal(result.data, undefined);

  const noPoint = await failed.pollMonitor();
  assert.equal(noPoint, null);
});
