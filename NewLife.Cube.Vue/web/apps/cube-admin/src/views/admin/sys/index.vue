<template>
  <div class="sys-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('sysSetting.title') }}</h3>
          <el-button type="primary" @click="handleSave">{{ t('coreSetting.save') }}</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="140px" v-loading="loading">
        <el-form-item :label="t('sysSetting.isNew')" prop="isNew">
          <el-tag :type="form.isNew ? 'success' : 'info'">
            {{ form.isNew ? t('common.yes') : t('common.no') }}
          </el-tag>
        </el-form-item>
        <el-form-item :label="t('sysSetting.pluginServer')" prop="pluginServer">
          <el-input v-model="form.pluginServer" :placeholder="t('sysSetting.pluginServerHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.pluginCache')" prop="pluginCache">
          <el-input-number v-model="form.pluginCache" :min="0" :placeholder="t('sysSetting.pluginCacheHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.networkDebug')" prop="networkDebug">
          <el-switch v-model="form.networkDebug" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.logLevel')" prop="logLevel">
          <el-select v-model="form.logLevel" :placeholder="t('sysSetting.logLevelHint')">
            <el-option label="All" :value="0" />
            <el-option label="Debug" :value="1" />
            <el-option label="Info" :value="2" />
            <el-option label="Warn" :value="3" />
            <el-option label="Error" :value="4" />
            <el-option label="Fatal" :value="5" />
            <el-option label="Off" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysSetting.logFileMaxBytes')" prop="logFileMaxBytes">
          <el-input-number v-model="form.logFileMaxBytes" :min="0" :placeholder="t('sysSetting.logFileMaxBytesHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.logFileBackups')" prop="logFileBackups">
          <el-input-number v-model="form.logFileBackups" :min="0" :placeholder="t('sysSetting.logFileBackupsHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.tempPath')" prop="tempPath">
          <el-input v-model="form.tempPath" :placeholder="t('sysSetting.tempPathHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.pluginPath')" prop="pluginPath">
          <el-input v-model="form.pluginPath" :placeholder="t('sysSetting.pluginPathHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.backupPath')" prop="backupPath">
          <el-input v-model="form.backupPath" :placeholder="t('sysSetting.backupPathHint')" />
        </el-form-item>
        <el-form-item :label="t('sysSetting.mode')" prop="mode">
          <el-select v-model="form.mode" :placeholder="t('sysSetting.modeHint')">
            <el-option :label="t('sysSetting.modeDevelopment')" value="Development" />
            <el-option :label="t('sysSetting.modeProduction')" value="Production" />
            <el-option :label="t('sysSetting.modeTest')" value="Test" />
          </el-select>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义系统设置类型接口
interface SysSetting {
  isNew: boolean;
  pluginServer: string;
  pluginCache: number;
  networkDebug: boolean;
  logLevel: number;
  logFileMaxBytes: number;
  logFileBackups: number;
  tempPath: string;
  pluginPath: string;
  backupPath: string;
  mode: string;
}

const loading = ref(false);
const formRef = ref<FormInstance | null>(null);
const { t } = useI18n();

// 表单数据
const form = reactive<SysSetting>({
  isNew: false,
  pluginServer: '',
  pluginCache: 600,
  networkDebug: false,
  logLevel: 2,
  logFileMaxBytes: 0,
  logFileBackups: 0,
  tempPath: '',
  pluginPath: '',
  backupPath: '',
  mode: 'Production',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  mode: [
    { required: true, message: t('sysSetting.modeRequired'), trigger: 'change' }
  ]
});

// 加载配置数据
const loadData = async () => {
  loading.value = true;
  try {
    const data = await request.get('/Admin/Sys');
    if (data) {
      Object.assign(form, data);
    }
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.value = false;
  }
};

// 保存配置
const handleSave = async () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        loading.value = true;
        await request.put('/Admin/Sys', form);
      } catch {
        // 错误提示已经在 request 拦截器中自动处理
      } finally {
        loading.value = false;
      }
    }
  });
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
