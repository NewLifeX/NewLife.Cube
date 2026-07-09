<template>
  <div class="cube-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>魔方设置</h3>
          <el-button type="primary" @click="handleSave">保存设置</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" label-width="140px" v-loading="loading">
        <el-tabs v-model="activeTab">
          <!-- 基本设置 -->
          <el-tab-pane label="基本设置" name="basic">
            <el-form-item label="调试模式" prop="debug">
              <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="显示运行时间" prop="showRunTime">
              <el-switch v-model="form.showRunTime" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="头像目录" prop="avatarPath">
              <el-input v-model="form.avatarPath" placeholder="设定后下载远程头像到本地，默认Avatars子目录" />
            </el-form-item>
            <el-form-item label="上传目录" prop="uploadPath">
              <el-input v-model="form.uploadPath" placeholder="默认Uploads" />
            </el-form-item>
            <el-form-item label="静态资源目录" prop="webRootPath">
              <el-input v-model="form.webRootPath" placeholder="默认wwwroot" />
            </el-form-item>
            <el-form-item label="资源地址" prop="resourceUrl">
              <el-input v-model="form.resourceUrl" placeholder="指向CDN，留空表示使用本地" />
            </el-form-item>
          </el-tab-pane>

          <!-- 安全设置 -->
          <el-tab-pane label="安全设置" name="security">
            <el-form-item label="跨域来源" prop="corsOrigins">
              <el-input v-model="form.corsOrigins" placeholder="允许其它源访问当前域，*表示任意域" />
            </el-form-item>
            <el-form-item label="iframe展示" prop="xFrameOptions">
              <el-select v-model="form.xFrameOptions" placeholder="请选择iframe展示方式">
                <el-option label="只允许相同域名（默认）" value="" />
                <el-option label="允许相同域名和端口" value="SAMEORIGIN" />
                <el-option label="允许任何域名" value="ALLOWALL" />
              </el-select>
            </el-form-item>
            <el-form-item label="Cookie模式" prop="sameSiteMode">
              <el-select v-model="form.sameSiteMode" placeholder="请选择Cookie模式">
                <el-option label="Unspecified" :value="-1" />
                <el-option label="None" :value="0" />
                <el-option label="Lax" :value="1" />
                <el-option label="Strict" :value="2" />
              </el-select>
            </el-form-item>
            <el-form-item label="Cookie域名" prop="cookieDomain">
              <el-input v-model="form.cookieDomain" placeholder="可用于把Cookie写到顶级域名" />
            </el-form-item>
            <el-form-item label="机器人错误码" prop="robotError">
              <el-input-number v-model="form.robotError" :min="0" :max="599" placeholder="设置后拦截各种爬虫，0不拦截" />
            </el-form-item>
            <el-form-item label="强制跳转" prop="forceRedirect">
              <el-input v-model="form.forceRedirect" placeholder="如https://*:8081" />
            </el-form-item>
          </el-tab-pane>

          <!-- 用户设置 -->
          <el-tab-pane label="用户设置" name="user">
            <el-form-item label="默认角色" prop="defaultRole">
              <el-input v-model="form.defaultRole" placeholder="默认普通用户" />
            </el-form-item>
            <el-form-item label="允许密码登录" prop="allowLogin">
              <el-switch v-model="form.allowLogin" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="允许注册" prop="allowRegister">
              <el-switch v-model="form.allowRegister" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="自动注册" prop="autoRegister">
              <el-switch v-model="form.autoRegister" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="密码强度" prop="paswordStrength">
              <el-input v-model="form.paswordStrength" placeholder="*表示无限制，默认8位起" />
            </el-form-item>
            <el-form-item label="登录失败次数" prop="maxLoginError">
              <el-input-number v-model="form.maxLoginError" :min="1" placeholder="默认5次" />
            </el-form-item>
            <el-form-item label="登录封禁时间" prop="loginForbiddenTime">
              <el-input-number v-model="form.loginForbiddenTime" :min="1" placeholder="默认300秒" />
            </el-form-item>
          </el-tab-pane>

          <!-- SSO设置 -->
          <el-tab-pane label="SSO设置" name="sso">
            <el-form-item label="强行绑定用户名" prop="forceBindUser">
              <el-switch v-model="form.forceBindUser" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="绑定用户代码" prop="forceBindUserCode">
              <el-switch v-model="form.forceBindUserCode" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="绑定用户手机" prop="forceBindUserMobile">
              <el-switch v-model="form.forceBindUserMobile" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="绑定用户邮箱" prop="forceBindUserMail">
              <el-switch v-model="form.forceBindUserMail" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="绑定用户昵称" prop="forceBindNickName">
              <el-switch v-model="form.forceBindNickName" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="使用SSO角色" prop="useSsoRole">
              <el-switch v-model="form.useSsoRole" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="使用SSO部门" prop="useSsoDepartment">
              <el-switch v-model="form.useSsoDepartment" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="注销所有系统" prop="logoutAll">
              <el-switch v-model="form.logoutAll" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="会话超时" prop="sessionTimeout">
              <el-input-number v-model="form.sessionTimeout" :min="0" placeholder="单位秒，0表示不超时" />
            </el-form-item>
            <el-form-item label="刷新用户周期" prop="refreshUserPeriod">
              <el-input-number v-model="form.refreshUserPeriod" :min="0" placeholder="默认600秒" />
            </el-form-item>
          </el-tab-pane>

          <!-- JWT设置 -->
          <el-tab-pane label="JWT设置" name="jwt">
            <el-form-item label="JWT密钥" prop="jwtSecret">
              <el-input v-model="form.jwtSecret" placeholder="如HS256:ABCD1234" />
            </el-form-item>
            <el-form-item label="令牌有效期" prop="tokenExpire">
              <el-input-number v-model="form.tokenExpire" :min="1" placeholder="默认7200秒" />
            </el-form-item>
            <el-form-item label="Cookie存储令牌" prop="tokenCookie">
              <el-switch v-model="form.tokenCookie" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="分享有效期" prop="shareExpire">
              <el-input-number v-model="form.shareExpire" :min="1" placeholder="默认7200秒" />
            </el-form-item>
          </el-tab-pane>

          <!-- 界面设置 -->
          <el-tab-pane label="界面设置" name="ui">
            <el-form-item label="工作台页面" prop="startPage">
              <el-input v-model="form.startPage" placeholder="进入后台的第一个内容页" />
            </el-form-item>
            <el-form-item label="主题样式" prop="theme">
              <el-select v-model="form.theme" placeholder="请选择主题样式">
                <el-option label="ACE" value="ACE" />
                <el-option label="layui" value="layui" />
              </el-select>
            </el-form-item>
            <el-form-item label="首页皮肤" prop="skin">
              <el-select v-model="form.skin" placeholder="请选择首页皮肤">
                <el-option label="ACE" value="ACE" />
                <el-option label="layui" value="layui" />
              </el-select>
            </el-form-item>
            <el-form-item label="登录提示" prop="loginTip">
              <el-input v-model="form.loginTip" type="textarea" placeholder="留空表示不显示登录提示信息" />
            </el-form-item>
            <el-form-item label="表单组样式" prop="formGroupClass">
              <el-input v-model="form.formGroupClass" placeholder="form-group col-xs-12 col-sm-6 col-lg-4" />
            </el-form-item>
            <el-form-item label="下拉选择框" prop="bootstrapSelect">
              <el-switch v-model="form.bootstrapSelect" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="最大下拉个数" prop="maxDropDownList">
              <el-input-number v-model="form.maxDropDownList" :min="1" placeholder="默认50" />
            </el-form-item>
            <el-form-item label="启用新UI" prop="enableNewUI">
              <el-switch v-model="form.enableNewUI" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="ECharts主题" prop="eChartsTheme">
              <el-input v-model="form.eChartsTheme" placeholder="图表样式主题" />
            </el-form-item>
            <el-form-item label="标题后缀" prop="titlePrefix">
              <el-switch v-model="form.titlePrefix" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="双击事件启用" prop="enableTableDoubleClick">
              <el-switch v-model="form.enableTableDoubleClick" :active-value="true" :inactive-value="false" />
            </el-form-item>
          </el-tab-pane>

          <!-- 其他设置 -->
          <el-tab-pane label="其他设置" name="others">
            <el-form-item label="版权信息" prop="copyright">
              <el-input v-model="form.copyright" placeholder="留空表示不显示版权信息" />
            </el-form-item>
            <el-form-item label="备案号" prop="registration">
              <el-input v-model="form.registration" placeholder="留空表示不显示备案信息" />
            </el-form-item>
            <el-form-item label="星尘Web" prop="starWeb">
              <el-input v-model="form.starWeb" placeholder="星尘控制台地址" />
            </el-form-item>
            <el-form-item label="OAuth服务" prop="enableOAuthServer">
              <el-switch v-model="form.enableOAuthServer" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="多租户" prop="enableTenant">
              <el-switch v-model="form.enableTenant" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="用户在线" prop="enableUserOnline">
              <el-select v-model="form.enableUserOnline" placeholder="请选择用户在线记录方式">
                <el-option label="不记录" :value="0" />
                <el-option label="仅记录已登录用户" :value="1" />
                <el-option label="记录所有访客" :value="2" />
              </el-select>
            </el-form-item>
            <el-form-item label="用户统计" prop="enableUserStat">
              <el-switch v-model="form.enableUserStat" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="数据保留时间" prop="dataRetention">
              <el-input-number v-model="form.dataRetention" :min="1" placeholder="审计日志与OAuth日志保留天数，默认30天" />
            </el-form-item>
            <el-form-item label="文件保留时间" prop="fileRetention">
              <el-input-number v-model="form.fileRetention" :min="1" placeholder="备份文件保留天数，默认15天" />
            </el-form-item>
            <el-form-item label="保留文件大小" prop="fileRetentionSize">
              <el-input-number v-model="form.fileRetentionSize" :min="1" placeholder="单位K字节，默认1024K" />
            </el-form-item>
            <el-form-item label="最大导出行数" prop="maxExport">
              <el-input-number v-model="form.maxExport" :min="1" placeholder="默认10,000,000" />
            </el-form-item>
            <el-form-item label="最大备份行数" prop="maxBackup">
              <el-input-number v-model="form.maxBackup" :min="1" placeholder="默认10,000,000" />
            </el-form-item>
          </el-tab-pane>
        </el-tabs>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义魔方配置类型接口
