/**
 * 通用字段校验规则（手机/电话/邮件/邮箱/网址）。
 *
 * 检测信号：
 * - itemType：mail/mobile/url（Cube.Vue contents 约定）
 * - 字段名：Phone/Mobile/Mail/Email/Tel/Url/Website/HomePage 等（大小写不敏感）
 *
 * 规则采用 Arco Design Vue 的 FieldRule（validator 回调形式），
 * 空值不触发格式校验（由 required 规则单独处理），仅对非空值做格式校验。
 */
import type { FieldMeta } from '../types/field';

/** Arco FieldRule 最小子集，避免直接耦合组件库类型 */
export interface ValidationRule {
  required?: boolean;
  message?: string;
  validator?: (value: unknown, callback: (error?: string) => void) => void;
}

type FormatKind = 'mobile' | 'phone' | 'email' | 'url';

const PATTERNS: Record<FormatKind, RegExp> = {
  // 中国大陆手机号
  mobile: /^1[3-9]\d{9}$/,
  // 固话：可带区号与分隔符，如 010-12345678、0755-1234567、12345678
  phone: /^(\d{3,4}[-\s]?)?\d{7,8}([-\s]?\d{1,6})?$/,
  email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  // 网址：可选协议；必须含点号且无空白
  url: /^(https?:\/\/)?[^\s]+\.[^\s]+$/i,
};

const MESSAGES: Record<FormatKind, string> = {
  mobile: '手机号格式不正确',
  phone: '电话号码格式不正确',
  email: '邮箱格式不正确',
  url: '网址格式不正确',
};

const NAME_KIND: Array<{ names: string[]; kind: FormatKind }> = [
  { names: ['mobile', 'cellphone', 'phonemobile'], kind: 'mobile' },
  { names: ['phone', 'tel', 'telephone', 'telephone1', 'telephone2'], kind: 'phone' },
  { names: ['mail', 'email', 'e-mail', 'useremail'], kind: 'email' },
  { names: ['url', 'website', 'homepage', 'web', 'site', 'link'], kind: 'url' },
];

const ITEM_TYPE_KIND: Record<string, FormatKind> = {
  mobile: 'mobile',
  mail: 'email',
  url: 'url',
};

function detectFormatKind(field: FieldMeta): FormatKind | null {
  const it = (field.itemType ?? '').trim().toLowerCase();
  if (ITEM_TYPE_KIND[it]) return ITEM_TYPE_KIND[it];

  const name = (field.name ?? '').trim().toLowerCase();
  // 精确匹配字段名（去除数字后缀，如 phone1）
  const stem = name.replace(/\d+$/, '');
  for (const { names, kind } of NAME_KIND) {
    if (names.includes(stem) || names.includes(name)) return kind;
  }
  return null;
}

/**
 * 解析字段应附加的格式校验规则。
 * 不含必填校验（由调用方按 isFieldRequired 单独追加），仅返回格式规则。
 */
export function fieldFormatRules(field: FieldMeta): ValidationRule[] {
  const kind = detectFormatKind(field);
  if (!kind) return [];

  const pattern = PATTERNS[kind];
  const message = MESSAGES[kind];
  return [
    {
      message,
      validator: (value, cb) => {
        if (value == null || value === '') return cb();
        const ok = pattern.test(String(value).trim());
        cb(ok ? undefined : message);
      },
    },
  ];
}
