<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import PageHeader from '../components/page-header.vue';

defineOptions({ name: 'ServerInfoPage' });

interface NameValue {
  name: string;
  value: unknown;
}

interface ProcessInfo {
  name: string;
  companyName: string;
  productName: string;
  description: string;
  version: string;
  size: number;
  fileName: string;
}

interface AssemblyInfo {
  name: string;
  title: string;
  fileVersion: string;
  version: string;
  compileTime: string;
  location: string;
}

interface MenuNode {
  id: number;
  name: string;
  displayName: string;
  url: string;
  icon?: string | null;
  visible: boolean;
  newWindow?: boolean;
  permissions?: Record<string, string>;
  children?: MenuNode[] | null;
}

function unwrap<T>(res: unknown): T {
  const payload = res as { data?: T };
  return (payload && typeof payload === 'object' && 'data' in payload ? payload.data : res) as T;
}

const systemInfo = reactive<Record<string, unknown>>({});
const serverHeaders = ref<NameValue[]>([]);
const requestInfo = ref<NameValue[]>([]);
const processes = ref<ProcessInfo[]>([]);
const assemblies = ref<AssemblyInfo[]>([]);
const menuTree = ref<MenuNode[]>([]);

const activeVars = ref(['server']);
const processKeyword = ref('');
const assemblyKeyword = ref('');
const menuModule = ref('');

const loading = reactive({
  system: false,
  serverVars: false,
  processes: false,
  assemblies: false,
  menu: false
});

const SYSTEM_LABELS = computed(() => ({
  system: $t('serverInfo.labelSystem'),
  path: $t('serverInfo.labelPath'),
  host: $t('serverInfo.labelHost'),
  local: $t('serverInfo.labelLocal'),
  remote: $t('serverInfo.labelRemote'),
  application: $t('serverInfo.labelApplication'),
  applicationTitle: $t('serverInfo.labelApplicationTitle'),
  version: $t('serverInfo.labelVersion'),
  os: $t('serverInfo.labelOs'),
  osVersion: $t('serverInfo.labelOsVersion'),
  machineId: $t('serverInfo.labelMachineId'),
  machineProduct: $t('serverInfo.labelMachineProduct'),
  cpu: $t('serverInfo.labelCpu'),
  openTime: $t('serverInfo.labelOpenTime'),
  serverTime: $t('serverInfo.labelServerTime'),
  memory: $t('serverInfo.labelMemory'),
  processTime: $t('serverInfo.labelProcessTime'),
  gc: $t('serverInfo.labelGc')
}));

const systemEntries = computed(() =>
  Object.entries(systemInfo).map(([key, value]) => ({
    key,
    label: (SYSTEM_LABELS.value as Record<string, string>)[key] || key,
    value: formatValue(value)
  }))
);

function formatValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return $t('serverInfo.noData');
  if (typeof value === 'boolean') return value ? $t('common.yesOrNo.yes') : $t('common.yesOrNo.no');
  if (Array.isArray(value)) return value.map(item => String(item)).join(', ');
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
}

