<template>
  <div class="anti-pattern-fixture">
    <!-- 控制组：以下均为合法写法，期望不被报告 -->
    <div class="valid-el">{{ validEl }}</div>
  </div>
</template>

<script setup lang="ts">
// ===== 控制组（合法，期望不报错）=====
const c1 = 'var(--el-color-primary)';
const c2 = 'var(--el-border-radius-small)';
const c3 = 'var(--cube-layout-test)';
const c4 = 'var(--navbar-test)';
const c5 = 'var(--radius-md)';
const c6 = 'var(--shadow-lg)';
const c7 = 'var(--ease)';
const c8 = 'var(--layout-nav-height)';
// HTML 数字实体不应误报
const emoji = '&#128269;';
// SCSS 插值不应误报
const interp = '#{ $x }';
</script>

<style scoped>
/* ===== 控制组：合法写法，期望不报错 ===== */
.valid-el {
  color: var(--el-color-primary);
  border-radius: var(--el-border-radius-small);
  background: color-mix(in srgb, var(--el-color-primary), transparent);
  color: transparent;
  border-color: currentColor;
  font-family: var(--cube-layout-test);
  box-shadow: var(--shadow-lg);
}
.nav-valid {
  padding: var(--navbar-test);
  transition: var(--ease);
  height: var(--layout-nav-height);
}

/* ===== 反模式 A：不存在的 --el- token（期望：未知的 Element token） ===== */
.bad-el-1 { border-radius: var(--el-border-radius-large, 16px); }
.bad-el-2 { font-family: var(--el-font-family-mono); }
.bad-el-3 { border-radius: var(--el-border-radius-sm); }
.bad-el-4 { color: var(--el-color-primary-invalid); }

/* ===== 反模式 B：未授权的自定义 token（期望：未授权的自定义 token） ===== */
.bad-custom-1 { color: var(--my-random-token); }
.bad-custom-2 { background: var(--random-color); }
.bad-custom-3 { border: 1px solid var(--bad-token); }

/* ===== 反模式 C：硬编码 16 进制颜色（期望：硬编码颜色） ===== */
.hard-hex-1 { color: #ff0000; }
.hard-hex-2 { background: #fff; }
.hard-hex-3 { border-color: #1a2b3c4d; }
.hard-hex-4 { border: 1px solid #abc; }

/* ===== 反模式 D：硬编码 rgb/rgba（期望：硬编码颜色） ===== */
.hard-rgb-1 { background: rgb(255, 0, 0); }
.hard-rgb-2 { color: rgba(0, 0, 0, 0.5); }

/* ===== 反模式 E：硬编码命名颜色（期望：硬编码颜色） ===== */
.hard-named-1 { color: red; }
.hard-named-2 { border: 1px solid blue; }
.hard-named-3 { background: green; }

/* ===== 反模式 F：var() 兜底色值（期望：硬编码颜色，不管是否兜底） ===== */
.fallback-hex { color: var(--maybe-token, #ffffff); } /* #fff 兜底 -> 硬编码颜色 */
.fallback-named { color: var(--maybe-token-2, red); } /* red 兜底 -> 硬编码 + 未授权 */
.fallback-el { color: var(--el-color-primary, #fff); } /* --el- 兜底仍为硬编码颜色 */

/* ===== 已知局限（期望：不会被捕获，记录为局限，非脚本缺陷） ===== */
.gap-hsl { color: hsl(0, 100%, 50%); }            /* hsl() 不在检查范围内（注释已正确忽略，不再误报） */
</style>
