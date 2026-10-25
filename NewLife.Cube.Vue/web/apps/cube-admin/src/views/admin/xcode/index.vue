<template>
  <div class="xcode-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('xcodeSetting.title') }}</h3>
          <el-button type="primary" @click="handleSave">{{ t('coreSetting.save') }}</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="160px" v-loading="loading">
        <el-form-item :label="t('sysSetting.isNew')" prop="isNew">
          <el-tag :type="form.isNew ? 'success' : 'info'">
            {{ form.isNew ? t('common.yes') : t('common.no') }}
          </el-tag>
        </el-form-item>

        <el-divider content-position="left">{{ t('xcodeSetting.groupBasic') }}</el-divider>
        <el-form-item :label="t('xcodeSetting.debug')" prop="debug">
          <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.showSQL')" prop="showSQL">
          <el-switch v-model="form.showSQL" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.showSQLTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.sqlPath')" prop="sqlPath">
          <el-input v-model="form.sqlPath" :placeholder="t('xcodeSetting.sqlPathHint')" clearable />
          <div class="form-tip">{{ t('xcodeSetting.sqlPathTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.traceSQLTime')" prop="traceSQLTime">
          <el-input-number v-model="form.traceSQLTime" :min="0" :placeholder="t('xcodeSetting.milliseconds')" />
          <div class="form-tip">{{ t('xcodeSetting.traceSQLTimeTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.sqlMaxLength')" prop="sqlMaxLength">
          <el-input-number v-model="form.sqlMaxLength" :min="0" :placeholder="t('xcodeSetting.characters')" />
          <div class="form-tip">{{ t('xcodeSetting.sqlMaxLengthTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.useParameter')" prop="useParameter">
          <el-switch v-model="form.useParameter" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.useParameterTip') }}</div>
        </el-form-item>

        <el-divider content-position="left">{{ t('xcodeSetting.groupBatch') }}</el-divider>
        <el-form-item :label="t('xcodeSetting.batchSize')" prop="batchSize">
          <el-input-number v-model="form.batchSize" :min="1" :placeholder="t('xcodeSetting.rowCount')" />
          <div class="form-tip">{{ t('xcodeSetting.batchSizeTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.batchInterval')" prop="batchInterval">
          <el-input-number v-model="form.batchInterval" :min="0" :placeholder="t('xcodeSetting.milliseconds')" />
          <div class="form-tip">{{ t('xcodeSetting.batchIntervalTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.commandTimeout')" prop="commandTimeout">
          <el-input-number v-model="form.commandTimeout" :min="0" :placeholder="t('xcodeSetting.seconds')" />
          <div class="form-tip">{{ t('xcodeSetting.commandTimeoutTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.retryOnFailure')" prop="retryOnFailure">
          <el-input-number v-model="form.retryOnFailure" :min="0" :placeholder="t('xcodeSetting.times')" />
          <div class="form-tip">{{ t('xcodeSetting.retryOnFailureTip') }}</div>
        </el-form-item>

        <el-divider content-position="left">{{ t('xcodeSetting.groupReverse') }}</el-divider>
        <el-form-item :label="t('xcodeSetting.checkComment')" prop="checkComment">
          <el-switch v-model="form.checkComment" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.checkCommentTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.checkDeleteIndex')" prop="checkDeleteIndex">
          <el-switch v-model="form.checkDeleteIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.reverseTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.checkAddIndex')" prop="checkAddIndex">
          <el-switch v-model="form.checkAddIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.reverseTip') }}</div>
        </el-form-item>
        <el-form-item :label="t('xcodeSetting.checkDuplicateIndex')" prop="checkDuplicateIndex">
          <el-switch v-model="form.checkDuplicateIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">{{ t('xcodeSetting.reverseTip') }}</div>
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

// 表单相关
const formRef = ref<FormInstance | null>(null);
const loading = ref(false);
const { t } = useI18n();

// 表单数据
const form = reactive({
  isNew: false,
  debug: false,
  showSQL: true,
  sqlPath: '',
  traceSQLTime: 1000,
  sqlMaxLength: 4096,
  useParameter: false,
  batchSize: 5000,
  batchInterval: 100,
  commandTimeout: 0,
  retryOnFailure: 0,
  checkComment: true,
  checkDeleteIndex: true,
  checkAddIndex: true,
  checkDuplicateIndex: true,
});

// 表单验证规则
const formRules: FormRules = {
  traceSQLTime: [
    { required: true, message: t('xcodeSetting.traceSQLTimeRequired'), trigger: 'blur' },
    { type: 'number', min: 0, message: t('xcodeSetting.minZero'), trigger: 'blur' }
  ],
  sqlMaxLength: [
    { required: true, message: t('xcodeSetting.sqlMaxLengthRequired'), trigger: 'blur' },
    { type: 'number', min: 0, message: t('xcodeSetting.minZero'), trigger: 'blur' }
  ],
  batchSize: [
    { required: true, message: t('xcodeSetting.batchSizeRequired'), trigger: 'blur' },
    { type: 'number', min: 1, message: t('xcodeSetting.minOne'), trigger: 'blur' }
  ],
  batchInterval: [
    { required: true, message: t('xcodeSetting.batchIntervalRequired'), trigger: 'blur' },
    { type: 'number', min: 0, message: t('xcodeSetting.minZero'), trigger: 'blur' }
  ],
  commandTimeout: [
    { type: 'number', min: 0, message: t('xcodeSetting.minZero'), trigger: 'blur' }
  ],
  retryOnFailure: [
    { type: 'number', min: 0, message: t('xcodeSetting.minZero'), trigger: 'blur' }
  ]
};

// 接口调用函数
const api = {
  getSettings: async () => {
    return await request.get('/Admin/XCode');
  },
  updateSettings: async (data: typeof form) => {
    return await request.put('/Admin/XCode', data);
  }
};

// 加载数据
const loadData = async () => {
  try {
    loading.value = true;
    const response = await api.getSettings();

    if (response) {
      Object.assign(form, response);
    }
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.value = false;
  }
};

// 保存设置
const handleSave = async () => {
  if (!formRef.value) return;

  try {
    await formRef.value.validate();
    loading.value = true;

    const response = await api.updateSettings(form);

    if (response) {
      Object.assign(form, response);
    }
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.value = false;
  }
};

// 组件挂载
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

.card-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
}

.form-tip {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
  line-height: 1.4;
}

.el-divider {
  margin: 24px 0 16px 0;
}

.el-input-number {
  width: 200px;
}
</style>
