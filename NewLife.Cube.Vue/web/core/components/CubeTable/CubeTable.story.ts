/**
 * CubeTable 的故事变体（CT gallery 用）
 *
 * story 只描述「传什么 props + 什么运行时状态」，不手写 demo 页。
 * 上下文用 `createCubeEngine` + 桩 `deps` 在模块内构造，因此这些截图用例
 * **不依赖任何后端**：`getPage` 返回固定元数据，`http` 读写内存数组。
 *
 * ⚠️ `context` 必须 `markRaw`：gallery 会把 props 包进 `reactive()`，
 * 深度代理会把 ctx 里的 `Ref` 自动解包（`ctx.list` 变成数组本体），
 * 组件里 `ctx.list.value` 就读成 undefined —— 表现是「一截图就白屏」。
 */
import { markRaw } from 'vue';
import CubeTable from './CubeTable.vue';
import { createCubeEngine } from '../../engine/createCubeEngine';
import type { CubeEngineContext, CubeEngineDeps, CubePageMeta } from '../../engine/types';

/** 覆盖全部单元格分支的元数据：值集 / 布尔 / 数值 / 日期 / 日期时间 / 图片 / 附件 / 色块 / 图标 / Json */
const META: CubePageMeta = {
  setting: { enableAdd: true, enableSelect: true, enableKey: true, doubleDelete: true },
  list: [
    { name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true },
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
    { name: 'Status', displayName: '状态', typeName: 'String', lovCode: 'Demo.Status' },
    { name: 'Count', displayName: '数量', typeName: 'Int32' },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
    { name: 'Created', displayName: '创建时间', typeName: 'DateTime' },
    { name: 'Updated', displayName: '更新日期', typeName: 'DateTime', itemType: 'date' },
    { name: 'Avatar', displayName: '图片', typeName: 'String', itemType: 'image' },
    { name: 'Doc', displayName: '附件', typeName: 'String', itemType: 'file' },
    { name: 'Payload', displayName: '配置', typeName: 'String', itemType: 'json' },
    { name: 'Remark', displayName: '备注', typeName: 'String', nullable: true },
  ],
  search: [
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
    { name: 'Status', displayName: '状态', typeName: 'String', lovCode: 'Demo.Status' },
    { name: 'Level', displayName: '级别', typeName: 'String', dataSource: { '1': '甲', '2': '乙' } },
    { name: 'Count', displayName: '数量', typeName: 'Int32' },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
    { name: 'Created', displayName: '创建时间', typeName: 'DateTime' },
  ],
  addForm: [
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50, nullable: false },
    { name: 'Status', displayName: '状态', typeName: 'String', lovCode: 'Demo.Status' },
    { name: 'Count', displayName: '数量', typeName: 'Int32' },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
    { name: 'Remark', displayName: '备注', typeName: 'String', length: 500 },
  ],
  editForm: [
    { name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true, readOnly: true },
    { name: 'Name', displayName: '名称', typeName: 'String', length: 50, nullable: false },
    { name: 'Status', displayName: '状态', typeName: 'String', lovCode: 'Demo.Status' },
    { name: 'Count', displayName: '数量', typeName: 'Int32' },
    { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
    { name: 'Remark', displayName: '备注', typeName: 'String', length: 500 },
  ],
  detail: [],
};

/**
 * 行内图片用 data URL：`/cube/image/*.png` 在 CT 环境里必然 404，
 * 破图占位要不要出现取决于网络时序，像素基线就会随机漂。
 */
const AVATAR_A =
  'data:image/svg+xml;utf8,<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\"><rect width=\"24\" height=\"24\" fill=\"%23409eff\"/></svg>';
const AVATAR_B =
  'data:image/svg+xml;utf8,<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\"><rect width=\"24\" height=\"24\" fill=\"%2367c23a\"/></svg>';

/** 确定性行数据：日期、值集码、空值都固定，像素基线才不会漂 */
function makeRows(): Record<string, unknown>[] {
  return [
    {
      Id: 1,
      Name: '注塑机 A',
      Status: '1',
      Count: 12,
      Enable: true,
      Created: '2026-10-19T08:30:00',
      Updated: '2026-10-18T00:00:00',
      Avatar: AVATAR_A,
      Doc: '/cube/file/a.pdf',
      Payload: '{"speed":120}',
      Remark: null,
    },
    {
      Id: 2,
      Name: '注塑机 B',
      Status: '2',
      Count: 0,
      Enable: false,
      Created: '2026-10-17T15:05:00',
      Updated: '2026-10-16T00:00:00',
      Avatar: '',
      Doc: '',
      Payload: '{"speed":0}',
      Remark: '停机待检',
    },
    {
      Id: 3,
      Name: '注塑机 C',
      Status: '0',
      Count: 7,
      Enable: true,
      Created: '2026-10-15T09:00:00',
      Updated: '2026-10-14T00:00:00',
      Avatar: AVATAR_B,
      Doc: '/cube/file/c.pdf',
      Payload: '{"speed":88,"feed":3}',
      Remark: '',
    },
  ];
}

