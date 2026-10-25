<template>
  <div class="sso-access-token-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('ssoAccessToken.title') }}</h3>
          <el-button type="primary" @click="handleGetAccessToken">
            {{ t('ssoAccessToken.getToken') }}
          </el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab" class="token-tabs">
        <!-- GET 请求 Tab -->
        <el-tab-pane :label="t('ssoAccessToken.tabGet')" name="get">
          <el-form :inline="true" :model="getForm" class="search-form">
            <el-form-item label="Client ID">
              <el-input
                v-model="getForm.client_id"
                :placeholder="t('ssoAccessToken.clientIdHint')"
                clearable
              />
            </el-form-item>
            <el-form-item label="Client Secret">
              <el-input
                v-model="getForm.client_secret"
                :placeholder="t('ssoAccessToken.clientSecretHint')"
                clearable
              />
            </el-form-item>
            <el-form-item label="Code">
              <el-input v-model="getForm.code" :placeholder="t('ssoAccessToken.codeHint')" clearable />
            </el-form-item>
            <el-form-item label="Grant Type">
              <el-select v-model="getForm.grant_type" :placeholder="t('ssoAccessToken.grantTypeHint')">
                <el-option label="authorization_code" value="authorization_code" />
                <el-option label="refresh_token" value="refresh_token" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleGetRequest">
                {{ t('ssoAccessToken.getRequest') }}
              </el-button>
              <el-button @click="resetGetForm">{{ t('button.reset') }}</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- POST 请求 Tab -->
        <el-tab-pane :label="t('ssoAccessToken.tabPost')" name="post">
          <el-form :model="postForm" label-width="120px" class="post-form">
            <el-form-item label="Client ID">
              <el-input
                v-model="postForm.client_id"
                :placeholder="t('ssoAccessToken.clientIdHint')"
              />
            </el-form-item>
            <el-form-item label="Client Secret">
              <el-input
                v-model="postForm.client_secret"
                :placeholder="t('ssoAccessToken.clientSecretHint')"
              />
            </el-form-item>
            <el-form-item label="Code">
              <el-input v-model="postForm.code" :placeholder="t('ssoAccessToken.codeHint')" />
            </el-form-item>
            <el-form-item label="Grant Type">
              <el-select
                v-model="postForm.grant_type"
                :placeholder="t('ssoAccessToken.grantTypeHint')"
              >
                <el-option label="authorization_code" value="authorization_code" />
                <el-option label="refresh_token" value="refresh_token" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handlePostRequest">
                {{ t('ssoAccessToken.postRequest') }}
              </el-button>
              <el-button @click="resetPostForm">{{ t('button.reset') }}</el-button>
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>

      <!-- Token 结果展示 -->
      <el-card v-if="tokenResult" class="result-card">
        <template #header>
          <h4>{{ t('ssoAccessToken.resultTitle') }}</h4>
        </template>
        <el-descriptions :column="1" border>
          <el-descriptions-item label="Access Token">
            <div class="token-display">
              <el-input v-model="tokenResult.access_token" readonly />
              <el-button
                type="primary"
                size="small"
                @click="copyToken(tokenResult.access_token)"
                class="copy-btn"
              >
                {{ t('common.copy') }}
              </el-button>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="Token Type">
            {{ tokenResult.token_type || 'Bearer' }}
          </el-descriptions-item>
          <el-descriptions-item label="Expires In">
            {{ tokenResult.expires_in ? t('ssoAccessToken.seconds', { n: tokenResult.expires_in }) : t('ssoAccessToken.unknown') }}
          </el-descriptions-item>
          <el-descriptions-item label="Refresh Token" v-if="tokenResult.refresh_token">
            <div class="token-display">
              <el-input v-model="tokenResult.refresh_token" readonly />
              <el-button
                type="success"
                size="small"
                @click="copyToken(tokenResult.refresh_token)"
                class="copy-btn"
              >
                {{ t('common.copy') }}
              </el-button>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="Scope">
            {{ tokenResult.scope || t('ssoAccessToken.unspecified') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('ssoAccessToken.obtainedAt')">
            {{ tokenResult.timestamp }}
          </el-descriptions-item>
        </el-descriptions>
      </el-card>

      <!-- Token 历史记录 -->
      <el-card v-if="tokenHistory.length > 0" class="history-card">
        <template #header>
          <h4>{{ t('ssoAccessToken.historyTitle') }}</h4>
        </template>
        <el-table :data="tokenHistory" border style="width: 100%">
          <el-table-column prop="client_id" label="Client ID" width="150" show-overflow-tooltip />
          <el-table-column prop="grant_type" label="Grant Type" width="150" />
          <el-table-column prop="access_token" label="Access Token" min-width="200" show-overflow-tooltip />
          <el-table-column prop="expires_in" :label="t('ssoAccessToken.expiresIn')" width="100" />
          <el-table-column prop="timestamp" :label="t('ssoAccessToken.obtainedAt')" width="160" />
          <el-table-column :label="t('common.actions')" width="120">
            <template #default="scope">
              <el-button type="primary" size="small" @click="useToken(scope.row)">
                {{ t('ssoAccessToken.use') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { ElMessage } from 'element-plus';

// 定义接口类型
interface AccessTokenParams {
  client_id?: string;
  client_secret?: string;
  code?: string;
  grant_type?: string;
}

interface TokenResult {
  access_token?: string;
  token_type?: string;
  expires_in?: number;
  refresh_token?: string;
  scope?: string;
  timestamp: string;
}

interface TokenHistoryItem extends TokenResult, AccessTokenParams {}

// 当前激活的 Tab
const activeTab = ref('get');
const { t } = useI18n();

// GET 请求表单
const getForm = reactive<AccessTokenParams>({
  client_id: '',
  client_secret: '',
  code: '',
  grant_type: 'authorization_code',
});

// POST 请求表单
const postForm = reactive<AccessTokenParams>({
  client_id: '',
  client_secret: '',
  code: '',
  grant_type: 'authorization_code',
});

// Token 结果
const tokenResult = ref<TokenResult | null>(null);
const tokenHistory = ref<TokenHistoryItem[]>([]);

// 处理 GET 请求
const handleGetRequest = async () => {
  if (!getForm.client_id || !getForm.client_secret) {
    ElMessage.warning(t('ssoAccessToken.clientRequired'));
    return;
  }

  try {
    const response = (await request.get('/Sso/Access_Token', {
      params: getForm,
    })) as unknown as TokenResult;

    const result: TokenResult = {
      access_token: response.access_token,
      token_type: response.token_type,
      expires_in: response.expires_in,
      refresh_token: response.refresh_token,
      scope: response.scope,
      timestamp: new Date().toLocaleString(),
    };

    tokenResult.value = result;

    // 添加到历史记录
    tokenHistory.value.unshift({
      ...result,
      ...getForm,
    });

    // 保持历史记录在10条以内
    if (tokenHistory.value.length > 10) {
      tokenHistory.value = tokenHistory.value.slice(0, 10);
    }

    ElMessage.success(t('ssoAccessToken.getSuccess'));
  } catch {
    ElMessage.error(t('ssoAccessToken.getFailed'));
  }
};

// 处理 POST 请求
const handlePostRequest = async () => {
  if (!postForm.client_id || !postForm.client_secret) {
    ElMessage.warning(t('ssoAccessToken.clientRequired'));
    return;
  }

  try {
    const response = (await request.post('/Sso/Access_Token', null, {
      params: postForm,
    })) as unknown as TokenResult;

    const result: TokenResult = {
      access_token: response.access_token,
      token_type: response.token_type,
      expires_in: response.expires_in,
      refresh_token: response.refresh_token,
      scope: response.scope,
      timestamp: new Date().toLocaleString(),
    };

    tokenResult.value = result;

    // 添加到历史记录
    tokenHistory.value.unshift({
      ...result,
      ...postForm,
    });

    // 保持历史记录在10条以内
    if (tokenHistory.value.length > 10) {
      tokenHistory.value = tokenHistory.value.slice(0, 10);
    }

    ElMessage.success(t('ssoAccessToken.getSuccess'));
  } catch {
    ElMessage.error(t('ssoAccessToken.getFailed'));
  }
};

// 获取 Access Token（通用方法）
const handleGetAccessToken = () => {
  if (activeTab.value === 'get') {
    handleGetRequest();
  } else {
    handlePostRequest();
  }
};

// 复制 Token
const copyToken = async (token?: string) => {
  if (!token) {
    ElMessage.warning(t('ssoAccessToken.tokenEmpty'));
    return;
  }

  try {
    await navigator.clipboard.writeText(token);
    ElMessage.success(t('ssoAccessToken.copied'));
  } catch {
    ElMessage.error(t('ssoAccessToken.copyFailed'));
  }
};

// 使用 Token（从历史记录中恢复）
const useToken = (item: TokenHistoryItem) => {
  tokenResult.value = {
    access_token: item.access_token,
    token_type: item.token_type,
    expires_in: item.expires_in,
    refresh_token: item.refresh_token,
    scope: item.scope,
    timestamp: item.timestamp,
  };

  // 同时恢复表单数据
  if (activeTab.value === 'get') {
    Object.assign(getForm, {
      client_id: item.client_id,
      client_secret: item.client_secret,
      code: item.code,
      grant_type: item.grant_type,
    });
  } else {
    Object.assign(postForm, {
      client_id: item.client_id,
      client_secret: item.client_secret,
      code: item.code,
      grant_type: item.grant_type,
    });
  }

  ElMessage.success(t('ssoAccessToken.restored'));
};

// 重置 GET 表单
const resetGetForm = () => {
  Object.assign(getForm, {
    client_id: '',
    client_secret: '',
    code: '',
    grant_type: 'authorization_code',
  });
};

// 重置 POST 表单
const resetPostForm = () => {
  Object.assign(postForm, {
    client_id: '',
    client_secret: '',
    code: '',
    grant_type: 'authorization_code',
  });
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.token-tabs {
  margin-bottom: 20px;
}

.search-form {
  margin-bottom: 20px;
}

.post-form {
  max-width: 600px;
}

.result-card {
  margin-top: 20px;
}

.token-display {
  display: flex;
  align-items: center;
  gap: 10px;
}

.copy-btn {
  flex-shrink: 0;
}

.history-card {
  margin-top: 20px;
}
</style>
