<!--
  登录页 SFC — 品牌级沉浸式登录界面

  设计参考：cube-vue-login-design/login-forest-green.html
  核心理念：使用 Element Plus CSS 变量驱动全部配色，自动适配多主题（cyber/forest/aurora/industrial）与黑白模式。

  功能：
  1. onMounted 调用 /Auth/LoginConfig 获取后端登录配置
  2. 根据配置渲染密码表单 / OAuth 入口 / 版权信息
  3. 密码登录成功后写入 token 并跳回 redirect 页面
  4. OAuth 跳转 URL 统一为 /Sso/Login/{name}?source=front-end&redirect_uri={redirect}
  5. 当 login.password === false 且 oauth.length === 1 时自动跳转
  6. Canvas 粒子动画系统，颜色从 --el-color-primary 动态读取，随主题切换实时变化

  依赖：
  - core/configure 的 getConfig() 获取 baseUrl
  - core/utils/token 的 setAccessToken() 写入 token
  - core/utils/loginApi 的 fetchLoginConfig / loginByPassword 调用后端
  - @cube/api-core 的类型定义
-->
<template>
  <div class="login-page">
    <!-- 背景层：渐变或自定义背景图 -->
    <div class="bg-layer" :style="backgroundStyle"></div>
    <!-- 装饰性辉光 -->
    <div class="bg-glow"></div>
    <!-- 粒子画布 -->
    <canvas ref="particleCanvas" class="particle-canvas"></canvas>

    <!-- 登录主容器 -->
    <div class="login-wrapper" v-loading="loading">
      <div class="login-card">
        <!-- 品牌区：Logo + 系统名称 -->
        <div class="brand">
          <img
            v-if="logoSrc"
            :src="logoSrc"
            class="brand-logo"
            alt="logo"
          />
          <!-- 默认魔方 Logo（使用 CSS 变量，随主题变色） -->
          <svg
            v-else
            class="brand-logo brand-logo--default"
            viewBox="0 0 56 56"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <rect x="8" y="8" width="40" height="40" rx="10" class="logo-bg" />
            <rect x="8" y="8" width="40" height="40" rx="10" class="logo-border" stroke-width="2" />
            <line x1="8" y1="21.5" x2="48" y2="21.5" class="logo-line" stroke-width="1.5" />
            <line x1="8" y1="34.5" x2="48" y2="34.5" class="logo-line" stroke-width="1.5" />
            <line x1="21.5" y1="8" x2="21.5" y2="48" class="logo-line" stroke-width="1.5" />
            <line x1="34.5" y1="8" x2="34.5" y2="48" class="logo-line" stroke-width="1.5" />
            <rect x="21.5" y="21.5" width="13" height="13" rx="3" class="logo-core" />
          </svg>
          <h1 class="brand-name">{{ systemName }}</h1>
          <p v-if="loginConfig?.loginTip" class="brand-hint">
            {{ loginConfig.loginTip }}
          </p>
        </div>

        <!-- 密码登录表单（抽为独立展示组件，loginConfig 通过 prop 传入） -->
        <LoginForm
          v-if="showPasswordForm"
          :login-config="loginConfig ?? undefined"
          @submit="handleLogin"
        />

        <!-- OAuth 第三方登录入口 -->
        <div v-if="oauthProviders.length > 0" class="oauth-section">
          <div v-if="showPasswordForm" class="oauth-divider">
            <div class="divider-line"></div>
            <span class="divider-text">其他登录方式</span>
            <div class="divider-line"></div>
          </div>
          <div class="oauth-buttons">
            <button
              v-for="item in oauthProviders"
              :key="item.name"
              type="button"
              class="oauth-btn"
              @click="handleOAuth(item.name)"
            >
              <img
                v-if="item.logo"
                :src="resolveLogo(item.logo)"
                class="oauth-logo"
                :alt="item.nickName || item.name"
              />
              <span v-else class="oauth-name">{{ item.nickName || item.name }}</span>
            </button>
          </div>
        </div>

        <!-- 配置加载失败提示 -->
        <div v-if="configError" class="config-error">
          {{ configError }}
        </div>

        <!-- 底部版权信息 -->
        <footer
          v-if="loginConfig?.copyright || loginConfig?.registration"
          class="login-footer"
        >
          <div v-if="loginConfig?.copyright" class="footer-copy" v-html="loginConfig.copyright"></div>
          <div v-if="loginConfig?.registration" class="footer-registration">
            {{ loginConfig.registration }}
          </div>
        </footer>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getConfig } from '../configure';
