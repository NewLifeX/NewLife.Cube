<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { FormInstance, FormItemRule, FormRules } from 'element-plus';
import type { FieldMapping } from '@newlifex/field-mapping';
import { groupFieldsByCategory, isBlankValue } from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import { useAuthStore } from '@/store/modules/auth';
import { buildTreeRows, formatCell, getRowValue, resolveImageSrc } from '../shared';
import IconPicker from './icon-picker.vue';
import RolePermissionPicker from './role-permission-picker.vue';
import AreaSelect from './area-select.vue';

defineOptions({ name: 'EntityFormDialog' });

interface Props {
  /** 弹窗是否可见 */
  modelValue: boolean;
  /** 新增、编辑或查看 */
  mode: 'add' | 'edit' | 'view';
  /** 表单字段（来自 /meta 的 addForm 或 editForm） */
  fields: FieldMapping[];
  /** 额外的选项来源字段（列表/搜索字段），用于映射下拉取数据字典 */
  optionFields?: FieldMapping[];
  /** 编辑时的记录数据 */
  record?: Record<string, unknown> | null;
  /** 提交中 */
  loading?: boolean;
  /** 实体显示名，用于标题 */
  entityName?: string;
  /** 主键字段名，编辑提交时需带回 */
  pkField?: string;
  /** 实体接口前缀，如 Admin/Role，用于识别专属控件 */
  apiType?: string;
  /** 树形实体的扁平列表数据，用于把父级下拉渲染成树 */
  treeData?: Record<string, unknown>[];
}

const props = withDefaults(defineProps<Props>(), {
  optionFields: () => [],
  record: null,
  loading: false,
  entityName: '',
  pkField: 'id',
  apiType: '',
  treeData: () => []
});

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  submit: [data: Record<string, unknown>];
}>();

const formRef = ref<FormInstance>();
const authStore = useAuthStore();

/** 角色表单权限列较多，弹窗加宽以便一屏展示 */
const dialogWidth = computed(() => (/(^|\/)Role$/i.test(props.apiType) ? '980px' : '760px'));
// 控件类型由字段元数据决定（文本/数字/布尔/数组），统一用宽松类型承载
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const model = ref<Record<string, any>>({});
/** 远端 LOV 选项缓存：lovCode → 选项 */
const lovOptions = ref<Record<string, { label: string; value: string }[]>>({});
/** 编辑初始值快照：字段名 → 序列化值，用于只提交真正变更的字段 */
const initialValues = ref<Record<string, string>>({});

const isView = computed(() => props.mode === 'view');

const title = computed(() => {
  const key =
    props.mode === 'add' ? 'page.form.addTitle' : props.mode === 'edit' ? 'page.form.editTitle' : 'page.form.viewTitle';
  return $t(key, { name: props.entityName || '' }).trim();
});

/** 表单内部页签：按后端字段分类分组，未分类字段归入基础信息 */
const formTabs = computed(() => groupFieldsByCategory(props.fields, item => item.field.category));
/** 当前页签下标（对应 formTabs 的索引） */
const activeTab = ref('0');

/** 后端分类 → 页签标题；未知分类直接显示分类名 */
const CATEGORY_LABELS: Record<string, 'page.form.basicInfo' | 'page.form.extensionInfo'> = {
  '': 'page.form.basicInfo',
  扩展: 'page.form.extensionInfo'
};

function tabLabel(category: string) {
  const key = CATEGORY_LABELS[category];
  return key ? $t(key) : category;
}

const wideWidgets = new Set(['textarea', 'html', 'markdown', 'json', 'code', 'file']);
const changeTriggerWidgets = new Set(['switch', 'select', 'lov', 'lovMulti', 'date', 'datetime', 'time', 'number', 'area']);

function fieldLabel(item: FieldMapping) {
  return item.field.displayName || item.field.header || item.field.name;
}

function isMultiple(item: FieldMapping) {
  return item.widget === 'lovMulti' || item.field.multiple === true;
}

