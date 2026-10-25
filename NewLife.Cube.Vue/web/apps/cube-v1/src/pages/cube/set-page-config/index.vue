<template>
  <div class="set-page-config-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('setPageConfig.title') }}</h3>
          <el-button type="primary" @click="handleSetConfig">
            {{ t('setPageConfig.saveConfig') }}
          </el-button>
        </div>
      </template>

      <el-form :model="configForm" :rules="formRules" ref="formRef" label-width="120px" class="config-form">
        <el-form-item :label="t('setPageConfig.kind')" prop="kind">
          <el-input v-model="configForm.kind" :placeholder="t('setPageConfig.kindHint')" />
        </el-form-item>
        <el-form-item :label="t('setPageConfig.page')" prop="page">
          <el-input v-model="configForm.page" :placeholder="t('setPageConfig.pageHint')" />
        </el-form-item>
        <el-form-item :label="t('setPageConfig.configData')" prop="configData">
          <el-input
            v-model="configForm.configData"
            type="textarea"
            :rows="15"
            :placeholder="t('setPageConfig.configDataHint')"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSetConfig" :loading="saving">
            {{ t('setPageConfig.saveConfig') }}
          </el-button>
          <el-button @click="resetForm">{{ t('button.reset') }}</el-button>
          <el-button @click="formatJson">{{ t('setPageConfig.formatJson') }}</el-button>
          <el-button @click="validateJson">{{ t('setPageConfig.validateJson') }}</el-button>
        </el-form-item>
      </el-form>

      <!-- 配置模板 -->
      <el-card class="template-card">
        <template #header>
          <h4>{{ t('setPageConfig.templatesTitle') }}</h4>
        </template>
        <el-row :gutter="20">
          <el-col :span="8" v-for="template in configTemplates" :key="template.nameKey">
            <el-card class="template-item" @click="loadTemplate(template)">
              <h5>{{ t(template.nameKey) }}</h5>
              <p>{{ t(template.descKey) }}</p>
            </el-card>
          </el-col>
        </el-row>
      </el-card>

      <!-- 保存历史记录 -->
      <el-card v-if="saveHistory.length > 0" class="history-card">
        <template #header>
          <h4>{{ t('setPageConfig.historyTitle') }}</h4>
        </template>
        <el-table :data="saveHistory" border style="width: 100%">
          <el-table-column prop="kind" :label="t('setPageConfig.kind')" width="150" />
          <el-table-column prop="page" :label="t('setPageConfig.page')" width="150" />
          <el-table-column
            prop="configData"
            :label="t('setPageConfig.configData')"
            min-width="200"
            show-overflow-tooltip
          />
          <el-table-column prop="saveTime" :label="t('setPageConfig.saveTime')" width="160" />
          <el-table-column :label="t('common.actions')" width="120">
            <template #default="scope">
              <el-button type="primary" size="small" @click="loadHistory(scope.row)">
                {{ t('setPageConfig.load') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';

// 定义接口类型
interface SetConfigParams {
  kind?: string;
  page?: string;
  configData?: string;
}

interface HistoryItem extends SetConfigParams {
  saveTime: string;
}

interface ConfigTemplate {
  nameKey: string;
  descKey: string;
  kind: string;
  page: string;
  data: string;
}

// 表单引用
const formRef = ref<FormInstance | null>(null);
const saving = ref(false);
const { t } = useI18n();

// 表单数据
const configForm = reactive<SetConfigParams>({
  kind: '',
  page: '',
  configData: '',
});

// 保存历史
const saveHistory = ref<HistoryItem[]>([]);

// 配置模板
const configTemplates = ref<ConfigTemplate[]>([
  {
    nameKey: 'setPageConfig.templateTable.name',
    descKey: 'setPageConfig.templateTable.desc',
    kind: 'table',
    page: 'list',
    data: JSON.stringify({
      columns: [
        { prop: 'id', label: 'ID', width: 80 },
        { prop: 'name', label: t('setPageConfig.fieldName'), minWidth: 150 },
        { prop: 'status', label: t('setPageConfig.fieldStatus'), width: 100 }
      ],
      pagination: {
        pageSize: 20,
        pageSizes: [10, 20, 50, 100]
      }
    }, null, 2)
  },
  {
    nameKey: 'setPageConfig.templateForm.name',
    descKey: 'setPageConfig.templateForm.desc',
    kind: 'form',
    page: 'edit',
    data: JSON.stringify({
      fields: [
        { name: 'name', label: t('setPageConfig.fieldName'), type: 'input', required: true },
        { name: 'email', label: t('setPageConfig.fieldEmail'), type: 'input', required: true },
        { name: 'status', label: t('setPageConfig.fieldStatus'), type: 'switch', required: false }
      ],
      layout: {
        labelWidth: '100px',
        size: 'default'
      }
    }, null, 2)
  },
  {
    nameKey: 'setPageConfig.templateMenu.name',
    descKey: 'setPageConfig.templateMenu.desc',
    kind: 'menu',
    page: 'navigation',
    data: JSON.stringify({
      items: [
        { name: t('setPageConfig.menuHome'), path: '/home', icon: 'home' },
        { name: t('setPageConfig.menuUsers'), path: '/users', icon: 'user' },
        { name: t('setPageConfig.menuSettings'), path: '/settings', icon: 'setting' }
      ],
      style: {
        theme: 'dark',
        collapsed: false
      }
    }, null, 2)
  }
]);

// 表单验证规则
const formRules = reactive<FormRules>({
  kind: [
    { required: true, message: t('setPageConfig.kindRequired'), trigger: 'blur' },
  ],
  page: [
    { required: true, message: t('setPageConfig.pageRequired'), trigger: 'blur' },
  ],
  configData: [
    { required: true, message: t('setPageConfig.configDataRequired'), trigger: 'blur' },
  ],
});

// 设置配置
const handleSetConfig = async () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      // 验证JSON格式
      try {
        JSON.parse(configForm.configData || '{}');
      } catch {
        ElMessage.error(t('setPageConfig.invalidJson'));
        return;
      }

      saving.value = true;
      try {
        await request.post('/Cube/SetPageConfig', JSON.parse(configForm.configData || '{}'), {
          params: {
            kind: configForm.kind,
            page: configForm.page,
          },
        });

        ElMessage.success(t('setPageConfig.saveSuccess'));

        // 添加到历史记录
        saveHistory.value.unshift({
          ...configForm,
          saveTime: new Date().toLocaleString(),
        });

        // 保持历史记录在10条以内
        if (saveHistory.value.length > 10) {
          saveHistory.value = saveHistory.value.slice(0, 10);
        }

      } catch (error) {
        ElMessage.error(t('setPageConfig.saveFailed'));
        console.error('Set page config error:', error);
      } finally {
        saving.value = false;
      }
    }
  });
};

