/**
 * Admin/Lov 值集管理页业务 TS（OSC-2610019c9d）。
 *
 * 列表加载、关键字、定义增删改、权限与抽屉状态。
 */
import { computed, onMounted, reactive, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { formatApiError } from '@/core/utils/apiError';
import {
  ensureLovCodePrefix,
  filterLovRows,
  lovPermFlags,
  lovRowOf,
  validateLovCode,
  type LovRow,
} from '@/core/utils/lovAdmin';
import { recordDrawerWidth, RECORD_DRAWER_WIDE } from '@/views/crud/recordDrawerWidth';

export interface LovDefForm {
  id: string;
  lovCode: string;
  name: string;
  type: 'ENUM' | 'LIST';
  valueField: string;
  labelField: string;
  enabled: boolean;
  remark: string;
}

function emptyForm(): LovDefForm {
  return {
    id: '',
    lovCode: '',
    name: '',
    type: 'ENUM',
    valueField: 'id',
    labelField: 'name',
    enabled: true,
    remark: '',
  };
}

/** 值集 API 类型路径：必须带前导 /，否则 resolveRequestUrl 会拼成 /apiAdmin/Lov → 404 */
const LOV_TYPE = '/Admin/Lov';

/** Admin/Lov 页全部业务 TS（薄 SFC 宿主） */
export function useLovPage() {
  const userStore = useUserStore();

  const rows = ref<LovRow[]>([]);
  const keyword = ref('');
  const loading = ref(false);
  const error = ref('');
  const saving = ref(false);

  const flags = computed(() => lovPermFlags(userStore.getMenuPermission('Admin/Lov')));
  const filteredRows = computed(() => filterLovRows(rows.value, keyword.value));

  const defVisible = ref(false);
  const defMode = ref<'add' | 'edit'>('add');
  const form = reactive<LovDefForm>(emptyForm());
  const formError = ref('');
  const drawerWidth = recordDrawerWidth(0);
  /** LIST 三 Tab 配置需要更宽抽屉（对齐 Cube.Vue 宽对话框） */
  const configDrawerWidth = RECORD_DRAWER_WIDE;

  const configVisible = ref(false);
  const configId = ref('');
  const configTitle = ref('');

  async function load() {
    if (!flags.value.canView) {
      rows.value = [];
      return;
    }
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.page.getList<Record<string, unknown>>(LOV_TYPE, {
        pageIndex: 0,
        pageSize: 0,
        Q: keyword.value.trim() || undefined,
      });
      const data = (res?.data as unknown) ?? res;
      const list = Array.isArray(data) ? data : [];
      rows.value = list
        .map((r) => lovRowOf(r as Record<string, unknown>))
        .filter((r): r is LovRow => r !== null);
    } catch (err) {
      error.value = formatApiError(err, '值集列表加载失败');
      rows.value = [];
    } finally {
      loading.value = false;
    }
  }

  function openAdd() {
    defMode.value = 'add';
    Object.assign(form, emptyForm());
    formError.value = '';
    defVisible.value = true;
  }

  function openEdit(row: LovRow) {
    defMode.value = 'edit';
    Object.assign(form, {
      id: row.id,
      lovCode: row.lovCode,
      name: row.name,
      type: row.type === 'LIST' ? 'LIST' : 'ENUM',
      valueField: row.valueField || 'id',
      labelField: row.labelField || 'name',
      enabled: row.enabled,
      remark: row.remark,
    });
    formError.value = '';
    defVisible.value = true;
  }

  function openConfig(row: LovRow) {
    configId.value = row.lovCode || row.id;
    configTitle.value = `配置 · ${row.lovCode}`;
    configVisible.value = true;
  }

  async function saveDef() {
    formError.value = '';
    const codeErr = validateLovCode(form.lovCode);
    if (codeErr && defMode.value === 'add') {
      formError.value = codeErr;
      return;
    }
    if (!form.name.trim()) {
      formError.value = '名称不能为空';
      return;
    }
    if (form.name.trim().length > 40) {
      formError.value = '名称最长 40 字符';
      return;
    }

    const lovCode =
      defMode.value === 'add' ? ensureLovCodePrefix(form.lovCode, form.type) : form.lovCode;
    const payload: Record<string, unknown> = {
      lovCode,
      name: form.name.trim(),
      type: form.type,
      valueField: form.valueField || 'id',
      labelField: form.labelField || 'name',
      enabled: form.enabled,
      remark: form.remark || '',
    };
    if (defMode.value === 'edit') payload.id = form.id || form.lovCode;

    saving.value = true;
    try {
      if (defMode.value === 'add') await cubeApi.page.add(LOV_TYPE, payload);
      else await cubeApi.page.update(LOV_TYPE, payload);
      Message.success(defMode.value === 'add' ? '添加成功' : '更新成功');
      defVisible.value = false;
      await load();
    } catch (err) {
      formError.value = formatApiError(err, '保存失败');
    } finally {
      saving.value = false;
    }
  }

  function confirmDelete(row: LovRow) {
    Modal.confirm({
      title: '确认删除',
      content: `确认删除值集 ${row.lovCode}？`,
      okText: '删除',
      okButtonProps: { status: 'danger' },
      onOk: async () => {
        try {
          await cubeApi.page.remove(LOV_TYPE, row.id || row.lovCode);
          Message.success('删除成功');
          await load();
        } catch (err) {
          Message.error(formatApiError(err, '删除失败'));
        }
      },
    });
  }

  onMounted(() => {
    void load();
  });

  return {
    rows,
    keyword,
    filteredRows,
    loading,
    error,
    saving,
    flags,
    defVisible,
    defMode,
    form,
    formError,
    drawerWidth,
    configDrawerWidth,
    configVisible,
    configId,
    configTitle,
    load,
    openAdd,
    openEdit,
    openConfig,
    saveDef,
    confirmDelete,
  };
}
