/** 悬浮球直径（小于设计原稿 48，避免挡内容） */
export const AI_FAB_SIZE = 36;
/** 贴边最小间距 */
export const AI_FAB_MARGIN = 16;
/** 超过该位移才算拖动，避免误开面板 */
export const AI_FAB_DRAG_THRESHOLD = 4;
/** 钉钉式卡片浮窗默认尺寸（非全高侧栏） */
export const AI_PANEL_WIDTH = 360;
export const AI_PANEL_HEIGHT = 480;
export const AI_PANEL_MIN_W = 280;
export const AI_PANEL_MIN_H = 360;

export const AI_RESIZE_DIRS = ['n', 's', 'e', 'w', 'ne', 'nw', 'se', 'sw'] as const;
export type AiResizeDir = (typeof AI_RESIZE_DIRS)[number];

export interface AiFabPos {
  x: number;
  y: number;
}

export interface AiPanelRect extends AiFabPos {
  w: number;
  h: number;
}

export type AiPanelPref = AiFabPos & { w?: number; h?: number };

export function isAiResizeDir(v: string | undefined): v is AiResizeDir {
  return !!v && (AI_RESIZE_DIRS as readonly string[]).includes(v);
}

export function clampAiBoxPos(
  x: number,
  y: number,
  boxW: number,
  boxH: number,
  vw: number,
  vh: number,
): AiFabPos {
  const maxX = Math.max(AI_FAB_MARGIN, vw - boxW - AI_FAB_MARGIN);
  const maxY = Math.max(AI_FAB_MARGIN, vh - boxH - AI_FAB_MARGIN);
  return {
    x: Math.round(Math.min(maxX, Math.max(AI_FAB_MARGIN, x))),
    y: Math.round(Math.min(maxY, Math.max(AI_FAB_MARGIN, y))),
  };
}

export function clampAiFabPos(
  x: number,
  y: number,
  vw: number,
  vh: number,
  size = AI_FAB_SIZE,
): AiFabPos {
  return clampAiBoxPos(x, y, size, size, vw, vh);
}

/** 缺省右下角（距边 24） */
export function defaultAiFabPos(vw: number, vh: number, size = AI_FAB_SIZE): AiFabPos {
  return clampAiFabPos(vw - size - 24, vh - size - 24, vw, vh, size);
}

export function clampAiPanelSize(w: number, h: number, vw: number, vh: number): { w: number; h: number } {
  const maxW = Math.max(AI_PANEL_MIN_W, vw - AI_FAB_MARGIN * 2);
  const maxH = Math.max(AI_PANEL_MIN_H, vh - AI_FAB_MARGIN * 2);
  return {
    w: Math.round(Math.min(maxW, Math.max(AI_PANEL_MIN_W, w))),
    h: Math.round(Math.min(maxH, Math.max(AI_PANEL_MIN_H, h))),
  };
}

/** 窄屏收一档；preferred 为用户拖出的宽高 */
export function resolveAiPanelSize(
  vw: number,
  vh: number,
  preferred?: { w?: number; h?: number },
): { w: number; h: number } {
  return clampAiPanelSize(preferred?.w ?? AI_PANEL_WIDTH, preferred?.h ?? AI_PANEL_HEIGHT, vw, vh);
}

export function clampAiPanelRect(rect: AiPanelRect, vw: number, vh: number): AiPanelRect {
  const size = clampAiPanelSize(rect.w, rect.h, vw, vh);
  const pos = clampAiBoxPos(rect.x, rect.y, size.w, size.h, vw, vh);
  return { ...pos, ...size };
}

/** 从某边/角拉伸；保最小尺寸并贴近视口 */
export function applyAiResize(
  start: AiPanelRect,
  dir: AiResizeDir,
  dx: number,
  dy: number,
  vw: number,
  vh: number,
): AiPanelRect {
  let w = start.w;
  let h = start.h;
  if (dir.includes('e')) w = start.w + dx;
  if (dir.includes('s')) h = start.h + dy;
  if (dir.includes('w')) w = start.w - dx;
  if (dir.includes('n')) h = start.h - dy;
  const size = clampAiPanelSize(w, h, vw, vh);
  let x = start.x;
  let y = start.y;
  if (dir.includes('w')) x = start.x + start.w - size.w;
  if (dir.includes('n')) y = start.y + start.h - size.h;
  return clampAiPanelRect({ x, y, w: size.w, h: size.h }, vw, vh);
}

/** 缺省右下；若给了悬浮球坐标则贴在球附近（打开时球会隐藏） */
export function defaultAiPanelPos(vw: number, vh: number, near?: AiFabPos): AiFabPos {
  const { w, h } = resolveAiPanelSize(vw, vh);
  if (near) {
    return clampAiBoxPos(near.x + AI_FAB_SIZE - w, near.y + AI_FAB_SIZE - h, w, h, vw, vh);
  }
  return clampAiBoxPos(vw - w - 24, vh - h - 24, w, h, vw, vh);
}

export function normalizeAiFab(raw: unknown): AiFabPos | undefined {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return undefined;
  const o = raw as Record<string, unknown>;
  const x = typeof o.x === 'number' ? o.x : Number(o.x);
  const y = typeof o.y === 'number' ? o.y : Number(o.y);
  if (!Number.isFinite(x) || !Number.isFinite(y)) return undefined;
  return { x, y };
}

export function normalizeAiPanel(raw: unknown): AiPanelPref | undefined {
  const pos = normalizeAiFab(raw);
  if (!pos) return undefined;
  const o = raw as Record<string, unknown>;
  const w = typeof o.w === 'number' ? o.w : Number(o.w);
  const h = typeof o.h === 'number' ? o.h : Number(o.h);
  return {
    ...pos,
    ...(Number.isFinite(w) && w > 0 ? { w } : {}),
    ...(Number.isFinite(h) && h > 0 ? { h } : {}),
  };
}
