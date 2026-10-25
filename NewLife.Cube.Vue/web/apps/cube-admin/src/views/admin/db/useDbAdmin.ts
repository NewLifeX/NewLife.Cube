import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import {
  normalizeDbList,
  normalizeDiffList,
  normalizeSchemaList,
  type DbDiffView,
  type DbItemView,
  type DbSchemaTableView,
} from '@newlifex/cube-vue/core/utils/dbSchema';

/**
 * 数据库管理页状态与动作
 *
 * 列表、表结构、实体结构、模型差异都来自 `DbController` 的 JSON 接口；
 * 备份/压缩/下载沿用各自的动作端点。视图只做渲染与绑定。
 */
export function useDbAdmin() {
  const { t } = useI18n();

  const databases = ref<DbItemView[]>([]);
  const loading = ref(false);
  const acting = ref('');

  const schemaVisible = ref(false);
  const schemaLoading = ref(false);
  const schemaMode = ref<'tables' | 'entities'>('tables');
  const schemas = ref<DbSchemaTableView[]>([]);

  const diffVisible = ref(false);
  const diffLoading = ref(false);
  const diffs = ref<DbDiffView[]>([]);

  /** 数据库列表 */
  async function loadDatabases() {
    loading.value = true;
    try {
      databases.value = normalizeDbList(await request.get('/Admin/Db'));
    } catch {
      databases.value = [];
      ElMessage.error(t('common.loadFailed'));
    } finally {
      loading.value = false;
    }
  }

  /** 表结构 / 实体结构 */
  async function openSchema(mode: 'tables' | 'entities', name: string) {
    schemaMode.value = mode;
    schemaVisible.value = true;
    schemaLoading.value = true;
    schemas.value = [];
    try {
      schemas.value = normalizeSchemaList(await request.get(`/Admin/Db/${mode}`, { params: { name } }));
    } catch {
      schemas.value = [];
    } finally {
      schemaLoading.value = false;
    }
  }

  /** 模型差异 */
  async function openDiff(name: string) {
    diffVisible.value = true;
    diffLoading.value = true;
    diffs.value = [];
    try {
      diffs.value = normalizeDiffList(await request.get('/Admin/Db/diff', { params: { name } }));
    } catch {
      diffs.value = [];
    } finally {
      diffLoading.value = false;
    }
  }

  /** 压缩数据库 */
  async function compact(name: string) {
    acting.value = `${name}:compact`;
    try {
      await request.post('/Admin/Db/compact', null, { params: { name } });
      ElMessage.success(t('db.compactSuccess'));
      await loadDatabases();
    } catch {
      ElMessage.error(t('db.compactFailed'));
    } finally {
      acting.value = '';
    }
  }

  /** 备份 / 备份并压缩 */
  async function backup(name: string, compress: boolean) {
    if (compress) {
      try {
        await ElMessageBox.confirm(t('db.compressConfirm'), t('db.confirmAction'), {
          confirmButtonText: t('button.confirm'),
          cancelButtonText: t('button.cancel'),
          type: 'warning',
        });
      } catch {
        return;
      }
    }

    acting.value = `${name}:${compress ? 'compress' : 'backup'}`;
    try {
      const url = compress ? '/Admin/Db/BackupAndCompress' : '/Admin/Db/Backup';
      await request.post(url, { name });
      ElMessage.success(compress ? t('db.compressSuccess') : t('db.backupSuccess'));
      await loadDatabases();
    } catch {
      ElMessage.error(compress ? t('db.compressFailed') : t('db.backupFailed'));
    } finally {
      acting.value = '';
    }
  }

  /** 下载表结构（XML） */
  async function downloadSchema(name: string) {
    acting.value = `${name}:download`;
    try {
      const blob = (await request.get('/Admin/Db/Download', {
        params: { name },
        responseType: 'blob',
      })) as unknown as Blob;

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${name}.xml`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch {
      ElMessage.error(t('db.downloadFailed'));
    } finally {
      acting.value = '';
    }
  }

  return {
    databases,
    loading,
    acting,
    schemaVisible,
    schemaLoading,
    schemaMode,
    schemas,
    diffVisible,
    diffLoading,
    diffs,
    loadDatabases,
    openSchema,
    openDiff,
    compact,
    backup,
    downloadSchema,
  };
}
