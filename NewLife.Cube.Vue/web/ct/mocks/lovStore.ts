// CT 用 lovStore 桩：与 lov-api 桩配套，使组件在无后端时也能渲染表格行、
// 翻译列、回显已选 label。覆盖 LovSelect/lovStore.ts 的对外方法。
//
// 设计：LIST 样本数据（SAMPLE/PAGED）作为"已加载列表行"，其 id→name 映射即 label 来源，
// 与真实后端"列表自身行不入 inlineEnums、靠组件登记/兜底拉取"的行为一致——不作弊塞 inlineEnums。

const SAMPLE = [
  { id: 1, name: '管理员' },
  { id: 2, name: '普通用户' },
  { id: 3, name: '审计员' },
  { id: 4, name: '访客' },
  { id: 5, name: '只读用户' },
];
const PAGED = Array.from({ length: 23 }, (_, i) => ({ id: i + 1, name: `用户${i + 1}` }));

function sourceFor(lovCode?: string) {
  return lovCode?.includes('Paged') ? PAGED : SAMPLE;
}

// 模块级 label 缓存（模拟 store 内存态）
const labelCache = new Map<string, string>();

export function getMeta(lovCode: string): Promise<{ type: string; options?: unknown[]; valueField?: string; labelField?: string; inlineEnums?: Record<string, Array<{ value: string; label: string }>> }> {
  if (lovCode.startsWith('List')) {
    const source = sourceFor(lovCode);
    return Promise.resolve({
      type: 'LIST',
      valueField: 'id',
      labelField: 'name',
      // 模拟真实 fetchLovMeta 返回的 inlineEnums（LIST 样本数据作为关闭态回显兜底）
      inlineEnums: { [lovCode]: source.map(({ id, name }) => ({ value: String(id), label: name })) },
    });
  }
  return Promise.resolve({
    type: 'ENUM',
    options: [
      { value: '0', label: '草稿' },
      { value: '1', label: '启用' },
      { value: '2', label: '禁用' },
    ],
  });
}

export function getCachedMeta(_lovCode: string): null {
  return null;
}

export function registerRows(lovCode: string, rows: Array<Record<string, unknown>>) {
  const source = sourceFor(lovCode);
  const map = new Map(source.map((r) => [String(r.id), r.name]));
  for (const row of rows) {
    const v = row.id != null ? String(row.id) : undefined;
    const l = row.name != null ? String(row.name) : undefined;
    if (v != null && l != null) labelCache.set(`${lovCode}:${v}`, l);
  }
  void map;
}

export function registerSelectedRow(lovCode: string, row: Record<string, unknown>) {
  const v = row.id != null ? String(row.id) : undefined;
  const l = row.name != null ? String(row.name) : undefined;
  if (v != null && l != null) labelCache.set(`${lovCode}:${v}`, l);
}

export async function resolveColumnLabels(lovCode: string, raw: unknown): Promise<string> {
  const values = Array.isArray(raw)
    ? raw.map(String)
    : String(raw ?? '')
        .split(/[,;]/)
        .map((s) => s.trim())
        .filter(Boolean);
  const source = sourceFor(lovCode);
  const map = new Map(source.map((r) => [String(r.id), r.name]));
  return values.map((v) => labelCache.get(`${lovCode}:${v}`) ?? map.get(v) ?? v).join('、');
}

export function getColumnLabel(lovCode: string, value: string): string {
  if (labelCache.has(`${lovCode}:${value}`)) return labelCache.get(`${lovCode}:${value}`)!;
  const source = sourceFor(lovCode);
  const map = new Map(source.map((r) => [String(r.id), r.name]));
  return map.get(value) ?? value;
}

export async function resolveSelectedLabel(lovCode: string, value: string | number | undefined): Promise<string> {
  if (value == null) return '';
  const key = `${lovCode}:${value}`;
  if (labelCache.has(key)) return labelCache.get(key)!;
  const source = sourceFor(lovCode);
  const map = new Map(source.map((r) => [String(r.id), r.name]));
  const fallback = map.get(String(value)) ?? String(value);
  labelCache.set(key, fallback);
  return fallback;
}

export function getSelectedLabel(lovCode: string, value: string | number | undefined): string {
  // 与真实 lovStore.getSelectedLabel 保持一致：只查 labelCache（已登记/已加载行），
  // 缺失即回退原始值（数字 id），不做"按 id 回退 SAMPLE 源"的作弊兜底。
  // 这样可以忠实暴露"多选确认后未登记行 → 回显数字而非文本"的缺陷（CT 测试据此变红）。
  if (value == null) return '';
  const key = `${lovCode}:${value}`;
  return labelCache.get(key) ?? String(value);
}

export function invalidateLov(): void {
  labelCache.clear();
}
