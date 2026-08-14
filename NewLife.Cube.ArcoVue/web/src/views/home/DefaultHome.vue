<template>
  <div class="default-home">
    <div class="home-toolbar">
      <a-space>
        <a-button :loading="mainLoading || serverVarLoading || processLoading || assemblyLoading" @click="refreshAll">
          刷新
        </a-button>
        <a-button v-if="canUpdate" status="warning" @click="confirmMemoryFree">释放内存</a-button>
        <a-button v-if="canUpdate" status="danger" @click="confirmRestart">重启</a-button>
      </a-space>
    </div>

    <a-card title="系统信息" class="home-card">
      <template #extra>
        <a-button size="mini" :loading="mainLoading" @click="loadMain">刷新</a-button>
      </template>
      <a-alert v-if="mainError" type="warning" show-icon class="home-alert">{{ mainError }}</a-alert>
      <a-empty v-else-if="!mainEntriesComputed.length" description="暂无系统信息" />
      <a-descriptions v-else :column="2" size="medium" bordered>
        <a-descriptions-item v-for="e in mainEntriesComputed" :key="e.label" :label="e.label">
          {{ e.value }}
        </a-descriptions-item>
      </a-descriptions>
    </a-card>

    <a-card title="服务器变量" class="home-card">
      <template #extra>
        <a-button size="mini" :loading="serverVarLoading" @click="loadServerVar">刷新</a-button>
      </template>
      <a-alert v-if="serverVarError" type="warning" show-icon class="home-alert">{{ serverVarError }}</a-alert>
      <a-empty
        v-else-if="!serverVar.server.length && !serverVar.request.length"
        description="暂无服务器变量"
      />
      <a-collapse v-else>
        <a-collapse-item header="服务器 Headers" key="server">
          <a-table
            :data="serverVar.server"
            :columns="kvColumns"
            :pagination="false"
            size="mini"
          />
        </a-collapse-item>
        <a-collapse-item header="请求信息" key="request">
          <a-table
            :data="serverVar.request"
            :columns="kvColumns"
            :pagination="false"
            size="mini"
          />
        </a-collapse-item>
      </a-collapse>
    </a-card>

    <a-card title="进程模块" class="home-card">
      <template #extra>
        <a-space>
          <a-select
            :model-value="processModel"
            size="mini"
            style="width: 120px"
            @change="loadProcess"
          >
            <a-option value="">仅用户模块</a-option>
            <a-option value="All">全部模块</a-option>
          </a-select>
          <a-button size="mini" :loading="processLoading" @click="loadProcess(processModel)">刷新</a-button>
        </a-space>
      </template>
      <a-alert v-if="processError" type="warning" show-icon class="home-alert">{{ processError }}</a-alert>
      <a-empty v-else-if="!processRows.length" description="暂无进程模块" />
      <a-table
        v-else
        :data="processFlat"
        :columns="processColumns"
        :pagination="false"
        size="mini"
      />
    </a-card>

    <a-card title="程序集" class="home-card">
      <template #extra>
        <a-space>
          <a-select
            :model-value="assemblyModel"
            size="mini"
            style="width: 120px"
            @change="loadAssembly"
          >
            <a-option value="">仅本应用</a-option>
            <a-option value="All">全部程序集</a-option>
          </a-select>
          <a-button size="mini" :loading="assemblyLoading" @click="loadAssembly(assemblyModel)">刷新</a-button>
        </a-space>
      </template>
      <a-alert v-if="assemblyError" type="warning" show-icon class="home-alert">{{ assemblyError }}</a-alert>
      <a-empty v-else-if="!assemblyRows.length" description="暂无程序集" />
      <a-table
        v-else
        :data="assemblyFlat"
        :columns="assemblyColumns"
        :pagination="false"
        size="mini"
      />
    </a-card>
  </div>
</template>

<script setup lang="ts">
/**
 * DefaultHome — 主页仪表盘（OSC-2608139feb）
 * /home 与 /Admin/Index 共用；消费 Admin/Index 的 Main/ServerVar/Process/Assembly。
 */
import { computed, ref } from 'vue';
import {
  assemblyTableRows,
  mainEntries,
  processTableRows,
  useDefaultHome,
} from './useDefaultHome';

const {
  main,
  mainLoading,
  mainError,
  serverVar,
  serverVarLoading,
  serverVarError,
  processRows,
  processLoading,
  processError,
  assemblyRows,
  assemblyLoading,
  assemblyError,
  canUpdate,
  loadMain,
  loadServerVar,
  loadProcess,
  loadAssembly,
  refreshAll,
  confirmMemoryFree,
  confirmRestart,
} = useDefaultHome();

const processModel = ref('');
const assemblyModel = ref('');

const mainEntriesComputed = computed(() => mainEntries(main.value));
const processFlat = computed(() => processTableRows(processRows.value));
const assemblyFlat = computed(() => assemblyTableRows(assemblyRows.value));

const kvColumns = [
  { title: '属性', dataIndex: 'name', ellipsis: true, tooltip: true },
  { title: '值', dataIndex: 'value', ellipsis: true, tooltip: true },
];
/** 进程模块七列表格：名称/公司/产品/说明/版本/大小(MB)/文件名 */
const processColumns = [
  { title: '名称', dataIndex: 'name', ellipsis: true, tooltip: true, width: 130 },
  { title: '公司', dataIndex: 'companyName', ellipsis: true, tooltip: true },
  { title: '产品', dataIndex: 'productName', ellipsis: true, tooltip: true },
  { title: '说明', dataIndex: 'description', ellipsis: true, tooltip: true },
  { title: '版本', dataIndex: 'version', ellipsis: true, tooltip: true, width: 110 },
  { title: '大小(MB)', dataIndex: 'size', width: 100 },
  { title: '文件名', dataIndex: 'fileName', ellipsis: true, tooltip: true },
];
/** 程序集六列表格：名称/显示名/文件版本/版本/编译时间/文件位置 */
const assemblyColumns = [
  { title: '名称', dataIndex: 'name', ellipsis: true, tooltip: true, width: 160 },
  { title: '显示名', dataIndex: 'title', ellipsis: true, tooltip: true },
  { title: '文件版本', dataIndex: 'fileVersion', ellipsis: true, tooltip: true, width: 110 },
  { title: '版本', dataIndex: 'version', ellipsis: true, tooltip: true, width: 110 },
  { title: '编译时间', dataIndex: 'compileTime', ellipsis: true, tooltip: true, width: 150 },
  { title: '文件位置', dataIndex: 'location', ellipsis: true, tooltip: true },
];
</script>

<style scoped>
.home-toolbar {
  margin-bottom: 16px;
}
.home-card {
  margin-bottom: 16px;
}
.home-alert {
  margin-bottom: 12px;
}
</style>
