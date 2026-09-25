/** 实体添加 / 编辑 / 详情抽屉宽度：字段超过 10 个用宽抽屉 */
export const RECORD_DRAWER_NARROW = 520;
export const RECORD_DRAWER_WIDE = 720;

export function recordDrawerWidth(fieldCount: number): number {
  return fieldCount > 10 ? RECORD_DRAWER_WIDE : RECORD_DRAWER_NARROW;
}
