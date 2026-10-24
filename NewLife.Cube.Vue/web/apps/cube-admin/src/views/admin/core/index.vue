<template>
  <div class="core-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('coreSetting.title') }}</h3>
          <el-button type="primary" @click="handleSave">{{ t('coreSetting.save') }}</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px" v-loading="loading">
        <el-form-item :label="t('coreSetting.systemName')" prop="name">
          <el-input v-model="form.name" :placeholder="t('coreSetting.systemNameHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.version')" prop="version">
          <el-input v-model="form.version" :placeholder="t('coreSetting.versionHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('coreSetting.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.company')" prop="company">
          <el-input v-model="form.company" :placeholder="t('coreSetting.companyHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.instance')" prop="instance">
          <el-input-number v-model="form.instance" :min="0" :placeholder="t('coreSetting.instanceHint')" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.develop')" prop="develop">
          <el-switch v-model="form.develop" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.enable')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('coreSetting.installTime')" prop="installTime">
          <el-date-picker
            v-model="form.installTime"
            type="datetime"
            :placeholder="t('coreSetting.installTime')"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 定义系统配置类型接口
interface SysConfig {
  isNew: boolean;
  name: string;
  version: string;
  displayName: string;
  company: string;
  instance: number;
  develop: boolean;
  enable: boolean;
  installTime: string;
}

const loading = ref(false);
const formRef = ref<FormInstance | null>(null);
const { t } = useI18n();

// 表单数据
const form = reactive<SysConfig>({
  isNew: false,
  name: '',
  version: '',
  displayName: '',
  company: '',
  instance: 0,
  develop: false,
  enable: true,
  installTime: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: t('coreSetting.systemNameRequired'), trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t('coreSetting.displayNameRequired'), trigger: 'blur' }
  ]
});

// 加载配置数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Admin/Core');

    if (response && response.data) {
      const data = response.data.data || response.data;
      Object.assign(form, data);
    }
  } catch (error) {
    ElMessage.error(t('coreSetting.loadFailed'));
    console.error('Failed to load the core settings:', error);
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
        await request.put('/Admin/Core', form);
        ElMessage.success(t('coreSetting.saveSuccess'));
      } catch (error) {
        ElMessage.error(t('coreSetting.saveFailed'));
        console.error('Failed to save the core settings:', error);
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
