/**
 * 从字段 Description 解析「明显键值对」→ dataSource（键提交、文案展示）。
 * 典型魔方配置：0表示不记录 / 0=不启用 / -1 Unspecified
 */

function cleanLabel(raw: string): string {
  return raw
    .replace(/\s+/g, ' ')
    .trim()
    // 去掉「。默认2」「.默认xxx」尾注
    .replace(/[。.;；]*默认[^，,;；]*$/g, '')
    .replace(/[。.;；]+$/g, '')
    .trim();
}

/**
 * 从说明文字提取数字键 → 标签。至少 2 个不同键才视为「明显键值对」。
 */
export function parseDescriptionDataSource(
  description?: string | null,
): Record<string, string> | undefined {
  const text = (description || '').trim();
  if (!text) return undefined;

  const map = new Map<string, string>();

  const add = (key: string, label: string) => {
    const k = key.trim();
    const lab = cleanLabel(label);
    if (!k || !lab) return;
    if (!map.has(k)) map.set(k, lab);
  };

  // 选项正文截止于中英文逗号/分号/句号，避免吞掉「。默认N」
  const VALUE = '([^，,;；。]+)';

  // 1) 0=不启用 / 1＝登录（全角等号）
  for (const m of text.matchAll(new RegExp(`(-?\\d+)\\s*[=＝]\\s*${VALUE}`, 'g'))) {
    add(m[1], m[2]);
  }

  // 2) 0表示不记录，1表示仅记录已登录用户
  for (const m of text.matchAll(new RegExp(`(-?\\d+)\\s*表示\\s*${VALUE}`, 'g'))) {
    add(m[1], m[2]);
  }

  // 3) -1 Unspecified，0 None，1 Lax（英文枚举名）
  for (const m of text.matchAll(/(-?\d+)\s+([A-Za-z][\w]*)/g)) {
    add(m[1], m[2]);
  }

  if (map.size < 2) return undefined;
  const out: Record<string, string> = {};
  for (const [k, v] of map) out[k] = v;
  return out;
}

/**
 * Int32 且无现成 dataSource 时，用 description 键值对补齐下拉字典。
 */
export function applyDescriptionDataSourceIfNeeded(field: {
  typeName?: string;
  description?: string;
  dataSource?: Record<string, string>;
}): void {
  const typeName = (field.typeName || '').trim();
  if (typeName !== 'Int32') return;
  if (field.dataSource && Object.keys(field.dataSource).length > 0) return;
  const parsed = parseDescriptionDataSource(field.description);
  if (parsed) field.dataSource = parsed;
}
