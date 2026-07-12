import { computed, toValue, type MaybeRefOrGetter } from 'vue';

/**
 * 密码规则项（用于实时提示）
 */
export interface PasswordRule {
  /** 规则描述文本 */
  label: string;
  /** 当前输入是否满足该规则 */
  satisfied: boolean;
}

/**
 * 密码规则描述符（含校验函数，提示与提交校验共用）
 */
export interface PasswordRuleDef {
  /** 规则描述文本（同时作为错误提示） */
  label: string;
  /** 校验函数：传入密码，返回是否满足 */
  test: (pwd: string) => boolean;
}

/**
 * 后端未下发密码强度规则（空串 / '*'）或规则无法解析时，兜底的最小长度限制。
 * 避免「完全不校验」导致弱密码（如单字符）直接通过登录。
 */
const PASSWORD_FALLBACK_MIN_LENGTH = 5;

/**
 * 构建兜底规则：仅「至少 N 位」。
 * 用作空串 / '*' / 无效正则 的最终兜底，既展示实时提示也参与提交校验。
 */
function buildFallbackRules(): PasswordRuleDef[] {
  return [
    {
      label: `至少 ${PASSWORD_FALLBACK_MIN_LENGTH} 位`,
      test: (p) => p.length >= PASSWORD_FALLBACK_MIN_LENGTH,
    },
  ];
}

/**
 * 根据后端下发的密码强度正则（security.passwordStrength）动态解析密码规则。
 *
 * - 空字符串或 '*'：表示未配置复杂度要求，回退为「至少 N 位」兜底规则（默认最少 5 位），
 *   既作为实时提示，也参与提交校验，避免完全不校验弱密码。
 * - 合法正则：尝试解析常见约束（长度、数字、大小写字母、特殊字符）；
 *   若无法解析出具体约束，则回退为整条正则的整体校验。
 * - 正则无效（后端配置错误）：同样回退为「至少 N 位」兜底规则。
 *
 * 这是密码提示与提交校验的唯一真理源，避免校验规则写死在代码里。
 *
 * @param strength 后端下发的 passwordStrength 正则
 * @returns 密码规则描述符数组
 */
export function parsePasswordRules(strength?: string): PasswordRuleDef[] {
  // 空字符串或 '*'：未配置复杂度要求，使用兜底规则（避免完全不校验）
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

  // 无法解析为具体约束时，回退为整条正则的整体校验
  if (rules.length === 0) {
    try {
      const re = new RegExp(strength);
      rules.push({ label: '符合密码安全规则', test: (p) => re.test(p) });
    } catch {
      // 正则无效视为配置错误，使用兜底规则，避免完全不校验弱密码
      return buildFallbackRules();
    }
  }

  return rules;
}

/**
 * 密码规则组合式：根据后端下发的强度正则与当前输入，
 * 派生出「规则描述符 / 实时提示列表 / 是否展示提示」三个状态。
 *
 * 抽离到独立模块后，既可供登录页直接使用，也可被 Vitest 直接单测，
 * 不再与具体页面耦合。
 *
 * @param strength 后端下发的 passwordStrength（响应式或 getter）
 * @param password 当前密码输入（响应式或 getter）
 */
export function usePasswordRules(
  strength: MaybeRefOrGetter<string | undefined>,
  password: MaybeRefOrGetter<string>,
) {
  /** 当前密码强度规则（统一来源），用于实时提示与提交校验 */
  const passwordRuleDefs = computed<PasswordRuleDef[]>(() => parsePasswordRules(toValue(strength)));

  /** 实时提示用的规则列表（带 satisfied 状态） */
  const passwordRules = computed<PasswordRule[]>(() => {
    const pwd = toValue(password);
    return passwordRuleDefs.value.map((r) => ({ label: r.label, satisfied: r.test(pwd) }));
  });

  /** 是否显示密码强度提示（有规则且用户已开始输入） */
  const showPasswordHints = computed<boolean>(() => {
    return passwordRules.value.length > 0 && toValue(password).length > 0;
  });

  return { passwordRuleDefs, passwordRules, showPasswordHints };
}
