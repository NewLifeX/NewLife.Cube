<template>
  <div>
    <div class="page-header">
      <h1>{{ t('adminHome.title') }}</h1>
      <p>{{ t('adminHome.subtitle') }}</p>
    </div>

    <el-row :gutter="20">
      <!-- 系统信息卡片 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('adminHome.systemInfo') }}</span>
              <el-button @click="refreshSystemInfo">
                <el-icon><Refresh /></el-icon>
                {{ t('grid.refresh') }}
              </el-button>
            </div>
          </template>

          <div v-loading="loading.system">
            <el-descriptions :column="3" border>
              <el-descriptions-item
                v-for="(value, key) in systemInfo"
                :key="key"
                :label="getSystemInfoLabel(String(key))"
              >
                {{ formatSystemInfoValue(value) }}
              </el-descriptions-item>
            </el-descriptions>

            <!-- 如果没有系统信息数据，显示提示 -->
            <el-empty v-if="Object.keys(systemInfo).length === 0" :description="t('adminHome.noSystemInfo')" />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" style="margin-top: 20px">
      <!-- 服务器变量 -->
      <el-col :span="12">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('adminHome.serverVars') }}</span>
              <el-button @click="getServerVarList" size="small">
                <el-icon><Refresh /></el-icon>
                {{ t('grid.refresh') }}
              </el-button>
            </div>
          </template>

          <div v-loading="loading.serverVars" style="max-height: 400px; overflow-y: auto;">
            <el-collapse v-model="activeServerVarTabs" accordion>
              <!-- 服务器Headers -->
              <el-collapse-item :title="t('adminHome.serverHeaders')" name="server">
                <el-table :data="serverHeaders" size="small" stripe>
                  <el-table-column prop="name" :label="t('adminHome.headerName')" width="180" />
                  <el-table-column prop="value" :label="t('adminHome.value')" show-overflow-tooltip>
                    <template #default="{ row }">
                      <span :title="row.value">{{ row.value }}</span>
                    </template>
                  </el-table-column>
                </el-table>
              </el-collapse-item>

              <!-- 请求信息 -->
              <el-collapse-item :title="t('adminHome.requestInfo')" name="request">
                <el-table :data="requestInfo" size="small" stripe>
                  <el-table-column prop="name" :label="t('adminHome.attrName')" width="180" />
                  <el-table-column prop="value" :label="t('adminHome.value')" show-overflow-tooltip>
                    <template #default="{ row }">
                      <span :title="row.value">{{ formatRequestValue(row.value) }}</span>
                    </template>
                  </el-table-column>
                </el-table>
              </el-collapse-item>
            </el-collapse>

            <!-- 如果没有数据 -->
            <el-empty v-if="serverHeaders.length === 0 && requestInfo.length === 0" :description="t('adminHome.noServerVars')" :image-size="80">
              <el-button @click="getServerVarList" size="small">{{ t('adminHome.retry') }}</el-button>
            </el-empty>
          </div>
        </el-card>
      </el-col>

      <!-- 加载模块列表 -->
      <el-col :span="12">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('adminHome.loadedModules') }}</span>
              <el-input
                v-model="processModel"
                :placeholder="t('adminHome.filterModules')"
                style="width: 150px"
                @change="getProcessList"
              />
            </div>
          </template>

          <div v-loading="loading.processes" style="max-height: 400px; overflow-y: auto;">
            <el-table :data="processes" size="small" stripe>
              <el-table-column prop="name" :label="t('adminHome.fileName')" width="200" show-overflow-tooltip />
              <el-table-column prop="productName" :label="t('adminHome.productName')" width="150" show-overflow-tooltip />
              <el-table-column prop="companyName" :label="t('adminHome.companyName')" width="120" show-overflow-tooltip />
              <el-table-column prop="version" :label="t('adminHome.version')" width="100" />
              <el-table-column prop="size" :label="t('fileManager.size')" width="100" align="right">
                <template #default="{ row }">
                  {{ formatFileSize(row.size) }}
                </template>
              </el-table-column>
              <el-table-column prop="fileName" :label="t('adminHome.fullPath')" min-width="300" show-overflow-tooltip />
              <template #empty>
                <el-empty :description="t('adminHome.noModules')" :image-size="80">
                  <el-button @click="getProcessList" size="small">{{ t('adminHome.retry') }}</el-button>
                </el-empty>
              </template>
            </el-table>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" style="margin-top: 20px">
      <!-- 程序集列表 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('adminHome.assemblies') }}</span>
              <div>
                <el-input
                  v-model="assemblyModel"
                  :placeholder="t('adminHome.filterAssemblies')"
                  style="width: 200px; margin-right: 10px"
                  @change="getAssemblyList"
                />
                <el-button type="warning" @click="handleMemoryFree">
                  <el-icon><Delete /></el-icon>
                  {{ t('adminHome.memoryFree') }}
                </el-button>
                <el-button type="danger" @click="handleRestart">
                  <el-icon><Switch /></el-icon>
                  {{ t('adminHome.restart') }}
                </el-button>
              </div>
            </div>
          </template>

          <div v-loading="loading.assemblies">
            <el-table :data="assemblies" size="small">
              <el-table-column prop="name" :label="t('adminHome.assemblyName')" width="200" show-overflow-tooltip />
              <el-table-column prop="title" :label="t('adminHome.assemblyTitle')" width="180" show-overflow-tooltip />
              <el-table-column prop="fileVersion" :label="t('adminHome.fileVersion')" width="120" />
              <el-table-column prop="version" :label="t('adminHome.assemblyVersion')" width="120" />
              <el-table-column prop="compileTime" :label="t('adminHome.compileTime')" width="150">
                <template #default="{ row }">
                  {{ formatDateTime(row.compileTime) }}
                </template>
              </el-table-column>
              <el-table-column prop="location" :label="t('adminHome.location')" min-width="300" show-overflow-tooltip />
            </el-table>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" style="margin-top: 20px">
      <!-- 菜单树 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('adminHome.menuTree') }}</span>
              <el-select v-model="menuModule" :placeholder="t('adminHome.selectModule')" style="width: 150px" @change="getMenuTree">
                <el-option :label="t('lovAdmin.all')" value="" />
                <el-option label="Admin" value="Admin" />
                <el-option label="Cube" value="Cube" />
              </el-select>
            </div>
          </template>

          <div v-loading="loading.menu">
            <el-tree
              :data="menuTree"
              :props="{ children: 'children', label: 'displayName' }"
              default-expand-all
              node-key="id"
            >
              <template #default="{ data }">
                <div class="menu-node">
                  <span class="menu-icon" v-if="data.icon">
                    <i :class="`fa ${data.icon}`"></i>
                  </span>
                  <span class="menu-name">{{ data.displayName }}</span>
                  <span class="menu-url" v-if="data.url">({{ data.url }})</span>
                  <el-tag v-if="!data.visible" type="info" size="small">{{ t('adminHome.hidden') }}</el-tag>
                  <el-tag v-if="data.newWindow" type="warning" size="small">{{ t('menuAdmin.newWindow') }}</el-tag>
                  <span class="menu-permissions" v-if="Object.keys(data.permissions).length > 0">
                    <el-tag
                      v-for="(permission, key) in data.permissions"
                      :key="key"
                      type="primary"
                      size="small"
                      style="margin-left: 4px"
                    >
                      {{ permission }}
                    </el-tag>
                  </span>
                </div>
              </template>
            </el-tree>

            <!-- 如果没有菜单数据 -->
            <el-empty v-if="menuTree.length === 0" :description="t('adminHome.noMenu')" :image-size="80">
              <el-button @click="getMenuTree" size="small">{{ t('adminHome.retry') }}</el-button>
            </el-empty>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessageBox } from 'element-plus'
