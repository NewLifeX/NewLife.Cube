<template>
  <div class="cube-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>魔方设置</h3>
          <el-button type="primary" @click="handleSave">保存设置</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" label-width="160px" v-loading="loading">
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
              <el-input
                v-model="form.avatarPath"
                placeholder="设定后下载远程头像到本地，默认Avatars子目录"
              />
            </el-form-item>
            <el-form-item label="文字头像字符数" prop="avatarChars">
              <el-input-number
                v-model="form.avatarChars"
                :min="1"
                :max="2"
                placeholder="头像不存在时自动生成SVG文字头像字符数，默认2"
              />
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
              <el-input
                v-model="form.corsOrigins"
                placeholder="允许其它源访问当前域，*表示任意域"
              />
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
              <el-input-number
                v-model="form.robotError"
                :min="0"
                :max="599"
                placeholder="设置后拦截各种爬虫，0不拦截"
              />
            </el-form-item>
            <el-form-item label="强制跳转" prop="forceRedirect">
              <el-input v-model="form.forceRedirect" placeholder="如https://*:8081" />
            </el-form-item>
            <el-form-item label="验证码场景" prop="captchaScene">
              <el-checkbox-group v-model="captchaSceneChecks">
                <el-checkbox :value="1" border>登录时</el-checkbox>
                <el-checkbox :value="2" border>注册时</el-checkbox>
                <el-checkbox :value="4" border>发验证码时</el-checkbox>
              </el-checkbox-group>
              <div class="field-tip">位掩码：可组合，0=不启用，如3=登录+注册均需验证码</div>
            </el-form-item>
            <el-form-item label="验证附件访问" prop="validateAttachment">
              <el-switch
                v-model="form.validateAttachment"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">访问附件时是否验证登录状态，默认开启</div>
            </el-form-item>
            <el-form-item label="公开附件分类" prop="publicAttachmentCategories">
              <el-input
                v-model="form.publicAttachmentCategories"
                placeholder="无需登录即可访问的附件分类，逗号分隔，如 markdown,avatar"
              />
            </el-form-item>
            <el-form-item label="私有附件分类" prop="ownerOnlyAttachmentCategories">
              <el-input
                v-model="form.ownerOnlyAttachmentCategories"
                placeholder="仅上传人本人可访问的附件分类，逗号分隔"
              />
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
              <el-switch
                v-model="form.allowRegister"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="允许明文密码" prop="allowPlainPassword">
              <el-switch
                v-model="form.allowPlainPassword"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">禁用后要求通过 Challenge 接口加密登录，防止明文传输</div>
            </el-form-item>
            <el-form-item label="自动注册" prop="autoRegister">
              <el-switch v-model="form.autoRegister" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="密码强度" prop="paswordStrength">
              <el-input
                v-model="form.paswordStrength"
                placeholder="*表示无限制，默认8位起，数字大小写字母和符号"
              />
            </el-form-item>
            <el-form-item label="登录失败次数" prop="maxLoginError">
              <el-input-number v-model="form.maxLoginError" :min="1" placeholder="默认5次" />
            </el-form-item>
            <el-form-item label="登录封禁时间" prop="loginForbiddenTime">
              <el-input-number v-model="form.loginForbiddenTime" :min="1" placeholder="默认300秒" />
            </el-form-item>
            <el-form-item label="三段IP封禁阈值" prop="maxLoginErrorBySubnet24">
              <el-input-number
                v-model="form.maxLoginErrorBySubnet24"
                :min="0"
                placeholder="三段IP连续失败封禁阈值，默认10，0不启用"
              />
            </el-form-item>
            <el-form-item label="两段IP封禁阈值" prop="maxLoginErrorBySubnet16">
              <el-input-number
                v-model="form.maxLoginErrorBySubnet16"
                :min="0"
                placeholder="两段IP连续失败封禁阈值，默认20，0不启用"
              />
            </el-form-item>
          </el-tab-pane>

          <!-- SSO设置 -->
          <el-tab-pane label="SSO设置" name="sso">
            <el-form-item label="强行绑定用户名" prop="forceBindUser">
              <el-switch
                v-model="form.forceBindUser"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="绑定用户代码" prop="forceBindUserCode">
              <el-switch
                v-model="form.forceBindUserCode"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="绑定用户手机" prop="forceBindUserMobile">
              <el-switch
                v-model="form.forceBindUserMobile"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="绑定用户邮箱" prop="forceBindUserMail">
              <el-switch
                v-model="form.forceBindUserMail"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="绑定用户昵称" prop="forceBindNickName">
              <el-switch
                v-model="form.forceBindNickName"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="使用SSO角色" prop="useSsoRole">
              <el-switch v-model="form.useSsoRole" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="使用SSO部门" prop="useSsoDepartment">
              <el-switch
                v-model="form.useSsoDepartment"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="SSO角色规则" prop="roleRules">
              <el-input
                v-model="form.roleRules"
                type="textarea"
                :rows="3"
                placeholder="原角色+部门通配符=目标角色，多条逗号分隔，如：游客+路由*=路由专员,普通用户+*车队=调度员"
              />
            </el-form-item>
            <el-form-item label="注销所有系统" prop="logoutAll">
              <el-switch v-model="form.logoutAll" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="会话超时" prop="sessionTimeout">
              <el-input-number
                v-model="form.sessionTimeout"
                :min="0"
                placeholder="单位秒，0表示不超时"
              />
            </el-form-item>
            <el-form-item label="刷新用户周期" prop="refreshUserPeriod">
              <el-input-number v-model="form.refreshUserPeriod" :min="0" placeholder="默认600秒" />
            </el-form-item>
            <el-form-item label="SSO跨域重定向白名单" prop="ssoSafeDomains">
              <el-input
                v-model="form.ssoSafeDomains"
                placeholder="允许携带Token重定向的域名，逗号分隔，支持*.company.com，留空仅同站"
              />
            </el-form-item>
            <el-form-item label="外部验证地址" prop="externalAuthUrl">
              <el-input
                v-model="form.externalAuthUrl"
                placeholder="本地验证失败时调用外部接口验证用户名密码并自动建号"
              />
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
            <el-form-item label="令牌滑动刷新阈值" prop="tokenRefreshThreshold">
              <el-input-number
                v-model="form.tokenRefreshThreshold"
                :min="0"
                placeholder="JWT剩余有效期低于该秒数时自动刷新，0禁用，默认900"
              />
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
              <el-input
                v-model="form.loginTip"
                type="textarea"
                placeholder="留空表示不显示登录提示信息"
              />
            </el-form-item>
            <el-form-item label="登录页Logo" prop="loginLogo">
              <el-input
                v-model="form.loginLogo"
                placeholder="登录页左上角Logo图片地址，留空使用默认"
              />
            </el-form-item>
            <el-form-item label="登录页背景图" prop="loginBackground">
              <el-input
                v-model="form.loginBackground"
                placeholder="登录页左侧背景大图地址，留空使用默认"
              />
            </el-form-item>
            <el-form-item label="表单组样式" prop="formGroupClass">
              <el-input
                v-model="form.formGroupClass"
                placeholder="form-group col-xs-12 col-sm-6 col-lg-4"
              />
            </el-form-item>
            <el-form-item label="下拉选择框" prop="bootstrapSelect">
              <el-switch
                v-model="form.bootstrapSelect"
                :active-value="true"
                :inactive-value="false"
              />
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
              <el-switch
                v-model="form.enableTableDoubleClick"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="星尘Web" prop="starWeb">
              <el-input v-model="form.starWeb" placeholder="星尘控制台地址" />
            </el-form-item>
          </el-tab-pane>

          <!-- AI设置 -->
          <el-tab-pane label="AI设置" name="ai">
            <el-form-item label="AI总开关" prop="aiSwitch">
              <el-switch v-model="form.aiSwitch" :active-value="true" :inactive-value="false" />
              <div class="field-tip">启用后可使用日志分析、通知润色等 AI 辅助功能</div>
            </el-form-item>
            <el-form-item label="AI服务商" prop="aiProvider">
              <el-input
                v-model="form.aiProvider"
                placeholder="NewLife / Ollama / DeepSeek / DashScope / OpenAI，默认NewLifeAI"
              />
            </el-form-item>
            <el-form-item label="AI服务地址" prop="aiEndpoint">
              <el-input v-model="form.aiEndpoint" placeholder="默认 https://ai.newlifex.com" />
            </el-form-item>
            <el-form-item label="AI ApiKey" prop="aiApiKey">
              <el-input v-model="form.aiApiKey" placeholder="默认 sk-CubeAI2026" />
            </el-form-item>
            <el-form-item label="AI默认模型" prop="aiModel">
              <el-input v-model="form.aiModel" placeholder="默认 newlife-flash" />
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
            <el-form-item label="OAuth服务" prop="enableOAuthServer">
              <el-switch
                v-model="form.enableOAuthServer"
                :active-value="true"
                :inactive-value="false"
              />
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
              <el-switch
                v-model="form.enableUserStat"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item label="启用短信" prop="enableSms">
              <el-switch v-model="form.enableSms" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="启用邮件" prop="enableMail">
              <el-switch v-model="form.enableMail" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item label="启用MFA" prop="enableMfa">
              <el-switch v-model="form.enableMfa" :active-value="true" :inactive-value="false" />
              <div class="field-tip">是否允许用户开启多因素认证（TOTP）</div>
            </el-form-item>
            <el-form-item label="文件存储提供服务" prop="fileStorageProvide">
              <el-switch
                v-model="form.fileStorageProvide"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">是否响应其他节点的文件下载请求，默认开启</div>
            </el-form-item>
            <el-form-item label="文件存储拉取文件" prop="fileStorageFetch">
              <el-switch
                v-model="form.fileStorageFetch"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">是否主动拉取其他节点发布的新文件，默认开启</div>
            </el-form-item>
            <el-form-item label="数据保留时间" prop="dataRetention">
              <el-input-number
                v-model="form.dataRetention"
                :min="1"
                placeholder="审计日志与OAuth日志保留天数，默认30天"
              />
            </el-form-item>
            <el-form-item label="文件保留时间" prop="fileRetention">
              <el-input-number
                v-model="form.fileRetention"
                :min="1"
                placeholder="备份文件保留天数，默认15天"
              />
            </el-form-item>
            <el-form-item label="保留文件大小" prop="fileRetentionSize">
              <el-input-number
                v-model="form.fileRetentionSize"
                :min="1"
                placeholder="单位K字节，默认1024K"
              />
            </el-form-item>
            <el-form-item label="最大导出行数" prop="maxExport">
              <el-input-number v-model="form.maxExport" :min="1" placeholder="默认10,000,000" />
            </el-form-item>
            <el-form-item label="最大备份行数" prop="maxBackup">
              <el-input-number v-model="form.maxBackup" :min="1" placeholder="默认10,000,000" />
            </el-form-item>
            <el-form-item label="API前缀" prop="apiPrefixes">
              <el-input
                v-model="form.apiPrefixes"
                placeholder="多个前缀用逗号或分号分隔，如 /api,/api/v1"
              />
            </el-form-item>
          </el-tab-pane>
        </el-tabs>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormInstance } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义魔方配置类型接口
