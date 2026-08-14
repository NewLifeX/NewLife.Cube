/**
 * DefaultHome 业务 TS（OSC-2608139feb）。
 *
 * 主页仪表盘：Main 系统信息 descriptions + ServerVar/Process/Assembly 三块可刷新卡片；
 * MemoryFree / Restart 带确认框，按 Admin/Index 的 Update 权限显隐。
 */
import { computed, onMounted, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { Auth, checkAuth } from '@cube/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { formatApiError } from '@/core/utils/apiError';

/** 从 request 响应解包 data */
function unwrapData(res: unknown): unknown {
  if (res && typeof res === 'object' && !Array.isArray(res) && 'data' in res) {
    return (res as { data?: unknown }).data;
  }
  return res;
}

/** 表格行摊平：`{name,value}` 原样；否则每行一个顶层键 → [属性, 值] */
export function flattenRows(rows: Record<string, unknown>[]): Array<{ name: string; value: string }> {
  const out: Array<{ name: string; value: string }> = [];
  for (const row of rows) {
    if (!row || typeof row !== 'object') continue;
    const keys = Object.keys(row);
    if (keys.length === 2 && 'name' in row && 'value' in row) {
      out.push({ name: String(row.name ?? ''), value: String(row.value ?? '') });
      continue;
    }
    for (const [k, v] of Object.entries(row)) {
      if (v == null || v === '') continue;
      out.push({ name: k, value: typeof v === 'object' ? JSON.stringify(v) : String(v) });
    }
  }
  return out;
}

/** Main 对象 → descriptions 条目；值为对象则 JSON.stringify；null/undefined 跳过 */
export function mainEntries(main: Record<string, unknown>): Array<{ label: string; value: string }> {
  const out: Array<{ label: string; value: string }> = [];
  for (const [k, v] of Object.entries(main)) {
    if (v == null || v === '') continue;
    out.push({
      label: k,
      value: typeof v === 'object' ? JSON.stringify(v) : String(v),
    });
  }
  return out;
}

/** 字节数 → MB 文本（保留两位小数）；非正数/非法值返回空串 */
export function formatSizeMb(value: unknown): string {
  const n = Number(value);
  if (!Number.isFinite(n) || n <= 0) return '';
  return `${(n / 1024 / 1024).toFixed(2)} MB`;
}

/** 进程模块行（后端 ProcessList：name/companyName/productName/description/version/size/fileName） */
export interface ProcessTableRow {
  name: string;
  companyName: string;
  productName: string;
  description: string;
  version: string;
  size: string;
  fileName: string;
}

/** 进程模块表格行映射：size 字节 → MB 文本 */
export function processTableRows(rows: Record<string, unknown>[]): ProcessTableRow[] {
  return rows.map((r) => ({
    name: String(r.name ?? ''),
    companyName: String(r.companyName ?? ''),
    productName: String(r.productName ?? ''),
    description: String(r.description ?? ''),
    version: String(r.version ?? ''),
    size: formatSizeMb(r.size),
    fileName: String(r.fileName ?? ''),
  }));
}

/** 程序集表格行（后端 AssemblyList：name/title/fileVersion/version/compileTime/location） */
export interface AssemblyTableRow {
  name: string;
  title: string;
  fileVersion: string;
  version: string;
  compileTime: string;
  location: string;
}

/** 程序集表格行映射（六列原样透传） */
export function assemblyTableRows(rows: Record<string, unknown>[]): AssemblyTableRow[] {
  return rows.map((r) => ({
    name: String(r.name ?? ''),
    title: String(r.title ?? ''),
    fileVersion: String(r.fileVersion ?? ''),
    version: String(r.version ?? ''),
    compileTime: String(r.compileTime ?? ''),
    location: String(r.location ?? ''),
  }));
}

/** DefaultHome 组件全部业务 TS（薄 SFC 宿主） */
export function useDefaultHome() {
  const userStore = useUserStore();

  const main = ref<Record<string, unknown>>({});
  const mainLoading = ref(false);
  const mainError = ref('');

  const serverVar = ref<{
    server: Array<{ name: string; value: string }>;
    request: Array<{ name: string; value: string }>;
  }>({ server: [], request: [] });
  const serverVarLoading = ref(false);
  const serverVarError = ref('');

  const processRows = ref<Record<string, unknown>[]>([]);
  const processLoading = ref(false);
  const processError = ref('');

  const assemblyRows = ref<Record<string, unknown>[]>([]);
  const assemblyLoading = ref(false);
  const assemblyError = ref('');

  /** Admin/Index 的 Update 权限；无菜单权限配置时允许（开发友好，与 resolveCrudFlags 一致） */
  const canUpdate = computed(() => {
    const perms = userStore.getMenuPermission('Admin/Index');
    const keys = Object.keys(perms ?? {}).length;
    return keys === 0 || checkAuth(perms, Auth.EDIT);
  });

  async function loadMain() {
    mainLoading.value = true;
    mainError.value = '';
    try {
      const res = await cubeApi.page.getIndexMain();
      const data = unwrapData(res);
      main.value = data && typeof data === 'object' && !Array.isArray(data)
        ? (data as Record<string, unknown>)
        : {};
    } catch (err) {
      mainError.value = formatApiError(err, '系统信息加载失败');
      main.value = {};
    } finally {
      mainLoading.value = false;
    }
  }

  async function loadServerVar() {
    serverVarLoading.value = true;
    serverVarError.value = '';
    try {
      const res = await cubeApi.page.getServerVarList();
      const data = unwrapData(res) as {
        server?: Array<{ name: string; value: string }>;
        request?: Array<{ name: string; value: string }>;
      };
      serverVar.value = { server: data?.server ?? [], request: data?.request ?? [] };
    } catch (err) {
      serverVarError.value = formatApiError(err, '服务器变量加载失败');
      serverVar.value = { server: [], request: [] };
    } finally {
      serverVarLoading.value = false;
    }
  }

  async function loadProcess(model?: string) {
    processLoading.value = true;
    processError.value = '';
    try {
      const res = await cubeApi.page.getProcessList(model);
      const data = unwrapData(res);
      processRows.value = Array.isArray(data) ? (data as Record<string, unknown>[]) : [];
    } catch (err) {
      processError.value = formatApiError(err, '进程模块加载失败');
      processRows.value = [];
    } finally {
      processLoading.value = false;
    }
  }

  async function loadAssembly(model?: string) {
    assemblyLoading.value = true;
    assemblyError.value = '';
    try {
      const res = await cubeApi.page.getAssemblyList(model);
      const data = unwrapData(res);
      assemblyRows.value = Array.isArray(data) ? (data as Record<string, unknown>[]) : [];
    } catch (err) {
      assemblyError.value = formatApiError(err, '程序集加载失败');
      assemblyRows.value = [];
    } finally {
      assemblyLoading.value = false;
    }
  }

  function refreshAll() {
    void loadMain();
    void loadServerVar();
    void loadProcess();
    void loadAssembly();
  }

  function confirmMemoryFree() {
    Modal.confirm({
      title: '确认释放工作集？',
      content: '将触发垃圾回收并收缩当前进程工作集内存',
      onOk: async () => {
        try {
          await cubeApi.page.memoryFree();
          Message.success('释放内存成功');
          void loadMain();
        } catch (err) {
          Message.error(formatApiError(err, '释放内存失败'));
        }
      },
    });
  }

  function confirmRestart() {
    Modal.confirm({
      title: '确认重启应用？',
      content: '未保存数据将丢失',
      onOk: async () => {
        try {
          await cubeApi.page.restart();
          Message.success('重启指令已提交');
        } catch (err) {
          Message.error(formatApiError(err, '重启失败'));
        }
      },
    });
  }

  onMounted(refreshAll);

  return {
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
  };
}
