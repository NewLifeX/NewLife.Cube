<template>
  <div class="sso-login-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>SSO 登录</h3>
          <el-button type="primary" @click="handleLogin">执行登录</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="loginForm" class="search-form">
        <el-form-item label="登录名">
          <el-input v-model="loginForm.name" placeholder="请输入登录名" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleLogin">登录</el-button>
          <el-button @click="resetForm">重置</el-button>
        </el-form-item>
      </el-form>

      <div v-if="loginResult" class="login-result">
        <el-alert
          :title="loginResult.success ? '登录成功' : '登录失败'"
          :type="loginResult.success ? 'success' : 'error'"
          :description="loginResult.message"
          show-icon
          :closable="false"
        />

        <el-table v-if="loginResult.data" :data="[loginResult.data]" border style="width: 100%; margin-top: 20px;">
          <el-table-column prop="key" label="字段" width="150" />
          <el-table-column prop="value" label="值" />
        </el-table>
      </div>

      <div v-if="loading" class="loading">
        <el-loading-directive v-loading="loading" />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { request } from 'cube-front/core/utils/request';

// 定义登录参数类型
interface SsoLoginParams {
  name: string;
}

// 定义登录结果类型
interface LoginResult {
  success: boolean;
  message: string;
  data?: Record<string, unknown>;
}

// 登录表单
const loginForm = reactive<SsoLoginParams>({
  name: '',
});

// 状态
const loading = ref(false);
const loginResult = ref<LoginResult | null>(null);

// 执行登录
const handleLogin = async () => {
  if (!loginForm.name) {
    loginResult.value = {
      success: false,
      message: '请输入登录名',
    };
    return;
  }

  loading.value = true;
  loginResult.value = null;

  try {
    const response = await request.get('/Sso/Login', {
      params: {
        name: loginForm.name,
      },
    });

    // 处理响应数据
    if (response) {
      loginResult.value = {
        success: true,
        message: '登录请求成功',
        data: flattenObject(response),
      };
    } else {
      loginResult.value = {
        success: false,
        message: '登录响应为空',
      };
    }
  } catch (error) {
    loginResult.value = {
      success: false,
      message: `登录失败: ${error}`,
    };
  } finally {
    loading.value = false;
  }
};

// 重置表单
const resetForm = () => {
  loginForm.name = '';
  loginResult.value = null;
};

// 扁平化对象用于表格显示
const flattenObject = (obj: Record<string, unknown>): Array<{key: string, value: string}> => {
  const result: Array<{key: string, value: string}> = [];

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

  flatten(obj);
  return result;
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}

.login-result {
  margin-top: 20px;
}

.loading {
  text-align: center;
  padding: 20px;
}
</style>
