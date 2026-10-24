<template>
  <div>
    <div class="page-header">
      <h1>{{ t('db.title') }}</h1>
      <p>{{ t('db.subtitle') }}</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>{{ t('db.operations') }}</span>
        </div>
      </template>

      <div class="db-operations">
        <el-form :model="backupForm" label-width="120px">
          <el-form-item :label="t('db.backupName')">
            <el-input
              v-model="backupForm.name"
              :placeholder="t('db.backupNameHint')"
              style="width: 300px"
            />
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              :loading="loading.backup"
              @click="handleBackup"
            >
              {{ t('db.backup') }}
            </el-button>
            <el-button
              type="success"
              :loading="loading.backupCompress"
              @click="handleBackupAndCompress"
            >
              {{ t('db.backupAndCompress') }}
            </el-button>
          </el-form-item>
        </el-form>

        <el-divider />

        <div class="download-section">
          <h3>{{ t('db.downloadTitle') }}</h3>
          <el-form :model="downloadForm" label-width="120px">
            <el-form-item :label="t('db.fileName')">
              <el-input
                v-model="downloadForm.name"
                :placeholder="t('db.fileNameHint')"
                style="width: 300px"
              />
            </el-form-item>

            <el-form-item>
              <el-button
                type="info"
                :loading="loading.download"
                @click="handleDownload"
              >
                {{ t('db.download') }}
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
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { request } from '@newlifex/cube-vue/core/utils/request'

const { t } = useI18n()

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
    console.log('Starting database backup:', params)

    const data = await request.post('/Admin/Db/Backup', params)
    console.log('Database backup response:', data)

    ElMessage.success(t('db.backupSuccess'))
    backupForm.name = ''
  } catch (error) {
    console.error('Database backup failed:', error)
    ElMessage.error(t('db.backupFailed'))
  } finally {
    loading.backup = false
  }
}

// 数据库备份并压缩
const handleBackupAndCompress = async () => {
  try {
    await ElMessageBox.confirm(
      t('db.compressConfirm'),
      t('db.confirmAction'),
      {
        confirmButtonText: t('button.confirm'),
        cancelButtonText: t('button.cancel'),
        type: 'warning',
      }
    )

    loading.backupCompress = true

    const params = backupForm.name ? { name: backupForm.name } : {}
    console.log('Starting database backup and compression:', params)

    const data = await request.post('/Admin/Db/BackupAndCompress', params)
    console.log('Database backup and compression response:', data)

    ElMessage.success(t('db.compressSuccess'))
    backupForm.name = ''
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Database backup and compression failed:', error)
      ElMessage.error(t('db.compressFailed'))
    }
  } finally {
    loading.backupCompress = false
  }
}

// 下载备份文件
const handleDownload = async () => {
  if (!downloadForm.name.trim()) {
    ElMessage.warning(t('db.fileNameRequired'))
    return
  }

  try {
    loading.download = true

    console.log('Starting backup file download:', downloadForm.name)

    // 使用 request 发送下载请求，获取二进制数据
    const blob = (await request.get('/Admin/Db/Download',
      {
        params: { name: downloadForm.name },
        responseType: 'blob'
      }
    )) as unknown as Blob

    // 创建下载链接
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = downloadForm.name
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)

    ElMessage.success(t('db.downloadSuccess'))
    downloadForm.name = ''
  } catch (error) {
    console.error('Backup file download failed:', error)
    ElMessage.error(t('db.downloadFailed'))
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