import { setAccessToken, setRefreshToken } from '../utils/token';
import { fetchLoginConfig, loginByPassword } from '../utils/loginApi';
import LoginForm from './LoginForm.vue';
import type { LoginConfig, OAuthProvider } from '@cube/api-core';

// ── 路由与配置 ──────────────────────────────────────────────────────
const route = useRoute();
const router = useRouter();
const config = getConfig();

/** 后端 API 基础地址 */
const baseUrl: string = config.request.baseUrl || '';

// ── 响应式状态 ──────────────────────────────────────────────────────
/** 配置加载中 */
const loading = ref<boolean>(true);
/** 后端返回的登录配置 */
const loginConfig = ref<LoginConfig | null>(null);
/** 配置加载错误信息 */
const configError = ref<string>('');
/** 粒子画布引用 */
const particleCanvas = ref<HTMLCanvasElement | null>(null);

// ── 计算属性 ────────────────────────────────────────────────────────
/** 是否显示密码表单 */
const showPasswordForm = computed<boolean>(
  () => loginConfig.value?.login?.password === true,
);

/** OAuth 提供商列表 */
const oauthProviders = computed<OAuthProvider[]>(() => {
  return loginConfig.value?.oauth ?? [];
});

/** 系统名称 */
const systemName = computed<string>(() => {
  return loginConfig.value?.name || loginConfig.value?.displayName || '魔方系统';
});

/** 登录页 Logo 地址（优先 loginLogo，其次 logo） */
const logoSrc = computed<string>(() => {
  const logo = loginConfig.value?.loginLogo || loginConfig.value?.logo;
  if (!logo) return '';
  return resolveLogo(logo);
});

/** 背景样式（支持自定义背景图） */
const backgroundStyle = computed<Record<string, string>>(() => {
  const bg = loginConfig.value?.loginBackground;
  if (bg) {
    return { backgroundImage: `url(${resolveLogo(bg)})`, backgroundSize: 'cover', backgroundPosition: 'center' };
  }
  return {};
});

// 密码强度校验逻辑已抽到 LoginForm.vue（通过 prop 传入 loginConfig），本容器不再持有。

// ── 工具方法 ────────────────────────────────────────────────────────

/**
 * 解析 Logo / 背景图 URL
 * 后端返回的路径可能是相对路径（如 /sso/github.png）或绝对 URL
 * @param logo 后端返回的资源路径
 * @returns 完整 URL
 */
