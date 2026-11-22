<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import type { LoginConfig, OAuthProvider } from '@newlifex/api-core';
import { cubeApi } from '@/api/cube';
import { useAuthStore } from '@/store/modules/auth';
import { useForm, useFormRules } from '@/hooks/common/form';
import { $t } from '@/locales';

defineOptions({ name: 'PwdLogin' });

const route = useRoute();
const authStore = useAuthStore();
const { formRef, validate } = useForm();

interface FormModel {
  userName: string;
  password: string;
  captchaCode: string;
}

const model = ref<FormModel>({
  userName: '',
  password: '',
  captchaCode: ''
});

const rememberMe = ref(true);

const loginConfig = ref<LoginConfig | null>(null);
const captcha = ref<{ captchaId: string; image: string } | null>(null);

const needCaptcha = computed(() => Boolean(loginConfig.value?.login?.captcha));

/** 后端是否开放密码登录（缺省视为开放） */
const showPasswordForm = computed(() => loginConfig.value?.login?.password !== false);

/** 已启用的第三方登录 */
const oauthProviders = computed<OAuthProvider[]>(() => loginConfig.value?.oauth ?? []);

/** 加载登录配置，按需拉取图片验证码 */
async function loadLoginConfig() {
  try {
    const res = await cubeApi.user.getLoginConfig();
    loginConfig.value = res.data ?? null;
  } catch {
    loginConfig.value = null;
  }

  if (needCaptcha.value) await refreshCaptcha();

  // 仅开放单一第三方登录时直接跳转
  if (showPasswordForm.value || oauthProviders.value.length !== 1) return;

  handleOAuth(oauthProviders.value[0].name);
}

function resolveLogo(logo?: string) {
  if (!logo) return '';
  if (/^https?:\/\//i.test(logo)) return logo;
  if (logo.startsWith('//')) return `${window.location.protocol}${logo}`;
  return logo;
}

function handleOAuth(name: string) {
  const redirect = (route.query.redirect as string) || '/';
  const redirectUri = `${window.location.origin}${redirect}`;
  window.location.href = `/Sso/Login/${name}?source=front-end&redirect_uri=${encodeURIComponent(redirectUri)}`;
}

async function refreshCaptcha() {
  model.value.captchaCode = '';
  try {
    const res = await cubeApi.user.getCaptcha();
    captcha.value = res.data ?? null;
  } catch {
    captcha.value = null;
  }
}

const rules = computed<Record<keyof FormModel, App.Global.FormRule[]>>(() => {
  const { formRules, createRequiredRule } = useFormRules();

  // 密码策略由魔方后端（CubeSetting）决定，前端只做非空校验，避免误拦合法密码
  return {
    userName: formRules.userName,
    password: [createRequiredRule($t('form.pwd.required'))],
    captchaCode: []
  };
});

async function handleSubmit() {
  await validate();

  if (needCaptcha.value && !model.value.captchaCode) {
    window.$message?.warning($t('page.login.common.captchaPlaceholder'));
    return;
  }

  await authStore.login(model.value.userName, model.value.password, {
    captchaId: captcha.value?.captchaId,
    captchaCode: model.value.captchaCode,
    remember: rememberMe.value
  });

  if (needCaptcha.value) await refreshCaptcha();
}

onMounted(loadLoginConfig);
</script>

<template>
  <ElForm
    v-if="showPasswordForm"
    ref="formRef"
    :model="model"
    :rules="rules"
    size="large"
    :show-label="false"
    @keyup.enter="handleSubmit"
  >
    <ElFormItem prop="userName">
      <ElInput v-model="model.userName" :placeholder="$t('page.login.common.userNamePlaceholder')" />
    </ElFormItem>
    <ElFormItem prop="password">
      <ElInput
        v-model="model.password"
        type="password"
        show-password
        :placeholder="$t('page.login.common.passwordPlaceholder')"
      />
    </ElFormItem>
    <ElFormItem v-if="needCaptcha" prop="captchaCode">
      <div class="w-full flex gap-8px">
        <ElInput v-model="model.captchaCode" :placeholder="$t('page.login.common.captchaPlaceholder')" />
        <div
          class="h-38px min-w-100px cursor-pointer flex-center overflow-hidden rd-4px border border-solid border-#d9d9d9 px-4px"
          title="点击刷新"
          v-html="captcha?.image"
          @click="refreshCaptcha"
        />
      </div>
    </ElFormItem>
    <ElSpace direction="vertical" :size="24" class="w-full" fill>
      <div class="flex-y-center justify-between">
        <ElCheckbox v-model="rememberMe">{{ $t('page.login.pwdLogin.rememberMe') }}</ElCheckbox>
      </div>
      <ElButton type="primary" size="large" round class="w-full" :loading="authStore.loginLoading" @click="handleSubmit">
        {{ $t('common.confirm') }}
      </ElButton>
    </ElSpace>
  </ElForm>

  <div v-if="oauthProviders.length" class="mt-24px">
    <ElDivider class="!my-0 text-12px text-[var(--el-text-color-secondary)]">
      {{ $t('page.login.pwdLogin.otherLoginMode') }}
    </ElDivider>
    <div class="mt-16px flex flex-wrap justify-center gap-16px">
      <ElTooltip
        v-for="item in oauthProviders"
        :key="item.name"
        :content="item.nickName || item.name"
        placement="top"
      >
        <ElButton
          class="size-44px !p-0"
          :aria-label="item.nickName || item.name"
          @click="handleOAuth(item.name)"
        >
          <img v-if="item.logo" :src="resolveLogo(item.logo)" class="size-24px object-contain" alt="" />
          <span v-else class="text-16px">{{ (item.nickName || item.name).slice(0, 1) }}</span>
        </ElButton>
      </ElTooltip>
    </div>
  </div>
</template>
