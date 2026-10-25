<template>
  <div class="attachment-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('attachmentAdmin.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('attachmentAdmin.upload') }}</el-button>
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
        <el-table-column prop="fileName" :label="t('cubeFile.fileName')" show-overflow-tooltip />
        <el-table-column prop="category" :label="t('lookup.category')" />
        <el-table-column prop="size" :label="t('attachmentAdmin.size')">
          <template #default="scope">
            {{ formatFileSize(scope.row.size) }}
          </template>
        </el-table-column>
        <el-table-column prop="contentType" :label="t('cubeFile.fileType')" />
        <el-table-column prop="downloads" :label="t('attachmentAdmin.downloads')" />
        <el-table-column prop="createTime" :label="t('cubeFile.uploadTime')" />
        <el-table-column prop="createUser" :label="t('attachmentAdmin.uploader')" />
        <el-table-column :label="t('userSearch.status')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.enable') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="180">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">
              {{ t('button.edit') }}
            </el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">
              {{ t('button.delete') }}
            </el-button>
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
      :title="formType === 'add' ? t('attachmentAdmin.upload') : t('attachmentAdmin.edit')"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('attachmentAdmin.fileUpload')" prop="file" v-if="formType === 'add'">
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
              {{ t('attachmentAdmin.dropHint') }}<em>{{ t('attachmentAdmin.clickUpload') }}</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">
                {{ t('attachmentAdmin.uploadTip') }}
              </div>
            </template>
          </el-upload>
        </el-form-item>
        <el-form-item :label="t('cubeFile.fileName')" prop="fileName">
          <el-input v-model="form.fileName" :placeholder="t('attachmentAdmin.fileNameHint')" />
        </el-form-item>
        <el-form-item :label="t('cronJob.displayName')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('attachmentAdmin.displayNameHint')" />
        </el-form-item>
        <el-form-item :label="t('lookup.category')" prop="category">
          <el-input v-model="form.category" :placeholder="t('attachmentAdmin.categoryHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.enableStatus')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
        <el-form-item :label="t('lookup.description')" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            :placeholder="t('cronJob.descriptionHint')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm" :loading="uploading">
          {{ formType === 'add' ? t('attachmentAdmin.uploadAction') : t('button.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { UploadFilled } from '@element-plus/icons-vue';
import type { FormInstance, FormRules, UploadProps, UploadFile } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

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
const { t } = useI18n();
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
    { required: true, message: t('attachmentAdmin.fileNameRequired'), trigger: 'blur' }
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
    ElMessage.error(t('common.loadFailed'));
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
    ElMessage.error(t('attachmentAdmin.sizeExceeded'));
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
    t('attachmentAdmin.deleteConfirm', { name: row.displayName || row.fileName }),
    t('fileManager.confirmDelete'),
    {
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/Attachment/${row.id}`);
      ElMessage.success(t('common.deleteSuccess'));
      loadData();
    } catch (error) {
      ElMessage.error(t('common.deleteFailed'));
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
            ElMessage.error(t('attachmentAdmin.fileRequired'));
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
          ElMessage.success(t('attachmentAdmin.uploadSuccess'));
        } else {
          await request.put(`/Cube/Attachment/${form.id}`, form);
          ElMessage.success(t('common.updateSuccess'));
        }

        dialogVisible.value = false;
        loadData();
      } catch (error) {
        ElMessage.error(
          formType.value === 'add' ? t('attachmentAdmin.uploadFailed') : t('common.updateFailed')
        );
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
