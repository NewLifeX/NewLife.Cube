/**
 * 无头像时展示的用户名缩写（对齐 SvgAvatarService.ExtractChars）
 *
 * - chars=1：首字素；英文取首字母大写
 * - chars=2：中文≤2 全取、>2 取末尾 2 字；英文多词取首尾词首字母；单词取前 2 字母
 */

function graphemes(name: string): string[] {
  return Array.from(name);
}

function isCjk(ch: string): boolean {
  const cp = ch.codePointAt(0) ?? 0;
  return (
    (cp >= 0x4e00 && cp <= 0x9fff) ||
    (cp >= 0x3400 && cp <= 0x4dbf) ||
    (cp >= 0x20000 && cp <= 0x2a6df)
  );
}

function clampChars(n?: number | null): 1 | 2 {
  const v = Number(n);
  if (v >= 2) return 2;
  return 1;
}

/**
 * @param name 显示名 / 用户名
 * @param chars 魔方设置 AvatarChars（1 或 2，默认 1 兼容旧调用）
 */
export function avatarInitial(name?: string | null, chars?: number | null): string {
  const n = (name ?? '').trim();
  if (!n) return '?';
  const c = clampChars(chars);
  const els = graphemes(n);
  if (!els.length) return '?';

  if (c === 1) {
    const ch = els[0];
    return /[a-zA-Z]/.test(ch) ? ch.toUpperCase() : ch;
  }

  const allCjk = els.every(isCjk);
  if (allCjk) {
    if (els.length <= 2) return els.join('');
    return els.slice(-2).join('');
  }

  const parts = n.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    const a = parts[0][0] ?? '';
    const b = parts[parts.length - 1][0] ?? '';
    return (a + b).toUpperCase();
  }

  const letters = n.replace(/[^a-zA-Z]/g, '').slice(0, 2);
  if (letters) return letters.toUpperCase();
  return els.slice(0, 2).join('');
}
