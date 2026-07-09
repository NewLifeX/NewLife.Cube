<template>
  <div class="save-layout-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>保存布局配置</h3>
          <el-button type="primary" @click="handleSaveLayout">保存布局</el-button>
        </div>
      </template>

      <el-form :model="layoutForm" :rules="formRules" ref="formRef" label-width="120px" class="layout-form">
        <el-form-item label="用户ID" prop="userid">
          <el-input-number v-model="layoutForm.userid" placeholder="请输入用户ID" :min="0" />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-input v-model="layoutForm.category" placeholder="请输入分类" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="layoutForm.name" placeholder="请输入名称" />
        </el-form-item>
        <el-form-item label="配置值" prop="value">
          <el-input
            v-model="layoutForm.value"
            type="textarea"
            :rows="10"
            placeholder="请输入配置值（JSON格式）"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSaveLayout" :loading="saving">保存布局</el-button>
          <el-button @click="resetForm">重置</el-button>
          <el-button @click="formatJson">格式化JSON</el-button>
        </el-form-item>
      </el-form>

      <!-- 保存历史记录 -->
      <el-card v-if="saveHistory.length > 0" class="history-card">
        <template #header>
          <h4>保存历史</h4>
        </template>
        <el-table :data="saveHistory" border style="width: 100%">
          <el-table-column prop="userid" label="用户ID" width="80" />
          <el-table-column prop="category" label="分类" width="120" />
          <el-table-column prop="name" label="名称" min-width="150" />
          <el-table-column prop="value" label="配置值" min-width="200" show-overflow-tooltip />
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
    { required: true, message: '请输入用户ID', trigger: 'blur' },
  ],
  category: [
    { required: true, message: '请输入分类', trigger: 'blur' },
  ],
  name: [
    { required: true, message: '请输入名称', trigger: 'blur' },
  ],
  value: [
    { required: true, message: '请输入配置值', trigger: 'blur' },
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

        ElMessage.success('布局保存成功');

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
        ElMessage.error('布局保存失败');
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
      ElMessage.success('JSON格式化成功');
    } catch {
      ElMessage.error('JSON格式不正确');
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
  ElMessage.success('历史记录已载入');
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
