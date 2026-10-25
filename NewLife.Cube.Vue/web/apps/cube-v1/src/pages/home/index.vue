<template>
  <div class="home-container">
    <el-card class="welcome-card">
      <template #header>
        <h1>{{ t('apiHome.title') }}</h1>
        <p>{{ t('apiHome.subtitle') }}</p>
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
                :key="api.nameKey"
                type="primary"
                @click="navigateToApi(api.path)"
                class="api-button"
              >
                {{ t(api.nameKey) }}
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
                :key="api.nameKey"
                type="success"
                @click="navigateToApi(api.path)"
                class="api-button"
              >
                {{ t(api.nameKey) }}
              </el-button>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- 统计信息 -->
    <el-card class="stats-card">
      <template #header>
        <h3>{{ t('apiHome.statsTitle') }}</h3>
      </template>
      <el-row :gutter="20">
        <el-col :span="6">
          <el-statistic :title="t('apiHome.cubeApiCount')" :value="cubeApis.length" />
        </el-col>
        <el-col :span="6">
          <el-statistic :title="t('apiHome.ssoApiCount')" :value="ssoApis.length" />
        </el-col>
        <el-col :span="6">
          <el-statistic
            :title="t('apiHome.totalApiCount')"
            :value="cubeApis.length + ssoApis.length"
          />
        </el-col>
        <el-col :span="6">
          <el-statistic
            :title="t('apiHome.pageCount')"
            :value="cubeApis.length + ssoApis.length"
          />
        </el-col>
      </el-row>
    </el-card>

    <!-- 最近访问 -->
    <el-card class="recent-card" v-if="recentVisited.length > 0">
      <template #header>
        <h3>{{ t('apiHome.recentTitle') }}</h3>
      </template>
      <div class="recent-list">
        <el-tag
          v-for="item in recentVisited"
          :key="item.path"
          @click="navigateToApi(item.path)"
          class="recent-tag"
          type="info"
        >
          {{ t(item.nameKey) }}
        </el-tag>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';

interface ApiItem {
  nameKey: string;
  path: string;
  method: string;
  descKey: string;
}

const router = useRouter();
const { t } = useI18n();

// Cube API 列表
const cubeApis: ApiItem[] = [
  {
    nameKey: 'apiHome.api.cubeInfo.name',
    path: '/Cube/Info',
    method: 'GET',
    descKey: 'apiHome.api.cubeInfo.desc',
  },
  {
    nameKey: 'apiHome.api.apiList.name',
    path: '/Cube/Apis',
    method: 'GET',
    descKey: 'apiHome.api.apiList.desc',
  },
  {
    nameKey: 'apiHome.api.userSearch.name',
    path: '/Cube/UserSearch',
    method: 'GET',
    descKey: 'apiHome.api.userSearch.desc',
  },
  {
    nameKey: 'apiHome.api.departmentSearch.name',
    path: '/Cube/DepartmentSearch',
    method: 'GET',
    descKey: 'apiHome.api.departmentSearch.desc',
  },
  {
    nameKey: 'apiHome.api.getArea.name',
    path: '/Cube/GetArea',
    method: 'GET',
    descKey: 'apiHome.api.getArea.desc',
  },
  {
    nameKey: 'apiHome.api.areaChilds.name',
    path: '/Cube/AreaChilds',
    method: 'GET',
    descKey: 'apiHome.api.areaChilds.desc',
  },
  {
    nameKey: 'apiHome.api.areaParents.name',
    path: '/Cube/AreaParents',
    method: 'GET',
    descKey: 'apiHome.api.areaParents.desc',
  },
  {
    nameKey: 'apiHome.api.areaAllParents.name',
    path: '/Cube/AreaAllParents',
    method: 'GET',
    descKey: 'apiHome.api.areaAllParents.desc',
  },
  {
    nameKey: 'apiHome.api.avatar.name',
    path: '/Cube/Avatar',
    method: 'GET',
    descKey: 'apiHome.api.avatar.desc',
  },
  {
    nameKey: 'apiHome.api.lookup.name',
    path: '/Cube/Lookup',
    method: 'GET',
    descKey: 'apiHome.api.lookup.desc',
  },
  {
    nameKey: 'apiHome.api.saveLayout.name',
    path: '/Cube/SaveLayout',
    method: 'POST',
    descKey: 'apiHome.api.saveLayout.desc',
  },
  {
    nameKey: 'apiHome.api.getPageConfig.name',
    path: '/Cube/GetPageConfig',
    method: 'GET',
    descKey: 'apiHome.api.getPageConfig.desc',
  },
  {
    nameKey: 'apiHome.api.setPageConfig.name',
    path: '/Cube/SetPageConfig',
    method: 'POST',
    descKey: 'apiHome.api.setPageConfig.desc',
  },
  {
    nameKey: 'apiHome.api.image.name',
    path: '/Cube/Image',
    method: 'GET',
    descKey: 'apiHome.api.image.desc',
  },
  {
    nameKey: 'apiHome.api.file.name',
    path: '/Cube/File',
    method: 'GET',
    descKey: 'apiHome.api.file.desc',
  },
];

