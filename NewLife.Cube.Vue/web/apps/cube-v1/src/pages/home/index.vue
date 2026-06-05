<template>
  <div class="home-container">
    <el-card class="welcome-card">
      <template #header>
        <h1>Cube V1 API 管理系统</h1>
        <p>基于 v1_OpenAPI.json 自动生成的 API 管理界面</p>
      </template>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-card class="api-group-card">
            <template #header>
              <h3>🎯 Cube API</h3>
            </template>
            <div class="api-list">
              <el-button
                v-for="api in cubeApis"
                :key="api.name"
                type="primary"
                @click="navigateToApi(api.path)"
                class="api-button"
              >
                {{ api.name }}
              </el-button>
            </div>
          </el-card>
        </el-col>

        <el-col :span="12">
          <el-card class="api-group-card">
            <template #header>
              <h3>🔐 SSO API</h3>
            </template>
            <div class="api-list">
              <el-button
                v-for="api in ssoApis"
                :key="api.name"
                type="success"
                @click="navigateToApi(api.path)"
                class="api-button"
              >
                {{ api.name }}
              </el-button>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- 统计信息 -->
    <el-card class="stats-card">
      <template #header>
        <h3>📊 统计信息</h3>
      </template>
      <el-row :gutter="20">
        <el-col :span="6">
          <el-statistic title="Cube API 数量" :value="cubeApis.length" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="SSO API 数量" :value="ssoApis.length" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="总 API 数量" :value="cubeApis.length + ssoApis.length" />
        </el-col>
        <el-col :span="6">
          <el-statistic title="页面数量" :value="cubeApis.length + ssoApis.length" />
        </el-col>
      </el-row>
    </el-card>

    <!-- 最近访问 -->
    <el-card class="recent-card" v-if="recentVisited.length > 0">
      <template #header>
        <h3>🕒 最近访问</h3>
      </template>
      <div class="recent-list">
        <el-tag
          v-for="item in recentVisited"
          :key="item.path"
          @click="navigateToApi(item.path)"
          class="recent-tag"
          type="info"
        >
          {{ item.name }}
        </el-tag>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';

interface ApiItem {
  name: string;
  path: string;
  method: string;
  description: string;
}

const router = useRouter();

// Cube API 列表
const cubeApis = reactive<ApiItem[]>([
  { name: 'Cube 信息', path: '/Cube/Info', method: 'GET', description: '获取 Cube 系统信息' },
  { name: 'API 列表', path: '/Cube/Apis', method: 'GET', description: '获取所有可用的 API 列表' },
  { name: '用户搜索', path: '/Cube/UserSearch', method: 'GET', description: '搜索用户信息' },
  { name: '部门搜索', path: '/Cube/DepartmentSearch', method: 'GET', description: '搜索部门信息' },
  { name: '获取区域', path: '/Cube/GetArea', method: 'GET', description: '获取指定区域信息' },
  { name: '区域子节点', path: '/Cube/AreaChilds', method: 'GET', description: '获取区域的子节点' },
  { name: '区域父节点', path: '/Cube/AreaParents', method: 'GET', description: '获取区域的父节点' },
  { name: '区域所有父节点', path: '/Cube/AreaAllParents', method: 'GET', description: '获取区域的所有父节点' },
  { name: '头像管理', path: '/Cube/Avatar', method: 'GET', description: '获取用户头像' },
  { name: '查找配置', path: '/Cube/Lookup', method: 'GET', description: '查找系统配置' },
  { name: '保存布局', path: '/Cube/SaveLayout', method: 'POST', description: '保存页面布局配置' },
  { name: '获取页面配置', path: '/Cube/GetPageConfig', method: 'GET', description: '获取页面配置' },
  { name: '设置页面配置', path: '/Cube/SetPageConfig', method: 'POST', description: '设置页面配置' },
  { name: '图片管理', path: '/Cube/Image', method: 'GET', description: '图片资源管理' },
  { name: '文件管理', path: '/Cube/File', method: 'GET', description: '文件资源管理' },
]);

