<template>
  <div class="xcode-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>XCode设置</h3>
          <el-button type="primary" @click="handleSave">保存设置</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="formRules" label-width="160px" v-loading="loading">
        <el-form-item label="新建" prop="isNew">
          <el-tag :type="form.isNew ? 'success' : 'info'">
            {{ form.isNew ? '是' : '否' }}
          </el-tag>
        </el-form-item>

        <el-divider content-position="left">基本设置</el-divider>
        <el-form-item label="调试" prop="debug">
          <el-switch v-model="form.debug" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="输出SQL" prop="showSQL">
          <el-switch v-model="form.showSQL" :active-value="true" :inactive-value="false" />
          <div class="form-tip">是否输出SQL语句，默认启用</div>
        </el-form-item>
        <el-form-item label="SQL目录" prop="sqlPath">
          <el-input v-model="form.sqlPath" placeholder="设置SQL输出的单独目录" clearable />
          <div class="form-tip">默认为空，SQL输出到当前日志中。生产环境建议输出到站点外单独的SqlLog目录</div>
        </el-form-item>
        <el-form-item label="SQL执行时间" prop="traceSQLTime">
          <el-input-number v-model="form.traceSQLTime" :min="0" placeholder="毫秒" />
          <div class="form-tip">跟踪SQL执行时间，大于该阀值将输出日志，默认1000毫秒</div>
        </el-form-item>
        <el-form-item label="SQL最大长度" prop="sqlMaxLength">
          <el-input-number v-model="form.sqlMaxLength" :min="0" placeholder="字符数" />
          <div class="form-tip">输出日志时的SQL最大长度，超长截断，默认4096，不截断用0</div>
        </el-form-item>
        <el-form-item label="参数化查询" prop="useParameter">
          <el-switch v-model="form.useParameter" :active-value="true" :inactive-value="false" />
          <div class="form-tip">参数化添删改查，默认关闭</div>
        </el-form-item>

        <el-divider content-position="left">批量操作</el-divider>
        <el-form-item label="批大小" prop="batchSize">
          <el-input-number v-model="form.batchSize" :min="1" placeholder="记录条数" />
          <div class="form-tip">用于批量操作数据，抽取、删除、备份、恢复，默认5000</div>
        </el-form-item>
        <el-form-item label="批操作间隙" prop="batchInterval">
          <el-input-number v-model="form.batchInterval" :min="0" placeholder="毫秒" />
          <div class="form-tip">用于批量删除数据时的暂停间隙，单位毫秒，默认100</div>
        </el-form-item>
        <el-form-item label="命令超时" prop="commandTimeout">
          <el-input-number v-model="form.commandTimeout" :min="0" placeholder="秒" />
          <div class="form-tip">查询执行超时时间，默认0秒不限制</div>
        </el-form-item>
        <el-form-item label="失败重试" prop="retryOnFailure">
          <el-input-number v-model="form.retryOnFailure" :min="0" placeholder="次数" />
          <div class="form-tip">执行命令超时后的重试次数，默认0不重试</div>
        </el-form-item>

        <el-divider content-position="left">反向工程</el-divider>
        <el-form-item label="检查注释" prop="checkComment">
          <el-switch v-model="form.checkComment" :active-value="true" :inactive-value="false" />
          <div class="form-tip">表注释或字段注释，反向工程，默认打开</div>
        </el-form-item>
        <el-form-item label="检查删除索引" prop="checkDeleteIndex">
          <el-switch v-model="form.checkDeleteIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">反向工程，默认打开</div>
        </el-form-item>
        <el-form-item label="检查添加索引" prop="checkAddIndex">
          <el-switch v-model="form.checkAddIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">反向工程，默认打开</div>
        </el-form-item>
        <el-form-item label="检查重复索引" prop="checkDuplicateIndex">
          <el-switch v-model="form.checkDuplicateIndex" :active-value="true" :inactive-value="false" />
          <div class="form-tip">反向工程，默认打开</div>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';

// 表单相关
const formRef = ref<FormInstance | null>(null);
const loading = ref(false);

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
    { required: true, message: '请输入SQL执行时间阀值', trigger: 'blur' },
    { type: 'number', min: 0, message: '值不能小于0', trigger: 'blur' }
  ],
  sqlMaxLength: [
    { required: true, message: '请输入SQL最大长度', trigger: 'blur' },
    { type: 'number', min: 0, message: '值不能小于0', trigger: 'blur' }
  ],
  batchSize: [
    { required: true, message: '请输入批大小', trigger: 'blur' },
    { type: 'number', min: 1, message: '值不能小于1', trigger: 'blur' }
  ],
  batchInterval: [
    { required: true, message: '请输入批操作间隙', trigger: 'blur' },
    { type: 'number', min: 0, message: '值不能小于0', trigger: 'blur' }
  ],
  commandTimeout: [
    { type: 'number', min: 0, message: '值不能小于0', trigger: 'blur' }
  ],
  retryOnFailure: [
    { type: 'number', min: 0, message: '值不能小于0', trigger: 'blur' }
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