function resolveLogo(logo: string): string {
  if (!logo) return '';
  if (/^https?:\/\//i.test(logo)) return logo;
  if (logo.startsWith('//')) return `${window.location.protocol}${logo}`;
  return `${baseUrl}${logo.startsWith('/') ? '' : '/'}${logo}`;
}

/**
 * 构建 OAuth 跳转 URL
 * 统一格式：{baseUrl}/Sso/Login/{name}?source=front-end&redirect_uri={encodedRedirect}
 */
function buildOAuthUrl(name: string, redirectUri: string): string {
  return `${baseUrl}/Sso/Login/${name}?source=front-end&redirect_uri=${encodeURIComponent(redirectUri)}`;
}

/**
 * 判断是否应自动跳转
 * 当密码登录被禁用且只有一个 OAuth 提供商时，直接跳转避免空白页
 */
function shouldAutoRedirect(): boolean {
  const cfg = loginConfig.value;
  if (!cfg) return false;
  const passwordEnabled = cfg.login?.password === true;
  const oauthCount = cfg.oauth?.length ?? 0;
  return !passwordEnabled && oauthCount === 1;
}

/** 清除指定字段的错误提示 */
// ── 事件处理 ────────────────────────────────────────────────────────

/**
 * 处理 OAuth 第三方登录跳转
 */
function handleOAuth(name: string): void {
  const redirectPath = (route.query.redirect as string) || '/';
  const redirectUri = `${window.location.origin}${redirectPath}`;
  window.location.href = buildOAuthUrl(name, redirectUri);
}

/**
 * 处理密码登录提交（容器层：仅负责与服务端交互）。
 * 客户端校验已前置到 LoginForm，校验通过才会 emit('submit')。
 */
async function handleLogin(payload: { username: string; password: string }): Promise<void> {
  try {
    const res = await loginByPassword(baseUrl, payload.username, payload.password);

    if (res.code === 0 && res.data?.accessToken) {
      // 登录成功：写入 token
      setAccessToken(res.data.accessToken);
      if (res.data.refreshToken) {
        setRefreshToken(res.data.refreshToken);
      }
      ElMessage.success('登录成功，正在跳转...');
      const redirect = (route.query.redirect as string) || '/';
      router.push(redirect);
    } else {
      ElMessage.error(res.message || '登录失败，请检查用户名和密码');
    }
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : '网络错误，请稍后重试';
    ElMessage.error(message);
  }
}

// ── 粒子系统（Canvas）────────────────────────────────────────────────
// 颜色从 --el-color-primary 动态读取，随主题切换实时变化

/** 粒子动画帧 ID */
let particleAnimId: number | null = null;
/** 主题变化观察器 */
let themeObserver: MutationObserver | null = null;
/** 鼠标位置 */
let mouseX = -1000;
let mouseY = -1000;
/** 粒子颜色列表（从 CSS 变量动态生成） */
let particleColors: string[] = [];
/** 粒子连线颜色 */
let connectionColor = 'rgba(34, 197, 94, 0.3)';
/** 粒子数组 */
let particles: Particle[] = [];
/** 事件处理函数引用（用于清理） */
let mouseMoveHandler: ((e: MouseEvent) => void) | null = null;
let mouseOutHandler: (() => void) | null = null;
let resizeHandler: (() => void) | null = null;
let resizeTimer: ReturnType<typeof setTimeout> | null = null;

/** 粒子结构 */
interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  radius: number;
  color: string;
  baseAlpha: number;
  alpha: number;
}

/**
 * 解析 CSS 颜色值为 RGB 分量
 * 支持 hex (#22c55e) 和 rgb(34, 197, 94) 两种格式
 */
function parseColor(color: string): { r: number; g: number; b: number } {
  color = color.trim();
  // Hex: #22c55e
  const hex = color.match(/^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i);
  if (hex) {
    return { r: parseInt(hex[1], 16), g: parseInt(hex[2], 16), b: parseInt(hex[3], 16) };
  }
  // RGB: rgb(34, 197, 94)
  const rgb = color.match(/rgb\((\d+)[,\s]+(\d+)[,\s]+(\d+)\)/);
  if (rgb) {
    return { r: +rgb[1], g: +rgb[2], b: +rgb[3] };
  }
  // Fallback: forest green
  return { r: 34, g: 197, b: 94 };
}

/**
 * 从 CSS 变量读取主题色并更新粒子颜色
 * 监听 --el-color-primary 的变化，重新生成粒子配色
 */
function updateParticleColors(): void {
  const style = getComputedStyle(document.documentElement);
  const primary = style.getPropertyValue('--el-color-primary').trim();
  const { r, g, b } = parseColor(primary);

  particleColors = [
    `rgba(${r}, ${g}, ${b}, 0.25)`,
    `rgba(${r}, ${g}, ${b}, 0.20)`,
    `rgba(${r}, ${g}, ${b}, 0.15)`,
    `rgba(${r}, ${g}, ${b}, 0.12)`,
  ];
  connectionColor = `rgba(${r}, ${g}, ${b}, 0.3)`;

  // 更新已有粒子的颜色
  for (const p of particles) {
    p.color = particleColors[Math.floor(Math.random() * particleColors.length)];
  }
}

