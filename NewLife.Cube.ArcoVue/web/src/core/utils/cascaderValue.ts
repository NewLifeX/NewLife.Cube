/**
 * 级联选择取值归一纯函数（OSC-2608139feb）。
 *
 * a-cascader 在 path-mode 开关不同时 onChange 取值形状不同：
 * - path-mode=true：选中路径数组 `[省,市,区]`，清空 `undefined`
 * - path-mode=false（防御）：标量叶子值，清空 `undefined`
 * 本函数把两类形状统一归一出「提交用叶子值」，保证后端实体字段（AreaId）
 * 永远拿到叶子 ID，同时清空场景一律发 `undefined` 而不是空数组。
 */
export function leafFromCascaderChange(val: unknown): number | string | undefined {
  if (val == null || val === '') return undefined;
  if (Array.isArray(val)) {
    if (val.length === 0) return undefined;
    return val[val.length - 1] as number | string;
  }
  // 防御：path-mode 误关时 a-cascader 直接给标量叶子，仍当叶子提交
  return val as number | string;
}