function isWide(item: FieldMapping) {
  return wideWidgets.has(item.widget);
}

/** 角色权限字段：菜单权限清单专属控件，替代纯文本输入 */
function isPermissionField(item: FieldMapping) {
  return item.field.name === 'Permission' && /(^|\/)Role$/i.test(props.apiType);
}

/** 编辑中的角色编号；新增时为 0，权限清单借用当前用户角色拉取菜单 */
const roleId = computed(() => Number(props.record ? getRowValue(props.record, props.pkField) : 0) || 0);

function isStringField(item: FieldMapping) {
  return (item.field.typeName || '').toLowerCase() === 'string';
}

const rules = computed<FormRules>(() => {
  const result: FormRules = {};
  if (isView.value) return result;
  for (const item of props.fields) {
    if (!item.field.required || item.widget === 'readonly' || item.field.mapField) continue;
    const rule: FormItemRule = {
      required: true,
      message: $t('page.form.required', { label: fieldLabel(item) }),
      trigger: changeTriggerWidgets.has(item.widget) ? 'change' : 'blur'
    };
    if (isMultiple(item)) rule.type = 'array';
    result[item.field.name] = [rule];
  }
  return result;
});

function optionsOf(item: FieldMapping): { label: string; value: string }[] {
  if (item.field.dataSource) {
    return Object.entries(item.field.dataSource).map(([value, text]) => ({ value, label: String(text) }));
  }
  const mapped = mappedOptions(item);
  if (mapped.length) return mapped;
  const code = item.field.lovCode;
  return (code && lovOptions.value[code]) || [];
}

/** 映射字段（如角色名 RoleName 映射 RoleID）的候选项：优先复用同表单 `${mapField}s` 字段的数据字典（如 RoleIds），
 *  没有时从列表/搜索字段中找同名（或加 s）且带数据字典的字段（如部门搜索项 DepartmentID） */
function mappedOptions(item: FieldMapping): { label: string; value: string }[] {
  const mapField = item.field.mapField;
  if (!mapField) return [];
  const names = [`${mapField}s`.toLowerCase(), mapField.toLowerCase()];
  const candidates = [...props.fields, ...props.optionFields];
  const source = candidates.find(
    field => field !== item && field.field.dataSource && names.includes((field.field.name || '').toLowerCase())
  );
  return source ? optionsOf(source) : [];
}

/** 可编辑的映射下拉：字段有 mapField，且能从同表单取到候选项 */
function isMappedSelect(item: FieldMapping) {
  return !!item.field.mapField && mappedOptions(item).length > 0;
}

/** 取值/提交键：可编辑映射下拉取 mapField（如 RoleID），其余取字段名 */
function valueKey(item: FieldMapping) {
  return isMappedSelect(item) ? `${item.field.mapField}` : item.field.name;
}

/** 是否渲染为下拉框 */
function isDropdown(item: FieldMapping) {
  return isMappedSelect(item) || (['select', 'lov', 'lovMulti'].includes(item.widget) && optionsOf(item).length > 0);
}

interface TreeSelectOption {
  value: string;
  label: string;
  children?: TreeSelectOption[];
}

/** 父级下拉的树形选项：由树形实体的扁平列表组装 */
const parentTreeOptions = computed<TreeSelectOption[]>(() => {
  if (!props.treeData.length) return [];

  const toOption = (row: Record<string, unknown>): TreeSelectOption => {
    const value = String(getRowValue(row, props.pkField) ?? '');
    const label = String(
      getRowValue(row, 'DisplayName') || getRowValue(row, 'TreeNodeName') || getRowValue(row, 'Name') || value
    );
    const children = row.children as Record<string, unknown>[] | undefined;
    return children?.length ? { value, label, children: children.map(toOption) } : { value, label };
  };

  return [
    { value: '0', label: $t('page.form.rootMenu') },
    ...buildTreeRows(props.treeData, props.pkField).map(toOption)
  ];
});