/**
 * 创建单个粒子
 */
function createParticle(canvasWidth: number, canvasHeight: number): Particle {
  const p: Particle = {
    x: Math.random() * canvasWidth,
    y: Math.random() * canvasHeight,
    vx: (Math.random() - 0.5) * 0.4,
    vy: (Math.random() - 0.5) * 0.4,
    radius: Math.random() * 2 + 0.5,
    color: particleColors[Math.floor(Math.random() * particleColors.length)] || 'rgba(34, 197, 94, 0.25)',
    baseAlpha: Math.random() * 0.3 + 0.15,
    alpha: 0,
  };
  p.alpha = p.baseAlpha;
  return p;
}

/**
 * 初始化粒子系统
 * 在桌面端启用，移动端和 prefers-reduced-motion 下跳过
 */
function initParticleSystem(): void {
  const canvas = particleCanvas.value;
  if (!canvas) return;

  // 移动端不启用粒子
  if (window.innerWidth <= 640) return;

  // 无障碍：用户偏好减少动画时跳过
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  // 读取主题色
  updateParticleColors();

  // 设置画布尺寸
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;

  // 创建粒子
  const count = Math.min(Math.floor((canvas.width * canvas.height) / 20000), 60);
  particles = [];
  for (let i = 0; i < count; i++) {
    particles.push(createParticle(canvas.width, canvas.height));
  }

  // 绘制粒子连线
  function drawConnections(): void {
    const maxDist = 130;
    for (let i = 0; i < particles.length; i++) {
      for (let j = i + 1; j < particles.length; j++) {
        const dx = particles[i].x - particles[j].x;
        const dy = particles[i].y - particles[j].y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < maxDist) {
          const alpha = (1 - dist / maxDist) * 0.08;
          ctx.save();
          ctx.globalAlpha = alpha;
          ctx.beginPath();
          ctx.moveTo(particles[i].x, particles[i].y);
          ctx.lineTo(particles[j].x, particles[j].y);
          ctx.strokeStyle = connectionColor;
          ctx.lineWidth = 0.6;
          ctx.stroke();
          ctx.restore();
        }
      }
    }
  }

  // 动画循环
  function animate(): void {
    if (!canvas) return;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawConnections();

    for (const p of particles) {
      // 更新位置
      p.x += p.vx;
      p.y += p.vy;

      // 边界反弹
      if (p.x < 0 || p.x > canvas.width) p.vx *= -1;
      if (p.y < 0 || p.y > canvas.height) p.vy *= -1;

      // 鼠标互动：粒子被鼠标排斥
      const dx = p.x - mouseX;
      const dy = p.y - mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < 120) {
        const force = (120 - dist) / 120;
        p.x += (dx / dist) * force * 1.5;
        p.y += (dy / dist) * force * 1.5;
        p.alpha = Math.min(p.baseAlpha + force * 0.3, 0.6);
      } else {
        p.alpha += (p.baseAlpha - p.alpha) * 0.05;
      }

      // 绘制粒子
      ctx.save();
      ctx.globalAlpha = p.alpha;
      ctx.beginPath();
      ctx.arc(p.x, p.y, Math.max(0.1, p.radius), 0, Math.PI * 2);
      ctx.fillStyle = p.color;
      ctx.fill();
      ctx.restore();
    }

    particleAnimId = requestAnimationFrame(animate);
  }

  // 事件监听
  mouseMoveHandler = (e: MouseEvent) => {
    mouseX = e.clientX;
    mouseY = e.clientY;
  };
  mouseOutHandler = () => {
    mouseX = -1000;
    mouseY = -1000;
  };
  resizeHandler = () => {
    if (resizeTimer) clearTimeout(resizeTimer);
    resizeTimer = setTimeout(() => {
      if (!canvas) return;
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
      const newCount = Math.min(Math.floor((canvas.width * canvas.height) / 20000), 60);
      particles = [];
      for (let i = 0; i < newCount; i++) {
        particles.push(createParticle(canvas.width, canvas.height));
      }
    }, 200);
  };

  window.addEventListener('mousemove', mouseMoveHandler);
  window.addEventListener('mouseout', mouseOutHandler);
  window.addEventListener('resize', resizeHandler);

  // 主题变化观察：当 data-theme 或 class 变化时重新读取颜色
  themeObserver = new MutationObserver(() => {
    updateParticleColors();
  });
  themeObserver.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-theme', 'class'],
  });

  // 启动动画
  animate();
}

