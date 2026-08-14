/**
 * Admin/File 专用页业务 TS（OSC-2608139feb）。
 *
 * 文件管理：目录导航、排序、上传/下载、压缩/解压、复制粘贴/移动、删除、剪切板。
 */
import { computed, onMounted, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';
import { saveBlob, blobOf } from '@/core/utils/download';

/** 文件行视图（后端 FileItem 归一，兼容 PascalCase/camelCase） */
export interface FileRowView {
  name: string;
  fullName: string;
  directory: boolean;
  lastWrite: string;
  size: string;
  isParent: boolean;
}

/** 文件行归一化 */
export function fileRowOf(row: Record<string, unknown>): FileRowView {
  const name = String(row.Name ?? row.name ?? '');
  const directory = Boolean(row.Directory ?? row.directory ?? false);
  return {
    name,
    fullName: String(row.FullName ?? row.fullName ?? ''),
    directory,
    lastWrite: formatDateTime(row.LastWrite ?? row.lastWrite ?? ''),
    size: String(row.Size ?? row.size ?? ''),
    isParent: name === '../',
  };
}

/** 排序键归一：后端仅接受 size/lastwrite（默认名称+目录优先） */
export function sortKeyOf(sort: string): string {
  return sort === 'size' || sort === 'lastwrite' ? sort : 'name';
}

/** 从 request 响应解包 data */
function unwrapData(res: unknown): unknown {
  if (res && typeof res === 'object' && !Array.isArray(res) && 'data' in res) {
    return (res as { data?: unknown }).data;
  }
  return res;
}

/** Admin/File 页全部业务 TS（薄 SFC 宿主） */
export function useFilePage() {
  const userStore = useUserStore();

  const current = ref('');
  const sort = ref('name');
  const rows = ref<FileRowView[]>([]);
  const clip = ref<FileRowView[]>([]);
  const message = ref('');
  const loading = ref(false);
  const error = ref('');
  const busy = ref(false);
  const fileInput = ref<HTMLInputElement | null>(null);

  const flags = computed(() =>
    resolveCrudFlags(userStore.getMenuPermission('Admin/File'), null),
  );

  /** 动作响应（Index JSON：current/list/clip/message）就地应用，避免多余请求 */
  function applyListResponse(res: unknown) {
    const data = unwrapData(res);
    if (!data || typeof data !== 'object' || Array.isArray(data)) return;
    const d = data as {
      current?: unknown;
      list?: unknown;
      clip?: unknown;
      message?: unknown;
    };
    if (d.current != null) current.value = String(d.current);
    if (Array.isArray(d.list)) {
      rows.value = (d.list as Record<string, unknown>[]).map(fileRowOf);
    }
    if (Array.isArray(d.clip)) {
      clip.value = (d.clip as Record<string, unknown>[]).map(fileRowOf);
    }
    message.value = d.message ? String(d.message) : '';
  }

  async function load(r?: string) {
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.page.getFileList({
        r: r ?? (current.value || undefined),
        sort: sortKeyOf(sort.value),
      });
      applyListResponse(res);
    } catch (err) {
      error.value = formatApiError(err, '文件列表加载失败');
      rows.value = [];
    } finally {
      loading.value = false;
    }
  }

  function onSortChange(value: string | number | boolean | Record<string, unknown> | undefined) {
    sort.value = String(value ?? 'name');
    void load();
  }

  /** 进入目录 / 返回上一级 */
  function enter(row: FileRowView) {
    if (!row.directory) return;
    void load(row.fullName || '');
  }

  /** 通用动作：执行后应用返回列表，失败提示 */
  async function runAction(
    action: () => Promise<unknown>,
    successMsg?: string,
  ) {
    if (busy.value) return;
    busy.value = true;
    try {
      const res = await action();
      applyListResponse(res);
      if (successMsg) Message.success(successMsg);
    } catch (err) {
      Message.error(formatApiError(err, '操作失败'));
    } finally {
      busy.value = false;
    }
  }

  function pickFile() {
    fileInput.value?.click();
  }

  async function onPickFile(ev: Event) {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (busy.value) return;
    busy.value = true;
    try {
      await cubeApi.page.uploadToDir(current.value || undefined, file);
      Message.success('上传成功');
      await load();
    } catch (err) {
      Message.error(formatApiError(err, '上传失败'));
    } finally {
      busy.value = false;
    }
  }

  async function download(row: FileRowView) {
    if (busy.value) return;
    busy.value = true;
    try {
      // 后端返回文件流；统一经 blobOf 解包（兼容拦截器透传 Blob 或 AxiosResponse 形态）
      const res = await cubeApi.client.request<Blob>({
        url: '/Admin/File/Download',
        method: 'post',
        params: { r: row.fullName },
        responseType: 'blob',
      });
      const blob = blobOf(res);
      if (!blob) throw new Error('未获取到文件内容');
      saveBlob(blob, row.name);
    } catch (err) {
      Message.error(formatApiError(err, '下载失败'));
    } finally {
      busy.value = false;
    }
  }

  function confirmRemove(row: FileRowView) {
    Modal.confirm({
      title: '确认删除？',
      content: row.directory ? `将递归删除目录「${row.name}」` : `将删除文件「${row.name}」`,
      onOk: () => runAction(() => cubeApi.page.deleteFileRow(row.fullName), '删除成功'),
    });
  }

  function compress(row: FileRowView) {
    Modal.confirm({
      title: '确认压缩？',
      content: `将在同目录生成「${row.name}_时间戳.zip」`,
      onOk: () => runAction(() => cubeApi.page.compressFile(row.fullName), '压缩成功'),
    });
  }

  function decompress(row: FileRowView) {
    Modal.confirm({
      title: '确认解压？',
      content: `将解压「${row.name}」到当前目录`,
      onOk: () => runAction(() => cubeApi.page.decompressFile(row.fullName), '解压成功'),
    });
  }

  function copy(row: FileRowView) {
    void runAction(
      () => cubeApi.page.copyFileToClip(current.value || undefined, row.fullName),
      '已复制到剪切板',
    );
  }

  function cancelCopy(row: FileRowView) {
    void runAction(
      () => cubeApi.page.cancelCopyFile(current.value || undefined, row.fullName),
    );
  }

  function paste() {
    void runAction(() => cubeApi.page.pasteClip(current.value || undefined), '粘贴成功');
  }

  function move() {
    void runAction(() => cubeApi.page.moveClip(current.value || undefined), '移动成功');
  }

  function clearClipboard() {
    void runAction(() => cubeApi.page.clearClipboard(current.value || undefined));
  }

  onMounted(() => {
    void load();
  });

  return {
    current,
    sort,
    rows,
    clip,
    message,
    loading,
    error,
    busy,
    fileInput,
    flags,
    load,
    onSortChange,
    enter,
    pickFile,
    onPickFile,
    download,
    confirmRemove,
    compress,
    decompress,
    copy,
    cancelCopy,
    paste,
    move,
    clearClipboard,
  };
}