import { Refresh, Delete, Switch } from '@element-plus/icons-vue'
import { request } from '@newlifex/cube-vue/core/utils/request'

// 系统信息接口
interface SystemInfo {
  [key: string]: string | number | boolean | null | undefined // 支持动态字段
}

interface ServerVar {
  name: string
  value: string
}

interface RequestInfoItem {
  name: string
  value: string | number | boolean | object | null
}

interface ProcessInfo {
  name: string
  companyName: string
  productName: string
  description: string
  version: string
  size: number
  fileName: string
}

interface AssemblyInfo {
  name: string
  title: string
  fileVersion: string
  version: string
  compileTime: string
  location: string
}

interface MenuNode {
  id: number
  name: string
  displayName: string
  fullName: string
  parentID: number
  url: string
  icon: string | null
  visible: boolean
  newWindow: boolean
  permissions: Record<string, string>
  children?: MenuNode[] | null
}

// 数据
const systemInfo = reactive<SystemInfo>({})
const { t } = useI18n()

const serverVars = ref<ServerVar[]>([]) // 保留兼容性
const serverHeaders = ref<ServerVar[]>([])
const requestInfo = ref<RequestInfoItem[]>([])
const processes = ref<ProcessInfo[]>([])
const assemblies = ref<AssemblyInfo[]>([])
const menuTree = ref<MenuNode[]>([])

