<template>
  <div class="unauthorized-container">
    <el-row justify="center" align="middle" class="unauthorized-content">
      <el-col :span="24" class="img-col">
        <img
          class="unauthorized-img"
          src="https://cdn.jsdelivr.net/gh/element-plus/element-plus@latest/docs/public/404.png"
          alt="unauthorized"
        />
      </el-col>
      <el-col :span="24" class="text-col">
        <div class="unauthorized-text">
          {{ t('pageUnauthorized.desc') }}
        </div>
      </el-col>
      <el-col :span="24" class="btn-col">
        <el-button type="primary" size="large" @click="goLogin">
          {{ t('pageUnauthorized.goLogin') }}
        </el-button>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ElButton, ElRow, ElCol } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { getConfig } from '../configure';

const router = useRouter();
const { t } = useI18n();
const {
  auth: { reLoginParams },
} = getConfig();
const loginPageUrl = reLoginParams?.loginPageUrl || '/login';

function goLogin() {
  router.replace({ path: loginPageUrl });
}
</script>

<style scoped>
.unauthorized-container {
  min-height: 100vh;
  background: var(--el-bg-color-page);
  display: flex;
  align-items: center;
  justify-content: center;
}
.unauthorized-content {
  width: 100%;
  text-align: center;
  flex-direction: column;
}
.img-col {
  margin-bottom: 32px;
}
.unauthorized-img {
  max-width: 400px;
  width: 100%;
  margin: 0 auto;
}
.text-col {
  font-size: 28px;
  color: var(--el-text-color-primary);
  margin-bottom: 32px;
}
.btn-col {
  margin-top: 12px;
}
</style>