function formatFileSize(bytes: number): string {
  if (!bytes) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${Number((bytes / 1024 ** index).toFixed(2))} ${units[index]}`;
}

async function loadSystemInfo() {
  loading.system = true;
  try {
    const data = unwrap<Record<string, unknown>>(await cubeApi.page.getAction('Admin/Index', 'Main'));
    Object.keys(systemInfo).forEach(key => delete systemInfo[key]);
    if (data && typeof data === 'object') {
      Object.entries(data).forEach(([key, value]) => {
        if (value !== null && value !== undefined) systemInfo[key] = value;
      });
    }
  } finally {
    loading.system = false;
  }
}

async function loadServerVars() {
  loading.serverVars = true;
  try {
    const data = unwrap<{ server?: NameValue[]; request?: NameValue[] }>(
      await cubeApi.page.getAction('Admin/Index', 'ServerVarList')
    );
    serverHeaders.value = Array.isArray(data?.server)
      ? data.server.map(item => ({ name: item.name || '', value: item.value ?? '' }))
      : [];
    requestInfo.value = Array.isArray(data?.request)
      ? data.request.map(item => ({ name: item.name || '', value: item.value ?? '' }))
      : [];
  } catch {
    serverHeaders.value = [];
    requestInfo.value = [];
  } finally {
    loading.serverVars = false;
  }
}

async function loadProcesses() {
  loading.processes = true;
  try {
    // model 为空也要显式传参：后端 [ApiController] 对非可空参数做隐式必填校验
    const data = unwrap<ProcessInfo[]>(
      await cubeApi.page.getAction('Admin/Index', 'ProcessList', { model: processKeyword.value })
    );
    processes.value = Array.isArray(data) ? data : [];
  } catch {
    processes.value = [];
  } finally {
    loading.processes = false;
  }
}

async function loadAssemblies() {
  loading.assemblies = true;
  try {
    const data = unwrap<AssemblyInfo[]>(
      await cubeApi.page.getAction('Admin/Index', 'AssemblyList', { model: assemblyKeyword.value })
    );
    assemblies.value = Array.isArray(data) ? data : [];
  } catch {
    assemblies.value = [];
  } finally {
    loading.assemblies = false;
  }
}

async function loadMenuTree() {
  loading.menu = true;
  try {
    const data = unwrap<MenuNode[]>(
      await cubeApi.page.getAction('Admin/Index', 'GetMenuTree', { module: menuModule.value })
    );
    menuTree.value = Array.isArray(data) ? data : [];
  } catch {
    menuTree.value = [];
  } finally {
    loading.menu = false;
  }
}

async function refreshAll() {
  await Promise.all([loadSystemInfo(), loadServerVars(), loadProcesses(), loadAssemblies(), loadMenuTree()]);
}

async function memoryFree() {
  try {
    await cubeApi.page.getAction('Admin/Index', 'MemoryFree');
    ElMessage.success($t('serverInfo.memoryFreeSuccess'));
    await loadSystemInfo();
  } catch {
    // 请求层已提示错误
  }
}

async function restart() {
  try {
    await ElMessageBox.confirm($t('serverInfo.restartConfirm'), $t('serverInfo.restartTitle'), {
      confirmButtonText: $t('common.confirm'),
      cancelButtonText: $t('common.cancel'),
      type: 'warning'
    });
    await cubeApi.page.postAction('Admin/Index', 'Restart');
  } catch {
    // 取消或错误已处理
  }
}

onMounted(refreshAll);
</script>

<template>
  <div class="server-info">
    <PageHeader :title="$t('serverInfo.title')" :subtitle="$t('serverInfo.subtitle')">
      <template #extra>
        <ElButton @click="refreshAll">{{ $t('common.refresh') }}</ElButton>
      </template>
    </PageHeader>

    <ElCard shadow="never">
      <template #header>
        <div class="server-info__card-header">
          <span>{{ $t('serverInfo.systemInfo') }}</span>
          <ElButton size="small" @click="loadSystemInfo">{{ $t('common.refresh') }}</ElButton>
        </div>
      </template>
      <div v-loading="loading.system">
        <ElDescriptions v-if="systemEntries.length" :column="3" border>
          <ElDescriptionsItem v-for="item in systemEntries" :key="item.key" :label="item.label">
            {{ item.value }}
          </ElDescriptionsItem>
        </ElDescriptions>
        <ElEmpty v-else :description="$t('serverInfo.noSystemInfo')" :image-size="80" />
      </div>
    </ElCard>

    <div class="server-info__grid">
      <ElCard shadow="never">
        <template #header>
          <div class="server-info__card-header">
            <span>{{ $t('serverInfo.serverVars') }}</span>
            <ElButton size="small" @click="loadServerVars">{{ $t('common.refresh') }}</ElButton>
          </div>
        </template>
        <div v-loading="loading.serverVars" class="server-info__scroll">
          <ElCollapse v-model="activeVars" accordion>
            <ElCollapseItem :title="$t('serverInfo.serverHeaders')" name="server">
              <ElTable :data="serverHeaders" size="small" stripe max-height="300">
                <ElTableColumn prop="name" :label="$t('serverInfo.headerName')" width="180" show-overflow-tooltip />
                <ElTableColumn :label="$t('serverInfo.value')" min-width="220" show-overflow-tooltip>
                  <template #default="{ row }">{{ formatValue(row.value) }}</template>
                </ElTableColumn>
              </ElTable>
            </ElCollapseItem>
            <ElCollapseItem :title="$t('serverInfo.requestInfo')" name="request">
              <ElTable :data="requestInfo" size="small" stripe max-height="300">
                <ElTableColumn prop="name" :label="$t('serverInfo.attrName')" width="180" show-overflow-tooltip />
                <ElTableColumn :label="$t('serverInfo.value')" min-width="220" show-overflow-tooltip>
                  <template #default="{ row }">{{ formatValue(row.value) }}</template>
                </ElTableColumn>
              </ElTable>
            </ElCollapseItem>
          </ElCollapse>
          <ElEmpty
            v-if="serverHeaders.length === 0 && requestInfo.length === 0"
            :description="$t('serverInfo.noServerVars')"
            :image-size="80"
          />
        </div>
      </ElCard>

      <ElCard shadow="never">
        <template #header>
          <div class="server-info__card-header">
            <span>{{ $t('serverInfo.loadedModules') }}</span>
            <ElInput
              v-model="processKeyword"
              :placeholder="$t('serverInfo.filterModules')"
              class="server-info__filter"
              clearable
              @change="loadProcesses"
            />
          </div>
        </template>
        <div v-loading="loading.processes" class="server-info__scroll">
          <ElTable :data="processes" size="small" stripe border max-height="360">
            <ElTableColumn prop="name" :label="$t('serverInfo.fileName')" min-width="180" show-overflow-tooltip />
            <ElTableColumn prop="productName" :label="$t('serverInfo.productName')" min-width="150" show-overflow-tooltip />
            <ElTableColumn prop="companyName" :label="$t('serverInfo.companyName')" min-width="120" show-overflow-tooltip />
            <ElTableColumn prop="version" :label="$t('serverInfo.version')" width="120" />
            <ElTableColumn :label="$t('serverInfo.size')" width="90" align="right">
              <template #default="{ row }">{{ formatFileSize(row.size) }}</template>
            </ElTableColumn>
          </ElTable>
        </div>
      </ElCard>
    </div>

    <ElCard shadow="never">
      <template #header>
        <div class="server-info__card-header">
          <span>{{ $t('serverInfo.assemblies') }}</span>
          <div class="server-info__actions">
            <ElInput
              v-model="assemblyKeyword"
              :placeholder="$t('serverInfo.filterAssemblies')"
              class="server-info__filter"
              clearable
              @change="loadAssemblies"
            />
            <ElButton type="warning" @click="memoryFree">{{ $t('serverInfo.memoryFree') }}</ElButton>
            <ElButton type="danger" @click="restart">{{ $t('serverInfo.restart') }}</ElButton>
          </div>
        </div>
      </template>
      <div v-loading="loading.assemblies">
        <ElTable :data="assemblies" size="small" stripe border max-height="360">
          <ElTableColumn prop="name" :label="$t('serverInfo.assemblyName')" min-width="180" show-overflow-tooltip />
          <ElTableColumn prop="title" :label="$t('serverInfo.assemblyTitle')" min-width="160" show-overflow-tooltip />
          <ElTableColumn prop="fileVersion" :label="$t('serverInfo.fileVersion')" width="130" />
          <ElTableColumn prop="version" :label="$t('serverInfo.assemblyVersion')" width="130" />
          <ElTableColumn prop="compileTime" :label="$t('serverInfo.compileTime')" width="160" />
          <ElTableColumn prop="location" :label="$t('serverInfo.location')" min-width="280" show-overflow-tooltip />
        </ElTable>
      </div>
    </ElCard>

    <ElCard shadow="never">
      <template #header>
        <div class="server-info__card-header">
          <span>{{ $t('serverInfo.menuTree') }}</span>
          <ElSelect v-model="menuModule" :placeholder="$t('serverInfo.selectModule')" class="server-info__filter" @change="loadMenuTree">
            <ElOption :label="$t('serverInfo.all')" value="" />
            <ElOption label="Admin" value="Admin" />
            <ElOption label="Cube" value="Cube" />
          </ElSelect>
        </div>
      </template>
      <div v-loading="loading.menu" class="server-info__tree">
        <ElTree
          :data="menuTree"
          :props="{ children: 'children', label: 'displayName' }"
          node-key="id"
          default-expand-all
        >
          <template #default="{ data }">
            <div class="server-info__menu-node">
              <span class="server-info__menu-name">{{ data.displayName }}</span>
              <span v-if="data.url" class="server-info__menu-url">({{ data.url }})</span>
              <ElTag v-if="!data.visible" size="small" type="info">{{ $t('serverInfo.hidden') }}</ElTag>
              <ElTag v-if="data.newWindow" size="small" type="warning">{{ $t('serverInfo.newWindow') }}</ElTag>
              <ElTag
                v-for="(permission, key) in data.permissions || {}"
                :key="key"
                size="small"
                class="server-info__permission"
              >
                {{ permission }}
              </ElTag>
            </div>
          </template>
        </ElTree>
        <ElEmpty v-if="!menuTree.length" :description="$t('serverInfo.noMenu')" :image-size="80" />
      </div>
    </ElCard>
  </div>
</template>

<style scoped>
.server-info {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.server-info :deep(.el-card__header) {
  padding-top: 16px;
}

.server-info__card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.server-info__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

@media (max-width: 1100px) {
  .server-info__grid {
    grid-template-columns: minmax(0, 1fr);
  }
}

.server-info__scroll {
  max-height: 420px;
  overflow: auto;
}

.server-info__filter {
  width: 180px;
}

.server-info__actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.server-info__menu-node {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
}

.server-info__menu-name {
  font-weight: 500;
  color: var(--el-text-color-primary);
}

.server-info__menu-url {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-family: var(--el-font-family-mono, monospace);
}

.server-info__permission {
  margin-left: 2px;
}
</style>
