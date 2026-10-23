/**
 * 测试夹具：构造一个不联网的 `CubeEngineContext`
 *
 * 内置子组件全部以 `ctx` 为 prop（受控），因此组件测试直接喂假上下文即可，
 * 不需要真实引擎、不需要后端、也不需要 MSW（规范：`docs/standards/testing-standard.md`）。
 */
import { computed, reactive, ref } from 'vue';
import { vi } from 'vitest';
import type { FieldMeta } from '../../../types/field';
import type { BackendToolbarItem, CubeEngineContext, CubeFormMode, PageSetting } from '../../../engine/types';

/** 常用测试字段 */
export const FIELDS: Record<string, FieldMeta> = {
  id: { name: 'Id', displayName: '编号', typeName: 'Int32', primaryKey: true },
  name: { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
  count: { name: 'Count', displayName: '数量', typeName: 'Int32' },
  enable: { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
  time: { name: 'Time', displayName: '时间', typeName: 'DateTime' },
  kind: { name: 'Kind', displayName: '类型', typeName: 'String', lovCode: 'EquipmentKinds' },
  avatar: { name: 'Avatar', displayName: '头像', typeName: 'String', itemType: 'image' },
  doc: { name: 'Doc', displayName: '附件', typeName: 'String', itemType: 'file' },
  tone: { name: 'Tone', displayName: '颜色', typeName: 'String', itemType: 'color' },
  glyph: { name: 'Glyph', displayName: '图标', typeName: 'String', itemType: 'icon' },
  payload: { name: 'Payload', displayName: '配置', typeName: 'String', itemType: 'json' },
  dict: { name: 'Dict', displayName: '字典', typeName: 'String', dataSource: { '1': '小时', '2': '班次' } },
  loose: { name: 'Loose', displayName: '备注', typeName: 'String', nullable: true },
};

export interface FakeCtxOptions {
  list?: Record<string, unknown>[];
  total?: number;
  page?: number;
  pageSize?: number;
  setting?: PageSetting;
  listFields?: FieldMeta[];
  searchFields?: FieldMeta[];
  formFields?: FieldMeta[];
  keyword?: string;
  selected?: Record<string, unknown>[];
  error?: string | null;
  loading?: boolean;
  formMode?: CubeFormMode;
  pendingBlob?: { blob: Blob; name: string } | null;
  toolbar?: BackendToolbarItem[];
}

/** 造一个全 `vi.fn()` 化的上下文（断言调用即可，不触发任何请求） */
export function makeFakeCtx(options: FakeCtxOptions = {}): CubeEngineContext {
  const list = ref(options.list ?? []);
  const total = ref(options.total ?? 0);
  const page = ref(options.page ?? 1);
  const pageSize = ref(options.pageSize ?? 20);
  const listFields = computed(() => options.listFields ?? [FIELDS.id, FIELDS.name]);
  const searchFields = computed(() => options.searchFields ?? []);
  const formFields = computed(() => options.formFields ?? [FIELDS.name]);

  return {
    schemaReady: ref(true),
    loading: ref(options.loading ?? false),
    submitting: ref(false),
    formLoading: ref(false),
    error: ref(options.error ?? null),
    list: computed(() => list.value),
    total: computed(() => total.value),
    page: computed(() => page.value),
    pageSize: computed(() => pageSize.value),
    keyword: ref(options.keyword ?? ''),
    hasKeyword: computed(() => true),
    selected: ref(options.selected ?? []),
    pendingBlob: ref(options.pendingBlob ?? null),
    searchModel: reactive<Record<string, unknown>>({}),
    formModel: reactive<Record<string, unknown>>({}),
    apiPrefix: computed(() => '/api/Equipments/Equipment'),
    pageSetting: ref(options.setting ?? {}),
    listFields,
    searchFields,
    toolbar: computed(() => options.toolbar ?? []),
    formFields,
    dialogVisible: ref(false),
    formMode: ref(options.formMode ?? 'add'),
    formId: ref(null),
    dialogTitle: computed(() => '新增'),
    init: () => Promise.resolve(),
    reload: () => Promise.resolve(),
    retry: () => Promise.resolve(),
    search: () => Promise.resolve(),
    searchByKeyword: () => Promise.resolve(),
    resetSearch: () => Promise.resolve(),
    changePage: () => Promise.resolve(),
    changePageSize: () => Promise.resolve(),
    refresh: () => Promise.resolve(),
    setSelected: () => undefined,
    openCreate: () => undefined,
    openEdit: () => Promise.resolve(),
    openView: () => Promise.resolve(),
    closeDialog: () => undefined,
    submitForm: () => Promise.resolve(true),
    remove: () => Promise.resolve(true),
    removeSelected: () => Promise.resolve(true),
    exportFile: () => Promise.resolve('file.xlsx'),
    downloadTemplate: () => Promise.resolve('tpl.xlsx'),
    importFile: () => Promise.resolve({ ok: true, message: '导入 1 行', rows: 1 }),
    setFilter: () => Promise.resolve(),
  };
}

/** 记录调用用的假动作集（满足 `CubeTableActions` 契约） */
export function makeFakeActions() {
  return {
    removeRow: vi.fn(async () => true),
    removeSelectedRows: vi.fn(async () => true),
    exportNow: vi.fn(async () => 'file.xlsx'),
    onImportFileChange: vi.fn(async () => undefined),
    openFilePicker: vi.fn(),
    bindBlobDownload: vi.fn(),
    needsConfirm: vi.fn(() => true),
  };
}
