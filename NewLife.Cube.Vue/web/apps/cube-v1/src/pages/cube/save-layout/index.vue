<template>
  <div class="save-layout-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('saveLayout.title') }}</h3>
          <el-button type="primary" @click="handleSaveLayout">
            {{ t('saveLayout.saveLayout') }}
          </el-button>
        </div>
      </template>

      <el-form :model="layoutForm" :rules="formRules" ref="formRef" label-width="120px" class="layout-form">
        <el-form-item :label="t('saveLayout.userId')" prop="userid">
          <el-input-number
            v-model="layoutForm.userid"
            :placeholder="t('saveLayout.userIdHint')"
            :min="0"
          />
        </el-form-item>
        <el-form-item :label="t('saveLayout.category')" prop="category">
          <el-input v-model="layoutForm.category" :placeholder="t('saveLayout.categoryHint')" />
        </el-form-item>
        <el-form-item :label="t('saveLayout.name')" prop="name">
          <el-input v-model="layoutForm.name" :placeholder="t('saveLayout.nameHint')" />
        </el-form-item>
        <el-form-item :label="t('saveLayout.value')" prop="value">
          <el-input
            v-model="layoutForm.value"
            type="textarea"
            :rows="10"
            :placeholder="t('saveLayout.valueHint')"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSaveLayout" :loading="saving">
            {{ t('saveLayout.saveLayout') }}
          </el-button>
          <el-button @click="resetForm">{{ t('button.reset') }}</el-button>
          <el-button @click="formatJson">{{ t('setPageConfig.formatJson') }}</el-button>
        </el-form-item>
      </el-form>

      <!-- 保存历史记录 -->
      <el-card v-if="saveHistory.length > 0" class="history-card">
        <template #header>
          <h4>{{ t('setPageConfig.historyTitle') }}</h4>
        </template>
        <el-table :data="saveHistory" border style="width: 100%">
          <el-table-column prop="userid" :label="t('saveLayout.userId')" width="80" />
          <el-table-column prop="category" :label="t('saveLayout.category')" width="120" />
          <el-table-column prop="name" :label="t('saveLayout.name')" min-width="150" />
          <el-table-column
            prop="value"
            :label="t('saveLayout.value')"
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
interface SaveLayoutParams {
  userid?: number;
  category?: string;
  name?: string;
  value?: string;
}

interface HistoryItem extends SaveLayoutParams {
  saveTime: string;
}

// 表单引用
const formRef = ref<FormInstance | null>(null);
const saving = ref(false);
const { t } = useI18n();

// 表单数据
const layoutForm = reactive<SaveLayoutParams>({
  userid: 0,
  category: '',
  name: '',
  value: '',
});

// 保存历史
const saveHistory = ref<HistoryItem[]>([]);

// 表单验证规则
const formRules = reactive<FormRules>({
  userid: [
    { required: true, message: t('saveLayout.userIdRequired'), trigger: 'blur' },
  ],
  category: [
    { required: true, message: t('saveLayout.categoryRequired'), trigger: 'blur' },
  ],
  name: [
    { required: true, message: t('saveLayout.nameRequired'), trigger: 'blur' },
  ],
  value: [
    { required: true, message: t('saveLayout.valueRequired'), trigger: 'blur' },
  ],
});

// 保存布局
const handleSaveLayout = async () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      saving.value = true;
      try {
        await request.post('/Cube/SaveLayout', null, {
          params: {
            userid: layoutForm.userid,
            category: layoutForm.category,
            name: layoutForm.name,
            value: layoutForm.value,
          },
        });

        ElMessage.success(t('saveLayout.saveSuccess'));

        // 添加到历史记录
        saveHistory.value.unshift({
          ...layoutForm,
          saveTime: new Date().toLocaleString(),
        });

        // 保持历史记录在10条以内
        if (saveHistory.value.length > 10) {
          saveHistory.value = saveHistory.value.slice(0, 10);
        }

      } catch (error) {
        ElMessage.error(t('saveLayout.saveFailed'));
        console.error('Save layout error:', error);
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
  Object.assign(layoutForm, {
    userid: 0,
    category: '',
    name: '',
    value: '',
  });
};

// 格式化JSON
const formatJson = () => {
  if (layoutForm.value) {
    try {
      const parsed = JSON.parse(layoutForm.value);
      layoutForm.value = JSON.stringify(parsed, null, 2);
      ElMessage.success(t('setPageConfig.formatSuccess'));
    } catch {
      ElMessage.error(t('setPageConfig.invalidJsonShort'));
    }
  }
};

// 载入历史记录
const loadHistory = (item: HistoryItem) => {
  Object.assign(layoutForm, {
    userid: item.userid,
    category: item.category,
    name: item.name,
    value: item.value,
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

.layout-form {
  max-width: 800px;
}

.history-card {
  margin-top: 20px;
}
</style>