// UI状态
const activeServerVarTabs = ref(['server']) // 默认展开服务器Headers
// 筛选参数
const processModel = ref('')
const assemblyModel = ref('')
const menuModule = ref('')

// 加载状态
const loading = reactive({
  system: false,
  serverVars: false,
  processes: false,
  assemblies: false,
  menu: false
})

// 系统信息字段标签映射
const getSystemInfoLabel = (key: string): string => {
  const labelMap: Record<string, string> = {
    processTime: t('adminHome.labelProcessTime'),
    cpu: t('adminHome.labelCpu'),
    memory: t('adminHome.labelMemory'),
    local: t('adminHome.labelLocal'),
    host: t('adminHome.labelHost'),
    version: t('adminHome.labelVersion'),
    os: t('adminHome.labelOs'),
    osVersion: t('adminHome.labelOsVersion'),
    machineName: t('adminHome.labelMachineName'),
    userName: t('adminHome.labelUserName'),
    workingSet: t('adminHome.labelWorkingSet'),
    totalMemory: t('adminHome.labelTotalMemory'),
    availableMemory: t('adminHome.labelAvailableMemory'),
    gcMemory: t('adminHome.labelGcMemory'),
    processor: t('adminHome.labelProcessor'),
    processorCount: t('adminHome.labelProcessorCount'),
    disk: t('adminHome.labelDisk'),
    network: t('adminHome.labelNetwork'),
    runtime: t('adminHome.labelRuntime'),
    startTime: t('adminHome.labelStartTime'),
    upTime: t('adminHome.labelUpTime')
  }

  return labelMap[key] || key.charAt(0).toUpperCase() + key.slice(1) // 如果没有映射，首字母大写
}

// 格式化系统信息值
const formatSystemInfoValue = (value: string | number | boolean | object | null | undefined): string => {
  if (value === null || value === undefined) {
    return t('adminHome.noData')
  }

  if (typeof value === 'boolean') {
    return value ? t('common.yes') : t('common.no')
  }

  if (typeof value === 'number') {
    return value.toString()
  }

  if (typeof value === 'object') {
    // 如果是对象，尝试格式化显示
    if (Array.isArray(value)) {
      return value.join(', ')
    }

    // 对于对象类型，显示关键信息
    try {
      // 特殊处理 host 对象
      if (value && typeof value === 'object' &&
          ('machineName' in value || 'ip' in value)) {
        const parts = []
        const hostObj = value as Record<string, string | number>
        if (hostObj.machineName) parts.push(`${t('adminHome.hostMachine')}: ${hostObj.machineName}`)
        if (hostObj.ip) parts.push(`IP: ${hostObj.ip}`)
        if (hostObj.port) parts.push(`${t('adminHome.hostPort')}: ${hostObj.port}`)
        if (hostObj.domain) parts.push(`${t('adminHome.hostDomain')}: ${hostObj.domain}`)
        return parts.length > 0 ? parts.join(', ') : JSON.stringify(value)
      }

      // 其他对象类型，尝试显示为 JSON 字符串
      return JSON.stringify(value, null, 0)
    } catch {
      return String(value) || t('adminHome.noData')
    }
  }

  return String(value) || t('adminHome.noData')
}

