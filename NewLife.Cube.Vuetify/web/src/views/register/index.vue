<template>
  <div class="register-page">
    <v-card width="460" class="register-card pa-6">
      <v-card-title class="text-center text-h5 font-weight-bold">注册账号</v-card-title>
      <v-card-text>
        <v-alert v-if="loadError" type="error" variant="tonal" density="compact" class="mb-4">
          {{ loadError }}
          <div class="mt-2">
            <v-btn size="small" variant="text" color="primary" @click="loadConfig">重试</v-btn>
          </div>
        </v-alert>

        <v-progress-linear v-else-if="configLoading" indeterminate class="mb-4" />

        <v-alert v-else-if="!registerAvailable" type="warning" variant="tonal" density="compact" class="mb-4">
          系统未开放注册，请联系管理员。
        </v-alert>

        <template v-else-if="pending">
          <v-alert type="success" variant="tonal" class="mb-4">{{ pending.message }}</v-alert>
          <v-alert v-if="pending.targets.length" type="info" variant="tonal" density="compact" class="mb-4">
            激活信息已发送至：{{ pending.targets.join('、') }}
          </v-alert>
          <v-alert v-else-if="pendingChannelText" type="info" variant="tonal" density="compact" class="mb-4">
            请查收{{ pendingChannelText }}中的激活信息。
          </v-alert>
          <v-btn block color="primary" size="large" @click="goLogin">返回登录</v-btn>
        </template>

        <template v-else>
          <v-alert v-if="notice" :type="notice.type" variant="tonal" density="compact" closable class="mb-4" @click:close="notice = null">
            {{ notice.text }}
          </v-alert>

          <v-tabs v-if="modes.length > 1" v-model="mode" density="compact" class="mb-4">
            <v-tab v-for="item in modes" :key="item" :value="item">{{ modeLabels[item] }}</v-tab>
          </v-tabs>
          <div v-else class="text-subtitle-2 text-medium-emphasis mb-4">{{ modeLabels[mode] }}</div>

          <v-form @submit.prevent="handleSubmit">
            <v-text-field
              v-model="form.username"
              :label="usernameLabel"
              prepend-inner-icon="mdi-account"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.username"
              @update:model-value="clearError('username')"
            />

            <v-text-field
              v-if="showEmail"
              v-model="form.email"
              :label="emailLabel"
              type="email"
              prepend-inner-icon="mdi-email-outline"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.email"
              @update:model-value="clearError('email')"
            />

            <v-text-field
              v-if="showMobile"
              v-model="form.mobile"
              :label="mobileLabel"
              prepend-inner-icon="mdi-cellphone"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.mobile"
              @update:model-value="clearError('mobile')"
            />

            <v-text-field
              v-if="mode !== 'password'"
              v-model="form.code"
              :label="codeLabel"
              prepend-inner-icon="mdi-shield-key-outline"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.code"
              @update:model-value="clearError('code')"
            >
              <template #append-inner>
                <v-btn
                  type="button"
                  variant="text"
                  size="small"
                  color="primary"
                  :disabled="countdown > 0 || sending"
                  :loading="sending"
                  @click="handleSendCode"
                >
                  {{ countdown > 0 ? `${countdown}s` : '发送验证码' }}
                </v-btn>
              </template>
            </v-text-field>

            <v-text-field
              v-model="form.password"
              :label="passwordLabel"
              type="password"
              prepend-inner-icon="mdi-lock"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.password"
              @update:model-value="clearError('password')"
            />

            <v-text-field
              v-model="form.confirmPassword"
              label="确认密码"
              type="password"
              prepend-inner-icon="mdi-lock-check"
              variant="outlined"
              density="comfortable"
              class="mb-2"
              :error-messages="errors.confirmPassword"
              @update:model-value="clearError('confirmPassword')"
            />

            <template v-if="captchaRequired">
              <div class="d-flex align-center ga-2 mb-2">
                <v-img
                  v-if="captcha?.image"
                  :src="captcha.image"
                  width="120"
                  height="40"
                  contain
                  class="rounded"
                  alt="图片验证码"
                />
                <v-btn
                  type="button"
                  variant="text"
                  size="small"
                  color="primary"
                  prepend-icon="mdi-refresh"
                  :loading="captchaLoading"
                  @click="refreshCaptcha"
                >
                  刷新
                </v-btn>
              </div>
              <v-text-field
                v-model="form.captchaCode"
                label="图片验证码"
                prepend-inner-icon="mdi-image-outline"
                variant="outlined"
                density="comfortable"
                class="mb-4"
                :error-messages="errors.captchaCode"
                @update:model-value="clearError('captchaCode')"
              />
            </template>

            <v-btn type="submit" color="primary" block size="large" :loading="submitting">
              {{ mode === 'password' ? '立即注册' : '注册并登录' }}
            </v-btn>
          </v-form>
        </template>
      </v-card-text>

      <v-card-text class="text-center pt-0">
        <span class="text-body-2 text-medium-emphasis">已有账号？</span>
        <v-btn variant="text" color="primary" @click="goLogin">去登录</v-btn>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { LoginConfig } from '@newlifex/cube';
