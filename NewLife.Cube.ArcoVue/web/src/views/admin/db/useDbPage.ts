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
import { getDbActionPermissions, splitDbDescription } from '@/core/utils/dbPage';
import { RECORD_DRAWER_WIDE } from '@/views/crud/recordDrawerWidth';

/** 后端 DbItem 归一（兼容 PascalCase/camelCase） */
export interface DbItemView {
  name: string;
  type: string;
  version: string;
  backups: number;
}

export interface DbCountItem {
  name: string;
  description?: string;
  tableName: string;
  count: number | null;
  /** 无实体模型的纯数据表行（T4-1：实体列表合并后的标记） */
  pureTable?: boolean;
}

/** 实体字段数据字典项（列对齐 CubeNC Db/Entities.cshtml 字段架构定义） */
export interface DbFieldItem {
  name: string;
  displayName?: string;
  type?: string;
  length?: number;
  precision?: number;
  scale?: number;
  key?: string;
  nullable?: boolean;
  description?: string;
}

type DbDrawerMode = 'entities' | 'fields';
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

/** 实体行展示名：描述首句（无描述回落技术名） */
export function dbEntityNameText(record: DbCountItem): string {
  return splitDbDescription(record.name, record.description).name;
}

/** 实体行备注：描述首个「。」之后的余下部分；纯表无描述时显示占位说明 */
export function dbEntityRemarkText(record: DbCountItem): string {
  const remark = splitDbDescription(record.name, record.description).remark;
  if (remark) return remark;

  return record.pureTable ? '无实体模型（仅数据表）' : '';
}

/** 合并实体清单与数据表清单：实体在前，无实体模型的纯表追加在后并标记 pureTable */
export function mergeDbEntityRows(entities: DbCountItem[], tables: DbCountItem[]): DbCountItem[] {
  const known = new Set(entities.map((item) => (item.tableName || item.name).toLowerCase()));
  const pureTables = tables
    .filter((item) => !known.has((item.tableName || item.name).toLowerCase()))
    .map((item) => ({ ...item, pureTable: true }));
  return [...entities, ...pureTables];
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
  const drawerMode = ref<DbDrawerMode>('entities');
  const drawerTitle = ref('');
  const drawerConnName = ref('');
  const drawerEntityType = ref('');
  const drawerRows = ref<DbCountItem[]>([]);
  const fieldRows = ref<DbFieldItem[]>([]);
  /** 抽屉宽度与实体对象 详情/编辑/添加 宽抽屉一致（RECORD_DRAWER_WIDE = 720） */
  const drawerWidth = RECORD_DRAWER_WIDE;
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

  async function openDrawer(name: string) {
    const request = ++drawerRequest;
    drawerVisible.value = true;
    drawerMode.value = 'entities';
    drawerConnName.value = name;
    drawerTitle.value = `实体 · ${name}`;
    drawerError.value = '';
    drawerRows.value = [];
    fieldRows.value = [];
    drawerEntityType.value = '';
    drawerLoading.value = true;
    try {
      const [entities, tables] = await Promise.all([
        cubeApi.page.dbEntities(name),
        cubeApi.page.dbTables(name),
      ]);
      if (request !== drawerRequest) return;
      const entityData = readDbResponse<{ entities?: DbCountItem[] }>(entities);
      const tableData = readDbResponse<{ tables?: DbCountItem[] }>(tables);
      drawerRows.value = mergeDbEntityRows(
        Array.isArray(entityData.entities) ? entityData.entities : [],
        Array.isArray(tableData.tables) ? tableData.tables : [],
      );
    } catch (err) {
      if (request !== drawerRequest) return;
      drawerError.value = formatApiError(err, '数据库信息加载失败');
    } finally {
      if (request === drawerRequest) drawerLoading.value = false;
    }
  }

  /** 打开数据字典（同一抽屉）：实体模型取 factory.Fields；无实体模型的纯表取数据库架构字段 */
  async function openDictionary(record: DbCountItem) {
    const request = ++drawerRequest;
    drawerMode.value = 'fields';
    drawerEntityType.value = record.name;
    drawerTitle.value = `数据字典 · ${dbEntityNameText(record)}`;
    drawerError.value = '';
    fieldRows.value = [];
    drawerLoading.value = true;
    try {
      const data = record.pureTable
        ? readDbResponse<{ fields?: DbFieldItem[] }>(
            await cubeApi.page.dbTableFields(drawerConnName.value, record.tableName),
          )
        : readDbResponse<{ fields?: DbFieldItem[] }>(
            await cubeApi.page.dbEntityFields(drawerConnName.value, record.name),
          );
      if (request !== drawerRequest) return;
      fieldRows.value = Array.isArray(data.fields) ? data.fields : [];
    } catch (err) {
      if (request !== drawerRequest) return;
      drawerError.value = formatApiError(err, '字段加载失败');
    } finally {
      if (request === drawerRequest) drawerLoading.value = false;
    }
  }

  /** 数据字典返回实体列表 */
  function backToEntities() {
    if (drawerConnName.value) void openDrawer(drawerConnName.value);
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
    drawerEntityType,
    fieldRows,
    drawerWidth,
    load,
    confirmBackup,
    downloadSchema,
    openDrawer,
    openDictionary,
    backToEntities,
    dbEntityNameText,
    dbEntityRemarkText,
    confirmCompact,
  };
}
