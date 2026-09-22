import { createCircle, createLine } from '@visactor/vtable/es/vrender';

/** 审批徽标内的几何图标（不用 Unicode：字框与墨水区不对齐，customLayout 时也无法用 AABB 纠偏） */
export type WfMarkKind = 'ring' | 'ringDot' | 'check' | 'cross' | 'undo';

export function wfMarkKind(status: string): WfMarkKind {
  switch (status) {
    case 'running':
      return 'ringDot';
    case 'approved':
      return 'check';
    case 'rejected':
      return 'cross';
    case 'withdrawn':
      return 'undo';
    default:
      return 'ring';
  }
}

function strokeLine(
  parent: { add: (n: any) => unknown },
  points: { x: number; y: number }[],
  color: string,
  width = 1.6,
) {
  parent.add(
    createLine({
      points,
      stroke: color,
      lineWidth: width,
      lineCap: 'round',
      lineJoin: 'round',
      pickable: false,
    }),
  );
}

/** 在 size×size 的色块内按几何中心绘制图标 */
export function appendWfStatusMark(
  parent: { add: (n: any) => unknown },
  kind: WfMarkKind,
  size: number,
  color: string,
): void {
  const cx = size / 2;
  const cy = size / 2;
  const r = 4.6;

  if (kind === 'ring' || kind === 'ringDot') {
    parent.add(
      createCircle({
        x: cx,
        y: cy,
        radius: r,
        fill: false,
        stroke: color,
        lineWidth: 1.5,
        pickable: false,
      }),
    );
    if (kind === 'ringDot') {
      parent.add(
        createCircle({
          x: cx,
          y: cy,
          radius: 1.7,
          fill: color,
          stroke: false,
          pickable: false,
        }),
      );
    }
    return;
  }

  if (kind === 'check') {
    strokeLine(
      parent,
      [
        { x: cx - 3.5, y: cy + 0.2 },
        { x: cx - 1.1, y: cy + 2.8 },
        { x: cx + 3.5, y: cy - 2.8 },
      ],
      color,
    );
    return;
  }

  if (kind === 'cross') {
    const d = 2.9;
    strokeLine(parent, [
      { x: cx - d, y: cy - d },
      { x: cx + d, y: cy + d },
    ], color);
    strokeLine(parent, [
      { x: cx + d, y: cy - d },
      { x: cx - d, y: cy + d },
    ], color);
    return;
  }

  // undo：以中心为圆心的 3/4 圆弧 + 箭头，几何对称
  const pts: { x: number; y: number }[] = [];
  const a0 = -Math.PI * 0.15;
  const a1 = Math.PI * 1.45;
  const n = 14;
  for (let i = 0; i <= n; i++) {
    const a = a0 + ((a1 - a0) * i) / n;
    pts.push({ x: cx + r * Math.cos(a), y: cy + r * Math.sin(a) });
  }
  strokeLine(parent, pts, color, 1.5);
  const tip = pts[pts.length - 1];
  const ang = a1;
  const ah = 2.4;
  strokeLine(
    parent,
    [
      {
        x: tip.x - ah * Math.cos(ang - 0.85),
        y: tip.y - ah * Math.sin(ang - 0.85),
      },
      tip,
      {
        x: tip.x - ah * Math.cos(ang + 0.85),
        y: tip.y - ah * Math.sin(ang + 0.85),
      },
    ],
    color,
    1.5,
  );
}
