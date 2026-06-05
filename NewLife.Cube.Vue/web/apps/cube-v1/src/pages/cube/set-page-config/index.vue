<template>
  <div class="set-page-config-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>设置页面配置</h3>
          <el-button type="primary" @click="handleSetConfig">保存配置</el-button>
        </div>
      </template>

      <el-form :model="configForm" :rules="formRules" ref="formRef" label-width="120px" class="config-form">
        <el-form-item label="类型" prop="kind">
          <el-input v-model="configForm.kind" placeholder="请输入配置类型" />
        </el-form-item>
        <el-form-item label="页面" prop="page">
          <el-input v-model="configForm.page" placeholder="请输入页面名称" />
        </el-form-item>
        <el-form-item label="配置数据" prop="configData">
          <el-input
            v-model="configForm.configData"
            type="textarea"
            :rows="15"
            placeholder="请输入配置数据（JSON格式）"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSetConfig" :loading="saving">保存配置</el-button>
          <el-button @click="resetForm">重置</el-button>
          <el-button @click="formatJson">格式化JSON</el-button>
          <el-button @click="validateJson">验证JSON</el-button>
        </el-form-item>
      </el-form>

      <!-- 配置模板 -->
      <el-card class="template-card">
        <template #header>
          <h4>配置模板</h4>
        </template>
        <el-row :gutter="20">
          <el-col :span="8" v-for="template in configTemplates" :key="template.name">
            <el-card class="template-item" @click="loadTemplate(template)">
              <h5>{{ template.name }}</h5>
              <p>{{ template.description }}</p>
            </el-card>
          </el-col>
        </el-row>
      </el-card>

      <!-- 保存历史记录 -->
      <el-card v-if="saveHistory.length > 0" class="history-card">
        <template #header>
          <h4>保存历史</h4>
        </template>
        <el-table :data="saveHistory" border style="width: 100%">
          <el-table-column prop="kind" label="类型" width="150" />
          <el-table-column prop="page" label="页面" width="150" />
          <el-table-column prop="configData" label="配置数据" min-width="200" show-overflow-tooltip />
          <el-table-column prop="saveTime" label="保存时间" width="160" />
          <el-table-column label="操作" width="120">
            <template #default="scope">
              <el-button type="primary" size="small" @click="loadHistory(scope.row)">载入</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { request } from 'cube-front/core/utils/request';
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
  name: string;
  description: string;
  kind: string;
  page: string;
  data: string;
}

// 表单引用
const formRef = ref<FormInstance | null>(null);
const saving = ref(false);

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
    name: '表格配置',
    description: '数据表格的基本配置',
    kind: 'table',
    page: 'list',
    data: JSON.stringify({
      columns: [
        { prop: 'id', label: 'ID', width: 80 },
        { prop: 'name', label: '名称', minWidth: 150 },
        { prop: 'status', label: '状态', width: 100 }
      ],
      pagination: {
        pageSize: 20,
        pageSizes: [10, 20, 50, 100]
      }
    }, null, 2)
  },
  {
    name: '表单配置',
    description: '表单字段的配置',
    kind: 'form',
    page: 'edit',
    data: JSON.stringify({
      fields: [
        { name: 'name', label: '名称', type: 'input', required: true },
        { name: 'email', label: '邮箱', type: 'input', required: true },
        { name: 'status', label: '状态', type: 'switch', required: false }
      ],
      layout: {
        labelWidth: '100px',
        size: 'default'
      }
    }, null, 2)
  },
  {
    name: '菜单配置',
    description: '页面菜单的配置',
    kind: 'menu',
    page: 'navigation',
    data: JSON.stringify({
      items: [
        { name: '首页', path: '/home', icon: 'home' },
        { name: '用户管理', path: '/users', icon: 'user' },
        { name: '设置', path: '/settings', icon: 'setting' }
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
    { required: true, message: '请输入配置类型', trigger: 'blur' },
  ],
  page: [
    { required: true, message: '请输入页面名称', trigger: 'blur' },
  ],
  configData: [
    { required: true, message: '请输入配置数据', trigger: 'blur' },
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
        ElMessage.error('配置数据不是有效的JSON格式');
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

        ElMessage.success('配置保存成功');

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
        ElMessage.error('配置保存失败');
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
      ElMessage.success('JSON格式化成功');
    } catch {
      ElMessage.error('JSON格式不正确');
    }
  }
};

// 验证JSON
const validateJson = () => {
  if (!configForm.configData) {
    ElMessage.warning('请先输入配置数据');
    return;
  }

  try {
    JSON.parse(configForm.configData);
    ElMessage.success('JSON格式验证通过');
  } catch {
    ElMessage.error('JSON格式验证失败');
  }
};

// 载入模板
const loadTemplate = (template: ConfigTemplate) => {
  Object.assign(configForm, {
    kind: template.kind,
    page: template.page,
    configData: template.data,
  });
  ElMessage.success(`模板 "${template.name}" 已载入`);
};

// 载入历史记录
const loadHistory = (item: HistoryItem) => {
  Object.assign(configForm, {
    kind: item.kind,
    page: item.page,
    configData: item.configData,
  });
  ElMessage.success('历史记录已载入');
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
