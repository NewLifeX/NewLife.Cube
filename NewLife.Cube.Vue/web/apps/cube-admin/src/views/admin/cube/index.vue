<template>
  <div class="cube-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('cubeSetting.title') }}</h3>
          <el-button type="primary" @click="handleSave">{{ t('cubeSetting.save') }}</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" label-width="160px" v-loading="loading">
        <el-tabs v-model="activeTab">
          <!-- 基本设置 -->
          <el-tab-pane :label="t('cubeSetting.tabBasic')" name="basic">
            <el-form-item :label="t('cubeSetting.debug')" prop="debug">
              <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.showRunTime')" prop="showRunTime">
              <el-switch v-model="form.showRunTime" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.avatarPath')" prop="avatarPath">
              <el-input
                v-model="form.avatarPath"
                :placeholder="t('cubeSetting.avatarPathHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.avatarChars')" prop="avatarChars">
              <el-input-number
                v-model="form.avatarChars"
                :min="1"
                :max="2"
                :placeholder="t('cubeSetting.avatarCharsHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.uploadPath')" prop="uploadPath">
              <el-input v-model="form.uploadPath" :placeholder="t('cubeSetting.uploadPathHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.webRootPath')" prop="webRootPath">
              <el-input v-model="form.webRootPath" :placeholder="t('cubeSetting.webRootPathHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.resourceUrl')" prop="resourceUrl">
              <el-input v-model="form.resourceUrl" :placeholder="t('cubeSetting.resourceUrlHint')" />
            </el-form-item>
          </el-tab-pane>

          <!-- 安全设置 -->
          <el-tab-pane :label="t('cubeSetting.tabSecurity')" name="security">
            <el-form-item :label="t('cubeSetting.corsOrigins')" prop="corsOrigins">
              <el-input
                v-model="form.corsOrigins"
                :placeholder="t('cubeSetting.corsOriginsHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.xFrameOptions')" prop="xFrameOptions">
              <el-select v-model="form.xFrameOptions" :placeholder="t('cubeSetting.xFrameOptionsHint')">
                <el-option :label="t('cubeSetting.xFrameDefault')" value="" />
                <el-option :label="t('cubeSetting.xFrameSameOrigin')" value="SAMEORIGIN" />
                <el-option :label="t('cubeSetting.xFrameAny')" value="ALLOWALL" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.sameSiteMode')" prop="sameSiteMode">
              <el-select v-model="form.sameSiteMode" :placeholder="t('cubeSetting.sameSiteModeHint')">
                <el-option label="Unspecified" :value="-1" />
                <el-option label="None" :value="0" />
                <el-option label="Lax" :value="1" />
                <el-option label="Strict" :value="2" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.cookieDomain')" prop="cookieDomain">
              <el-input v-model="form.cookieDomain" :placeholder="t('cubeSetting.cookieDomainHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.robotError')" prop="robotError">
              <el-input-number
                v-model="form.robotError"
                :min="0"
                :max="599"
                :placeholder="t('cubeSetting.robotErrorHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.forceRedirect')" prop="forceRedirect">
              <el-input v-model="form.forceRedirect" :placeholder="t('cubeSetting.forceRedirectHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.captchaScene')" prop="captchaScene">
              <el-checkbox-group v-model="captchaSceneChecks">
                <el-checkbox :value="1" border>{{ t('cubeSetting.captchaLogin') }}</el-checkbox>
                <el-checkbox :value="2" border>{{ t('cubeSetting.captchaRegister') }}</el-checkbox>
                <el-checkbox :value="4" border>{{ t('cubeSetting.captchaSend') }}</el-checkbox>
              </el-checkbox-group>
              <div class="field-tip">{{ t('cubeSetting.captchaSceneTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.validateAttachment')" prop="validateAttachment">
              <el-switch
                v-model="form.validateAttachment"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">{{ t('cubeSetting.validateAttachmentTip') }}</div>
            </el-form-item>
            <el-form-item
              :label="t('cubeSetting.publicAttachmentCategories')"
              prop="publicAttachmentCategories"
            >
              <el-input
                v-model="form.publicAttachmentCategories"
                :placeholder="t('cubeSetting.publicAttachmentCategoriesHint')"
              />
            </el-form-item>
            <el-form-item
              :label="t('cubeSetting.ownerOnlyAttachmentCategories')"
              prop="ownerOnlyAttachmentCategories"
            >
              <el-input
                v-model="form.ownerOnlyAttachmentCategories"
                :placeholder="t('cubeSetting.ownerOnlyAttachmentCategoriesHint')"
              />
            </el-form-item>
          </el-tab-pane>

          <!-- 用户设置 -->
          <el-tab-pane :label="t('cubeSetting.tabUser')" name="user">
            <el-form-item :label="t('cubeSetting.defaultRole')" prop="defaultRole">
              <el-input v-model="form.defaultRole" :placeholder="t('cubeSetting.defaultRoleHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.allowLogin')" prop="allowLogin">
              <el-switch v-model="form.allowLogin" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.allowRegister')" prop="allowRegister">
              <el-switch
                v-model="form.allowRegister"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.allowPlainPassword')" prop="allowPlainPassword">
              <el-switch
                v-model="form.allowPlainPassword"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">{{ t('cubeSetting.allowPlainPasswordTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.autoRegister')" prop="autoRegister">
              <el-switch v-model="form.autoRegister" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.passwordStrength')" prop="paswordStrength">
              <el-input
                v-model="form.paswordStrength"
                :placeholder="t('cubeSetting.passwordStrengthHint')"
              />
            </el-form-item>
            <el-form-item
              :label="t('cubeSetting.passwordComplexity')"
              prop="enablePasswordComplexity"
            >
              <el-switch
                v-model="form.enablePasswordComplexity"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">{{ t('cubeSetting.passwordComplexityTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.maxLoginError')" prop="maxLoginError">
              <el-input-number
                v-model="form.maxLoginError"
                :min="1"
                :placeholder="t('cubeSetting.maxLoginErrorHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.loginForbiddenTime')" prop="loginForbiddenTime">
              <el-input-number
                v-model="form.loginForbiddenTime"
                :min="1"
                :placeholder="t('cubeSetting.loginForbiddenTimeHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.subnet24Threshold')" prop="maxLoginErrorBySubnet24">
              <el-input-number
                v-model="form.maxLoginErrorBySubnet24"
                :min="0"
                :placeholder="t('cubeSetting.subnet24ThresholdHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.subnet16Threshold')" prop="maxLoginErrorBySubnet16">
              <el-input-number
                v-model="form.maxLoginErrorBySubnet16"
                :min="0"
                :placeholder="t('cubeSetting.subnet16ThresholdHint')"
              />
            </el-form-item>
          </el-tab-pane>

          <!-- SSO设置 -->
          <el-tab-pane :label="t('cubeSetting.tabSso')" name="sso">
            <el-form-item :label="t('cubeSetting.forceBindUser')" prop="forceBindUser">
              <el-switch
                v-model="form.forceBindUser"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.forceBindUserCode')" prop="forceBindUserCode">
              <el-switch
                v-model="form.forceBindUserCode"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.forceBindUserMobile')" prop="forceBindUserMobile">
              <el-switch
                v-model="form.forceBindUserMobile"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.forceBindUserMail')" prop="forceBindUserMail">
              <el-switch
                v-model="form.forceBindUserMail"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.forceBindNickName')" prop="forceBindNickName">
              <el-switch
                v-model="form.forceBindNickName"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.useSsoRole')" prop="useSsoRole">
              <el-switch v-model="form.useSsoRole" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.useSsoDepartment')" prop="useSsoDepartment">
              <el-switch
                v-model="form.useSsoDepartment"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.roleRules')" prop="roleRules">
              <el-input
                v-model="form.roleRules"
                type="textarea"
                :rows="3"
                :placeholder="t('cubeSetting.roleRulesHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.logoutAll')" prop="logoutAll">
              <el-switch v-model="form.logoutAll" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.sessionTimeout')" prop="sessionTimeout">
              <el-input-number
                v-model="form.sessionTimeout"
                :min="0"
                :placeholder="t('cubeSetting.sessionTimeoutHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.refreshUserPeriod')" prop="refreshUserPeriod">
              <el-input-number
                v-model="form.refreshUserPeriod"
                :min="0"
                :placeholder="t('cubeSetting.refreshUserPeriodHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.ssoSafeDomains')" prop="ssoSafeDomains">
              <el-input
                v-model="form.ssoSafeDomains"
                :placeholder="t('cubeSetting.ssoSafeDomainsHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.externalAuthUrl')" prop="externalAuthUrl">
              <el-input
                v-model="form.externalAuthUrl"
                :placeholder="t('cubeSetting.externalAuthUrlHint')"
              />
            </el-form-item>
          </el-tab-pane>

          <!-- JWT设置 -->
          <el-tab-pane :label="t('cubeSetting.tabJwt')" name="jwt">
            <el-form-item :label="t('cubeSetting.jwtSecret')" prop="jwtSecret">
              <el-input v-model="form.jwtSecret" :placeholder="t('cubeSetting.jwtSecretHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.tokenExpire')" prop="tokenExpire">
              <el-input-number
                v-model="form.tokenExpire"
                :min="1"
                :placeholder="t('cubeSetting.tokenExpireHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.tokenCookie')" prop="tokenCookie">
              <el-switch v-model="form.tokenCookie" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.shareExpire')" prop="shareExpire">
              <el-input-number
                v-model="form.shareExpire"
                :min="1"
                :placeholder="t('cubeSetting.shareExpireHint')"
              />
            </el-form-item>
            <el-form-item
              :label="t('cubeSetting.tokenRefreshThreshold')"
              prop="tokenRefreshThreshold"
            >
              <el-input-number
                v-model="form.tokenRefreshThreshold"
                :min="0"
                :placeholder="t('cubeSetting.tokenRefreshThresholdHint')"
              />
            </el-form-item>
          </el-tab-pane>

          <!-- 界面设置 -->
          <el-tab-pane :label="t('cubeSetting.tabUi')" name="ui">
            <el-form-item :label="t('cubeSetting.startPage')" prop="startPage">
              <el-input v-model="form.startPage" :placeholder="t('cubeSetting.startPageHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.theme')" prop="theme">
              <el-select v-model="form.theme" :placeholder="t('cubeSetting.themeHint')">
                <el-option label="ACE" value="ACE" />
                <el-option label="layui" value="layui" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.skin')" prop="skin">
              <el-select v-model="form.skin" :placeholder="t('cubeSetting.skinHint')">
                <el-option label="ACE" value="ACE" />
                <el-option label="layui" value="layui" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.loginTip')" prop="loginTip">
              <el-input
                v-model="form.loginTip"
                type="textarea"
                :placeholder="t('cubeSetting.loginTipHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.loginLogo')" prop="loginLogo">
              <el-input v-model="form.loginLogo" :placeholder="t('cubeSetting.loginLogoHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.loginBackground')" prop="loginBackground">
              <el-input
                v-model="form.loginBackground"
                :placeholder="t('cubeSetting.loginBackgroundHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.formGroupClass')" prop="formGroupClass">
              <el-input
                v-model="form.formGroupClass"
                placeholder="form-group col-xs-12 col-sm-6 col-lg-4"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.bootstrapSelect')" prop="bootstrapSelect">
              <el-switch
                v-model="form.bootstrapSelect"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.maxDropDownList')" prop="maxDropDownList">
              <el-input-number
                v-model="form.maxDropDownList"
                :min="1"
                :placeholder="t('cubeSetting.maxDropDownListHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableNewUI')" prop="enableNewUI">
              <el-switch v-model="form.enableNewUI" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.eChartsTheme')" prop="eChartsTheme">
              <el-input v-model="form.eChartsTheme" :placeholder="t('cubeSetting.eChartsThemeHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.titlePrefix')" prop="titlePrefix">
              <el-switch v-model="form.titlePrefix" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item
              :label="t('cubeSetting.enableTableDoubleClick')"
              prop="enableTableDoubleClick"
            >
              <el-switch
                v-model="form.enableTableDoubleClick"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.starWeb')" prop="starWeb">
              <el-input v-model="form.starWeb" :placeholder="t('cubeSetting.starWebHint')" />
            </el-form-item>
          </el-tab-pane>

          <!-- AI设置 -->
          <el-tab-pane :label="t('cubeSetting.tabAi')" name="ai">
            <el-form-item :label="t('cubeSetting.aiSwitch')" prop="aiSwitch">
              <el-switch v-model="form.aiSwitch" :active-value="true" :inactive-value="false" />
              <div class="field-tip">{{ t('cubeSetting.aiSwitchTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.aiProvider')" prop="aiProvider">
              <el-input v-model="form.aiProvider" :placeholder="t('cubeSetting.aiProviderHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.aiEndpoint')" prop="aiEndpoint">
              <el-input
                v-model="form.aiEndpoint"
                placeholder="https://ai.newlifex.com"
              />
            </el-form-item>
            <el-form-item label="AI ApiKey" prop="aiApiKey">
              <el-input v-model="form.aiApiKey" placeholder="sk-CubeAI2026" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.aiModel')" prop="aiModel">
              <el-input v-model="form.aiModel" placeholder="newlife-flash" />
            </el-form-item>
          </el-tab-pane>

          <!-- 其他设置 -->
          <el-tab-pane :label="t('cubeSetting.tabOthers')" name="others">
            <el-form-item :label="t('cubeSetting.copyright')" prop="copyright">
              <el-input v-model="form.copyright" :placeholder="t('cubeSetting.copyrightHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.registration')" prop="registration">
              <el-input v-model="form.registration" :placeholder="t('cubeSetting.registrationHint')" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableOAuthServer')" prop="enableOAuthServer">
              <el-switch
                v-model="form.enableOAuthServer"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableTenant')" prop="enableTenant">
              <el-switch v-model="form.enableTenant" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.userOnline')" prop="enableUserOnline">
              <el-select
                v-model="form.enableUserOnline"
                :placeholder="t('cubeSetting.userOnlineHint')"
              >
                <el-option :label="t('cubeSetting.userOnlineNone')" :value="0" />
                <el-option :label="t('cubeSetting.userOnlineLogged')" :value="1" />
                <el-option :label="t('cubeSetting.userOnlineAll')" :value="2" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.userStat')" prop="enableUserStat">
              <el-switch
                v-model="form.enableUserStat"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableSms')" prop="enableSms">
              <el-switch v-model="form.enableSms" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableMail')" prop="enableMail">
              <el-switch v-model="form.enableMail" :active-value="true" :inactive-value="false" />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.enableMfa')" prop="enableMfa">
              <el-switch v-model="form.enableMfa" :active-value="true" :inactive-value="false" />
              <div class="field-tip">{{ t('cubeSetting.enableMfaTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.fileStorageProvide')" prop="fileStorageProvide">
              <el-switch
                v-model="form.fileStorageProvide"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">{{ t('cubeSetting.fileStorageProvideTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.fileStorageFetch')" prop="fileStorageFetch">
              <el-switch
                v-model="form.fileStorageFetch"
                :active-value="true"
                :inactive-value="false"
              />
              <div class="field-tip">{{ t('cubeSetting.fileStorageFetchTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('cubeSetting.dataRetention')" prop="dataRetention">
              <el-input-number
                v-model="form.dataRetention"
                :min="1"
                :placeholder="t('cubeSetting.dataRetentionHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.fileRetention')" prop="fileRetention">
              <el-input-number
                v-model="form.fileRetention"
                :min="1"
                :placeholder="t('cubeSetting.fileRetentionHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.fileRetentionSize')" prop="fileRetentionSize">
              <el-input-number
                v-model="form.fileRetentionSize"
                :min="1"
                :placeholder="t('cubeSetting.fileRetentionSizeHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.maxExport')" prop="maxExport">
              <el-input-number
                v-model="form.maxExport"
                :min="1"
                :placeholder="t('cubeSetting.maxExportHint')"
              />
            </el-form-item>
            <el-form-item :label="t('cubeSetting.maxBackup')" prop="maxBackup">
              <el-input-number
                v-model="form.maxBackup"
                :min="1"
                :placeholder="t('cubeSetting.maxExportHint')"
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
import { useI18n } from 'vue-i18n';
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
  enablePasswordComplexity: boolean;
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
}

const loading = ref(false);
const activeTab = ref('basic');
const formRef = ref<FormInstance | null>(null);
const { t } = useI18n();

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
  defaultRole: t('cubeSetting.defaultRoleValue'),
  allowLogin: true,
  allowRegister: true,
  allowPlainPassword: true,
  autoRegister: true,
  paswordStrength: '',
  enablePasswordComplexity: true,
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
    ElMessage.error(t('cubeSetting.loadFailed'));
    console.error('Failed to load config:', error);
  } finally {
    loading.value = false;
  }
};

// 保存配置
const handleSave = async () => {
  try {
    loading.value = true;
    await request.put('/Admin/Cube', form);
    ElMessage.success(t('cubeSetting.saveSuccess'));
  } catch (error) {
    ElMessage.error(t('cubeSetting.saveFailed'));
    console.error('Failed to save config:', error);
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