/** 父级字段：树形实体的 ParentID 改为树形下拉 */
function isTreeParentField(item: FieldMapping) {
  return parentTreeOptions.value.length > 0 && /^parent(id)?$/i.test(item.field.name);
}

/** 角色“数据部门”：部门编号列表改为部门树形多选下拉 */
function isRoleDepartmentField(item: FieldMapping) {
  return /(^|\/)Role$/i.test(props.apiType) && item.field.name.toLowerCase() === 'datadepartmentids';
}

/** 地区级联目标层级（area1~area4），缺省三级 */
function areaLevel(item: FieldMapping) {
  const level = Number(item.props?.level);

  return Number.isFinite(level) && level > 0 ? level : 3;
}

/** 部门树选项，数据来源为部门实体全量列表 */
const departmentOptions = ref<TreeSelectOption[]>([]);
let departmentLoaded = false;

function toDepartmentOption(row: Record<string, unknown>): TreeSelectOption {
  const value = String(getRowValue(row, 'ID') ?? '');
  const label = String(getRowValue(row, 'Name') ?? value);
  const children = row.children as Record<string, unknown>[] | undefined;
  return children?.length ? { value, label, children: children.map(toDepartmentOption) } : { value, label };
}

async function loadDepartments() {
  if (departmentLoaded || !props.fields.some(item => isRoleDepartmentField(item))) return;

  try {
    const res = await cubeApi.page.getList<Record<string, unknown>>('/Admin/Department', {
      pageIndex: 1,
      pageSize: 1000
    });
    departmentOptions.value = buildTreeRows(res.data ?? [], 'id').map(toDepartmentOption);
    departmentLoaded = true;
  } catch {
    // 拉取失败时保留空下拉，不阻断角色表单
  }
}

/** 部门编号列表串 → 树形多选值 */
function departmentValue(item: FieldMapping): string[] {
  const raw = model.value[item.field.name];
  if (Array.isArray(raw)) return raw.map(String);
  return raw ? String(raw).split(',').map(value => value.trim()).filter(Boolean) : [];
}

/** 树形多选值 → 部门编号列表串 */
function setDepartmentValue(item: FieldMapping, value: unknown) {
  const list = Array.isArray(value) ? value.map(String) : value ? [String(value)] : [];
  model.value[item.field.name] = list.join(',');
}

/** 记录值 → 控件值 */
function normalizeForControl(item: FieldMapping, raw: unknown): unknown {
  if (isMultiple(item)) {
    if (raw === null || raw === undefined || raw === '') return [];
    const values = Array.isArray(raw) ? raw : String(raw).split(',');
    return values.map(value => String(value).trim()).filter(Boolean);
  }
  if (raw === null || raw === undefined) {
    return item.widget === 'switch' ? false : undefined;
  }
  switch (item.widget) {
    case 'switch':
      return Boolean(raw);
    case 'number': {
      const num = Number(raw);
      return Number.isNaN(num) ? undefined : num;
    }
    case 'area': {
      const num = Number(raw);
      return Number.isNaN(num) || num <= 0 ? undefined : num;
    }
    case 'date':
      return String(raw).slice(0, 10);
    case 'datetime':
      return String(raw).slice(0, 19).replace(' ', 'T');
    case 'time': {
      const text = String(raw);
      return text.length > 8 ? text.slice(-8) : text;
    }
    case 'select':
    case 'lov':
      return String(raw);
    default:
      return String(raw);
  }
}