interface CubeSetting {
  isNew: boolean;
  debug: boolean;
  showRunTime: boolean;
  avatarPath: string;
  uploadPath: string;
  webRootPath: string;
  resourceUrl: string;
  corsOrigins: string;
  xFrameOptions: string;
  sameSiteMode: number;
  cookieDomain: string;
  shareExpire: number;
  robotError: number;
  forceRedirect: string;
  defaultRole: string;
  allowLogin: boolean;
  allowRegister: boolean;
  autoRegister: boolean;
  paswordStrength: string;
  maxLoginError: number;
  loginForbiddenTime: number;
  forceBindUser: boolean;
  forceBindUserCode: boolean;
  forceBindUserMobile: boolean;
  forceBindUserMail: boolean;
  forceBindNickName: boolean;
  useSsoRole: boolean;
  useSsoDepartment: boolean;
  logoutAll: boolean;
  sessionTimeout: number;
  refreshUserPeriod: number;
  jwtSecret: string;
  tokenExpire: number;
  tokenCookie: boolean;
  startPage: string;
  theme: string;
  skin: string;
  loginTip: string;
  formGroupClass: string;
  bootstrapSelect: boolean;
  maxDropDownList: number;
  copyright: string;
  registration: string;
  enableNewUI: boolean;
  eChartsTheme: string;
  titlePrefix: boolean;
  enableTableDoubleClick: boolean;
  starWeb: string;
  enableOAuthServer: boolean;
  enableTenant: boolean;
  enableUserOnline: number;
  enableUserStat: boolean;
  dataRetention: number;
  fileRetention: number;
  fileRetentionSize: number;
  maxExport: number;
  maxBackup: number;
}