// 格式化文件大小
const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'

  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))

  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

// 格式化日期时间
const formatDateTime = (dateTime: string): string => {
  if (!dateTime) return t('adminHome.unknown')

  try {
    const date = new Date(dateTime)
    // 检查是否为有效日期
    if (isNaN(date.getTime())) return dateTime

    // 如果是默认的 2000-01-01 这种日期，显示为“未知”
    if (date.getFullYear() === 2000 && date.getMonth() === 0 && date.getDate() === 1) {
      return t('adminHome.unknown')
    }

    return date.toLocaleString(undefined, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return dateTime
  }
}

// 刷新系统信息
const refreshSystemInfo = async () => {
  loading.system = true
  await getMainInfo()
  // 如果Main接口数据不够完整，可以调用备用接口补充
  // await getSystemInfo()
  loading.system = false
}

// 获取主要信息（系统信息）
const getMainInfo = async () => {
  try {
    const data = (await request.get('/Admin/Index/Main')) as unknown as Record<string, unknown>

    // 直接使用 API 返回的所有字段，不进行固定字段映射
    // 清空之前的数据
    Object.keys(systemInfo).forEach(key => {
      delete systemInfo[key]
    })

    // 将所有返回的字段添加到 systemInfo 中
    if (data && typeof data === 'object') {
      Object.keys(data).forEach(key => {
        if (data[key] !== null && data[key] !== undefined) {
          systemInfo[key] = data[key] as string | number | boolean | null
        }
      })
    }
  } catch (error) {
    console.error('Failed to load the main info:', error)
    // 错误提示已在 request 拦截器中处理
  }
}

// 获取服务器变量列表
const getServerVarList = async () => {
  try {
    loading.serverVars = true

    const data = (await request.get('/Admin/Index/ServerVarList')) as unknown as {
      server?: Array<{ name?: string; value?: string }>;
      request?: Array<{ name?: string; value?: string | number | boolean | object | null }>;
    }

    if (data && typeof data === 'object') {
      // 处理服务器Headers数据
      if (data.server && Array.isArray(data.server)) {
        serverHeaders.value = data.server.map((item: { name?: string; value?: string }) => ({
          name: item.name || '',
          value: item.value || ''
        }))
      } else {
        serverHeaders.value = []
      }

      // 处理请求信息数据
      if (data.request && Array.isArray(data.request)) {
        requestInfo.value = data.request.map((item: { name?: string; value?: string | number | boolean | object | null }) => ({
          name: item.name || '',
          value: item.value ?? ''
        }))
      } else {
        requestInfo.value = []
      }

      // 兼容旧的serverVars变量 - 合并所有数据
      const allVars = [
        ...serverHeaders.value,
        ...requestInfo.value.map(item => ({
          name: item.name,
          value: formatRequestValue(item.value)
        }))
      ]
      serverVars.value = allVars
    } else {
      serverHeaders.value = []
      requestInfo.value = []
      serverVars.value = []
    }
  } catch {
    serverHeaders.value = []
    requestInfo.value = []
    serverVars.value = []
  } finally {
    loading.serverVars = false
  }
}

// 获取进程列表
const getProcessList = async () => {
  try {
    loading.processes = true

    const data = await request.get('/Admin/Index/ProcessList', {
      params: processModel.value ? { model: processModel.value } : {}
    })

    if (Array.isArray(data)) {
      processes.value = data
    } else if (data && typeof data === 'object' && Array.isArray(data.data)) {
      processes.value = data.data
    } else {
      processes.value = []
    }
  } catch {
    processes.value = []
  } finally {
    loading.processes = false
  }
}

// 获取程序集列表
const getAssemblyList = async () => {
  try {
    loading.assemblies = true

    const data = await request.get('/Admin/Index/AssemblyList', {
      params: assemblyModel.value ? { model: assemblyModel.value } : {}
    })

    if (Array.isArray(data)) {
      assemblies.value = data
    } else if (data && typeof data === 'object' && Array.isArray(data.data)) {
      assemblies.value = data.data
    } else {
      assemblies.value = []
    }
  } catch {
    assemblies.value = []
  } finally {
    loading.assemblies = false
  }
}

// 获取菜单树
const getMenuTree = async () => {
  try {
    loading.menu = true

    const data = await request.get('/Admin/Index/GetMenuTree', {
      params: menuModule.value ? { module: menuModule.value } : {}
    })

    if (Array.isArray(data)) {
      menuTree.value = data
    } else if (data && typeof data === 'object' && Array.isArray(data.data)) {
      menuTree.value = data.data
    } else {
      menuTree.value = []
    }
  } catch {
    menuTree.value = []
  } finally {
    loading.menu = false
  }
}

// 内存回收
const handleMemoryFree = async () => {
  try {
    await request.get('/Admin/Index/MemoryFree')
    // 刷新系统信息
    getMainInfo()
  } catch {
    // 错误提示已在 request 拦截器中处理
  }
}

// 重启系统
const handleRestart = async () => {
  try {
    await ElMessageBox.confirm(
      t('adminHome.restartConfirm'),
      t('adminHome.restartTitle'),
      {
        confirmButtonText: t('button.confirm'),
        cancelButtonText: t('button.cancel'),
        type: 'warning',
      }
    )

    await request.post('/Admin/Index/Restart')
  } catch (error) {
    if (error !== 'cancel') {
      // 其他错误已在 request 拦截器中处理
    }
  }
}

// 格式化请求值
const formatRequestValue = (value: string | number | boolean | object | null): string => {
  if (value === null || value === undefined) {
    return t('adminHome.empty')
  }

  if (typeof value === 'boolean') {
    return value ? t('common.yes') : t('common.no')
  }

  if (typeof value === 'number') {
    return value.toString()
  }

  if (typeof value === 'object') {
    try {
      // 特殊处理某些对象结构
      if (value && typeof value === 'object') {
        const obj = value as Record<string, string | number | boolean>

        // 处理 PathBase/Path 这种结构
        if ('value' in obj && 'hasValue' in obj) {
          const hasValue = obj.hasValue as boolean
          if (hasValue) {
            return String(obj.value) || t('adminHome.empty')
          } else {
            return t('adminHome.empty')
          }
        }

        // 处理 Host 这种复杂结构
        if ('host' in obj && 'port' in obj) {
          return `${obj.host}:${obj.port}`
        }

        // 其他对象显示JSON
        return JSON.stringify(value, null, 0)
      }

      return JSON.stringify(value, null, 0)
    } catch {
      return String(value) || t('adminHome.empty')
    }
  }

  return String(value) || t('adminHome.empty')
}

// 组件挂载时加载数据
onMounted(async () => {
  // 首先加载主要的系统信息
  loading.system = true
  try {
    await getMainInfo()
  } finally {
    loading.system = false
  }

  // 然后加载其他信息
  getServerVarList()
  getProcessList()
  getAssemblyList()
  getMenuTree()
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

/* 菜单树样式 */
.menu-node {
  display: flex;
  align-items: center;
  flex: 1;
  gap: 8px;
}

.menu-icon {
  color: #409eff;
  min-width: 16px;
}

.menu-name {
  font-weight: 500;
  color: #303133;
}

.menu-url {
  color: #909399;
  font-size: 12px;
  font-family: monospace;
}

.menu-permissions {
  margin-left: auto;
  display: flex;
  gap: 4px;
}

/* 隐藏菜单项的样式 */
.el-tree-node__content:has(.menu-node .el-tag) {
  opacity: 0.7;
}
</style>