/** 控件值 → 提交值 */
function serializeValue(item: FieldMapping, value: unknown): unknown {
  if (isMultiple(item)) {
    const list = Array.isArray(value) ? value : value ? [String(value)] : [];
    const csv = list.join(',');
    return csv === '' ? undefined : csv;
  }
  if (item.widget === 'switch') return value === true;
  if (item.widget === 'number') return typeof value === 'number' ? value : undefined;
  if (item.widget === 'area') {
    const num = Number(value);
    return Number.isNaN(num) || num <= 0 ? undefined : num;
  }
  // 图标字段允许清空，空串需真实提交，否则后端保留旧图标
  if (item.widget === 'icon') return typeof value === 'string' ? value.trim() : '';
  // 数据部门可清空，需提交空串覆盖旧值
  if (isRoleDepartmentField(item)) return typeof value === 'string' ? value : '';
  if (item.widget === 'select' || item.widget === 'lov') {
    if (value === undefined || value === null || value === '') return undefined;
    // 枚举与数值字典需按数字提交，字符串字段保持原样
    return !isStringField(item) && /^-?\d+(\.\d+)?$/.test(String(value)) ? Number(value) : String(value);
  }
  if (value === undefined || value === null) return undefined;
  if (typeof value === 'string' && value.trim() === '') {
    return item.field.nullable ? '' : undefined;
  }
  return value;
}

const CREATOR_ID_NAMES = new Set(['createuserid', 'creatorid', 'createbyid', 'createdbyid']);
const CREATOR_NAME_NAMES = new Set(['createuser', 'creator', 'createby', 'createdby']);

/** 新增时“创建者/创建人”默认取当前登录用户：编号型字段填用户 ID，名称型字段填登录名 */
function creatorDefault(item: FieldMapping): { matched: boolean; value: unknown } {
  const name = (item.field.name || '').toLowerCase();
  const label = fieldLabel(item);
  const matched = CREATOR_ID_NAMES.has(name) || CREATOR_NAME_NAMES.has(name) || /创建[者人]|创建用户/.test(label);
  if (!matched) return { matched: false, value: undefined };

  if (CREATOR_ID_NAMES.has(name) || name.endsWith('id') || item.widget === 'number') {
    const id = Number(authStore.cubeInfo.id);
    return { matched: true, value: Number.isFinite(id) ? id : undefined };
  }
  return { matched: true, value: authStore.cubeInfo.name || authStore.cubeInfo.displayName };
}

/** 同名选项的映射字段：详情只返回显示名，需再按候选编号查询归属 */
interface AmbiguousMapped {
  item: FieldMapping;
  candidates: { label: string; value: string }[];
}

/** 映射下拉回填：详情接口只返回显示名（如 RoleName/DepartmentName）时，按选项文本反查编号。
 *  同名选项（如两个“行政部”）无法按文本确定归属，交由 resolveAmbiguousMapped 精确确认，
 *  确认失败则保持空值，保存时该字段不提交，避免把关联写错 */
function mappedValue(item: FieldMapping, raw: unknown): { value: unknown; pending?: AmbiguousMapped } {
  if (raw !== undefined && raw !== null && raw !== '') return { value: raw };
  const text = props.record ? getRowValue(props.record, item.field.name) : undefined;
  if (text === undefined || text === null || text === '') return { value: raw };
  const matches = optionsOf(item).filter(option => option.label === String(text));
  if (matches.length === 1) return { value: matches[0].value };
  if (matches.length > 1 && props.mode === 'edit') return { value: raw, pending: { item, candidates: matches } };
  return { value: raw };
}

/** 关闭弹窗时仍在途的精确确认结果不再回填 */
let mappedProbeToken = 0;

/** 同名映射项精确确认：按“候选编号 + 关键字”检索，命中当前记录者即其真实编号。
 *  任一查询失败或命中不唯一时放弃回填，宁可下拉留空，也不写入错误关联 */
async function resolveAmbiguousMapped(list: AmbiguousMapped[]) {
  const token = ++mappedProbeToken;
  const record = props.record;
  if (!record || !props.apiType) return;
  const pk = getRowValue(record, props.pkField);
  if (pk === undefined || pk === null || pk === '') return;
  const keyword = String(getRowValue(record, 'name') ?? getRowValue(record, 'displayName') ?? '').trim();
  if (!keyword) return;

  for (const { item, candidates } of list) {
    const mapField = item.field.mapField;
    if (!mapField) continue;
    const hits: string[] = [];
    for (const candidate of candidates) {
      try {
        const res = await cubeApi.page.getList<Record<string, unknown>>(props.apiType, {
          pageIndex: 1,
          pageSize: 100,
          q: keyword,
          [mapField]: /^-?\d+$/.test(candidate.value) ? Number(candidate.value) : candidate.value
        });
        if ((res.data ?? []).some(row => String(getRowValue(row, props.pkField)) === String(pk))) hits.push(candidate.value);
      } catch {
        return;
      }
    }
    if (token !== mappedProbeToken || !props.modelValue) return;
    if (hits.length === 1 && isBlankValue(model.value[item.field.name])) model.value[item.field.name] = hits[0];
  }
}