// 重置表单
const resetForm = () => {
  if (formRef.value) {
    formRef.value.resetFields();
  }
  Object.assign(configForm, {
    kind: '',
    page: '',
    configData: '',
  });
};

// 格式化JSON
const formatJson = () => {
  if (configForm.configData) {
    try {
      const parsed = JSON.parse(configForm.configData);
      configForm.configData = JSON.stringify(parsed, null, 2);
      ElMessage.success(t('setPageConfig.formatSuccess'));
    } catch {
      ElMessage.error(t('setPageConfig.invalidJsonShort'));
    }
  }
};

// 验证JSON
const validateJson = () => {
  if (!configForm.configData) {
    ElMessage.warning(t('setPageConfig.configDataRequired'));
    return;
  }

  try {
    JSON.parse(configForm.configData);
    ElMessage.success(t('setPageConfig.validateSuccess'));
  } catch {
    ElMessage.error(t('setPageConfig.validateFailed'));
  }
};

// 载入模板
const loadTemplate = (template: ConfigTemplate) => {
  Object.assign(configForm, {
    kind: template.kind,
    page: template.page,
    configData: template.data,
  });
  ElMessage.success(t('setPageConfig.templateLoaded', { name: t(template.nameKey) }));
};

// 载入历史记录
const loadHistory = (item: HistoryItem) => {
  Object.assign(configForm, {
    kind: item.kind,
    page: item.page,
    configData: item.configData,
  });
  ElMessage.success(t('setPageConfig.historyLoaded'));
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.config-form {
  max-width: 800px;
}

.template-card {
  margin-top: 20px;
}

.template-item {
  cursor: pointer;
  transition: all 0.3s;
  margin-bottom: 10px;
}

.template-item:hover {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.template-item h5 {
  margin: 0 0 8px 0;
  color: #409eff;
}

.template-item p {
  margin: 0;
  color: #666;
  font-size: 12px;
}

.history-card {
  margin-top: 20px;
}
</style>
