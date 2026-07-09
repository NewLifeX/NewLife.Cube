<template>
  <div class="cube-file-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>文件管理</h3>
          <el-button type="primary" @click="handleGetFile">获取文件</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="文件ID">
          <el-input v-model="searchForm.id" placeholder="请输入文件ID" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleGetFile">获取文件</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 文件列表 -->
      <el-table :data="fileList" border style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="文件ID" width="120" />
        <el-table-column prop="name" label="文件名" min-width="200" />
        <el-table-column prop="size" label="文件大小" width="100">
          <template #default="scope">
            {{ formatFileSize(scope.row.size) }}
          </template>
        </el-table-column>
        <el-table-column prop="type" label="文件类型" width="120" />
        <el-table-column prop="url" label="文件URL" min-width="200" show-overflow-tooltip />
        <el-table-column prop="uploadTime" label="上传时间" width="160" />
        <el-table-column label="操作" width="200">
          <template #default="scope">
            <el-button type="primary" size="small" @click="previewFile(scope.row)">预览</el-button>
            <el-button type="success" size="small" @click="downloadFile(scope.row)">下载</el-button>
            <el-button type="info" size="small" @click="copyFileUrl(scope.row)">复制链接</el-button>
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
          <img :src="selectedFile.url" :alt="selectedFile.name" class="preview-image" />
        </div>
        <!-- 文本文件预览 -->
        <div v-else-if="isTextFile(selectedFile)" class="text-preview">
          <el-input
            v-model="fileContent"
            type="textarea"
            :rows="20"
            readonly
            placeholder="文本内容将显示在这里"
          />
        </div>
        <!-- 其他文件类型 -->
        <div v-else class="other-file-preview">
          <el-empty description="该文件类型不支持预览">
            <el-button type="primary" @click="downloadFile(selectedFile)">下载文件</el-button>
          </el-empty>
        </div>
      </div>
      <template #footer>
        <el-button @click="previewVisible = false">关闭</el-button>
        <el-button type="primary" @click="downloadFile(selectedFile)">下载</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
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

// 获取文件
const handleGetFile = async () => {
  if (!searchForm.id) {
    ElMessage.warning('请输入文件ID');
    return;
  }

  loading.value = true;
  try {
    const response = await request.get('/Cube/File', {
      params: {
        id: searchForm.id,
      },
    });

    // 处理响应数据
    if (response) {
      const fileData: FileData = {
        id: searchForm.id,
        name: response.name || response.fileName || `文件_${searchForm.id}`,
        size: response.size || 0,
        type: response.type || response.contentType || 'unknown',
        url: response.url || `/Cube/File?id=${searchForm.id}`,
        uploadTime: response.uploadTime || new Date().toLocaleString(),
      };

      fileList.value = [fileData];
      total.value = 1;
      ElMessage.success('文件获取成功');
    } else {
      fileList.value = [];
      total.value = 0;
    }
  } catch {
    ElMessage.error('文件获取失败');
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

  ElMessage.success('文件下载完成');
};

// 复制文件链接
const copyFileUrl = async (file: FileData) => {
  try {
    await navigator.clipboard.writeText(file.url);
    ElMessage.success('文件链接已复制到剪贴板');
  } catch {
    ElMessage.error('复制失败，请手动复制');
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
    const response = await request.get(file.url, {
      responseType: 'text',
    });
    fileContent.value = response;
  } catch {
    fileContent.value = '文本内容加载失败';
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