function buildModel() {
  const next: Record<string, unknown> = {};
  const snapshot: Record<string, string> = {};
  const pending: AmbiguousMapped[] = [];
  for (const item of props.fields) {
    const name = item.field.name;
    const source = props.record ? getRowValue(props.record, valueKey(item)) : undefined;
    const mapped = isMappedSelect(item) ? mappedValue(item, source) : { value: source };
    if (mapped.pending) pending.push(mapped.pending);
    const value = normalizeForControl(item, mapped.value);
    // 关联编号为 0 表示未关联，此时下拉无匹配项，按空值处理
    next[name] = isMappedSelect(item) && (value === 0 || value === '0') ? undefined : value;
  }

  if (props.mode === 'add') {
    for (const item of props.fields) {
      const name = item.field.name;
      if (!isBlankValue(next[name])) continue;
      const preset = creatorDefault(item);
      if (preset.matched && !isBlankValue(preset.value)) next[name] = normalizeForControl(item, preset.value);
    }
  }

  for (const item of props.fields) {
    const name = item.field.name;
    snapshot[name] = JSON.stringify(serializeValue(item, next[name]) ?? null);
  }
  model.value = next;
  initialValues.value = snapshot;
  if (pending.length) resolveAmbiguousMapped(pending);
  else mappedProbeToken++;
}

async function loadLovOptions() {
  const codes = [
    ...new Set(
      props.fields
        .filter(item => item.field.lovCode && !item.field.dataSource)
        .map(item => item.field.lovCode as string)
    )
  ].filter(code => !lovOptions.value[code]);
  if (!codes.length) return;

  try {
    const res = await cubeApi.page.lookup(codes.join(','));
    const dict = res.data ?? {};
    const next = { ...lovOptions.value };
    Object.entries(dict).forEach(([code, rows]) => {
      next[code] = (rows ?? []).map(row => {
        const value = row.value ?? row.id ?? row.Id ?? row.key;
        const text = row.text ?? row.name ?? row.Name ?? row.label;
        return { value: String(value ?? ''), label: String(text ?? value ?? '') };
      });
    });
    lovOptions.value = next;
  } catch {
    // 拉取失败时降级为文本输入，不阻断表单
  }
}

function readonlyText(item: FieldMapping) {
  return formatCell(item.field, { [item.field.name]: model.value[item.field.name] });
}

/** 查看模式展示文本：优先取详情回包原值，密码掩码 */
function viewText(item: FieldMapping) {
  if (item.widget === 'password') return model.value[item.field.name] ? '******' : '';

  const recordValue = props.record ? getRowValue(props.record, item.field.name) : undefined;
  const value = recordValue === undefined ? model.value[item.field.name] : recordValue;
  return formatCell(item.field, { [item.field.name]: value });
}

/** 图片字段的预览地址；空值或非图片地址时返回空串 */
function imagePreviewSrc(item: FieldMapping) {
  if (item.widget !== 'image') return '';
  return resolveImageSrc(model.value[item.field.name]);
}

