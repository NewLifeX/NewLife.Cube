<template>
  <div class="cube-file-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('cubeFile.title') }}</h3>
          <el-button type="primary" @click="handleGetFile">{{ t('cubeFile.getFile') }}</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item :label="t('cubeFile.fileId')">
          <el-input v-model="searchForm.id" :placeholder="t('cubeFile.fileIdHint')" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleGetFile">{{ t('cubeFile.getFile') }}</el-button>
          <el-button @click="resetSearch">{{ t('button.reset') }}</el-button>
        </el-form-item>
      </el-form>

      <!-- 文件列表 -->
      <el-table :data="fileList" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" :label="t('cubeFile.fileId')" width="120" />
        <el-table-column prop="name" :label="t('cubeFile.fileName')" min-width="200" />
        <el-table-column prop="size" :label="t('cubeFile.fileSize')" width="100">
          <template #default="scope">
            {{ formatFileSize(scope.row.size) }}
          </template>
        </el-table-column>
        <el-table-column prop="type" :label="t('cubeFile.fileType')" width="120" />
        <el-table-column
          prop="url"
          :label="t('cubeFile.fileUrl')"
          min-width="200"
          show-overflow-tooltip
        />
        <el-table-column prop="uploadTime" :label="t('cubeFile.uploadTime')" width="160" />
        <el-table-column :label="t('common.actions')" width="200">
          <template #default="scope">
            <el-button type="primary" size="small" @click="previewFile(scope.row)">
              {{ t('cubeFile.preview') }}
            </el-button>
            <el-button type="success" size="small" @click="downloadFile(scope.row)">
              {{ t('cubeFile.download') }}
            </el-button>
            <el-button type="info" size="small" @click="copyFileUrl(scope.row)">
              {{ t('cubeFile.copyLink') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 文件预览对话框 -->
    <el-dialog v-model="previewVisible" :title="previewFile.name" width="80%">
      <div class="file-preview-container">
        <!-- 图片预览 -->
        <div v-if="isImageFile(selectedFile)" class="image-preview">
          <img :src="selectedFile?.url" :alt="selectedFile?.name" class="preview-image" />
        </div>
        <!-- 文本文件预览 -->
        <div v-else-if="isTextFile(selectedFile)" class="text-preview">
          <el-input
            v-model="fileContent"
            type="textarea"
            :rows="20"
            readonly
            :placeholder="t('cubeFile.textPlaceholder')"
          />
        </div>
        <!-- 其他文件类型 -->
        <div v-else class="other-file-preview">
          <el-empty :description="t('cubeFile.previewUnsupported')">
            <el-button type="primary" @click="downloadFile(selectedFile!)">
              {{ t('cubeFile.downloadFile') }}
            </el-button>
          </el-empty>
        </div>
      </div>
      <template #footer>
        <el-button @click="previewVisible = false">{{ t('button.close') }}</el-button>
        <el-button type="primary" @click="downloadFile(selectedFile!)">
          {{ t('cubeFile.download') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { ElMessage } from 'element-plus';

// 定义接口类型
interface FileParams {
  id?: string;
}

interface FileData {
  id: string;
  name: string;
  size: number;
  type: string;
  url: string;
  uploadTime: string;
}

// 表单数据
const searchForm = reactive<FileParams>({
  id: '',
});

// 文件列表
const fileList = ref<FileData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 预览相关
const previewVisible = ref(false);
const selectedFile = ref<FileData | null>(null);
const fileContent = ref('');
const { t } = useI18n();

// 获取文件
const handleGetFile = async () => {
  if (!searchForm.id) {
    ElMessage.warning(t('cubeFile.fileIdRequired'));
    return;
  }

  loading.value = true;
  try {
    const response = (await request.get('/Cube/File', {
      params: {
        id: searchForm.id,
      },
    })) as unknown as {
      name?: string;
      fileName?: string;
      size?: number;
      type?: string;
      contentType?: string;
      url?: string;
      uploadTime?: string;
    };

    // 处理响应数据
    if (response) {
      const fileData: FileData = {
        id: searchForm.id,
        name: response.name || response.fileName || t('cubeFile.defaultFileName', { id: searchForm.id }),
        size: response.size || 0,
        type: response.type || response.contentType || 'unknown',
        url: response.url || `/Cube/File?id=${searchForm.id}`,
        uploadTime: response.uploadTime || new Date().toLocaleString(),
      };

      fileList.value = [fileData];
      total.value = 1;
      ElMessage.success(t('cubeFile.getSuccess'));
    } else {
      fileList.value = [];
      total.value = 0;
    }
  } catch {
    ElMessage.error(t('cubeFile.getFailed'));
    fileList.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
};

// 重置搜索
const resetSearch = () => {
  searchForm.id = '';
  fileList.value = [];
  total.value = 0;
};

// 预览文件
const previewFile = (file: FileData) => {
  selectedFile.value = file;
  previewVisible.value = true;

  // 如果是文本文件，尝试获取内容
  if (isTextFile(file)) {
    loadTextContent(file);
  }
};

// 下载文件
const downloadFile = (file: FileData) => {
  const link = document.createElement('a');
  link.href = file.url;
  link.download = file.name;
  link.target = '_blank';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  ElMessage.success(t('cubeFile.downloadDone'));
};

// 复制文件链接
const copyFileUrl = async (file: FileData) => {
  try {
    await navigator.clipboard.writeText(file.url);
    ElMessage.success(t('cubeFile.linkCopied'));
  } catch {
    ElMessage.error(t('ssoAccessToken.copyFailed'));
  }
};

// 判断是否为图片文件
const isImageFile = (file: FileData | null): boolean => {
  if (!file) return false;
  const imageTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
  return imageTypes.includes(file.type.toLowerCase()) ||
         /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file.name);
};

// 判断是否为文本文件
const isTextFile = (file: FileData | null): boolean => {
  if (!file) return false;
  const textTypes = ['text/plain', 'text/html', 'text/css', 'text/javascript', 'application/json'];
  return textTypes.includes(file.type.toLowerCase()) ||
         /\.(txt|html|css|js|json|xml|md)$/i.test(file.name);
};

// 加载文本内容
const loadTextContent = async (file: FileData) => {
  try {
    const response = (await request.get(file.url, {
      responseType: 'text',
    })) as unknown as string;
    fileContent.value = response;
  } catch {
    fileContent.value = t('cubeFile.textLoadFailed');
  }
};

// 格式化文件大小
const formatFileSize = (size: number): string => {
  if (size === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(size) / Math.log(k));
  return parseFloat((size / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 页码变更处理
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
};

// 每页显示条数变更处理
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1;
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.file-preview-container {
  min-height: 400px;
}

.image-preview {
  text-align: center;
  padding: 20px;
}

.preview-image {
  max-width: 100%;
  max-height: 500px;
  border-radius: 8px;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.text-preview {
  padding: 20px;
}

.other-file-preview {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 300px;
}
</style>