interface EngineOptions {
  /** 元数据接口失败（验证失败态与重试入口） */
  failMeta?: boolean;
  /** 行数据条数（0 = 空态） */
  rows?: number;
  /** 整页只读 */
  readOnly?: boolean;
  /** 打开新增弹窗 */
  openDialog?: boolean;
  /** 搜索条件折叠阈值演示用：截断 search 数组 */
  searchLimit?: number;
}

/**
 * 内存版 `deps`：列表读内存、写回内存，导出给一个空 blob。
 * 只实现 CT 需要走到的分支，不代表后端契约（契约由引擎单测把着）。
 */
function buildEngine(options: EngineOptions = {}): CubeEngineContext {
  let rows = makeRows().slice(0, options.rows ?? 3);
  const setting = { ...META.setting, isReadOnly: options.readOnly ? true : undefined };
  const allSearch = META.search ?? [];
  const search = options.searchLimit === undefined ? allSearch : allSearch.slice(0, options.searchLimit);

  const getPage = async () => {
    if (options.failMeta) throw new Error('元数据接口不可用（CT 桩）');
    return { ...META, setting, search };
  };

  const respondGet = (url: string, params?: Record<string, unknown>) => {
    if (url.endsWith('/Detail')) return { code: 0, data: rows[0] ?? {} };
    const keyword = typeof params?.q === 'string' ? params.q : '';
    const filtered = keyword ? rows.filter((r) => String(r.Name).includes(keyword)) : rows;
    return { code: 0, data: filtered, page: { totalCount: filtered.length } };
  };

  const http = Object.assign(
    async (config: { url: string; method?: string; params?: Record<string, unknown>; data?: unknown }) => {
      const method = config.method ?? 'get';
      if (method === 'get') return respondGet(config.url, config.params);
      if (method === 'post') rows = [{ Id: rows.length + 1, ...((config.data ?? {}) as object) }, ...rows];
      if (method === 'put') {
        const data = (config.data ?? {}) as { Id?: number };
        rows = rows.map((r) => (r.Id === data.Id ? { ...r, ...data } : r));
      }
      if (method === 'delete') {
        const id = config.params?.id;
        rows = rows.filter((r) => r.Id !== id);
      }
      return { code: 0, data: null, message: '保存成功！' };
    },
    {
      get: async (url: string, config?: { params?: Record<string, unknown> }) => respondGet(url, config?.params),
      post: async () => ({ code: 0, data: null }),
      put: async () => ({ code: 0, data: null }),
      delete: async () => ({ code: 0, data: null }),
    },
  );

  const deps: CubeEngineDeps = {
    getPage,
    http: http as unknown as CubeEngineDeps['http'],
    exportFile: async () => new Blob(['demo'], { type: 'text/csv' }),
    getDetail: async (_type, id) => rows.find((r) => r.Id === id) ?? rows[0] ?? {},
    deleteSelect: async (_type, keys) => {
      rows = rows.filter((r) => !keys.includes(r.Id as number));
    },
    // CT 不弹 element-plus 通知：截图里多一个 toast 会让基线随机漂
    notify: {},
  };

  const ctx = createCubeEngine({ apiPrefix: '/api/Demo/Device', pageSize: 20 }, deps);
  void ctx.init().then(() => {
    if (options.openDialog) ctx.openCreate();
  });
  return ctx;
}

/** 每个变体一套独立引擎实例（互不干扰，也不共享翻页状态） */
const engine = (options: EngineOptions = {}) => markRaw(buildEngine(options));

export const stories = [
  // 测试状态：元数据已加载、3 行数据、搜索区 6 条件单行收起、工具栏含新增/批量删除/导入/导出/设置列/刷新
  { id: 'CubeTable/Default', component: CubeTable, props: { context: engine() } },

  // 测试状态：整页只读（后端 isReadOnly）—— 新增 / 批量删除 / 导入 / 操作列全部不出现，只留导出与刷新
  { id: 'CubeTable/ReadOnly', component: CubeTable, props: { context: engine({ readOnly: true }) } },

  // 测试状态：0 行 —— 空态文案「暂无数据」，工具栏仍在（能筛选条件还没填）
  { id: 'CubeTable/Empty', component: CubeTable, props: { context: engine({ rows: 0 }) } },

  // 测试状态：元数据接口失败 —— 顶部错误条 + 就地重试入口，表格不留下半截列头
  { id: 'CubeTable/LoadFail', component: CubeTable, props: { context: engine({ failMeta: true }) } },

  // 测试状态：搜索条件超过一行 —— 收起态只给前 4 个，右侧出现「更多(2)」
  { id: 'CubeTable/ManyFilters', component: CubeTable, props: { context: engine({ searchLimit: 6 }) } },

  // 测试状态：新增弹窗已打开 —— 字段控件与必填星号、底部「取消/保存」
  { id: 'CubeTable/FormDialog', component: CubeTable, props: { context: engine({ openDialog: true }) } },
];