interface CubeSetting {
  isNew: boolean;
  debug: boolean;
  showRunTime: boolean;
  avatarPath: string;
  avatarChars: number;
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
  allowPlainPassword: boolean;
  autoRegister: boolean;
  paswordStrength: string;
  maxLoginError: number;
  loginForbiddenTime: number;
  maxLoginErrorBySubnet24: number;
  maxLoginErrorBySubnet16: number;
  forceBindUser: boolean;
  forceBindUserCode: boolean;
  forceBindUserMobile: boolean;
  forceBindUserMail: boolean;
  forceBindNickName: boolean;
  useSsoRole: boolean;
  useSsoDepartment: boolean;
  roleRules: string;
  logoutAll: boolean;
  sessionTimeout: number;
  refreshUserPeriod: number;
  ssoSafeDomains: string;
  externalAuthUrl: string;
  jwtSecret: string;
  tokenExpire: number;
  tokenCookie: boolean;
  tokenRefreshThreshold: number;
  validateAttachment: boolean;
  publicAttachmentCategories: string;
  ownerOnlyAttachmentCategories: string;
  startPage: string;
  theme: string;
  skin: string;
  loginTip: string;
  loginLogo: string;
  loginBackground: string;
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
  aiSwitch: boolean;
  aiProvider: string;
  aiEndpoint: string;
  aiApiKey: string;
  aiModel: string;
  enableOAuthServer: boolean;
  enableTenant: boolean;
  enableUserOnline: number;
  enableUserStat: boolean;
  enableSms: boolean;
  enableMail: boolean;
  captchaScene: number;
  enableMfa: boolean;
  fileStorageProvide: boolean;
  fileStorageFetch: boolean;
  dataRetention: number;
  fileRetention: number;
  fileRetentionSize: number;
  maxExport: number;
  maxBackup: number;
  apiPrefixes: string;
}