// SSO API 列表
const ssoApis = reactive<ApiItem[]>([
  { name: 'SSO 登录', path: '/Sso/Login', method: 'GET', description: 'SSO 登录入口' },
  { name: '登录信息', path: '/Sso/LoginInfo', method: 'GET', description: '获取登录信息' },
  { name: 'SSO 登出', path: '/Sso/Logout', method: 'GET', description: 'SSO 登出' },
  { name: '绑定账户', path: '/Sso/Bind', method: 'GET', description: '绑定 SSO 账户' },
  { name: '解绑账户', path: '/Sso/UnBind', method: 'GET', description: '解绑 SSO 账户' },
  { name: '授权认证', path: '/Sso/Authorize', method: 'GET', description: 'OAuth 授权认证' },
  { name: 'Auth2 认证', path: '/Sso/Auth2', method: 'GET', description: 'Auth2 认证' },
  { name: 'Access Token', path: '/Sso/Access_Token', method: 'GET/POST', description: '获取访问令牌' },
  { name: 'Token 管理', path: '/Sso/Token', method: 'GET/POST', description: 'Token 管理' },
  { name: '密码令牌', path: '/Sso/PasswordToken', method: 'GET/POST', description: '密码令牌管理' },
  { name: '用户信息', path: '/Sso/UserInfo', method: 'GET', description: '获取用户信息' },
  { name: '刷新令牌', path: '/Sso/Refresh_Token', method: 'GET/POST', description: '刷新访问令牌' },
  { name: '认证验证', path: '/Sso/Auth', method: 'GET', description: '认证验证' },
  { name: '获取密钥', path: '/Sso/GetKey', method: 'GET', description: '获取加密密钥' },
  { name: '验证令牌', path: '/Sso/Verify', method: 'GET', description: '验证令牌有效性' },
  { name: '用户认证', path: '/Sso/UserAuth', method: 'GET/POST', description: '用户认证' },
  { name: 'SSO 头像', path: '/Sso/Avatar', method: 'GET', description: 'SSO 用户头像' },
]);

// 最近访问记录
const recentVisited = ref<ApiItem[]>([]);

// 导航到 API 页面
const navigateToApi = (path: string) => {
  // 找到对应的 API 信息
  const api = [...cubeApis, ...ssoApis].find(item => item.path === path);
  if (api) {
    // 添加到最近访问记录
    const existingIndex = recentVisited.value.findIndex(item => item.path === path);
    if (existingIndex > -1) {
      recentVisited.value.splice(existingIndex, 1);
    }
    recentVisited.value.unshift(api);

    // 限制最近访问记录数量
    if (recentVisited.value.length > 10) {
      recentVisited.value = recentVisited.value.slice(0, 10);
    }
  }

  // 导航到页面
  router.push(path);
};
</script>

<style scoped>
.home-container {
  max-width: 1200px;
  margin: 0 auto;
}

.welcome-card {
  margin-bottom: 20px;
}

.welcome-card h1 {
  color: #409eff;
  margin-bottom: 10px;
}

.welcome-card p {
  color: #666;
  margin: 0;
}

.api-group-card {
  height: 400px;
  margin-bottom: 20px;
}

.api-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-height: 320px;
  overflow-y: auto;
}

.api-button {
  width: 100%;
  justify-content: flex-start;
  text-align: left;
}

.stats-card {
  margin-bottom: 20px;
}

.recent-card {
  margin-bottom: 20px;
}

.recent-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.recent-tag {
  cursor: pointer;
  transition: all 0.3s;
}

.recent-tag:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

/* 滚动条样式 */
.api-list::-webkit-scrollbar {
  width: 6px;
}

.api-list::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 3px;
}

.api-list::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 3px;
}

.api-list::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}
</style>
