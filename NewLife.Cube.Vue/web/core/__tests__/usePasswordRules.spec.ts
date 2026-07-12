/**
 * 密码规则解析单元测试（不依赖浏览器，纯逻辑验证）
 *
 * 验证 parsePasswordRules 作为「密码提示 + 提交校验」的唯一真理源，
 * 能根据后端下发的 passwordStrength 动态生成规则，而不是写死在前端。
 * 对应组件测试 login-password-rules.cy.ts 的 5 类场景。
 *
 * 运行：pnpm --filter ./NewLife.Cube.Vue/web run test:unit usePasswordRules
 */
import { describe, it, expect } from 'vitest';
import { ref } from 'vue';
import { parsePasswordRules, usePasswordRules } from '../composables/usePasswordRules';

interface Case {
  name: string;
  strength: string;
  /** 期望的规则文案 */
  labels: string[];
  /** 抽样密码 → 是否满足全部规则 */
  samples: { pwd: string; allPass: boolean }[];
}

const CASES: Case[] = [
  {
    name: '无规则（*）',
    strength: '*',
    labels: ['至少 5 位'],
    samples: [
      { pwd: 'a', allPass: false },
      { pwd: '12345', allPass: true },
    ],
  },
  {
    name: '空串',
    strength: '',
    labels: ['至少 5 位'],
    samples: [
      { pwd: 'ab', allPass: false },
      { pwd: 'abcde', allPass: true },
    ],
  },
  {
    name: '最小长度 .{6,}',
    strength: '.{6,}',
    labels: ['至少 6 位'],
    samples: [
      { pwd: 'abc', allPass: false },
      { pwd: 'abcdef', allPass: true },
    ],
  },
  {
    name: '复合 ^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$',
    strength: '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$',
    labels: ['至少包含 1 个数字', '至少包含 1 个小写字母', '至少包含 1 个大写字母', '至少 8 位'],
    samples: [
      { pwd: 'Ab1', allPass: false }, // 长度不足
      { pwd: 'Abcdef12', allPass: true },
    ],
  },
  {
    name: '回退正则 ^[a-z]+$',
    strength: '^[a-z]+$',
    labels: ['符合密码安全规则'],
    samples: [
      { pwd: 'abc123', allPass: false },
      { pwd: 'abc', allPass: true },
    ],
  },
];

describe('parsePasswordRules 动态解析', () => {
  CASES.forEach((c) => {
    it(`${c.name}：解析出 ${c.labels.length} 条规则`, () => {
      const rules = parsePasswordRules(c.strength);
      expect(rules.map((r) => r.label)).toEqual(c.labels);
    });

    it(`${c.name}：抽样密码校验符合预期`, () => {
      const rules = parsePasswordRules(c.strength);
      c.samples.forEach((s) => {
        const allPass = rules.every((r) => r.test(s.pwd));
        expect(allPass, `密码「${s.pwd}」应${s.allPass ? '通过' : '不通过'}`).toBe(s.allPass);
      });
    });
  });

  it('非法正则应回退为兜底规则（至少 5 位），不抛异常', () => {
    expect(() => parsePasswordRules('([')).not.toThrow();
    const rules = parsePasswordRules('([');
    expect(rules).toHaveLength(1);
    expect(rules[0].label).toBe('至少 5 位');
    expect(rules[0].test('123')).toBe(false);
    expect(rules[0].test('12345')).toBe(true);
  });
});

describe('usePasswordRules 组合式', () => {
  it('passwordRules 带 satisfied 状态，随输入实时变化', () => {
    const password = ref('abc');
    const { passwordRules } = usePasswordRules(() => '.{6,}', password);
    expect(passwordRules.value.every((r) => r.satisfied)).toBe(false);
    password.value = 'abcdef';
    expect(passwordRules.value.every((r) => r.satisfied)).toBe(true);
  });

  it('showPasswordHints：有规则且已输入时为真，空输入时为假', () => {
    const password = ref('');
    const { showPasswordHints } = usePasswordRules(() => '.{6,}', password);
    expect(showPasswordHints.value).toBe(false);
    password.value = 'abc';
    expect(showPasswordHints.value).toBe(true);
  });

  it('无规则（*）时也展示兜底提示，空输入时为假', () => {
    const { showPasswordHints } = usePasswordRules(
      () => '*',
      () => 'anything',
    );
    expect(showPasswordHints.value).toBe(true);
  });
});
