<template>
  <div class="attachment-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>附件管理</h3>
          <el-button type="primary" @click="handleAdd">上传附件</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
      >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="fileName" label="文件名" show-overflow-tooltip />
        <el-table-column prop="category" label="分类" />
        <el-table-column prop="size" label="大小">
          <template #default="scope">
            {{ formatFileSize(scope.row.size) }}
          </template>
        </el-table-column>
        <el-table-column prop="contentType" label="类型" />
        <el-table-column prop="downloads" label="下载次数" />
        <el-table-column prop="createTime" label="上传时间" />
        <el-table-column prop="createUser" label="上传者" />
        <el-table-column label="状态" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 附件表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '上传附件' : '编辑附件'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="文件上传" prop="file" v-if="formType === 'add'">
          <el-upload
            ref="uploadRef"
            :auto-upload="false"
            :on-change="handleFileChange"
            :before-upload="beforeUpload"
            drag
            action="#"
            :limit="1"
            accept="*/*"
          >
            <el-icon class="el-icon--upload"><upload-filled /></el-icon>
            <div class="el-upload__text">
              将文件拖拽到此处，或<em>点击上传</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">
                支持任意格式文件，单个文件不超过100MB
              </div>
            </template>
          </el-upload>
        </el-form-item>
        <el-form-item label="文件名" prop="fileName">
          <el-input v-model="form.fileName" placeholder="请输入文件名" />
        </el-form-item>
        <el-form-item label="显示名" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名" />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-input v-model="form.category" placeholder="请输入分类" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm" :loading="uploading">
          {{ formType === 'add' ? '上传' : '保存' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';

import { ElMessage, ElMessageBox } from 'element-plus';
import { UploadFilled } from '@element-plus/icons-vue';
import type { FormInstance, FormRules, UploadProps, UploadFile } from 'element-plus';
import { request } from 'cube-front/core/utils/request';
import { apiDataToList } from 'cube-front/core/utils/api-helpers';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

// 附件类型接口，继承 BaseEntity
interface Attachment extends BaseEntity {
  fileName: string;
  displayName: string;
  category: string;
  size: number;
  contentType: string;
  hash: string;
  downloads: number;
  enable: boolean;
  description: string;
  createUser: string;
}

// 响应式数据
const loading = ref(false);
const uploading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const uploadRef = ref();
const tableData = ref<Attachment[]>([]);
const selectedFile = ref<File | null>(null);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  category: '',
  dateRange: null as [string, string] | null,
  ...pageInfoDefault
});

// 附件表单
const form = reactive<Partial<Attachment>>({
  fileName: '',
  displayName: '',
  category: '',
  enable: true,
  description: '',
});

// 表单验证规则
const formRules = reactive<FormRules>({
  fileName: [
    { required: true, message: '请输入文件名', trigger: 'blur' }
  ],
});

// 格式化文件大小
const formatFileSize = (bytes: number) => {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};


// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const params: Record<string, unknown> = { ...queryParams };
    if (queryParams.dateRange) {
      params.startTime = queryParams.dateRange[0];
      params.endTime = queryParams.dateRange[1];
    }
    const response = await request.get('/Cube/Attachment', { params });
    const { list, page } = apiDataToList<Attachment>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error('加载数据失败');
    console.error('加载数据失败:', error);
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { q: '', category: '', dateRange: null, pageIndex: 1 }, e || {});
};

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 文件选择变化
const handleFileChange: UploadProps['onChange'] = (uploadFile: UploadFile) => {
  selectedFile.value = uploadFile.raw || null;
  if (selectedFile.value) {
    form.fileName = selectedFile.value.name;
    form.displayName = selectedFile.value.name;
  }
};

// 上传前检查
const beforeUpload = (file: File) => {
  const isLt100M = file.size / 1024 / 1024 < 100;
  if (!isLt100M) {
    ElMessage.error('上传文件大小不能超过 100MB!');
  }
  return isLt100M;
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Attachment) => {
  formType.value = 'edit';
  Object.assign(form, row);
  dialogVisible.value = true;
};

// 删除
const handleDelete = (row: Attachment) => {
  ElMessageBox.confirm(
    `确定要删除附件"${row.displayName || row.fileName}"吗？`,
    '确认删除',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/Attachment?id=${row.id}`);
      ElMessage.success('删除成功');
      loadData();
    } catch (error) {
      ElMessage.error('删除失败');
      console.error('删除失败:', error);
    }
  });
};

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    fileName: '',
    displayName: '',
    category: '',
    enable: true,
    description: '',
  });
  selectedFile.value = null;
  uploadRef.value?.clearFiles();
  formRef.value?.clearValidate();
};

// 提交表单
const submitForm = () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        uploading.value = true;

        if (formType.value === 'add') {
          if (!selectedFile.value) {
            ElMessage.error('请选择文件');
            return;
          }

          const formData = new FormData();
          formData.append('file', selectedFile.value);
          Object.keys(form).forEach(key => {
            const value = form[key as keyof typeof form];
            if (value !== undefined) {
              formData.append(key, String(value));
            }
          });

          await request.post('/Cube/Attachment', formData, {
            headers: {
              'Content-Type': 'multipart/form-data',
            },
          });
          ElMessage.success('上传成功');
        } else {
          await request.put('/Cube/Attachment', form);
          ElMessage.success('更新成功');
        }

        dialogVisible.value = false;
        loadData();
      } catch (error) {
        ElMessage.error(`${formType.value === 'add' ? '上传' : '更新'}失败`);
        console.error('操作失败:', error);
      } finally {
        uploading.value = false;
      }
    }
  });
};

// 初始化
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

.search-form {
  margin-bottom: 16px;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