/**
 * 销毁粒子系统，清理所有资源
 */
function destroyParticleSystem(): void {
  if (particleAnimId !== null) {
    cancelAnimationFrame(particleAnimId);
    particleAnimId = null;
  }
  if (themeObserver) {
    themeObserver.disconnect();
    themeObserver = null;
  }
  if (mouseMoveHandler) {
    window.removeEventListener('mousemove', mouseMoveHandler);
    mouseMoveHandler = null;
  }
  if (mouseOutHandler) {
    window.removeEventListener('mouseout', mouseOutHandler);
    mouseOutHandler = null;
  }
  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler);
    resizeHandler = null;
  }
  if (resizeTimer) {
    clearTimeout(resizeTimer);
    resizeTimer = null;
  }
  particles = [];
}

// ── 生命周期 ────────────────────────────────────────────────────────

onMounted(async () => {
  // 初始化粒子动画
  initParticleSystem();

  // 检查 baseUrl 是否已配置
  if (!baseUrl) {
    configError.value = '未配置后端 API 地址（VITE_API_URL），无法获取登录配置';
    loading.value = false;
    return;
  }

  try {
    const res = await fetchLoginConfig(baseUrl);

    if (res.code === 0 && res.data) {
      loginConfig.value = res.data;

      // 自动跳转：禁用密码 + 单第三方
      if (shouldAutoRedirect() && loginConfig.value.oauth?.[0]) {
        handleOAuth(loginConfig.value.oauth[0].name);
        return;
      }
    } else {
      configError.value = res.message || '获取登录配置失败';
    }
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : '获取登录配置失败';
    configError.value = message;
  } finally {
    loading.value = false;
  }
});

onUnmounted(() => {
  destroyParticleSystem();
});
</script>

<style scoped>
/* ==================== 页面容器 ==================== */
.login-page {
  position: relative;
  min-height: 100vh;
  overflow-y: auto;
  background: var(--el-bg-color);
}

/* ==================== 背景层 ==================== */
/* 极淡主题色渐变，营造氛围但不喧宾夺主 */
.bg-layer {
  position: fixed;
  inset: 0;
  z-index: 0;
  background: linear-gradient(
    180deg,
    var(--el-color-primary-light-9) 0%,
    var(--el-bg-color) 40%,
    var(--el-bg-color) 60%,
    var(--el-color-primary-light-9) 100%
  );
}

/* 装饰性辉光：角落淡色光斑 */
.bg-glow {
  position: fixed;
  inset: 0;
  z-index: 1;
  pointer-events: none;
  background:
    radial-gradient(circle at 15% 20%, var(--el-color-primary-light-9) 0%, transparent 40%),
    radial-gradient(circle at 85% 80%, var(--el-color-primary-light-9) 0%, transparent 40%);
}

/* 粒子画布 */
.particle-canvas {
  position: fixed;
  inset: 0;
  z-index: 2;
  pointer-events: none;
}

/* ==================== 主布局 ==================== */
.login-wrapper {
  position: relative;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: 24px;
}

