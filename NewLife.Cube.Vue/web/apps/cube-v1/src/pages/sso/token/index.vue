<template>
  <div class="sso-token-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('ssoToken.title') }}</h3>
          <el-button type="primary" @click="handleGetToken">{{ t('ssoToken.getToken') }}</el-button>
        </div>
      </template>

      <el-form :model="tokenForm" :rules="tokenRules" ref="tokenFormRef" label-width="120px">
        <el-form-item :label="t('ssoToken.clientId')" prop="client_id">
          <el-input v-model="tokenForm.client_id" :placeholder="t('ssoToken.clientIdHint')" />
        </el-form-item>
        <el-form-item :label="t('ssoToken.clientSecret')" prop="client_secret">
          <el-input
            v-model="tokenForm.client_secret"
            type="password"
            :placeholder="t('ssoToken.clientSecretHint')"
          />
        </el-form-item>
        <el-form-item :label="t('ssoToken.username')" prop="username">
          <el-input v-model="tokenForm.username" :placeholder="t('ssoToken.usernameHint')" />
        </el-form-item>
        <el-form-item :label="t('ssoToken.password')" prop="password">
          <el-input
            v-model="tokenForm.password"
            type="password"
            :placeholder="t('ssoToken.passwordHint')"
          />
        </el-form-item>
        <el-form-item :label="t('ssoToken.refreshToken')" prop="refresh_token">
          <el-input v-model="tokenForm.refresh_token" :placeholder="t('ssoToken.refreshTokenHint')" />
        </el-form-item>
        <el-form-item :label="t('ssoToken.grantType')" prop="grant_type">
          <el-select v-model="tokenForm.grant_type" :placeholder="t('ssoToken.grantTypeHint')">
            <el-option label="password" value="password" />
            <el-option label="client_credentials" value="client_credentials" />
            <el-option label="authorization_code" value="authorization_code" />
            <el-option label="refresh_token" value="refresh_token" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleGetToken" :loading="loading">
            {{ t('ssoToken.getToken') }}
          </el-button>
          <el-button @click="handlePostToken" :loading="loading">
            {{ t('ssoToken.postGetToken') }}
          </el-button>
          <el-button @click="resetForm">{{ t('button.reset') }}</el-button>
        </el-form-item>
      </el-form>

      <div v-if="tokenResult" class="token-result">
        <el-alert
          :title="tokenResult.success ? t('ssoToken.getSuccess') : t('ssoToken.getFailed')"
          :type="tokenResult.success ? 'success' : 'error'"
          :description="tokenResult.message"
          show-icon
          :closable="false"
        />

        <el-table v-if="tokenResult.data" :data="tokenResult.data" border style="width: 100%; margin-top: 20px;">
          <el-table-column prop="key" :label="t('ssoToken.field')" width="200" />
          <el-table-column prop="value" :label="t('ssoToken.value')" />
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { request } from '@newlifex/cube-vue/core/utils/request';
import type { FormInstance, FormRules } from 'element-plus';

// 定义Token参数类型
interface SsoTokenParams {
  client_id: string;
  client_secret: string;
  username: string;
  password: string;
  refresh_token: string;
  grant_type: string;
}

// 定义结果类型
interface TokenResult {
  success: boolean;
  message: string;
  data?: Array<{key: string, value: string}>;
}

// 表单引用
const tokenFormRef = ref<FormInstance | null>(null);
const { t } = useI18n();

// Token表单
const tokenForm = reactive<SsoTokenParams>({
  client_id: '',
  client_secret: '',
  username: '',
  password: '',
  refresh_token: '',
  grant_type: 'password',
});

// 表单验证规则
const tokenRules = reactive<FormRules>({
  client_id: [
    { required: true, message: t('ssoToken.clientIdRequired'), trigger: 'blur' }
  ],
  client_secret: [
    { required: true, message: t('ssoToken.clientSecretRequired'), trigger: 'blur' }
  ],
  grant_type: [
    { required: true, message: t('ssoToken.grantTypeRequired'), trigger: 'change' }
  ]
});

// 状态
const loading = ref(false);
const tokenResult = ref<TokenResult | null>(null);

// GET方式获取Token
const handleGetToken = async () => {
  if (!tokenFormRef.value) return;

  await tokenFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;

    loading.value = true;
    tokenResult.value = null;

    try {
      const response = await request.get('/Sso/Token', {
        params: {
          client_id: tokenForm.client_id,
          client_secret: tokenForm.client_secret,
          username: tokenForm.username || undefined,
          password: tokenForm.password || undefined,
          refresh_token: tokenForm.refresh_token || undefined,
          grant_type: tokenForm.grant_type,
        },
      });

      tokenResult.value = {
        success: true,
        message: t('ssoToken.getSuccess'),
        data: flattenObject(response),
      };
    } catch (error) {
      tokenResult.value = {
        success: false,
        message: `${t('ssoToken.getFailed')}: ${error}`,
      };
    } finally {
      loading.value = false;
    }
  });
};

// POST方式获取Token
const handlePostToken = async () => {
  if (!tokenFormRef.value) return;

  await tokenFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;

    loading.value = true;
    tokenResult.value = null;

    try {
      const response = await request.post('/Sso/Token', null, {
        params: {
          client_id: tokenForm.client_id,
          client_secret: tokenForm.client_secret,
          username: tokenForm.username || undefined,
          password: tokenForm.password || undefined,
          refresh_token: tokenForm.refresh_token || undefined,
          grant_type: tokenForm.grant_type,
        },
      });

      tokenResult.value = {
        success: true,
        message: t('ssoToken.getSuccess'),
        data: flattenObject(response),
      };
    } catch (error) {
      tokenResult.value = {
        success: false,
        message: `${t('ssoToken.getFailed')}: ${error}`,
      };
    } finally {
      loading.value = false;
    }
  });
};

// 重置表单
const resetForm = () => {
  if (tokenFormRef.value) {
    tokenFormRef.value.resetFields();
  }
  tokenResult.value = null;
};

// 扁平化对象用于表格显示
const flattenObject = (obj: unknown): Array<{key: string, value: string}> => {
  const result: Array<{key: string, value: string}> = [];

  if (!obj || typeof obj !== 'object') {
    return result;
  }

  const flatten = (current: Record<string, unknown>, prefix = '') => {
    Object.keys(current).forEach(key => {
      const value = current[key];
      const newKey = prefix ? `${prefix}.${key}` : key;

      if (value && typeof value === 'object' && !Array.isArray(value)) {
        flatten(value as Record<string, unknown>, newKey);
      } else {
        result.push({
          key: newKey,
          value: String(value),
        });
      }
    });
  };

  flatten(obj as Record<string, unknown>);
  return result;
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.token-result {
  margin-top: 20px;
}
</style>
