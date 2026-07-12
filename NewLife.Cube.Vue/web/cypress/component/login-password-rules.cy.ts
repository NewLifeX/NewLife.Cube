/**
 * 登录页密码规则组件测试（Cypress 组件测试 + 截图证据）
 *
 * 直接挂载 LoginForm（prop 驱动的展示组件），注入不同 passwordStrength，
 * 覆盖 5 类密码强度规则，断言：
 *   1. 实时提示：.password-hints / .password-hint-item(.satisfied)
 *   2. 提交校验错误：.input-error（按动态规则生成，而非写死）
 *   3. 校验通过时 emit('submit')（通过隐藏节点回显）
 * 并对每个场景存截图作为视觉证据。
 *
 * 运行：
 *   pnpm run test:component cypress/component/login-password-rules.cy.ts
 *
 * 组件测试走 Cypress 自带 vite devServer，无需提前启动前端(5187)/后端(7116)。
 */
import { defineComponent, h } from 'vue';
import LoginForm from '../../core/pages/LoginForm.vue';
import type { LoginConfig } from '@cube/api-core';

/**
 * 测试壳：包装 LoginForm，监听其 submit 事件并把载荷回显到隐藏节点。
 * 用 DOM 回显而非依赖 cy.mount 的 on/事件细节，断言更稳健、跨版本兼容。
 */
const Harness = defineComponent({
  name: 'LoginFormHarness',
  components: { LoginForm },
  props: {
    /** 后端下发的密码强度正则（security.passwordStrength） */
    strength: { type: String, default: '' },
  },
  data() {
    return { submitted: '' as string };
  },
  methods: {
    onSubmitted(payload: { username: string; password: string }) {
      this.submitted = JSON.stringify(payload);
    },
  },
  render() {
    const loginConfig = {
      security: { passwordStrength: this.strength },
    } as unknown as LoginConfig;
    return h('div', { class: 'harness' }, [
      h(LoginForm, { loginConfig, onSubmit: this.onSubmitted }),
      h('div', { id: 'submit-payload', 'data-testid': 'submit-payload' }, this.submitted),
    ]);
  },
});

/** 5 类密码规则场景 */
interface Scenario {
  key: string;
  title: string;
  /** 后端下发的 passwordStrength */
  strength: string;
  /** 不满足规则的密码（用于触发错误与提示） */
  failingPwd: string;
  /** 满足规则的密码 */
  validPwd: string;
  /** 实时提示条数（无规则时回退为 1 条「至少 5 位」兜底提示） */
  hintCount: number;
  /** 提交 failingPwd 时应出现的错误文案 */
  expectedError: string;
}

const SCENARIOS: Scenario[] = [
  {
    key: 'A-no-rule',
    title: '无规则（*）：兜底至少 5 位',
    strength: '*',
    failingPwd: 'ab',
    validPwd: 'abcde',
    hintCount: 1,
    expectedError: '密码需至少 5 位',
  },
  {
    key: 'B-min-length',
    title: '最小长度：.{6,}',
    strength: '.{6,}',
    failingPwd: 'abc',
    validPwd: 'abcdef',
    hintCount: 1,
    expectedError: '密码需至少 6 位',
  },
  {
    key: 'C-complex',
    title: '复合规则：数字+小写+大写+至少8位',
    strength: '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$',
    failingPwd: 'Ab1',
    validPwd: 'Abcdef12',
    hintCount: 4,
    expectedError: '密码需至少 8 位',
  },
  {
    key: 'D-empty',
    title: '空串：兜底至少 5 位',
    strength: '',
    failingPwd: 'ab',
    validPwd: 'abcde',
    hintCount: 1,
    expectedError: '密码需至少 5 位',
  },
  {
    key: 'E-fallback',
    title: '无法解析的正则：回退为整条规则校验',
    strength: '^[a-z]+$',
    failingPwd: 'abc123',
    validPwd: 'abc',
    hintCount: 1,
    expectedError: '密码需符合密码安全规则',
  },
];

/** 向用户名/密码输入区填入内容（el-input 内部为 input） */
function typeCredentials(username: string, password: string) {
  cy.get('[data-cy="username"] input').clear().type(username);
  cy.get('[data-cy="password"] input').clear().type(password);
}

describe('LoginForm 密码规则动态校验（5 类场景）', { testTimeout: 30000 }, () => {
  beforeEach(() => {
    // 截图视口固定，保证证据一致
    cy.viewport(480, 760);
    Cypress.config('defaultCommandTimeout', 15000);
  });

  SCENARIOS.forEach((sc) => {
    it(`场景 ${sc.key} — ${sc.title}`, () => {
      cy.mount(Harness, { props: { strength: sc.strength } });

      // ── 1) 不满足规则的密码：应触发错误 + 提示 ──
      if (sc.hintCount === 0) {
        // 无提示场景（理论上不存在，保留为安全分支）：空密码应报「请输入密码」
        typeCredentials('admin', '');
        cy.get('.login-btn').click();
        cy.get('.input-error').should('contain.text', '请输入密码');
      } else {
        typeCredentials('admin', sc.failingPwd);
        // 实时提示应渲染出全部规则条
        cy.get('.password-hints').should('exist');
        cy.get('.password-hint-item').should('have.length', sc.hintCount);
        // 提交应被拦截并报出对应规则错误
        cy.get('.login-btn').click();
        cy.get('.input-error').should('contain.text', sc.expectedError);
        cy.screenshot(`login-rules-${sc.key}-failing`);
      }

      // ── 2) 满足规则的密码：应通过校验并 emit('submit') ──
      typeCredentials('admin', sc.validPwd);
      if (sc.hintCount > 0) {
        // 所有提示项应标记为已满足
        cy.get('.password-hint-item.satisfied').should('have.length', sc.hintCount);
      } else {
        // 无规则时不应出现提示区
        cy.get('.password-hints').should('not.exist');
      }
      cy.get('.login-btn').click();
      // 校验通过 → 容器应收到 submit（通过隐藏节点回显验证）
      cy.get('#submit-payload').should('contain.text', '"username":"admin"');
      cy.screenshot(`login-rules-${sc.key}-valid`);
    });
  });

  it('场景 C：复合规则的每条子规则应独立实时反馈', () => {
    cy.mount(Harness, { props: { strength: SCENARIOS[2].strength } });
    // 逐字符构造，验证子规则随输入实时点亮
    typeCredentials('admin', 'abcdefgh'); // 8 位小写，缺数字/大写
    cy.get('.password-hint-item').should('have.length', 4);
    // 小写、长度已满足；数字、大写未满足
    cy.get('.password-hint-item.satisfied').should('have.length', 2);
    cy.screenshot('login-rules-C-partial');

    typeCredentials('admin', 'Abcdefg1'); // 补充大写 + 数字
    cy.get('.password-hint-item.satisfied').should('have.length', 4);
    cy.screenshot('login-rules-C-all-satisfied');
  });
});
