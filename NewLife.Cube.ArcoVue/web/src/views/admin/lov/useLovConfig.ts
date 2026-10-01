/**
 * 值集配置抽屉业务 TS（OSC-2610019c9d）。
 *
 * GetConfig / SaveConfig；LIST 配置对齐 Cube.Vue lov/config.vue 字段与编辑弹层。
 */
import { computed, reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import {
  buildEnumSaveBody,
  buildListSaveBody,
  canSaveConfig,
  emptyListConfig,
  isSameAppListUrl,
  type LovEnumItemDraft,
  type LovListConfigDraft,
  type LovSearchFieldDraft,
  type LovTableColumnDraft,
} from '@/core/utils/lovAdmin';

export interface LovConfigProps {
  visible: boolean;
  lovId: string;
  canEdit: boolean;
}

function unwrapBody(res: unknown): Record<string, unknown> {
  if (!res || typeof res !== 'object') return {};
  const r = res as { data?: unknown };
  const body =
    'data' in r && r.data && typeof r.data === 'object' && !Array.isArray(r.data)
      ? (r.data as Record<string, unknown>)
      : (res as Record<string, unknown>);
  if ('lovCode' in body || 'enumItems' in body || 'listConfig' in body || 'type' in body) {
    return body;
  }
  if (body.data && typeof body.data === 'object' && !Array.isArray(body.data)) {
    return body.data as Record<string, unknown>;
  }
  return body;
}

function emptySearchField(sort = 0): LovSearchFieldDraft {
  return {
    field: '',
    title: '',
    componentType: 'input',
    paramType: 'BODY',
    required: false,
    defaultValue: '',
    sort,
    refLovCode: '',
  };
}

function emptyTableColumn(sort = 0): LovTableColumnDraft {
  return {
    field: '',
    title: '',
    width: 120,
    align: 'left',
    sortable: false,
    refLovCode: '',
    formatType: '',
    sort,
  };
}

/** 值集配置抽屉 composable */
export function useLovConfig(props: LovConfigProps, emit: (e: 'update:visible', v: boolean) => void) {
  const loading = ref(false);
  const saving = ref(false);
  const error = ref('');
  const type = ref('');
  const lovCode = ref('');
  const name = ref('');
  const source = ref('MANUAL');
  const activeTab = ref('listConfig');
  const enumItems = ref<LovEnumItemDraft[]>([]);
  const listConfig = ref<LovListConfigDraft>(emptyListConfig());
  const searchFields = ref<LovSearchFieldDraft[]>([]);
  const tableColumns = ref<LovTableColumnDraft[]>([]);

  const sameAppUrl = computed(() => isSameAppListUrl(listConfig.value.requestUrl));
  const saveEnabled = computed(
    () => props.canEdit && canSaveConfig(type.value) && !loading.value && !error.value,
  );

  const sfVisible = ref(false);
  const sfIndex = ref(-1);
  const sfForm = reactive<LovSearchFieldDraft>(emptySearchField());

  const tcVisible = ref(false);
  const tcIndex = ref(-1);
  const tcForm = reactive<LovTableColumnDraft>(emptyTableColumn());

  function reset() {
    error.value = '';
    type.value = '';
    lovCode.value = '';
    name.value = '';
    source.value = 'MANUAL';
    activeTab.value = 'listConfig';
    enumItems.value = [];
    listConfig.value = emptyListConfig();
    searchFields.value = [];
    tableColumns.value = [];
    sfVisible.value = false;
    tcVisible.value = false;
  }

  async function load() {
    if (!props.lovId) return;
    loading.value = true;
    error.value = '';
    try {
      const res = await cubeApi.client.request({
        url: '/Admin/Lov/GetConfig',
        method: 'get',
        params: { id: props.lovId },
      });
      const cfg = unwrapBody(res);
      type.value = String(cfg.type ?? '');
      lovCode.value = String(cfg.lovCode ?? props.lovId);
      name.value = String(cfg.name ?? '');
      source.value = String(cfg.source ?? 'MANUAL');

      if (type.value === 'ENUM') {
        activeTab.value = 'enumItems';
        const items = Array.isArray(cfg.enumItems) ? cfg.enumItems : [];
        enumItems.value = items.map((it, i) => {
          const row = it as Record<string, unknown>;
          return {
            value: String(row.value ?? ''),
            label: String(row.label ?? ''),
            sort: Number(row.sort ?? i) || 0,
            enabled: row.enabled !== false,
          };
        });
      } else if (type.value === 'LIST') {
        const lc = (cfg.listConfig ?? {}) as Record<string, unknown>;
        const base = emptyListConfig();
        listConfig.value = {
          requestUrl: String(lc.requestUrl ?? base.requestUrl),
          method: String(lc.method ?? base.method) || 'GET',
          pageable: Boolean(lc.pageable ?? base.pageable),
          pageNumField: String(lc.pageNumField ?? base.pageNumField),
          pageSizeField: String(lc.pageSizeField ?? base.pageSizeField),
          dataPath: String(lc.dataPath ?? base.dataPath),
          totalPath: String(lc.totalPath ?? base.totalPath),
          fixedParams: String(lc.fixedParams ?? base.fixedParams),
          proxyRequest: Boolean(lc.proxyRequest ?? base.proxyRequest),
        };
        if (isSameAppListUrl(listConfig.value.requestUrl)) {
          listConfig.value.proxyRequest = false;
        }
        const sfs = Array.isArray(cfg.searchFields) ? cfg.searchFields : [];
        searchFields.value = sfs.map((it, i) => {
          const row = it as Record<string, unknown>;
          return {
            field: String(row.field ?? ''),
            title: String(row.title ?? ''),
            componentType: String(row.componentType ?? 'input'),
            paramType: String(row.paramType ?? 'BODY'),
            required: Boolean(row.required),
            defaultValue: row.defaultValue != null ? String(row.defaultValue) : '',
            sort: Number(row.sort ?? i) || 0,
            refLovCode: row.refLovCode != null ? String(row.refLovCode) : '',
          };
        });
        const cols = Array.isArray(cfg.tableColumns) ? cfg.tableColumns : [];
        tableColumns.value = cols.map((it, i) => {
          const row = it as Record<string, unknown>;
          return {
            field: String(row.field ?? ''),
            title: String(row.title ?? ''),
            width: Number(row.width ?? 120) || 120,
            align: String(row.align ?? 'left'),
            sortable: Boolean(row.sortable),
            refLovCode: row.refLovCode != null ? String(row.refLovCode) : '',
            formatType: row.formatType != null ? String(row.formatType) : '',
            sort: Number(row.sort ?? i) || 0,
          };
        });
        activeTab.value = searchFields.value.length > 0 ? 'searchFields' : 'listConfig';
      }
    } catch (err) {
      reset();
      error.value = formatApiError(err, '加载配置失败');
    } finally {
      loading.value = false;
    }
  }

  watch(
    () => listConfig.value.requestUrl,
    (url) => {
      if (isSameAppListUrl(url)) listConfig.value.proxyRequest = false;
    },
  );

  function addEnumRow() {
    enumItems.value.push({ value: '', label: '', sort: enumItems.value.length, enabled: true });
  }

  function removeEnumRow(index: number) {
    enumItems.value.splice(index, 1);
  }

  function openSearchField(index: number) {
    sfIndex.value = index;
    Object.assign(sfForm, searchFields.value[index]);
    sfVisible.value = true;
  }

  function addSearchField() {
    sfIndex.value = -1;
    Object.assign(sfForm, emptySearchField(searchFields.value.length));
    sfVisible.value = true;
  }

  function confirmSearchField() {
    if (!sfForm.field.trim()) {
      Message.warning('字段名不能为空');
      return;
    }
    const data: LovSearchFieldDraft = { ...sfForm };
    if (sfIndex.value >= 0) searchFields.value[sfIndex.value] = data;
    else searchFields.value.push(data);
    sfVisible.value = false;
  }

  function removeSearchField(index: number) {
    searchFields.value.splice(index, 1);
  }

  function openTableColumn(index: number) {
    tcIndex.value = index;
    Object.assign(tcForm, tableColumns.value[index]);
    tcVisible.value = true;
  }

  function addTableColumn() {
    tcIndex.value = -1;
    Object.assign(tcForm, emptyTableColumn(tableColumns.value.length));
    tcVisible.value = true;
  }

  function confirmTableColumn() {
    if (!tcForm.field.trim()) {
      Message.warning('字段名不能为空');
      return;
    }
    const data: LovTableColumnDraft = { ...tcForm };
    if (tcIndex.value >= 0) tableColumns.value[tcIndex.value] = data;
    else tableColumns.value.push(data);
    tcVisible.value = false;
  }

  function removeTableColumn(index: number) {
    tableColumns.value.splice(index, 1);
  }

  async function save() {
    if (!saveEnabled.value) return;
    saving.value = true;
    try {
      const id = props.lovId;
      const body =
        type.value === 'ENUM'
          ? buildEnumSaveBody(id, enumItems.value)
          : buildListSaveBody(id, {
              listConfig: listConfig.value,
              searchFields: searchFields.value,
              tableColumns: tableColumns.value,
            });
      const res = await cubeApi.client.request({
        url: '/Admin/Lov/SaveConfig',
        method: 'post',
        data: body,
      });
      const payload = (res as { data?: { code?: number; message?: string } })?.data ?? res;
      const code = (payload as { code?: number })?.code;
      if (code != null && code !== 0) {
        Message.error((payload as { message?: string })?.message || '保存失败');
        return;
      }
      Message.success('保存成功');
      emit('update:visible', false);
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
    } finally {
      saving.value = false;
    }
  }

  function close() {
    emit('update:visible', false);
  }

  watch(
    () => [props.visible, props.lovId] as const,
    ([vis]) => {
      if (vis && props.lovId) {
        loading.value = true;
        void load();
      }
      if (!vis) reset();
    },
  );

  return {
    loading,
    saving,
    error,
    type,
    lovCode,
    name,
    source,
    activeTab,
    enumItems,
    listConfig,
    searchFields,
    tableColumns,
    sameAppUrl,
    saveEnabled,
    sfVisible,
    sfForm,
    tcVisible,
    tcForm,
    addEnumRow,
    removeEnumRow,
    openSearchField,
    addSearchField,
    confirmSearchField,
    removeSearchField,
    openTableColumn,
    addTableColumn,
    confirmTableColumn,
    removeTableColumn,
    save,
    close,
  };
}