function buildPayload() {
  const payload: Record<string, unknown> = {};
  for (const item of props.fields) {
    const key = valueKey(item);
    const mapped = key !== item.field.name;
    // 不可编辑的映射展示字段不提交；可编辑映射下拉按 mapField（如 RoleID）提交
    if (item.widget === 'readonly' || (item.field.mapField && !mapped)) continue;
    const name = item.field.name;
    const value = serializeValue(item, model.value[name]);
    // 后端更新时整实体校验必填项，需提交全部表单字段；
    // 密码未改动或留空时不提交，避免把已有密码回写
    if (props.mode === 'edit' && item.widget === 'password') {
      const current = String(model.value[name] ?? '');
      if (current === '' || JSON.stringify(value ?? null) === initialValues.value[name]) continue;
    }
    if (value === undefined) continue;
    payload[key] = mapped && /id$/i.test(key) ? Number(value) : value;
  }
  if (props.mode === 'edit' && props.record) {
    payload[props.pkField] = getRowValue(props.record, props.pkField);
  }
  return payload;
}

async function handleSubmit() {
  if (isView.value) {
    close();
    return;
  }
  try {
    await formRef.value?.validate();
  } catch (invalid) {
    // 校验失败时切到第一个错误字段所在的页签，避免提示藏在未激活页签里
    const first = Object.keys(invalid as Record<string, unknown>)[0];
    const index = formTabs.value.findIndex(group => group.fields.some(item => item.field.name === first));
    if (index >= 0) activeTab.value = String(index);
    return;
  }
  emit('submit', buildPayload());
}

function close() {
  emit('update:modelValue', false);
}

watch(
  () => props.modelValue,
  visible => {
    if (!visible) return;
    activeTab.value = '0';
    buildModel();
    loadLovOptions();
    loadDepartments();
  },
  { immediate: true }
);
</script>

