<!--
  个人中心 — 当前登录用户的资料总览（只读）

  展示 /Auth/Info 返回的当前用户信息：头像 / 显示名 / 登录名 / 角色 / 在线状态、基本资料、
  邮箱手机验证状态、登录与注册统计。安全相关（改密码、验证/更换邮箱手机）跳转安全中心。
  数据在挂载时直接拉 /Auth/Info（与 ProfileSecurity 同范式），不依赖导航守卫是否已填充 store。
-->
<template>
  <div v-loading="loading" class="profile-page">
    <h2 class="page-title">{{ t('profile.title') }}</h2>

    <!-- 常规整页：单卡片内分段，段间用标题 + 分隔线 -->
    <el-card class="profile-card" shadow="never">
      <!-- 头部：头像 + 显示名 + 登录名 + 角色 + 在线状态 + 安全中心入口 -->
      <div class="header-row">
        <div class="header-avatar">
          <img
            v-if="avatarUrl && !avatarError"
            :src="avatarUrl"
            :alt="displayName"
            class="header-avatar-img"
            @error="avatarError = true"
          />
          <span v-else class="header-avatar-initial">{{ initial }}</span>
        </div>
        <div class="header-info">
          <div class="header-name">{{ displayName }}</div>
          <div class="header-sub">
            <span class="header-account">{{ info?.name || '-' }}</span>
            <el-tag v-if="info?.roleNames" size="small" type="info">{{ info.roleNames }}</el-tag>
            <el-tag :type="info?.online ? 'success' : 'info'" size="small">
              {{ info?.online ? t('profile.online') : t('profile.offline') }}
            </el-tag>
          </div>
        </div>
        <div class="header-actions">
          <el-button type="primary" plain @click="goSecurity">
            {{ t('profile.securityManage') }}
          </el-button>
        </div>
      </div>

      <!-- 基本信息 -->
      <el-divider />
      <h3 class="section-title">{{ t('profile.basic') }}</h3>
      <el-descriptions :column="2" :label-width="'90px'">
        <el-descriptions-item :label="t('common.name')">
          {{ info?.name || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.displayName')">
          {{ info?.displayName || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.role')">
          {{ info?.roleNames || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('common.status')">
          {{ info?.enable == null ? '-' : (info.enable ? t('profile.enabled') : t('profile.disabled')) }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 联系方式（只读，管理跳转安全中心） -->
      <h3 class="section-title">{{ t('profile.contact') }}</h3>
      <el-descriptions :column="1" :label-width="'90px'">
        <el-descriptions-item :label="t('security.mail')">
          <div class="contact-cell">
            <span>{{ info?.mail || t('security.unbound') }}</span>
            <el-tag :type="info?.mailVerified ? 'success' : 'warning'" size="small">
              {{ info?.mailVerified ? t('security.verified') : t('security.unverified') }}
            </el-tag>
          </div>
        </el-descriptions-item>
        <el-descriptions-item :label="t('security.mobile')">
          <div class="contact-cell">
            <span>{{ info?.mobile || t('security.unbound') }}</span>
            <el-tag :type="info?.mobileVerified ? 'success' : 'warning'" size="small">
              {{ info?.mobileVerified ? t('security.verified') : t('security.unverified') }}
            </el-tag>
          </div>
        </el-descriptions-item>
      </el-descriptions>

      <!-- 登录信息 -->
      <h3 class="section-title">{{ t('profile.loginInfo') }}</h3>
      <el-descriptions :column="2" :label-width="'90px'">
        <el-descriptions-item :label="t('profile.logins')">
          {{ info?.logins ?? '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.lastLogin')">
          {{ info?.lastLogin || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.lastLoginIP')">
          {{ info?.lastLoginIP || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.registerTime')">
          {{ info?.registerTime || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('profile.registerIP')">
          {{ info?.registerIP || '-' }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '../utils/request';
import { resolveAssetUrl } from '../utils/url';

/** 个人中心展示所需的用户信息子集（对齐 /Auth/Info 返回的 UserInfo） */
interface ProfileInfo {
  id?: number;
  name?: string;
  displayName?: string;
  roleNames?: string;
  online?: boolean;
  enable?: boolean;
  mail?: string;
  mobile?: string;
  mailVerified?: boolean;
  mobileVerified?: boolean;
  logins?: number;
  lastLogin?: string;
  lastLoginIP?: string;
  registerTime?: string;
  registerIP?: string;
}

const { t } = useI18n();
const router = useRouter();

/** 当前用户信息 */
const info = ref<ProfileInfo | null>(null);
/** 加载中 */
const loading = ref(false);
/** 头像加载失败标志（回退首字母） */
const avatarError = ref(false);

const displayName = computed(
  () => info.value?.displayName || info.value?.name || t('userMenu.user'),
);
const initial = computed(() => Array.from(displayName.value)[0]?.toUpperCase() || '?');
// 头像地址后端以 / 开头返回时统一拼接 baseUrl，口径与 UserProfile 组件一致
const avatarUrl = computed(() =>
  info.value?.id ? resolveAssetUrl(`/Cube/Avatar/${info.value.id}`) : '',
);

/** 跳转安全中心（改密码 / 验证更换邮箱手机） */
function goSecurity(): void {
  router.push('/profile/security').catch(() => {});
}

/** 拉取当前用户信息（只读展示；挂载即取，独立于导航守卫） */
async function fetchInfo(): Promise<void> {
  loading.value = true;
  try {
    const res = (await request.get('/Auth/Info')) as unknown as {
      code?: number;
      data?: ProfileInfo;
    };
    if (res?.code === 0 && res.data) {
      info.value = res.data;
    } else {
      ElMessage.error(t('profile.loadFailed'));
    }
  } catch (err: unknown) {
    console.error('Failed to load profile:', err);
    ElMessage.error(t('profile.loadFailed'));
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  fetchInfo();
});
</script>

<style scoped>
.profile-page {
  max-width: 860px;
  margin: 0 auto;
  padding: 24px 16px;
}

.page-title {
  margin: 0 0 16px;
  font-size: 22px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.profile-card {
  border-radius: 12px;
}

/* ───────── 头部 ───────── */
.header-row {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-avatar {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  overflow: hidden;
  flex-shrink: 0;
  background: linear-gradient(
    135deg,
    var(--el-color-primary) 0%,
    var(--el-color-primary-dark-2) 100%
  );
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.header-avatar-initial {
  font-size: 26px;
  font-weight: 700;
  color: var(--el-color-white);
}

.header-info {
  flex: 1;
  min-width: 0;
}

.header-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.header-sub {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
}

.header-account {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.header-actions {
  flex-shrink: 0;
}

/* ───────── 分段标题 ───────── */
.section-title {
  margin: 20px 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.profile-card :deep(.el-divider--horizontal) {
  margin: 20px 0 4px;
}

/* ───────── 联系方式 ───────── */
.contact-cell {
  /* inline-flex：避免块级 flex 把 descriptions 的 label/内容挤成两行 */
  display: inline-flex;
  align-items: center;
  gap: 10px;
}
</style>
