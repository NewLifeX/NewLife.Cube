/**
 * Admin/Db 专用页业务 TS（OSC-2608139feb）。
 *
 * 数据库管理：列表（不含连接串）+ 备份/备份并压缩 + 下载架构 XML。
 */
import { computed, onMounted, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { Auth, checkAuth } from '@cube/page-utils';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { formatApiError } from '@/core/utils/apiError';
import { blobOf, saveBlob } from '@/core/utils/download';

/** 后端 DbItem 归一（兼容 PascalCase/camelCase） */
export interface DbItemView {
  name: string;
  type: string;
  version: string;
  backups: number;
}

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

  /** 备份/备份并压缩（Insert 权限）；无权限配置时允许（开发友好） */
  const canBackup = computed(() => {
    const perms = userStore.getMenuPermission('Admin/Db');
    const keys = Object.keys(perms ?? {}).length;
    return keys === 0 || checkAuth(perms, Auth.ADD);
  });

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

  onMounted(() => {
    void load();
  });

  return {
    rows,
    loading,
    error,
    busy,
    canBackup,
    load,
    confirmBackup,
    downloadSchema,
  };
}
