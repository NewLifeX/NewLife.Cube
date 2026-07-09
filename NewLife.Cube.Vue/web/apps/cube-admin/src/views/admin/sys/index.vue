<template>
  <div class="sys-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>系统设置</h3>
          <el-button type="primary" @click="handleSave">保存设置</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="140px" v-loading="loading">
        <el-form-item label="新建" prop="isNew">
          <el-tag :type="form.isNew ? 'success' : 'info'">
            {{ form.isNew ? '是' : '否' }}
          </el-tag>
        </el-form-item>
        <el-form-item label="插件服务器" prop="pluginServer">
          <el-input v-model="form.pluginServer" placeholder="插件服务器地址" />
        </el-form-item>
        <el-form-item label="插件缓存时间" prop="pluginCache">
          <el-input-number v-model="form.pluginCache" :min="0" placeholder="插件缓存时间（秒）" />
        </el-form-item>
        <el-form-item label="网络调试" prop="networkDebug">
          <el-switch v-model="form.networkDebug" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="日志级别" prop="logLevel">
          <el-select v-model="form.logLevel" placeholder="请选择日志级别">
            <el-option label="All" :value="0" />
            <el-option label="Debug" :value="1" />
            <el-option label="Info" :value="2" />
            <el-option label="Warn" :value="3" />
            <el-option label="Error" :value="4" />
            <el-option label="Fatal" :value="5" />
            <el-option label="Off" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item label="日志文件上限" prop="logFileMaxBytes">
          <el-input-number v-model="form.logFileMaxBytes" :min="0" placeholder="日志文件最大字节数" />
        </el-form-item>
        <el-form-item label="日志文件备份" prop="logFileBackups">
          <el-input-number v-model="form.logFileBackups" :min="0" placeholder="日志文件备份数量" />
        </el-form-item>
        <el-form-item label="临时目录" prop="tempPath">
          <el-input v-model="form.tempPath" placeholder="临时目录路径" />
        </el-form-item>
        <el-form-item label="插件目录" prop="pluginPath">
          <el-input v-model="form.pluginPath" placeholder="插件目录路径" />
        </el-form-item>
        <el-form-item label="备份目录" prop="backupPath">
          <el-input v-model="form.backupPath" placeholder="备份目录路径" />
        </el-form-item>
        <el-form-item label="运行模式" prop="mode">
          <el-select v-model="form.mode" placeholder="请选择运行模式">
            <el-option label="开发模式" value="Development" />
            <el-option label="生产模式" value="Production" />
            <el-option label="测试模式" value="Test" />
          </el-select>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
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
    { required: true, message: '请选择运行模式', trigger: 'change' }
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