// SSO API 列表
const ssoApis: ApiItem[] = [
  {
    nameKey: 'apiHome.api.ssoLogin.name',
    path: '/Sso/Login',
    method: 'GET',
    descKey: 'apiHome.api.ssoLogin.desc',
  },
  {
    nameKey: 'apiHome.api.loginInfo.name',
    path: '/Sso/LoginInfo',
    method: 'GET',
    descKey: 'apiHome.api.loginInfo.desc',
  },
  {
    nameKey: 'apiHome.api.ssoLogout.name',
    path: '/Sso/Logout',
    method: 'GET',
    descKey: 'apiHome.api.ssoLogout.desc',
  },
  {
    nameKey: 'apiHome.api.bind.name',
    path: '/Sso/Bind',
    method: 'GET',
    descKey: 'apiHome.api.bind.desc',
  },
  {
    nameKey: 'apiHome.api.unbind.name',
    path: '/Sso/UnBind',
    method: 'GET',
    descKey: 'apiHome.api.unbind.desc',
  },
  {
    nameKey: 'apiHome.api.authorize.name',
    path: '/Sso/Authorize',
    method: 'GET',
    descKey: 'apiHome.api.authorize.desc',
  },
  {
    nameKey: 'apiHome.api.auth2.name',
    path: '/Sso/Auth2',
    method: 'GET',
    descKey: 'apiHome.api.auth2.desc',
  },
  {
    nameKey: 'apiHome.api.accessToken.name',
    path: '/Sso/Access_Token',
    method: 'GET/POST',
    descKey: 'apiHome.api.accessToken.desc',
  },
  {
    nameKey: 'apiHome.api.token.name',
    path: '/Sso/Token',
    method: 'GET/POST',
    descKey: 'apiHome.api.token.desc',
  },
  {
    nameKey: 'apiHome.api.passwordToken.name',
    path: '/Sso/PasswordToken',
    method: 'GET/POST',
    descKey: 'apiHome.api.passwordToken.desc',
  },
  {
    nameKey: 'apiHome.api.userInfo.name',
    path: '/Sso/UserInfo',
    method: 'GET',
    descKey: 'apiHome.api.userInfo.desc',
  },
  {
    nameKey: 'apiHome.api.refreshToken.name',
    path: '/Sso/Refresh_Token',
    method: 'GET/POST',
    descKey: 'apiHome.api.refreshToken.desc',
  },
  {
    nameKey: 'apiHome.api.auth.name',
    path: '/Sso/Auth',
    method: 'GET',
    descKey: 'apiHome.api.auth.desc',
  },
  {
    nameKey: 'apiHome.api.getKey.name',
    path: '/Sso/GetKey',
    method: 'GET',
    descKey: 'apiHome.api.getKey.desc',
  },
  {
    nameKey: 'apiHome.api.verify.name',
    path: '/Sso/Verify',
    method: 'GET',
    descKey: 'apiHome.api.verify.desc',
  },
  {
    nameKey: 'apiHome.api.userAuth.name',
    path: '/Sso/UserAuth',
    method: 'GET/POST',
    descKey: 'apiHome.api.userAuth.desc',
  },
  {
    nameKey: 'apiHome.api.ssoAvatar.name',
    path: '/Sso/Avatar',
    method: 'GET',
    descKey: 'apiHome.api.ssoAvatar.desc',
  },
];

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