const loading = ref(false);
const activeTab = ref('basic');
const formRef = ref<FormInstance | null>(null);

// 表单数据
const form = reactive<CubeSetting>({
  isNew: false,
  debug: false,
  showRunTime: false,
  avatarPath: '',
  uploadPath: '',
  webRootPath: '',
  resourceUrl: '',
  corsOrigins: '',
  xFrameOptions: '',
  sameSiteMode: -1,
  cookieDomain: '',
  shareExpire: 7200,
  robotError: 0,
  forceRedirect: '',
  defaultRole: '',
  allowLogin: true,
  allowRegister: false,
  autoRegister: true,
  paswordStrength: '',
  maxLoginError: 5,
  loginForbiddenTime: 300,
  forceBindUser: false,
  forceBindUserCode: false,
  forceBindUserMobile: false,
  forceBindUserMail: false,
  forceBindNickName: false,
  useSsoRole: true,
  useSsoDepartment: true,
  logoutAll: true,
  sessionTimeout: 0,
  refreshUserPeriod: 600,
  jwtSecret: '',
  tokenExpire: 7200,
  tokenCookie: false,
  startPage: '',
  theme: '',
  skin: '',
  loginTip: '',
  formGroupClass: '',
  bootstrapSelect: false,
  maxDropDownList: 50,
  copyright: '',
  registration: '',
  enableNewUI: false,
  eChartsTheme: '',
  titlePrefix: true,
  enableTableDoubleClick: true,
  starWeb: '',
  enableOAuthServer: false,
  enableTenant: false,
  enableUserOnline: 2,
  enableUserStat: true,
  dataRetention: 30,
  fileRetention: 15,
  fileRetentionSize: 1024,
  maxExport: 10000000,
  maxBackup: 10000000,
});

// 加载配置数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Admin/Cube');

    if (response && response.data) {
      const data = response.data.data || response.data;
      Object.assign(form, data);
    }
  } catch (error) {
    ElMessage.error('加载配置失败');
    console.error('加载配置失败:', error);
  } finally {
    loading.value = false;
  }
};

// 保存配置
const handleSave = async () => {
  try {
    loading.value = true;
    await request.put('/Admin/Cube', form);
    ElMessage.success('保存配置成功');
  } catch (error) {
    ElMessage.error('保存配置失败');
    console.error('保存配置失败:', error);
  } finally {
    loading.value = false;
  }
};

// 初始化加载数据
onMounted(() => {
  loadData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
