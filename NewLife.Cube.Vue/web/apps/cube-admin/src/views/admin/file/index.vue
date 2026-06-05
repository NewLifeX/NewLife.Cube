<template>
  <div>
    <div class="page-header">
      <h1>文件管理</h1>
      <p>文件上传、下载、压缩解压等管理操作</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>文件操作</span>
          <div>
            <el-button type="primary" @click="showUploadDialog = true">
              <el-icon><Plus /></el-icon>
              上传文件
            </el-button>
            <el-button @click="refreshFileList">
              <el-icon><Refresh /></el-icon>
              刷新
            </el-button>
          </div>
        </div>
      </template>

      <div class="file-operations">
        <el-form :model="queryForm" inline>
          <el-form-item label="路径">
            <el-input
              v-model="queryForm.r"
              placeholder="请输入文件路径"
              style="width: 300px"
            />
          </el-form-item>
          <el-form-item label="排序">
            <el-select v-model="queryForm.sort" placeholder="选择排序方式" style="width: 120px">
              <el-option label="名称" value="name" />
              <el-option label="大小" value="size" />
              <el-option label="时间" value="time" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="getFileList">查询</el-button>
          </el-form-item>
        </el-form>
      </div>

      <div class="file-list" v-loading="loading.list">
        <el-table :data="fileList" style="width: 100%">
          <el-table-column prop="name" label="文件名" />
          <el-table-column prop="size" label="大小" />
          <el-table-column prop="modified" label="修改时间" />
          <el-table-column prop="type" label="类型" />
          <el-table-column label="操作" width="300">
            <template #default="{ row }">
              <el-button size="small" @click="handleDownload(row)">
                下载
              </el-button>
              <el-button size="small" @click="handleCompress(row)">
                压缩
              </el-button>
              <el-button size="small" @click="handleDecompress(row)">
                解压
              </el-button>
              <el-button size="small" @click="handleCopy(row)">
                复制
              </el-button>
              <el-button size="small" type="danger" @click="handleDelete(row)">
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>

    <!-- 上传对话框 -->
    <el-dialog v-model="showUploadDialog" title="上传文件" width="500px">
      <el-form :model="uploadForm" label-width="80px">
        <el-form-item label="上传路径">
          <el-input v-model="uploadForm.r" placeholder="请输入上传路径" />
        </el-form-item>
        <el-form-item label="选择文件">
          <el-upload
            ref="uploadRef"
            :action="uploadAction"
            :data="uploadForm"
            :auto-upload="false"
            :show-file-list="true"
            :limit="1"
          >
            <el-button type="primary">选择文件</el-button>
          </el-upload>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showUploadDialog = false">取消</el-button>
        <el-button type="primary" :loading="loading.upload" @click="handleUpload">
          上传
        </el-button>
      </template>
    </el-dialog>

    <!-- 头像上传对话框 -->
    <el-dialog v-model="showAvatarDialog" title="上传头像" width="400px">
      <el-upload
        ref="avatarUploadRef"
        action="/Admin/File/UploadAvatar"
        :auto-upload="false"
        :show-file-list="true"
        :limit="1"
        accept="image/*"
      >
        <el-button type="primary">选择头像</el-button>
        <template #tip>
          <div class="el-upload__tip">
            只能上传jpg/png文件，且不超过2MB
          </div>
        </template>
      </el-upload>

      <template #footer>
        <el-button @click="showAvatarDialog = false">取消</el-button>
        <el-button type="primary" :loading="loading.avatar" @click="handleAvatarUpload">
          上传
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessageBox } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { request } from 'cube-front/core/utils/request'

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

// 上传地址
const uploadAction = '/Admin/File/Upload'

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
    const blob = await request.post('/Admin/File/Download',
      { r: row.path },
      {
        responseType: 'blob'
      }
    )

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
      `确定要删除文件 "${row.name}" 吗？`,
      '确认删除',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
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
