/**
 * 密码规则解析（对齐 Vue 皮肤 composables/usePasswordRules.ts）
 *
 * 根据后端下发的密码强度正则（security.passwordStrength）动态解析密码规则，
 * 作为密码提示与提交校验的唯一真理源。
 */

export interface PasswordRuleDef {
  /** 规则描述文本（同时作为错误提示） */
  label: string;
  /** 校验函数：传入密码，返回是否满足 */
  test: (pwd: string) => boolean;
}

/** 后端未下发规则或规则无效时的兜底最小长度 */
export const PASSWORD_FALLBACK_MIN_LENGTH = 5;

function buildFallbackRules(): PasswordRuleDef[] {
  return [
    {
      label: `至少 ${PASSWORD_FALLBACK_MIN_LENGTH} 位`,
      test: (p) => p.length >= PASSWORD_FALLBACK_MIN_LENGTH,
    },
  ];
}

/**
 * 解析后端下发的密码强度正则
 *
 * - 空串或 '*'：未配置复杂度要求 → 兜底「至少 N 位」
 * - 合法正则：解析常见约束（长度/数字/大小写/特殊字符）；无法解析则整条正则整体校验
 * - 正则无效：回退兜底
 *
 * @param strength 后端 passwordStrength 正则
 * @returns 密码规则描述符数组
 */
export function parsePasswordRules(strength?: string): PasswordRuleDef[] {
  if (!strength || strength.trim() === '' || strength === '*') return buildFallbackRules();

  const rules: PasswordRuleDef[] = [];

  // 数字
  if (/\(\?=\.\*(?:\\d|\[0-9\])/.test(strength)) {
    rules.push({ label: '至少包含 1 个数字', test: (p) => /\d/.test(p) });
  }
  // 小写字母
  if (/\(\?=\.\*\[a-z\]/.test(strength)) {
    rules.push({ label: '至少包含 1 个小写字母', test: (p) => /[a-z]/.test(p) });
  }
  // 大写字母
  if (/\(\?=\.\*\[A-Z\]/.test(strength)) {
    rules.push({ label: '至少包含 1 个大写字母', test: (p) => /[A-Z]/.test(p) });
  }
  // 特殊字符
  if (/\(\?=\.\*\[\^/.test(strength)) {
    rules.push({ label: '至少包含 1 个特殊字符', test: (p) => /[^0-9a-zA-Z]/.test(p) });
  }
  // 长度
  const lenMatch = strength.match(/\.\{(\d+)(?:,(\d+)?)?\}/);
  if (lenMatch) {
    const min = parseInt(lenMatch[1], 10);
    const max = lenMatch[2] ? parseInt(lenMatch[2], 10) : null;
    if (max) {
      rules.push({ label: `长度为 ${min}-${max} 位`, test: (p) => p.length >= min && p.length <= max });
    } else {
      rules.push({ label: `至少 ${min} 位`, test: (p) => p.length >= min });
    }
  }

  if (rules.length === 0) {
    try {
      const re = new RegExp(strength);
      rules.push({ label: '符合密码安全规则', test: (p) => re.test(p) });
    } catch {
      return buildFallbackRules();
    }
  }

  return rules;
}

export default parsePasswordRules;
