<template>
  <div class="core-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>基本设置</h3>
          <el-button type="primary" @click="handleSave">保存设置</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="120px" v-loading="loading">
        <el-form-item label="系统名称" prop="name">
          <el-input v-model="form.name" placeholder="用于标识系统的英文名" />
        </el-form-item>
        <el-form-item label="版本号" prop="version">
          <el-input v-model="form.version" placeholder="系统版本号" />
        </el-form-item>
        <el-form-item label="显示名称" prop="displayName">
          <el-input v-model="form.displayName" placeholder="用户可见的名称" />
        </el-form-item>
        <el-form-item label="公司名称" prop="company">
          <el-input v-model="form.company" placeholder="公司名称" />
        </el-form-item>
        <el-form-item label="实例编号" prop="instance">
          <el-input-number v-model="form.instance" :min="0" placeholder="实例编号" />
        </el-form-item>
        <el-form-item label="开发模式" prop="develop">
          <el-switch v-model="form.develop" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="安装时间" prop="installTime">
          <el-date-picker
            v-model="form.installTime"
            type="datetime"
            placeholder="安装时间"
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
    { required: true, message: '请输入系统名称', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入显示名称', trigger: 'blur' }
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
    ElMessage.error('加载配置失败');
    console.error('加载配置失败:', error);
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
        ElMessage.success('保存配置成功');
      } catch (error) {
        ElMessage.error('保存配置失败');
        console.error('保存配置失败:', error);
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
