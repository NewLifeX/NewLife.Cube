import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick, markRaw, h } from 'vue';
import { useRoute } from 'vue-router';
import { SearchIcon, ChevronDownIcon } from 'tdesign-icons-vue-next';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput.vue';
import { Auth } from '@newlifex/cube';
import { toCamelCase, resolveColumnWidth, resolveColumnAlign } from '@newlifex/cube';
import { normalizeChartList, shouldDisposeCharts, toggleListView, validateRequiredFields } from '@newlifex/cube';
import { Button as TButton } from 'tdesign-vue-next';
import * as echarts from 'echarts';
const route = useRoute();
const userStore = useUserStore();
const typePath = computed(() => '/' + (route.params.type || ''));
// --- 权限控制 ---
const menuPerms = computed(() => userStore.getMenuPermission(typePath.value));
const canAdd = computed(() => String(Auth.ADD) in menuPerms.value);
const canEdit = computed(() => String(Auth.EDIT) in menuPerms.value);
const canDelete = computed(() => String(Auth.DELETE) in menuPerms.value);
const canExport = computed(() => String(Auth.EXPORT) in menuPerms.value);
const canImport = computed(() => String(Auth.IMPORT) in menuPerms.value);
const exportOptions = [
    { content: '导出 Excel', value: 'Excel' },
    { content: '导出 CSV', value: 'Csv' },
    { content: '导出 JSON', value: 'Json' },
    { content: '导出 XML', value: 'Xml' },
    { content: '导出模板', value: 'ExcelTemplate' },
];
// Fields
const listFields = ref([]);
const editFields = ref([]);
const detailFields = ref([]);
// Data
const data = ref([]);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);
const keyword = ref('');
const loading = ref(false);
const selectedIds = ref([]);
const statData = ref(null);
// Dialog
const showForm = ref(false);
const formError = ref('');
const showDetail = ref(false);
const showDeleteConfirm = ref(false);
const deleteTargetId = ref(null);
const formData = ref({});
const detailData = ref({});
const isEdit = ref(false);
// ECharts
const chartList = ref([]);
const chartInstances = ref([]);
/** 视图模式：表格 / 图表 */
const viewMode = ref('table');
function toggleView() {
    viewMode.value = toggleListView(viewMode.value);
}
// 离开图表视图时释放实例，避免隐藏容器尺寸为 0 导致图表错位
watch(viewMode, (mode) => {
    if (!shouldDisposeCharts(mode))
        return;
    for (const inst of chartInstances.value) {
        inst?.dispose();
    }
    chartInstances.value = [];
});
// 统计行（footData）
const footData = computed(() => {
    if (!statData.value)
        return [];
    const row = {};
    for (const f of listFields.value) {
        const key = toCamelCase(f.name);
        row[key] = statData.value[f.name] ?? statData.value[key] ?? '';
    }
    // 第一列显示"合计"
    const firstField = listFields.value[0];
    if (firstField)
        row[toCamelCase(firstField.name)] = '合计';
    return [row];
});
// Table columns
const tableColumns = computed(() => {
    const cols = [{ colKey: 'row-select', type: 'multiple', width: 50 }];
    for (const f of listFields.value) {
        cols.push({
            colKey: toCamelCase(f.name),
            title: f.displayName || f.name,
            ellipsis: true,
            width: resolveColumnWidth(f),
            align: resolveColumnAlign(f),
        });
    }
    cols.push({
        colKey: 'op', title: '操作', width: 180, fixed: 'right',
        cell: (_h, { row }) => {
            const btns = [
                h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleDetail(row.id) }, () => '查看'),
            ];
            if (canEdit.value)
                btns.push(h(TButton, { theme: 'primary', variant: 'text', size: 'small', onClick: () => handleEditById(row.id) }, () => '编辑'));
            if (canDelete.value)
                btns.push(h(TButton, { theme: 'danger', variant: 'text', size: 'small', onClick: () => confirmDelete(row.id) }, () => '删除'));
            return h('div', { style: 'display: flex; gap: 8px' }, btns);
        },
    });
    return cols;
});
// Pagination
const pagination = computed(() => ({
    current: pageIndex.value,
    pageSize: pageSize.value,
    total: total.value,
    showJumper: true,
    showSizer: true,
}));
function onPageChange(info) {
    pageIndex.value = info.current;
    pageSize.value = info.pageSize;
    loadData();
}
async function loadFields() {
    try {
        const pageRes = await api.page.getPage(typePath.value);
        const pageMeta = pageRes?.data ?? {};
        listFields.value = pageMeta.list ?? pageMeta.fields?.list ?? [];
        editFields.value = pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [];
        detailFields.value = pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [];
    }
    catch { /* ignore */ }
}
async function loadData() {
    loading.value = true;
    try {
        const params = { pageIndex: pageIndex.value, pageSize: pageSize.value };
        if (keyword.value)
            params.key = keyword.value;
        const res = await api.page.getList(typePath.value, params);
        data.value = res?.data?.data ?? res?.data ?? [];
        total.value = res?.data?.totalCount ?? res?.page?.totalCount ?? 0;
        statData.value = res?.stat ?? null;
    }
    catch { /* ignore */ }
    loading.value = false;
}
// ECharts
async function loadChartData() {
    try {
        const res = await api.page.getChartData(typePath.value);
        chartList.value = normalizeChartList(res.data);
    }
    catch {
        chartList.value = [];
    }
}
function setChartRef(el, idx) {
    if (!el)
        return;
    nextTick(() => {
        if (chartInstances.value[idx])
            chartInstances.value[idx].dispose();
        const instance = markRaw(echarts.init(el));
        if (chartList.value[idx])
            instance.setOption(chartList.value[idx]);
        chartInstances.value[idx] = instance;
    });
}
function onChartResize() {
    for (const inst of chartInstances.value) {
        inst?.resize();
    }
}
function handleSearch() { pageIndex.value = 1; loadData(); }
// CRUD
function handleAdd() { formData.value = {}; isEdit.value = false; formError.value = ''; showForm.value = true; }
async function handleEditById(id) {
    try {
        const res = await api.page.getDetail(typePath.value, id);
        formData.value = res?.data ? { ...res.data } : {};
        isEdit.value = true;
        formError.value = '';
        showForm.value = true;
    }
    catch { /* ignore */ }
}
async function handleDetail(id) {
    try {
        const res = await api.page.getDetail(typePath.value, id);
        detailData.value = res?.data ?? {};
        showDetail.value = true;
    }
    catch { /* ignore */ }
}
async function handleSave() {
    // 表单键为字段名的小驼峰形式，校验时按同一口径取值
    const invalid = validateRequiredFields(editFields.value, formData.value, toCamelCase);
    if (invalid.length) {
        formError.value = invalid.map((e) => e.message).join('；');
        return;
    }
    formError.value = '';
    try {
        if (isEdit.value)
            await api.page.update(typePath.value, formData.value);
        else
            await api.page.add(typePath.value, formData.value);
        showForm.value = false;
        await loadData();
    }
    catch { /* ignore */ }
}
function confirmDelete(id) { deleteTargetId.value = id; showDeleteConfirm.value = true; }
async function handleDeleteConfirm() {
    if (deleteTargetId.value == null)
        return;
    try {
        await api.page.remove(typePath.value, deleteTargetId.value);
        showDeleteConfirm.value = false;
        deleteTargetId.value = null;
        await loadData();
    }
    catch { /* ignore */ }
}
async function handleBatchDelete() {
    if (selectedIds.value.length === 0)
        return;
    try {
        await api.page.deleteSelect(typePath.value, selectedIds.value);
        selectedIds.value = [];
        await loadData();
    }
    catch { /* ignore */ }
}
function handleExport(item) {
    const format = item?.value || 'Excel';
    window.open(api.page.getExportUrl(typePath.value, format), '_blank');
}
// Watch route
watch(typePath, () => {
    pageIndex.value = 1;
    keyword.value = '';
    selectedIds.value = [];
    loadFields();
    loadData();
    loadChartData();
}, { immediate: true });
onMounted(() => {
    loadChartData();
    window.addEventListener('resize', onChartResize);
});
onBeforeUnmount(() => {
    window.removeEventListener('resize', onChartResize);
    for (const inst of chartInstances.value) {
        inst?.dispose();
    }
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
if (__VLS_ctx.viewMode === 'chart' && __VLS_ctx.chartList.length) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    for (const [_, idx] of __VLS_getVForSourceType((__VLS_ctx.chartList))) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
            key: (idx),
            ref: ((el) => __VLS_ctx.setChartRef(el, idx)),
            ...{ style: {} },
        });
    }
}
else if (__VLS_ctx.viewMode === 'chart') {
    const __VLS_0 = {}.TEmpty;
    /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
        description: "暂无图表",
        ...{ style: {} },
    }));
    const __VLS_2 = __VLS_1({
        description: "暂无图表",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
}
const __VLS_4 = {}.TSpace;
/** @type {[typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ style: {} },
    breakLine: true,
}));
const __VLS_6 = __VLS_5({
    ...{ style: {} },
    breakLine: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
if (__VLS_ctx.canAdd) {
    const __VLS_8 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        ...{ 'onClick': {} },
        theme: "primary",
    }));
    const __VLS_10 = __VLS_9({
        ...{ 'onClick': {} },
        theme: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
    let __VLS_12;
    let __VLS_13;
    let __VLS_14;
    const __VLS_15 = {
        onClick: (__VLS_ctx.handleAdd)
    };
    __VLS_11.slots.default;
    var __VLS_11;
}
if (__VLS_ctx.canDelete) {
    const __VLS_16 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        ...{ 'onClick': {} },
        theme: "danger",
        disabled: (__VLS_ctx.selectedIds.length === 0),
    }));
    const __VLS_18 = __VLS_17({
        ...{ 'onClick': {} },
        theme: "danger",
        disabled: (__VLS_ctx.selectedIds.length === 0),
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    let __VLS_20;
    let __VLS_21;
    let __VLS_22;
    const __VLS_23 = {
        onClick: (__VLS_ctx.handleBatchDelete)
    };
    __VLS_19.slots.default;
    (__VLS_ctx.selectedIds.length);
    var __VLS_19;
}
if (__VLS_ctx.canExport) {
    const __VLS_24 = {}.TDropdown;
    /** @type {[typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        ...{ 'onClick': {} },
        options: (__VLS_ctx.exportOptions),
    }));
    const __VLS_26 = __VLS_25({
        ...{ 'onClick': {} },
        options: (__VLS_ctx.exportOptions),
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
    let __VLS_28;
    let __VLS_29;
    let __VLS_30;
    const __VLS_31 = {
        onClick: (__VLS_ctx.handleExport)
    };
    __VLS_27.slots.default;
    const __VLS_32 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
        variant: "outline",
    }));
    const __VLS_34 = __VLS_33({
        variant: "outline",
    }, ...__VLS_functionalComponentArgsRest(__VLS_33));
    __VLS_35.slots.default;
    const __VLS_36 = {}.ChevronDownIcon;
    /** @type {[typeof __VLS_components.ChevronDownIcon, typeof __VLS_components.chevronDownIcon, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({}));
    const __VLS_38 = __VLS_37({}, ...__VLS_functionalComponentArgsRest(__VLS_37));
    var __VLS_35;
    var __VLS_27;
}
if (__VLS_ctx.canImport) {
    const __VLS_40 = {}.TUpload;
    /** @type {[typeof __VLS_components.TUpload, typeof __VLS_components.tUpload, typeof __VLS_components.TUpload, typeof __VLS_components.tUpload, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        ...{ 'onSuccess': {} },
        action: (`${__VLS_ctx.typePath}/import`),
        autoUpload: (true),
        theme: "custom",
        accept: ".csv,.xls,.xlsx",
    }));
    const __VLS_42 = __VLS_41({
        ...{ 'onSuccess': {} },
        action: (`${__VLS_ctx.typePath}/import`),
        autoUpload: (true),
        theme: "custom",
        accept: ".csv,.xls,.xlsx",
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    let __VLS_44;
    let __VLS_45;
    let __VLS_46;
    const __VLS_47 = {
        onSuccess: (__VLS_ctx.loadData)
    };
    __VLS_43.slots.default;
    const __VLS_48 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        variant: "outline",
    }));
    const __VLS_50 = __VLS_49({
        variant: "outline",
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    var __VLS_51;
    var __VLS_43;
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_52 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    ...{ 'onClick': {} },
    variant: "outline",
}));
const __VLS_54 = __VLS_53({
    ...{ 'onClick': {} },
    variant: "outline",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
let __VLS_56;
let __VLS_57;
let __VLS_58;
const __VLS_59 = {
    onClick: (__VLS_ctx.toggleView)
};
__VLS_55.slots.default;
(__VLS_ctx.viewMode === 'table' ? '图表' : '表格');
var __VLS_55;
const __VLS_60 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    ...{ 'onEnter': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索...",
    ...{ style: {} },
    clearable: true,
}));
const __VLS_62 = __VLS_61({
    ...{ 'onEnter': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索...",
    ...{ style: {} },
    clearable: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
let __VLS_64;
let __VLS_65;
let __VLS_66;
const __VLS_67 = {
    onEnter: (__VLS_ctx.handleSearch)
};
__VLS_63.slots.default;
{
    const { 'suffix-icon': __VLS_thisSlot } = __VLS_63.slots;
    const __VLS_68 = {}.SearchIcon;
    /** @type {[typeof __VLS_components.SearchIcon, typeof __VLS_components.searchIcon, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        ...{ 'onClick': {} },
        ...{ style: {} },
    }));
    const __VLS_70 = __VLS_69({
        ...{ 'onClick': {} },
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    let __VLS_72;
    let __VLS_73;
    let __VLS_74;
    const __VLS_75 = {
        onClick: (__VLS_ctx.handleSearch)
    };
    var __VLS_71;
}
var __VLS_63;
var __VLS_7;
const __VLS_76 = {}.TTable;
/** @type {[typeof __VLS_components.TTable, typeof __VLS_components.tTable, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    ...{ 'onSelectChange': {} },
    ...{ 'onPageChange': {} },
    data: (__VLS_ctx.data),
    columns: (__VLS_ctx.tableColumns),
    loading: (__VLS_ctx.loading),
    rowKey: "id",
    selectedRowKeys: (__VLS_ctx.selectedIds),
    pagination: (__VLS_ctx.pagination),
    footData: (__VLS_ctx.footData),
    bordered: true,
    stripe: true,
}));
const __VLS_78 = __VLS_77({
    ...{ 'onSelectChange': {} },
    ...{ 'onPageChange': {} },
    data: (__VLS_ctx.data),
    columns: (__VLS_ctx.tableColumns),
    loading: (__VLS_ctx.loading),
    rowKey: "id",
    selectedRowKeys: (__VLS_ctx.selectedIds),
    pagination: (__VLS_ctx.pagination),
    footData: (__VLS_ctx.footData),
    bordered: true,
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
let __VLS_80;
let __VLS_81;
let __VLS_82;
const __VLS_83 = {
    onSelectChange: ((keys) => __VLS_ctx.selectedIds = keys)
};
const __VLS_84 = {
    onPageChange: (__VLS_ctx.onPageChange)
};
__VLS_asFunctionalDirective(__VLS_directives.vShow)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.viewMode === 'table') }, null, null);
var __VLS_79;
const __VLS_85 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_86 = __VLS_asFunctionalComponent(__VLS_85, new __VLS_85({
    visible: (__VLS_ctx.showForm),
    header: (__VLS_ctx.isEdit ? '编辑' : '新增'),
    footer: (false),
    width: "600px",
}));
const __VLS_87 = __VLS_86({
    visible: (__VLS_ctx.showForm),
    header: (__VLS_ctx.isEdit ? '编辑' : '新增'),
    footer: (false),
    width: "600px",
}, ...__VLS_functionalComponentArgsRest(__VLS_86));
__VLS_88.slots.default;
const __VLS_89 = {}.TForm;
/** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
// @ts-ignore
const __VLS_90 = __VLS_asFunctionalComponent(__VLS_89, new __VLS_89({
    labelAlign: "right",
    labelWidth: (100),
}));
const __VLS_91 = __VLS_90({
    labelAlign: "right",
    labelWidth: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_90));
__VLS_92.slots.default;
for (const [f] of __VLS_getVForSourceType((__VLS_ctx.editFields))) {
    const __VLS_93 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_94 = __VLS_asFunctionalComponent(__VLS_93, new __VLS_93({
        key: (f.name),
        label: (f.displayName || f.name),
    }));
    const __VLS_95 = __VLS_94({
        key: (f.name),
        label: (f.displayName || f.name),
    }, ...__VLS_functionalComponentArgsRest(__VLS_94));
    __VLS_96.slots.default;
    /** @type {[typeof FieldInput, ]} */ ;
    // @ts-ignore
    const __VLS_97 = __VLS_asFunctionalComponent(FieldInput, new FieldInput({
        field: (f),
        modelValue: (__VLS_ctx.formData[__VLS_ctx.toCamelCase(f.name)]),
    }));
    const __VLS_98 = __VLS_97({
        field: (f),
        modelValue: (__VLS_ctx.formData[__VLS_ctx.toCamelCase(f.name)]),
    }, ...__VLS_functionalComponentArgsRest(__VLS_97));
    var __VLS_96;
}
var __VLS_92;
if (__VLS_ctx.formError) {
    const __VLS_100 = {}.TAlert;
    /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        theme: "error",
        message: (__VLS_ctx.formError),
        ...{ style: {} },
    }));
    const __VLS_102 = __VLS_101({
        theme: "error",
        message: (__VLS_ctx.formError),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_104 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    ...{ 'onClick': {} },
    variant: "outline",
}));
const __VLS_106 = __VLS_105({
    ...{ 'onClick': {} },
    variant: "outline",
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
let __VLS_108;
let __VLS_109;
let __VLS_110;
const __VLS_111 = {
    onClick: (...[$event]) => {
        __VLS_ctx.showForm = false;
    }
};
__VLS_107.slots.default;
var __VLS_107;
const __VLS_112 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    ...{ 'onClick': {} },
    theme: "primary",
}));
const __VLS_114 = __VLS_113({
    ...{ 'onClick': {} },
    theme: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
let __VLS_116;
let __VLS_117;
let __VLS_118;
const __VLS_119 = {
    onClick: (__VLS_ctx.handleSave)
};
__VLS_115.slots.default;
var __VLS_115;
var __VLS_88;
const __VLS_120 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    visible: (__VLS_ctx.showDetail),
    header: "详情",
    footer: (false),
    width: "600px",
}));
const __VLS_122 = __VLS_121({
    visible: (__VLS_ctx.showDetail),
    header: "详情",
    footer: (false),
    width: "600px",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
const __VLS_124 = {}.TDescriptions;
/** @type {[typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    column: (1),
    bordered: true,
}));
const __VLS_126 = __VLS_125({
    column: (1),
    bordered: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
for (const [f] of __VLS_getVForSourceType((__VLS_ctx.detailFields))) {
    const __VLS_128 = {}.TDescriptionsItem;
    /** @type {[typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
        key: (f.name),
        label: (f.displayName || f.name),
    }));
    const __VLS_130 = __VLS_129({
        key: (f.name),
        label: (f.displayName || f.name),
    }, ...__VLS_functionalComponentArgsRest(__VLS_129));
    __VLS_131.slots.default;
    (__VLS_ctx.detailData[__VLS_ctx.toCamelCase(f.name)] ?? '-');
    var __VLS_131;
}
var __VLS_127;
var __VLS_123;
const __VLS_132 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    ...{ 'onConfirm': {} },
    visible: (__VLS_ctx.showDeleteConfirm),
    header: "确认删除",
    confirmBtn: "删除",
    cancelBtn: ('取消'),
}));
const __VLS_134 = __VLS_133({
    ...{ 'onConfirm': {} },
    visible: (__VLS_ctx.showDeleteConfirm),
    header: "确认删除",
    confirmBtn: "删除",
    cancelBtn: ('取消'),
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
let __VLS_136;
let __VLS_137;
let __VLS_138;
const __VLS_139 = {
    onConfirm: (__VLS_ctx.handleDeleteConfirm)
};
__VLS_135.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
var __VLS_135;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            SearchIcon: SearchIcon,
            ChevronDownIcon: ChevronDownIcon,
            FieldInput: FieldInput,
            toCamelCase: toCamelCase,
            TButton: TButton,
            typePath: typePath,
            canAdd: canAdd,
            canDelete: canDelete,
            canExport: canExport,
            canImport: canImport,
            exportOptions: exportOptions,
            editFields: editFields,
            detailFields: detailFields,
            data: data,
            keyword: keyword,
            loading: loading,
            selectedIds: selectedIds,
            showForm: showForm,
            formError: formError,
            showDetail: showDetail,
            showDeleteConfirm: showDeleteConfirm,
            formData: formData,
            detailData: detailData,
            isEdit: isEdit,
            chartList: chartList,
            viewMode: viewMode,
            toggleView: toggleView,
            footData: footData,
            tableColumns: tableColumns,
            pagination: pagination,
            onPageChange: onPageChange,
            loadData: loadData,
            setChartRef: setChartRef,
            handleSearch: handleSearch,
            handleAdd: handleAdd,
            handleSave: handleSave,
            handleDeleteConfirm: handleDeleteConfirm,
            handleBatchDelete: handleBatchDelete,
            handleExport: handleExport,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