import { AuthLogic } from '@newlifex/cube';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import {
  buildRegisterPayload,
  getRegisterErrorMessage,
  isValidEmail,
  isValidMobile,
  resolveRegisterModes,
  resolveRegisterTenant,
  validateRegister,
  type RegisterFormErrors,
  type RegisterFormInput,
  type RegisterMode,
} from '@/utils/register';

type AlertType = 'success' | 'info' | 'warning' | 'error';

interface Notice {
  type: AlertType;
  text: string;
}

interface PendingResult {
  message: string;
  channels: string[];
  targets: string[];
}

const modeLabels: Record<RegisterMode, string> = {
  password: '账号注册',
  mobile: '手机注册',
  mail: '邮箱注册',
};

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();

const config = ref<LoginConfig | null>(null);
const configLoading = ref(true);
const loadError = ref('');
const mode = ref<RegisterMode>('password');
const form = reactive<RegisterFormInput>({
  username: '',
  email: '',
  mobile: '',
  password: '',
  confirmPassword: '',
  code: '',
  captchaCode: '',
});
const errors = ref<RegisterFormErrors>({});
const submitting = ref(false);
const sending = ref(false);
const countdown = ref(0);
const notice = ref<Notice | null>(null);
const pending = ref<PendingResult | null>(null);
const captcha = ref<{ captchaId: string; image: string } | null>(null);
const captchaLoading = ref(false);

// 复用共享认证逻辑：AuthLogic.register 已封装 Challenge 场景之外的注册请求、
// 令牌写入与 captcha/tenant 参数透传，皮肤只负责表单与反馈。
const authLogic = new AuthLogic(cubeApi, () => {});
const tenant = resolveRegisterTenant(route.query as Record<string, unknown>);
let timer: ReturnType<typeof setInterval> | null = null;

const modes = computed(() => resolveRegisterModes(config.value?.register));
const registerAvailable = computed(() => modes.value.length > 0);
const registerCaptchaRequired = computed(() => config.value?.register?.captcha === true);
const sendCodeCaptchaRequired = computed(() => config.value?.login?.sendCode === true);
const captchaRequired = computed(() => registerCaptchaRequired.value || sendCodeCaptchaRequired.value);
const requireMailVerify = computed(() => config.value?.register?.requireMailVerify === true);
const requireMobileVerify = computed(() => config.value?.register?.requireMobileVerify === true);
const showEmail = computed(() => mode.value !== 'mobile' || requireMailVerify.value);
const showMobile = computed(() => mode.value !== 'mail' || requireMobileVerify.value);
const usernameLabel = computed(() => (mode.value === 'password' ? '用户名' : '用户名（可选）'));
const emailLabel = computed(() => (mode.value === 'mail' || requireMailVerify.value ? '邮箱 *' : '邮箱（可选）'));
const mobileLabel = computed(() => (mode.value === 'mobile' || requireMobileVerify.value ? '手机号 *' : '手机号（可选）'));
const passwordLabel = computed(() => (mode.value === 'password' ? '密码 *' : '密码（可选）'));
const codeLabel = computed(() => (mode.value === 'mobile' ? '手机验证码' : '邮箱验证码'));
const pendingChannelText = computed(() => {
  const labels: Record<string, string> = { mail: '邮箱', sms: '手机短信' };
  return pending.value?.channels.map((item) => labels[item] ?? item).join('、') ?? '';
});

onMounted(loadConfig);
onUnmounted(stopCountdown);

/** 加载登录/注册配置，按租户参数对齐后端能力 */
async function loadConfig() {
  configLoading.value = true;
  loadError.value = '';
  try {
    const res = await cubeApi.user.getLoginConfig(tenant.tenantCode);
    config.value = res.data ?? null;
    if (modes.value.length > 0 && !modes.value.includes(mode.value)) mode.value = modes.value[0];
    if (captchaRequired.value) await refreshCaptcha();
  } catch (error) {
    loadError.value = getRegisterErrorMessage(error);
  } finally {
    configLoading.value = false;
  }
}

/** 刷新图片验证码 */
async function refreshCaptcha() {
  captchaLoading.value = true;
  try {
    const res = await cubeApi.user.getCaptcha();
    captcha.value = res.data ? { captchaId: res.data.captchaId, image: res.data.image } : null;
    form.captchaCode = '';
    clearError('captchaCode');
  } catch {
    captcha.value = null;
    notice.value = { type: 'error', text: '图片验证码加载失败，请点击刷新重试' };
  } finally {
    captchaLoading.value = false;
  }
}