/* ==================== 登录卡片 ==================== */
.login-card {
  width: 420px;
  max-width: 100%;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 24px;
  box-shadow: var(--el-box-shadow-light);
  padding: 40px 36px 32px;
  position: relative;
  overflow: hidden;
  animation: card-appear 0.6s cubic-bezier(0.16, 1, 0.3, 1) both;
  transition: box-shadow 0.3s ease, border-color 0.3s ease;
}

.login-card:hover {
  border-color: var(--el-color-primary-light-7);
  box-shadow: 0 12px 40px var(--el-color-primary-light-8);
}

/* 卡片顶部主题色装饰条 */
.login-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-dark-2));
  opacity: 0.8;
}

@keyframes card-appear {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ==================== 品牌区 ==================== */
.brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 32px;
}

/* Logo（图片或默认 SVG） */
.brand-logo {
  width: 56px;
  height: 56px;
  margin-bottom: 16px;
  object-fit: contain;
  filter: drop-shadow(0 4px 12px var(--el-color-primary-light-7));
}

/* 默认魔方 Logo — 使用 CSS 变量随主题变色 */
.brand-logo--default .logo-bg {
  fill: var(--el-color-primary);
  opacity: 0.15;
}

.brand-logo--default .logo-border {
  stroke: var(--el-color-primary);
  fill: none;
}

.brand-logo--default .logo-line {
  stroke: var(--el-color-primary);
  opacity: 0.6;
}

.brand-logo--default .logo-core {
  fill: var(--el-color-primary);
}

/* 系统名称 */
.brand-name {
  margin: 0;
  font-size: 26px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  letter-spacing: -0.5px;
}

/* 登录提示 */
.brand-hint {
  margin: 8px 0 0;
  font-size: 15px;
  color: var(--el-text-color-regular);
}

/* ==================== OAuth 区域 ==================== */
.oauth-section {
  margin-top: 8px;
}

/* 分割线 */
.oauth-divider {
  display: flex;
  align-items: center;
  gap: 16px;
  margin: 8px 0 4px;
}

.divider-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(
    90deg,
    transparent,
    var(--el-border-color),
    transparent
  );
}

.divider-text {
  font-size: 13px;
  color: var(--el-text-color-placeholder);
  white-space: nowrap;
}

/* OAuth 按钮组 */
.oauth-buttons {
  display: flex;
  gap: 12px;
}

.oauth-btn {
  flex: 1;
  height: 48px;
  background: var(--el-color-primary-light-9);
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.25s ease, border-color 0.25s ease, transform 0.25s ease, box-shadow 0.25s ease;
  color: var(--el-text-color-regular);
}

.oauth-btn:hover {
  background: var(--el-bg-color-overlay);
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px var(--el-color-primary-light-8);
}

.oauth-btn:active {
  transform: translateY(0);
}

.oauth-logo {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

.oauth-name {
  font-size: 13px;
  font-weight: 500;
}

/* ==================== 配置错误 ==================== */
.config-error {
  margin-top: 16px;
  padding: 12px 16px;
  background: var(--el-color-danger-light-9);
  border: 1px solid var(--el-color-danger-light-7);
  border-radius: 8px;
  color: var(--el-color-danger);
  font-size: 13px;
  text-align: center;
}

/* ==================== 底部版权 ==================== */
.login-footer {
  text-align: center;
  margin-top: 28px;
  padding-top: 20px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.footer-copy {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.6;
}

.footer-copy :deep(a) {
  color: var(--el-color-primary);
  text-decoration: none;
}

.footer-registration {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

/* ==================== 响应式 ==================== */
@media (max-width: 640px) {
  .login-card {
    width: 100%;
    padding: 32px 24px 24px;
    border-radius: 16px;
  }

  .brand-name {
    font-size: 20px;
  }

  .particle-canvas {
    display: none;
  }

  .login-wrapper {
    padding: 16px;
  }
}

/* ==================== 无障碍：减少动画 ==================== */
@media (prefers-reduced-motion: reduce) {
  .login-card {
    animation: none;
  }

  .particle-canvas {
    display: none;
  }
}
</style>
