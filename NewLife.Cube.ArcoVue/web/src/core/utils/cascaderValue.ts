/**
 * 级联选择取值归一纯函数（OSC-2608139feb）。
 *
 * a-cascader 在 path-mode 开关不同时 onChange 取值形状不同：
 * - path-mode=true：选中路径数组 `[省,市,区]`，清空 `undefined`
 * - path-mode=false（防御）：标量叶子值，清空 `undefined`
 * 本函数把两类形状统一归一出「提交用叶子值」，保证后端实体字段（AreaId）
 * 永远拿到叶子 ID，同时清空场景一律发 `undefined` 而不是空数组。
 */
/** 未选地区：null / '' / 0（实体 AreaId 默认值，不是合法区划编码） */
export function isEmptyAreaId(val: unknown): boolean {
  return val == null || val === '' || val === 0 || val === '0';
}

export function leafFromCascaderChange(val: unknown): number | string | undefined {
  if (isEmptyAreaId(val)) return undefined;
  if (Array.isArray(val)) {
    if (val.length === 0) return undefined;
    return val[val.length - 1] as number | string;
  }
  // 防御：path-mode 误关时 a-cascader 直接给标量叶子，仍当叶子提交
  return val as number | string;
}

/**
 * 级联输入框展示：Arco 只对 isLeaf 节点走 label，非叶子走 fallback 会拼出编码。
 * 用路径 ID → 名称统一成「省 / 市 / 区」字符形式。
 */
export function formatAreaPathLabel(
  value: unknown,
  nameOf: (id: string | number) => string | undefined,
): string {
  const ids = Array.isArray(value) ? value : isEmptyAreaId(value) ? [] : [value];
  return ids
    .filter((id) => !isEmptyAreaId(id))
    .map((id) => {
      const name = nameOf(id as string | number);
      return name && name.trim() ? name : String(id);
    })
    .join(' / ');
}

/**
 * 地区叶子判定：Area.Level≥4（乡镇街道）或 9 位及以上编码。
 * 非叶子单击只展开；叶子单击或任意节点双击完成选择。
 */
export function isAreaLeaf(id: unknown, level?: unknown): boolean {
  const lv = Number(level);
  if (Number.isFinite(lv) && lv >= 4) return true;
  const n = String(id ?? '').replace(/\D/g, '');
  return n.length >= 9;
}

/**
 * a-cascader option 槽数据 → path-mode 路径（供双击选定）。
 * Arco 传入 CascaderOptionInfo（pathValue / path / value）。
 */
export function pathFromCascaderOption(data: unknown): (number | string)[] {
  if (!data || typeof data !== 'object') return [];
  const rec = data as {
    pathValue?: unknown;
    path?: { value?: unknown }[];
    value?: unknown;
    raw?: { value?: unknown };
  };
  if (Array.isArray(rec.pathValue) && rec.pathValue.length) {
    return rec.pathValue as (number | string)[];
  }
  if (Array.isArray(rec.path) && rec.path.length) {
    return rec.path.map((p) => (p?.value ?? p) as number | string);
  }
  const v = rec.value ?? rec.raw?.value;
  return v == null || v === '' ? [] : [v as number | string];
}

