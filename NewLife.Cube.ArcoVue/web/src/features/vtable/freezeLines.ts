import { frozenLeftCount, frozenRightCount, type ColumnPref } from '@/core/utils/viewProfile';

/** 仅用户在字段配置里钉的左右冻结；勾选列/操作列的默认冻结不画示意线 */
export function customFreezeSides(columns: ColumnPref[]): { left: boolean; right: boolean } {
  return {
    left: frozenLeftCount(columns) > 0,
    right: frozenRightCount(columns) > 0,
  };
}

/** 示意线高度不超过实际行高（含表头），避免落到表体空白或分页上 */
export function freezeLineHeight(contentHeight: number, hostHeight: number): number {
  if (!(contentHeight > 0)) return 0;
  if (!(hostHeight > 0)) return Math.round(contentHeight);
  return Math.round(Math.min(contentHeight, hostHeight));
}

/**
 * 冻结线 X：用冻结区几何，不用 getCellRelativeRect。
 * 右冻结列在未撑满/customLayout 时，getCellRelativeRect 会给出排版坐标而非贴右后的可视左边界。
 */
export function freezeLineXs(opts: {
  showLeft: boolean;
  showRight: boolean;
  /** 左冻结区右缘（getFrozenColsWidth） */
  frozenBlockRight: number;
  /** 右冻结区左缘（tableWidth - getRightFrozenColsWidth） */
  rightFrozenBlockLeft: number;
}): { left: number | null; right: number | null } {
  const left =
    opts.showLeft && opts.frozenBlockRight > 0 ? Math.round(opts.frozenBlockRight) : null;
  const right =
    opts.showRight && opts.rightFrozenBlockLeft > 0 ? Math.round(opts.rightFrozenBlockLeft) : null;
  return { left, right };
}
