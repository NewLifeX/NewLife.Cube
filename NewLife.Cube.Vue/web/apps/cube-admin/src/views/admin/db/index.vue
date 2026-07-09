<template>
  <div>
    <div class="page-header">
      <h1>数据库管理</h1>
      <p>数据库备份与管理操作</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>数据库操作</span>
        </div>
      </template>

      <div class="db-operations">
        <el-form :model="backupForm" label-width="120px">
          <el-form-item label="备份名称">
            <el-input
              v-model="backupForm.name"
              placeholder="请输入备份名称，留空则自动生成"
              style="width: 300px"
            />
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              :loading="loading.backup"
              @click="handleBackup"
            >
              数据库备份
            </el-button>
            <el-button
              type="success"
              :loading="loading.backupCompress"
              @click="handleBackupAndCompress"
            >
              备份并压缩
            </el-button>
          </el-form-item>
        </el-form>

        <el-divider />

        <div class="download-section">
          <h3>下载备份文件</h3>
          <el-form :model="downloadForm" label-width="120px">
            <el-form-item label="文件名称">
              <el-input
                v-model="downloadForm.name"
                placeholder="请输入要下载的备份文件名"
                style="width: 300px"
              />
            </el-form-item>

            <el-form-item>
              <el-button
                type="info"
                :loading="loading.download"
                @click="handleDownload"
              >
                下载备份文件
              </el-button>
            </el-form-item>
          </el-form>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { request } from '@newlifex/cube-vue/core/utils/request'

// 表单数据
const backupForm = reactive({
  name: ''
})

const downloadForm = reactive({
  name: ''
})

// 加载状态
const loading = reactive({
  backup: false,
  backupCompress: false,
  download: false
})

// 数据库备份
const handleBackup = async () => {
  try {
    loading.backup = true

    const params = backupForm.name ? { name: backupForm.name } : {}
    console.log('开始数据库备份:', params)

    const data = await request.post('/Admin/Db/Backup', params)
    console.log('数据库备份响应:', data)

    ElMessage.success('数据库备份成功')
    backupForm.name = ''
  } catch (error) {
    console.error('数据库备份错误:', error)
    ElMessage.error('数据库备份失败')
  } finally {
    loading.backup = false
  }
}

// 数据库备份并压缩
const handleBackupAndCompress = async () => {
  try {
    await ElMessageBox.confirm(
      '备份并压缩操作可能需要较长时间，确定要继续吗？',
      '确认操作',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning',
      }
    )

    loading.backupCompress = true

    const params = backupForm.name ? { name: backupForm.name } : {}
    console.log('开始数据库备份并压缩:', params)

    const data = await request.post('/Admin/Db/BackupAndCompress', params)
    console.log('数据库备份并压缩响应:', data)

    ElMessage.success('数据库备份并压缩成功')
    backupForm.name = ''
  } catch (error) {
    if (error !== 'cancel') {
      console.error('数据库备份压缩错误:', error)
      ElMessage.error('数据库备份并压缩失败')
    }
  } finally {
    loading.backupCompress = false
  }
}

// 下载备份文件
const handleDownload = async () => {
  if (!downloadForm.name.trim()) {
    ElMessage.warning('请输入要下载的文件名')
    return
  }

  try {
    loading.download = true

    console.log('开始下载备份文件:', downloadForm.name)

    // 使用 request 发送下载请求，获取二进制数据
    const blob = await request.get('/Admin/Db/Download',
      {
        params: { name: downloadForm.name },
        responseType: 'blob'
      }
    )

    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = downloadForm.name
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)

    ElMessage.success('文件下载成功')
    downloadForm.name = ''
  } catch (error) {
    console.error('文件下载错误:', error)
    ElMessage.error('文件下载失败')
  } finally {
    loading.download = false
  }
}
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

.db-operations {
  padding: 20px 0;
}

.download-section {
  margin-top: 20px;
}

.download-section h3 {
  margin: 0 0 20px 0;
  font-size: 16px;
  font-weight: 500;
}
</style>
