<template>
  <div class="unauthorized-container">
    <el-row justify="center" align="middle" class="unauthorized-content">
      <el-col :span="24" class="img-col">
        <img
          class="unauthorized-img"
          src="https://cdn.jsdelivr.net/gh/element-plus/element-plus@latest/docs/public/404.png"
          alt="未授权"
        />
      </el-col>
      <el-col :span="24" class="text-col">
        <div class="unauthorized-text">
          抱歉，您未登录或身份认证已失效，请点击下方按钮重新登录！
        </div>
      </el-col>
      <el-col :span="24" class="btn-col">
        <el-button type="primary" size="large" @click="goLogin">立即跳转</el-button>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ElButton, ElRow, ElCol } from 'element-plus';
import { useRouter } from 'vue-router';
import { getConfig } from '../configure';

const router = useRouter();
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
  background: #f7f8fa;
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
  color: #333;
  margin-bottom: 32px;
}
.btn-col {
  margin-top: 12px;
}
</style>