/** 发送手机/邮箱注册验证码 */
async function handleSendCode() {
  if (sending.value || countdown.value > 0) return;

  const isSms = mode.value === 'mobile';
  const account = (isSms ? form.mobile : form.email).trim();
  if (!account) {
    errors.value = { ...errors.value, [isSms ? 'mobile' : 'email']: isSms ? '请输入手机号' : '请输入邮箱' };
    return;
  }
  if (isSms && !isValidMobile(account)) {
    errors.value = { ...errors.value, mobile: '手机号格式不正确' };
    return;
  }
  if (!isSms && !isValidEmail(account)) {
    errors.value = { ...errors.value, email: '邮箱格式不正确' };
    return;
  }
  if (sendCodeCaptchaRequired.value) {
    if (!captcha.value?.captchaId) {
      errors.value = { ...errors.value, captchaCode: '图片验证码加载失败，请刷新' };
      return;
    }
    if (!form.captchaCode.trim()) {
      errors.value = { ...errors.value, captchaCode: '请输入图片验证码' };
      return;
    }
  }

  sending.value = true;
  try {
    // 发码需要按 login.sendCode 透传图片验证码，AuthLogic/RegisterLogic 当前未覆盖该场景，
    // 因此这里直接调用 api-core 的 sendCode；注册提交仍统一走 AuthLogic.register。
    await cubeApi.user.sendCode({
      channel: isSms ? 'Sms' : 'Mail',
      username: account,
      action: 'register',
      ...(sendCodeCaptchaRequired.value && captcha.value
        ? { captchaId: captcha.value.captchaId, captchaCode: form.captchaCode.trim() }
        : {}),
    });
    notice.value = { type: 'success', text: '验证码已发送，请查收' };
    startCountdown();
    if (sendCodeCaptchaRequired.value) await refreshCaptcha();
  } catch (error) {
    notice.value = { type: 'error', text: getRegisterErrorMessage(error) };
    if (sendCodeCaptchaRequired.value) await refreshCaptcha();
  } finally {
    sending.value = false;
  }
}

/** 提交注册 */
async function handleSubmit() {
  if (submitting.value) return;

  const result = validateRegister(form, config.value, mode.value, {
    captchaRequired: registerCaptchaRequired.value,
    captchaId: registerCaptchaRequired.value ? captcha.value?.captchaId : undefined,
  });
  errors.value = result.errors;
  if (!result.valid) {
    notice.value = { type: 'warning', text: '请检查输入信息' };
    return;
  }

  submitting.value = true;
  notice.value = null;
  const payload = buildRegisterPayload(form, mode.value, {
    captchaId: registerCaptchaRequired.value ? captcha.value?.captchaId : undefined,
    captchaCode: registerCaptchaRequired.value ? form.captchaCode : undefined,
    appId: tenant.appId,
    tenantCode: tenant.tenantCode,
  });

  try {
    const res = await authLogic.register(payload);

    // 待激活：后端已发送激活邮件/短信，不返回 token
    if (res.data?.pendingActivation) {
      pending.value = {
        message: res.message || '注册成功，请先激活邮箱/手机后再登录',
        channels: res.data.channels ?? [],
        targets: res.data.targets ?? [],
      };
      return;
    }

    // 注册成功并已登录
    if (res.data?.accessToken) {
      notice.value = { type: 'success', text: res.message || '注册成功，正在进入系统...' };
      await userStore.fetchUserInfo().catch(() => undefined);
      await userStore.fetchMenus().catch(() => undefined);
      await router.replace('/home');
      return;
    }

    notice.value = { type: 'success', text: res.message || '注册成功，请登录' };
    await goLogin();
  } catch (error) {
    notice.value = { type: 'error', text: getRegisterErrorMessage(error) };
    if (registerCaptchaRequired.value) await refreshCaptcha();
  } finally {
    submitting.value = false;
  }
}

/** 返回登录页，保留租户查询参数 */
async function goLogin() {
  await router.push({
    path: '/login',
    query: tenant.tenantCode ? { tenant: tenant.tenantCode } : undefined,
  });
}

/** 清除单个字段错误 */
function clearError(field: keyof RegisterFormErrors) {
  if (!errors.value[field]) return;
  errors.value = { ...errors.value, [field]: undefined };
}

/** 启动发码倒计时 */
function startCountdown(seconds = 60) {
  stopCountdown();
  countdown.value = seconds;
  timer = setInterval(() => {
    countdown.value -= 1;
    if (countdown.value <= 0) stopCountdown();
  }, 1000);
}

/** 停止发码倒计时 */
function stopCountdown() {
  if (timer) {
    clearInterval(timer);
    timer = null;
  }
  if (countdown.value < 0) countdown.value = 0;
}
</script>

<style scoped>
.register-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: 24px;
  background: linear-gradient(135deg, #1867c0 0%, #5cbbf6 100%);
}

.register-card {
  max-width: 100%;
}
</style>