const loading = ref(false);
const activeTab = ref('basic');
const formRef = ref<FormInstance | null>(null);

// 验证码场景位掩码（1=登录，2=注册，4=发验证码），用于复选框组与表单字段互转
const captchaSceneChecks = computed<number[]>({
  get: () => {
    const v = form.captchaScene || 0;
    const arr: number[] = [];
    if (v & 1) arr.push(1);
    if (v & 2) arr.push(2);
    if (v & 4) arr.push(4);
    return arr;
  },
  set: (val) => {
    form.captchaScene = (val || []).reduce((a, b) => a + b, 0);
  },
});

// 表单数据
const form = reactive<CubeSetting>({
  isNew: false,
  debug: true,
  showRunTime: true,
  avatarPath: 'Avatars',
  avatarChars: 2,
  uploadPath: 'Uploads',
  webRootPath: 'wwwroot',
  resourceUrl: '',
  corsOrigins: '',
  xFrameOptions: '',
  sameSiteMode: -1,
  cookieDomain: '',
  shareExpire: 7200,
  robotError: 0,
  forceRedirect: '',
  defaultRole: '普通用户',
  allowLogin: true,
  allowRegister: true,
  allowPlainPassword: true,
  autoRegister: true,
  paswordStrength: '',
  maxLoginError: 5,
  loginForbiddenTime: 300,
  maxLoginErrorBySubnet24: 10,
  maxLoginErrorBySubnet16: 20,
  forceBindUser: true,
  forceBindUserCode: true,
  forceBindUserMobile: true,
  forceBindUserMail: true,
  forceBindNickName: true,
  useSsoRole: true,
  useSsoDepartment: true,
  roleRules: '',
  logoutAll: true,
  sessionTimeout: 0,
  refreshUserPeriod: 600,
  ssoSafeDomains: '',
  externalAuthUrl: '',
  jwtSecret: '',
  tokenExpire: 7200,
  tokenCookie: false,
  tokenRefreshThreshold: 900,
  validateAttachment: true,
  publicAttachmentCategories: '',
  ownerOnlyAttachmentCategories: '',
  startPage: '',
  theme: '',
  skin: '',
  loginTip: '',
  loginLogo: '',
  loginBackground: '',
  formGroupClass: 'form-group col-xs-12 col-sm-6',
  bootstrapSelect: true,
  maxDropDownList: 50,
  copyright: '',
  registration: '',
  enableNewUI: false,
  eChartsTheme: '',
  titlePrefix: true,
  enableTableDoubleClick: true,
  starWeb: '',
  aiSwitch: false,
  aiProvider: 'NewLifeAI',
  aiEndpoint: 'https://ai.newlifex.com',
  aiApiKey: 'sk-CubeAI2026',
  aiModel: 'newlife-flash',
  enableOAuthServer: true,
  enableTenant: false,
  enableUserOnline: 2,
  enableUserStat: true,
  enableSms: false,
  enableMail: false,
  captchaScene: 0,
  enableMfa: false,
  fileStorageProvide: true,
  fileStorageFetch: true,
  dataRetention: 30,
  fileRetention: 15,
  fileRetentionSize: 1024,
  maxExport: 10000000,
  maxBackup: 10000000,
  apiPrefixes: '',
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

.field-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
  margin-top: 4px;
}
</style>
