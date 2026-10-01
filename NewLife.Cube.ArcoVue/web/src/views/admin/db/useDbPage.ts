/**
 * Admin/Db 专用页业务 TS（OSC-2608139feb）。
 *
 * 数据库管理：列表（不含连接串）+ 备份/备份并压缩 + 下载架构 XML。
 */
import { computed, onMounted, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { Auth, checkAuth } from '@newlifex/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { formatApiError } from '@/core/utils/apiError';
import { blobOf, saveBlob } from '@/core/utils/download';
import { flattenDiff, getDbActionPermissions, type DbDiffTable } from '@/core/utils/dbPage';
import { recordDrawerWidth } from '@/views/crud/recordDrawerWidth';

/** 后端 DbItem 归一（兼容 PascalCase/camelCase） */
export interface DbItemView {
  name: string;
  type: string;
  version: string;
  backups: number;
}

export interface DbCountItem {
  name: string;
  tableName: string;
  count: number | null;
}

type DbDrawerMode = 'tables' | 'entities' | 'diff';
type DbApiResponse<T> = { code?: number; message?: string; data?: T };

/** DbItem 行归一化；name 为空的行丢弃 */
export function dbItemOf(row: Record<string, unknown>): DbItemView | null {
  const name = String(row.Name ?? row.name ?? '');
  if (!name) return null;
  return {
    name,
    type: String(row.Type ?? row.type ?? ''),
    version: String(row.Version ?? row.version ?? ''),
    backups: Number(row.Backups ?? row.backups ?? 0) || 0,
  };
}

/** Admin/Db 页全部业务 TS（薄 SFC 宿主） */
export function useDbPage() {
  const userStore = useUserStore();

  const rows = ref<DbItemView[]>([]);
  const loading = ref(false);
  const error = ref('');
  const busy = ref(false);
  const compactingName = ref('');
  const drawerVisible = ref(false);
  const drawerLoading = ref(false);
  const drawerError = ref('');
  const drawerMode = ref<DbDrawerMode>('tables');
  const drawerTitle = ref('');
  const drawerRows = ref<DbCountItem[]>([]);
  const diffRows = ref<ReturnType<typeof flattenDiff>>([]);
  const drawerWidth = recordDrawerWidth(0);
  let drawerRequest = 0;

  /** 备份/备份并压缩（Insert 权限）；无权限配置时允许（开发友好） */
  const canBackup = computed(() => {
    const perms = userStore.getMenuPermission('Admin/Db');
    const keys = Object.keys(perms ?? {}).length;
    return keys === 0 || checkAuth(perms, Auth.ADD);
  });
  const dbActionPermissions = computed(() => getDbActionPermissions(userStore.getMenuPermission('Admin/Db')));

  async function load() {
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.page.getDbList();
      const data = (res as unknown as { data?: unknown })?.data ?? res;
      rows.value = (Array.isArray(data) ? data : [])
        .map((r) => dbItemOf(r as Record<string, unknown>))
        .filter((r): r is DbItemView => r !== null);
    } catch (err) {
      error.value = formatApiError(err, '数据库列表加载失败');
      rows.value = [];
    } finally {
      loading.value = false;
    }
  }

  /** 备份指定数据库（后端 Backup/BackupAndCompress 的 name 为连接名） */
  async function runBackup(name: string, compress: boolean) {
    if (busy.value) return;
    busy.value = true;
    try {
      if (compress) await cubeApi.page.backupAndCompressDb(name);
      else await cubeApi.page.backupDb(name);
      Message.success(compress ? `备份并压缩 ${name} 成功` : `备份 ${name} 成功`);
      await load();
    } catch (err) {
      Message.error(formatApiError(err, '备份失败'));
    } finally {
      busy.value = false;
    }
  }

  /** 确认备份指定数据库 */
  function confirmBackup(name: string, compress: boolean) {
    Modal.confirm({
      title: compress ? `确认备份并压缩 ${name}？` : `确认备份 ${name}？`,
      content: compress ? '备份并压缩可能耗时较长' : '将在备份目录生成备份文件',
      onOk: () => runBackup(name, compress),
    });
  }

  async function downloadSchema(name: string) {
    try {
      // 后端返回 application/xml 文件流；拦截器不会解包非 octet-stream 响应，统一经 blobOf 取 Blob
      const res = await cubeApi.client.request<Blob>({
        url: '/Admin/Db/Download',
        method: 'get',
        params: { name },
        responseType: 'blob',
      });
      const blob = blobOf(res);
      if (!blob) throw new Error('未获取到架构文件');
      saveBlob(blob, `${name}.xml`);
    } catch (err) {
      Message.error(formatApiError(err, '下载失败'));
    }
  }

  function readDbResponse<T>(response: unknown): T {
    const result = response as DbApiResponse<T>;
    if (result.code !== undefined && result.code !== 0) throw new Error(result.message || '操作失败');
    if (result.data === undefined) throw new Error(result.message || '未返回数据');
    return result.data;
  }

  async function openDrawer(name: string, mode: DbDrawerMode) {
    const request = ++drawerRequest;
    drawerVisible.value = true;
    drawerMode.value = mode;
    drawerTitle.value = `${mode === 'tables' ? '表' : mode === 'entities' ? '实体' : '差异'} · ${name}`;
    drawerError.value = '';
    drawerRows.value = [];
    diffRows.value = [];
    drawerLoading.value = true;
    try {
      if (mode === 'tables') {
        const data = readDbResponse<{ tables?: DbCountItem[] }>(await cubeApi.page.dbTables(name));
        if (request !== drawerRequest) return;
        drawerRows.value = Array.isArray(data.tables) ? data.tables : [];
      } else if (mode === 'entities') {
        const data = readDbResponse<{ entities?: DbCountItem[] }>(await cubeApi.page.dbEntities(name));
        if (request !== drawerRequest) return;
        drawerRows.value = Array.isArray(data.entities) ? data.entities : [];
      } else {
        const data = readDbResponse<{ tables?: DbDiffTable[] }>(await cubeApi.page.dbDiff(name));
        if (request !== drawerRequest) return;
        diffRows.value = flattenDiff(Array.isArray(data.tables) ? data.tables : []);
      }
    } catch (err) {
      if (request !== drawerRequest) return;
      drawerError.value = formatApiError(err, '数据库信息加载失败');
    } finally {
      if (request === drawerRequest) drawerLoading.value = false;
    }
  }

  async function runCompact(name: string) {
    if (compactingName.value) return;
    compactingName.value = name;
    try {
      const result = await cubeApi.page.dbCompact(name);
      const response = result as DbApiResponse<unknown>;
      if (response.code !== undefined && response.code !== 0) throw new Error(response.message || '压缩失败');
      Message.success(response.message || '压缩完成');
      await load();
    } catch (err) {
      Message.error(formatApiError(err, '压缩失败'));
    } finally {
      compactingName.value = '';
    }
  }

  function confirmCompact(name: string) {
    Modal.confirm({
      title: `确认压缩数据库 ${name}？`,
      content: 'SQLite 将执行 VACUUM。',
      onOk: () => runCompact(name),
    });
  }

  onMounted(() => {
    void load();
  });

  return {
    rows,
    loading,
    error,
    busy,
    canBackup,
    canInspect: computed(() => dbActionPermissions.value.canInspect),
    canCompact: computed(() => dbActionPermissions.value.canCompact),
    compactingName,
    drawerVisible,
    drawerLoading,
    drawerError,
    drawerMode,
    drawerTitle,
    drawerRows,
    diffRows,
    drawerWidth,
    load,
    confirmBackup,
    downloadSchema,
    openDrawer,
    confirmCompact,
  };
}
