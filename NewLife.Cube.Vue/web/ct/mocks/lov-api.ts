// CT 用 lov-api 桩：返回固定样本数据，使组件在无后端时也能渲染表格行。
// - SAMPLE：普通列表（5 条，pageable:false 故事用）。
// - PAGED：分页列表（23 条，code 含 'Paged' 时分页故事用，pageSize 默认 20 → 2 页）。
const SAMPLE = [
  { id: 1, name: 'Administrator' },
  { id: 2, name: 'Normal user' },
  { id: 3, name: 'Auditor' },
  { id: 4, name: 'Guest' },
  { id: 5, name: 'Read-only user' },
];
const PAGED = Array.from({ length: 23 }, (_, i) => ({ id: i + 1, name: `User ${i + 1}` }));

function paginate(
  req: { lovCode?: string; pageNum?: number; pageSize?: number } | undefined,
  source: Array<{ id: number; name: string }>,
) {
  const pageNum = req?.pageNum ?? 1;
  const pageSize = req?.pageSize ?? 20;
  const start = (pageNum - 1) * pageSize;
  return { data: source.slice(start, start + pageSize), total: source.length };
}

export const fetchLovListData = async (
  req?: { lovCode?: string; pageNum?: number; pageSize?: number },
): Promise<{ data: Array<{ id: number; name: string }>; total: number }> => {
  const source = req?.lovCode?.includes('Paged') ? PAGED : SAMPLE;
  return paginate(req, source);
};
export const fetchLovListDataDirect = async (
  _cfg?: unknown,
  req?: { lovCode?: string; pageNum?: number; pageSize?: number },
): Promise<{ data: Array<{ id: number; name: string }>; total: number }> => {
  const source = req?.lovCode?.includes('Paged') ? PAGED : SAMPLE;
  return paginate(req, source);
};
export const fetchBatchLabel = async () => ({});
export const shouldDirectRequest = () => false;

// LovSelect 内部按 code 调 fetchLovMeta 解析类型（ENUM/LIST）。
// 这里按前缀返回确定性 meta，使 CT 能渲染真实分支，无需后端。
export const fetchLovMeta = async (lovCode: string): Promise<{
  meta: Array<Record<string, unknown>>;
  inlineEnums: Record<string, Array<{ value: string; label: string }>>;
}> => {
  if (lovCode.startsWith('List')) {
    const source = lovCode.includes('Paged') ? PAGED : SAMPLE;
    return {
      meta: [
        {
          lovCode,
          type: 'LIST',
          name: 'User',
          valueField: 'id',
          labelField: 'name',
          listConfig: { pageable: false },
          searchFields: [],
          tableColumns: [
            { field: 'id', title: 'No.' },
            { field: 'name', title: 'Name' },
          ],
        },
      ],
      // 把 LIST 样本数据作为 inlineEnums 回传，使 LovSelect 在关闭态也能显示已选 label（如「管理员」）。
      inlineEnums: {
        [lovCode]: source.map(({ id, name }) => ({ value: String(id), label: name })),
      },
    };
  }
  return {
    meta: [
      {
        lovCode,
        type: 'ENUM',
        name: 'Status',
        options: [
          { value: '0', label: 'Draft' },
          { value: '1', label: 'Enabled' },
          { value: '2', label: 'Disabled' },
        ],
      },
    ],
    inlineEnums: {},
  };
};