<template>
  <ElDialog
    :model-value="modelValue"
    :title="title"
    :width="dialogWidth"
    top="6vh"
    destroy-on-close
    append-to-body
    @update:model-value="emit('update:modelValue', $event)"
  >
    <ElForm ref="formRef" :model="model" :rules="rules" label-width="110px" class="entity-form">
      <ElTabs
        v-model="activeTab"
        class="entity-form__tabs"
        :class="{ 'entity-form__tabs--plain': formTabs.length <= 1 }"
      >
        <ElTabPane
          v-for="(group, groupIndex) in formTabs"
          :key="group.category"
          :label="tabLabel(group.category)"
          :name="String(groupIndex)"
        >
          <div class="grid grid-cols-1 gap-x-16px md:grid-cols-2">
            <ElFormItem
              v-for="item in group.fields"
              :key="item.field.name"
              :label="fieldLabel(item)"
              :prop="item.field.name"
              :class="{ 'md:col-span-2': isWide(item) || isPermissionField(item) || isRoleDepartmentField(item) }"
            >
          <ElInput
            v-if="isView"
            :model-value="viewText(item)"
            :type="isWide(item) ? 'textarea' : 'text'"
            :rows="isWide(item) ? 3 : undefined"
            disabled
          />
          <ElInput
            v-else-if="item.widget === 'readonly'"
            :model-value="readonlyText(item)"
            disabled
            :placeholder="item.field.description"
          />
          <ElInput
            v-else-if="item.widget === 'password'"
            v-model="model[item.field.name]"
            type="password"
            show-password
            autocomplete="new-password"
            :placeholder="item.field.description"
          />
          <RolePermissionPicker
            v-else-if="isPermissionField(item)"
            v-model="model[item.field.name]"
            :api-type="props.apiType"
            :role-id="roleId"
          />
          <ElTreeSelect
            v-else-if="isRoleDepartmentField(item)"
            :model-value="departmentValue(item)"
            :data="departmentOptions"
            node-key="value"
            :props="{ label: 'label', children: 'children' }"
            multiple
            show-checkbox
            :render-after-expand="false"
            filterable
            clearable
            class="w-full"
            :placeholder="item.field.description"
            @update:model-value="setDepartmentValue(item, $event)"
          />
          <ElInput
            v-else-if="isWide(item)"
            v-model="model[item.field.name]"
            type="textarea"
            :rows="item.widget === 'html' || item.widget === 'markdown' ? 4 : 3"
            :placeholder="item.field.description"
          />
          <ElInputNumber
            v-else-if="item.widget === 'number'"
            v-model="model[item.field.name]"
            class="w-full"
            controls-position="right"
          />
          <ElSwitch v-else-if="item.widget === 'switch'" v-model="model[item.field.name]" />
          <ElDatePicker
            v-else-if="item.widget === 'date'"
            v-model="model[item.field.name]"
            type="date"
            value-format="YYYY-MM-DD"
            class="w-full"
          />
          <ElDatePicker
            v-else-if="item.widget === 'datetime'"
            v-model="model[item.field.name]"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            class="w-full"
          />
          <ElTimePicker
            v-else-if="item.widget === 'time'"
            v-model="model[item.field.name]"
            value-format="HH:mm:ss"
            class="w-full"
          />
          <ElTreeSelect
            v-else-if="isTreeParentField(item)"
            v-model="model[item.field.name]"
            :data="parentTreeOptions"
            node-key="value"
            :props="{ label: 'label', children: 'children' }"
            :render-after-expand="false"
            default-expand-all
            filterable
            clearable
            class="w-full"
          />
          <AreaSelect
            v-else-if="item.widget === 'area'"
            v-model="model[item.field.name]"
            :level="areaLevel(item)"
            :placeholder="item.field.description"
          />
          <ElSelect
            v-else-if="isDropdown(item)"
            v-model="model[item.field.name]"
            :multiple="isMultiple(item)"
            filterable
            clearable
            class="w-full"
          >
            <ElOption v-for="option in optionsOf(item)" :key="option.value" :label="option.label" :value="option.value" />
          </ElSelect>
          <div v-else-if="item.widget === 'icon'" class="icon-field">
            <IconPicker v-model="model[item.field.name]" />
            <ElInput
              v-model="model[item.field.name]"
              :placeholder="item.field.description || '点击左侧图标选择'"
              class="icon-field__input"
            />
          </div>
          <div v-else-if="item.widget === 'image'" class="image-field">
            <img
              v-if="imagePreviewSrc(item)"
              :src="imagePreviewSrc(item)"
              class="image-field__preview"
              alt=""
              draggable="false"
            />
            <div v-else class="image-field__preview image-field__preview--empty">
              <SvgIcon icon="mdi:image-outline" class="text-20px" />
            </div>
            <ElInput
              v-model="model[item.field.name]"
              :placeholder="item.field.description || item.field.name"
              class="image-field__input"
            />
          </div>
          <ElInput
            v-else
            v-model="model[item.field.name]"
            :placeholder="item.field.description || item.field.name"
          />
            </ElFormItem>
          </div>
        </ElTabPane>
      </ElTabs>
    </ElForm>
    <template #footer>
      <ElButton v-if="!isView" @click="close">{{ $t('common.cancel') }}</ElButton>
      <ElButton type="primary" :loading="loading" @click="handleSubmit">
        {{ isView ? $t('common.close') : $t('common.confirm') }}
      </ElButton>
    </template>
  </ElDialog>
</template>

<style scoped>
.entity-form {
  max-height: 62vh;
  overflow-y: auto;
  padding-right: 8px;
}

/* 表单内部页签：单组字段时隐藏页签头，保持原有扁平表单外观 */
.entity-form__tabs--plain :deep(.el-tabs__header) {
  display: none;
}

.entity-form__tabs :deep(.el-tabs__header) {
  margin-bottom: 12px;
}

/* 图标字段：可点击的图标预览 + 图标名输入 */
.icon-field {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.icon-field__input {
  flex: 1;
  min-width: 0;
}

/* 图片字段：32px 圆角预览 + 地址输入 */
.image-field {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.image-field__preview {
  flex: none;
  width: 32px;
  height: 32px;
  border-radius: 4px;
  object-fit: cover;
  background: var(--el-fill-color-light);
}

.image-field__preview--empty {
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px dashed var(--el-border-color);
  color: var(--el-text-color-placeholder);
}

.image-field__input {
  flex: 1;
  min-width: 0;
}
</style>
