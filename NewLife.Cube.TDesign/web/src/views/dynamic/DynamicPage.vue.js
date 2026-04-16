import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick, markRaw, h } from 'vue';
import { useRoute } from 'vue-router';
import { SearchIcon, ChevronDownIcon } from 'tdesign-icons-vue-next';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput.vue';
import { Auth } from '@cube/api-core';
import { toCamelCase } from '@cube/field-mapping';
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
const showDetail = ref(false);
const showDeleteConfirm = ref(false);
const deleteTargetId = ref(null);
const formData = ref({});
const detailData = ref({});
const isEdit = ref(false);
// ECharts
const chartList = ref([]);
const chartInstances = ref([]);
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
        cols.push({ colKey: toCamelCase(f.name), title: f.displayName || f.name, ellipsis: true });
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
        chartList.value = Array.isArray(res.data) && res.data.length > 0 ? res.data : [];
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
function handleAdd() { formData.value = {}; isEdit.value = false; showForm.value = true; }
async function handleEditById(id) {
    try {
        const res = await api.page.getDetail(typePath.value, id);
        formData.value = res?.data ? { ...res.data } : {};
        isEdit.value = true;
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
if (__VLS_ctx.chartList.length) {
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
const __VLS_0 = {}.TSpace;
/** @type {[typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ style: {} },
    breakLine: true,
}));
const __VLS_2 = __VLS_1({
    ...{ style: {} },
    breakLine: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
if (__VLS_ctx.canAdd) {
    const __VLS_4 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        ...{ 'onClick': {} },
        theme: "primary",
    }));
    const __VLS_6 = __VLS_5({
        ...{ 'onClick': {} },
        theme: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
    let __VLS_8;
    let __VLS_9;
    let __VLS_10;
    const __VLS_11 = {
        onClick: (__VLS_ctx.handleAdd)
    };
    __VLS_7.slots.default;
    var __VLS_7;
}
if (__VLS_ctx.canDelete) {
    const __VLS_12 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
        ...{ 'onClick': {} },
        theme: "danger",
        disabled: (__VLS_ctx.selectedIds.length === 0),
    }));
    const __VLS_14 = __VLS_13({
        ...{ 'onClick': {} },
        theme: "danger",
        disabled: (__VLS_ctx.selectedIds.length === 0),
    }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    let __VLS_16;
    let __VLS_17;
    let __VLS_18;
    const __VLS_19 = {
        onClick: (__VLS_ctx.handleBatchDelete)
    };
    __VLS_15.slots.default;
    (__VLS_ctx.selectedIds.length);
    var __VLS_15;
}
if (__VLS_ctx.canExport) {
    const __VLS_20 = {}.TDropdown;
    /** @type {[typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, ]} */ ;
    // @ts-ignore
    const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
        ...{ 'onClick': {} },
        options: (__VLS_ctx.exportOptions),
    }));
    const __VLS_22 = __VLS_21({
        ...{ 'onClick': {} },
        options: (__VLS_ctx.exportOptions),
    }, ...__VLS_functionalComponentArgsRest(__VLS_21));
    let __VLS_24;
    let __VLS_25;
    let __VLS_26;
    const __VLS_27 = {
        onClick: (__VLS_ctx.handleExport)
    };
    __VLS_23.slots.default;
    const __VLS_28 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
        variant: "outline",
    }));
    const __VLS_30 = __VLS_29({
        variant: "outline",
    }, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    const __VLS_32 = {}.ChevronDownIcon;
    /** @type {[typeof __VLS_components.ChevronDownIcon, typeof __VLS_components.chevronDownIcon, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({}));
    const __VLS_34 = __VLS_33({}, ...__VLS_functionalComponentArgsRest(__VLS_33));
    var __VLS_31;
    var __VLS_23;
}
if (__VLS_ctx.canImport) {
    const __VLS_36 = {}.TUpload;
    /** @type {[typeof __VLS_components.TUpload, typeof __VLS_components.tUpload, typeof __VLS_components.TUpload, typeof __VLS_components.tUpload, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
        ...{ 'onSuccess': {} },
        action: (`${__VLS_ctx.typePath}/ImportFile`),
        autoUpload: (true),
        theme: "custom",
        accept: ".csv,.xls,.xlsx",
    }));
    const __VLS_38 = __VLS_37({
        ...{ 'onSuccess': {} },
        action: (`${__VLS_ctx.typePath}/ImportFile`),
        autoUpload: (true),
        theme: "custom",
        accept: ".csv,.xls,.xlsx",
    }, ...__VLS_functionalComponentArgsRest(__VLS_37));
    let __VLS_40;
    let __VLS_41;
    let __VLS_42;
    const __VLS_43 = {
        onSuccess: (__VLS_ctx.loadData)
    };
    __VLS_39.slots.default;
    const __VLS_44 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
        variant: "outline",
    }));
    const __VLS_46 = __VLS_45({
        variant: "outline",
    }, ...__VLS_functionalComponentArgsRest(__VLS_45));
    __VLS_47.slots.default;
    var __VLS_47;
    var __VLS_39;
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_48 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    ...{ 'onEnter': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索...",
    ...{ style: {} },
    clearable: true,
}));
const __VLS_50 = __VLS_49({
    ...{ 'onEnter': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索...",
    ...{ style: {} },
    clearable: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
let __VLS_52;
let __VLS_53;
let __VLS_54;
const __VLS_55 = {
    onEnter: (__VLS_ctx.handleSearch)
};
__VLS_51.slots.default;
{
    const { 'suffix-icon': __VLS_thisSlot } = __VLS_51.slots;
    const __VLS_56 = {}.SearchIcon;
    /** @type {[typeof __VLS_components.SearchIcon, typeof __VLS_components.searchIcon, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        ...{ 'onClick': {} },
        ...{ style: {} },
    }));
    const __VLS_58 = __VLS_57({
        ...{ 'onClick': {} },
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    let __VLS_60;
    let __VLS_61;
    let __VLS_62;
    const __VLS_63 = {
        onClick: (__VLS_ctx.handleSearch)
    };
    var __VLS_59;
}
var __VLS_51;
var __VLS_3;
const __VLS_64 = {}.TTable;
/** @type {[typeof __VLS_components.TTable, typeof __VLS_components.tTable, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
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
const __VLS_66 = __VLS_65({
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
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
let __VLS_68;
let __VLS_69;
let __VLS_70;
const __VLS_71 = {
    onSelectChange: ((keys) => __VLS_ctx.selectedIds = keys)
};
const __VLS_72 = {
    onPageChange: (__VLS_ctx.onPageChange)
};
var __VLS_67;
const __VLS_73 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_74 = __VLS_asFunctionalComponent(__VLS_73, new __VLS_73({
    visible: (__VLS_ctx.showForm),
    header: (__VLS_ctx.isEdit ? '编辑' : '新增'),
    footer: (false),
    width: "600px",
}));
const __VLS_75 = __VLS_74({
    visible: (__VLS_ctx.showForm),
    header: (__VLS_ctx.isEdit ? '编辑' : '新增'),
    footer: (false),
    width: "600px",
}, ...__VLS_functionalComponentArgsRest(__VLS_74));
__VLS_76.slots.default;
const __VLS_77 = {}.TForm;
/** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
// @ts-ignore
const __VLS_78 = __VLS_asFunctionalComponent(__VLS_77, new __VLS_77({
    labelAlign: "right",
    labelWidth: (100),
}));
const __VLS_79 = __VLS_78({
    labelAlign: "right",
    labelWidth: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_78));
__VLS_80.slots.default;
for (const [f] of __VLS_getVForSourceType((__VLS_ctx.editFields))) {
    const __VLS_81 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_82 = __VLS_asFunctionalComponent(__VLS_81, new __VLS_81({
        key: (f.name),
        label: (f.displayName || f.name),
    }));
    const __VLS_83 = __VLS_82({
        key: (f.name),
        label: (f.displayName || f.name),
    }, ...__VLS_functionalComponentArgsRest(__VLS_82));
    __VLS_84.slots.default;
    /** @type {[typeof FieldInput, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(FieldInput, new FieldInput({
        field: (f),
        modelValue: (__VLS_ctx.formData[__VLS_ctx.toCamelCase(f.name)]),
    }));
    const __VLS_86 = __VLS_85({
        field: (f),
        modelValue: (__VLS_ctx.formData[__VLS_ctx.toCamelCase(f.name)]),
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    var __VLS_84;
}
var __VLS_80;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_88 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    ...{ 'onClick': {} },
    variant: "outline",
}));
const __VLS_90 = __VLS_89({
    ...{ 'onClick': {} },
    variant: "outline",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
let __VLS_92;
let __VLS_93;
let __VLS_94;
const __VLS_95 = {
    onClick: (...[$event]) => {
        __VLS_ctx.showForm = false;
    }
};
__VLS_91.slots.default;
var __VLS_91;
const __VLS_96 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    ...{ 'onClick': {} },
    theme: "primary",
}));
const __VLS_98 = __VLS_97({
    ...{ 'onClick': {} },
    theme: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
let __VLS_100;
let __VLS_101;
let __VLS_102;
const __VLS_103 = {
    onClick: (__VLS_ctx.handleSave)
};
__VLS_99.slots.default;
var __VLS_99;
var __VLS_76;
const __VLS_104 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    visible: (__VLS_ctx.showDetail),
    header: "详情",
    footer: (false),
    width: "600px",
}));
const __VLS_106 = __VLS_105({
    visible: (__VLS_ctx.showDetail),
    header: "详情",
    footer: (false),
    width: "600px",
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
__VLS_107.slots.default;
const __VLS_108 = {}.TDescriptions;
/** @type {[typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    column: (1),
    bordered: true,
}));
const __VLS_110 = __VLS_109({
    column: (1),
    bordered: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
__VLS_111.slots.default;
for (const [f] of __VLS_getVForSourceType((__VLS_ctx.detailFields))) {
    const __VLS_112 = {}.TDescriptionsItem;
    /** @type {[typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        key: (f.name),
        label: (f.displayName || f.name),
    }));
    const __VLS_114 = __VLS_113({
        key: (f.name),
        label: (f.displayName || f.name),
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    __VLS_115.slots.default;
    (__VLS_ctx.detailData[__VLS_ctx.toCamelCase(f.name)] ?? '-');
    var __VLS_115;
}
var __VLS_111;
var __VLS_107;
const __VLS_116 = {}.TDialog;
/** @type {[typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, typeof __VLS_components.TDialog, typeof __VLS_components.tDialog, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    ...{ 'onConfirm': {} },
    visible: (__VLS_ctx.showDeleteConfirm),
    header: "确认删除",
    confirmBtn: "删除",
    cancelBtn: ('取消'),
}));
const __VLS_118 = __VLS_117({
    ...{ 'onConfirm': {} },
    visible: (__VLS_ctx.showDeleteConfirm),
    header: "确认删除",
    confirmBtn: "删除",
    cancelBtn: ('取消'),
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
let __VLS_120;
let __VLS_121;
let __VLS_122;
const __VLS_123 = {
    onConfirm: (__VLS_ctx.handleDeleteConfirm)
};
__VLS_119.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
var __VLS_119;
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
            showDetail: showDetail,
            showDeleteConfirm: showDeleteConfirm,
            formData: formData,
            detailData: detailData,
            isEdit: isEdit,
            chartList: chartList,
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
