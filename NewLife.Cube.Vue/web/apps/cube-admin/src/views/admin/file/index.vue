<template>
  <div>
    <div class="page-header">
      <h1>{{ t('fileManager.title') }}</h1>
      <p>{{ t('fileManager.subtitle') }}</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>{{ t('fileManager.operations') }}</span>
          <div>
            <el-button type="primary" @click="showUploadDialog = true">
              <el-icon><Plus /></el-icon>
              {{ t('fileManager.uploadFile') }}
            </el-button>
            <el-button @click="refreshFileList">
              <el-icon><Refresh /></el-icon>
              {{ t('grid.refresh') }}
            </el-button>
          </div>
        </div>
      </template>

      <div class="file-operations">
        <el-form :model="queryForm" inline>
          <el-form-item :label="t('fileManager.path')">
            <el-input
              v-model="queryForm.r"
              :placeholder="t('fileManager.pathHint')"
              style="width: 300px"
            />
          </el-form-item>
          <el-form-item :label="t('fileManager.sort')">
            <el-select v-model="queryForm.sort" :placeholder="t('fileManager.sortHint')" style="width: 120px">
              <el-option :label="t('common.name')" value="name" />
              <el-option :label="t('fileManager.size')" value="size" />
              <el-option :label="t('fileManager.time')" value="time" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="getFileList">{{ t('button.search') }}</el-button>
          </el-form-item>
        </el-form>
      </div>

      <div class="file-list" v-loading="loading.list">
        <el-table :data="fileList" style="width: 100%">
          <el-table-column prop="name" :label="t('fileManager.fileName')" />
          <el-table-column prop="size" :label="t('fileManager.size')" />
          <el-table-column prop="modified" :label="t('fileManager.modified')" />
          <el-table-column prop="type" :label="t('fileManager.type')" />
          <el-table-column :label="t('common.actions')" width="300">
            <template #default="{ row }">
              <el-button size="small" @click="handleDownload(row)">
                {{ t('fileManager.download') }}
              </el-button>
              <el-button size="small" @click="handleCompress(row)">
                {{ t('fileManager.compress') }}
              </el-button>
              <el-button size="small" @click="handleDecompress(row)">
                {{ t('fileManager.decompress') }}
              </el-button>
              <el-button size="small" @click="handleCopy(row)">
                {{ t('fileManager.copy') }}
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                {{ t('button.delete') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>

    <!-- 上传对话框 -->
    <el-dialog v-model="showUploadDialog" :title="t('fileManager.uploadFile')" width="500px">
      <el-form :model="uploadForm" label-width="80px">
        <el-form-item :label="t('fileManager.uploadPath')">
          <el-input v-model="uploadForm.r" :placeholder="t('fileManager.uploadPathHint')" />
        </el-form-item>
        <el-form-item :label="t('fileManager.selectFile')">
          <el-upload
            ref="uploadRef"
            :action="uploadAction"
            :data="uploadForm"
            :auto-upload="false"
            :show-file-list="true"
            :limit="1"
          >
            <el-button type="primary">{{ t('fileManager.selectFile') }}</el-button>
          </el-upload>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showUploadDialog = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" :loading="loading.upload" @click="handleUpload">
          {{ t('fileManager.upload') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 头像上传对话框 -->
    <el-dialog v-model="showAvatarDialog" :title="t('fileManager.uploadAvatar')" width="400px">
      <el-upload
        ref="avatarUploadRef"
        :action="uploadAvatarAction"
        :auto-upload="false"
        :show-file-list="true"
        :limit="1"
        accept="image/*"
      >
        <el-button type="primary">{{ t('fileManager.selectAvatar') }}</el-button>
        <template #tip>
          <div class="el-upload__tip">
            {{ t('fileManager.avatarTip') }}
          </div>
        </template>
      </el-upload>

      <template #footer>
        <el-button @click="showAvatarDialog = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" :loading="loading.avatar" @click="handleAvatarUpload">
          {{ t('fileManager.upload') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessageBox } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { request } from '@newlifex/cube-vue/core/utils/request'
import { getConfig } from '@newlifex/cube-vue/core/configure'

// 文件信息接口
interface FileInfo {
  name: string
  size: string
  modified: string
  type: string
  path: string
}

// 对话框状态
const showUploadDialog = ref(false)
const showAvatarDialog = ref(false)
const { t } = useI18n()

// 表单数据
const queryForm = reactive({
  r: '',
  sort: 'name',
  message: ''
})

const uploadForm = reactive({
  r: ''
})

// 数据
const fileList = ref<FileInfo[]>([])

// 加载状态
const loading = reactive({
  list: false,
  upload: false,
  avatar: false
})

// 上传组件引用
const uploadRef = ref()
const avatarUploadRef = ref()

// 上传地址（Admin 区域实体接口，baseUrl 仅主机、不内含 /api，故路径自带 /api 前缀）
const uploadAction = (getConfig().request.baseUrl || '') + '/api/Admin/File/Upload'
const uploadAvatarAction = (getConfig().request.baseUrl || '') + '/api/Admin/File/UploadAvatar'

// 获取文件列表
const getFileList = async () => {
  try {
    loading.list = true

    const data = await request.get('/Admin/File', {
      params: {
        r: queryForm.r || undefined,
        sort: queryForm.sort || undefined,
        message: queryForm.message || undefined
      }
    })

    // 处理不同的响应格式
    if (data && typeof data === 'object' && 'data' in data) {
      fileList.value = Array.isArray(data.data) ? data.data : []
    } else if (Array.isArray(data)) {
      fileList.value = data
    } else {
      fileList.value = []
    }
  } catch {
    fileList.value = []
  } finally {
    loading.list = false
  }
}

// 刷新文件列表
const refreshFileList = () => {
  getFileList()
}

// 上传文件
const handleUpload = async () => {
  try {
    loading.upload = true

    // 触发上传
    await uploadRef.value.submit()

    showUploadDialog.value = false
    uploadForm.r = ''
    refreshFileList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.upload = false
  }
}

// 上传头像
const handleAvatarUpload = async () => {
  try {
    loading.avatar = true

    // 触发上传
    await avatarUploadRef.value.submit()

    showAvatarDialog.value = false
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  } finally {
    loading.avatar = false
  }
}

// 下载文件
const handleDownload = async (row: FileInfo) => {
  try {
    // 使用 request 发送下载请求，获取二进制数据
    const blob = (await request.post('/Admin/File/Download',
      { r: row.path },
      {
        responseType: 'blob'
      }
    )) as unknown as Blob

    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = row.name
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  }
}

// 压缩文件
const handleCompress = async (row: FileInfo) => {
  try {
    await request.post('/Admin/File/Compress', { r: row.path })
    refreshFileList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  }
}

// 解压文件
const handleDecompress = async (row: FileInfo) => {
  try {
    await request.post('/Admin/File/Decompress', { r: row.path })
    refreshFileList()
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  }
}

// 复制文件
const handleCopy = async (row: FileInfo) => {
  try {
    await request.post('/Admin/File/Copy', { r: row.path, f: row.name })
    // 复制操作成功会通过request拦截器自动提示
  } catch {
    // 错误提示已经在 request 拦截器中自动处理
  }
}

// 删除文件
const handleDelete = async (row: FileInfo) => {
  try {
    await ElMessageBox.confirm(
      t('fileManager.deleteConfirm', { name: row.name }),
      t('fileManager.confirmDelete'),
      {
        confirmButtonText: t('button.confirm'),
        cancelButtonText: t('button.cancel'),
        type: 'warning',
      }
    )

    await request.post('/Admin/File/Delete', { r: row.path })
    refreshFileList()
  } catch (error) {
    if (error !== 'cancel') {
      // 错误提示已经在 request 拦截器中自动处理
    }
  }
}

// 组件挂载时获取文件列表
onMounted(() => {
  getFileList()
})
</script>

<style scoped>
.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 500;
}

.page-header p {
  margin: 0;
  color: #666;
  font-size: 14px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.file-operations {
  margin-bottom: 20px;
}

.file-list {
  margin-top: 20px;
}
</style>
