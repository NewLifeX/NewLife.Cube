<script setup lang="ts">
import { reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';

defineOptions({ name: 'LovConfigDialog' });

interface EnumItem {
  id: number | string;
  lovDefId: string;
  value: string;
  label: string;
  sort: number;
  enabled: boolean;
  extra?: string | null;
}

interface ListConfig {
  id: number | string;
  lovDefId: string;
  requestUrl?: string;
  method: string;
  pageable: boolean;
  pageNumField?: string;
  pageSizeField?: string;
  dataPath?: string;
  totalPath?: string;
  fixedParams?: string;
  proxyRequest?: boolean;
}

interface SearchField {
  id: number | string;
  lovDefId: string;
  field: string;
  title?: string;
  componentType: string;
  paramType: string;
  required: boolean;
  defaultValue?: string;
  sort: number;
  refLovCode?: string;
}

interface TableColumn {
  id: number | string;
  lovDefId: string;
  field: string;
  title?: string;
  width: number;
  align: string;
  sortable: boolean;
  refLovCode?: string;
  formatType?: string;
  sort: number;
}

interface LovConfigForm {
  id: string;
  lovCode: string;
  name: string;
  type: string;
  source: string;
  valueField?: string;
  labelField?: string;
  enabled: boolean;
  enumItems: EnumItem[];
  listConfig: ListConfig;
  searchFields: SearchField[];
  tableColumns: TableColumn[];
}

const props = defineProps<{
  modelValue: boolean;
  lovCode: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [val: boolean];
  saved: [];
}>();

const activeTab = ref('enumItems');
const saving = ref(false);
const loading = ref(false);

const emptyListConfig = (): ListConfig => ({
  id: 0,
  lovDefId: '',
  requestUrl: '',
  method: 'GET',
  pageable: false,
  pageNumField: 'pageNo',
  pageSizeField: 'pageSize',
  dataPath: 'data.list',
  totalPath: 'data.total',
  fixedParams: '',
  proxyRequest: false
});

const form = ref<LovConfigForm>({
  id: '',
  lovCode: '',
  name: '',
  type: 'ENUM',
  source: 'MANUAL',
  valueField: '',
  labelField: '',
  enabled: true,
  enumItems: [],
  listConfig: emptyListConfig(),
  searchFields: [],
  tableColumns: []
});

// 请求地址以 / 开头（同源同应用）→ 强制前端直连，禁止代理
// 必须声明在 form 之后：immediate 立即求值会读到尚未初始化的 form（TDZ）
const isSameAppUrl = ref(false);
watch(
  () => form.value.listConfig?.requestUrl,
  url => {
    isSameAppUrl.value = !!url && url.startsWith('/');
    if (isSameAppUrl.value) form.value.listConfig.proxyRequest = false;
  },
  { immediate: true }
);

/* ---------------- 搜索字段 ---------------- */
const sfVisible = ref(false);
const sfIndex = ref(-1);
const sfForm = reactive<SearchField>({
  id: 0,
  lovDefId: '',
  field: '',
  title: '',
  componentType: 'input',
  paramType: 'BODY',
  required: false,
  defaultValue: '',
  sort: 0,
  refLovCode: ''
});

function editSearchField(index: number) {
  sfIndex.value = index;
  Object.assign(sfForm, form.value.searchFields[index]);
  sfVisible.value = true;
}

function addSearchField() {
  sfIndex.value = -1;
  Object.assign(sfForm, {
    id: 0,
    lovDefId: form.value.id,
    field: '',
    title: '',
    componentType: 'input',
    paramType: 'BODY',
    required: false,
    defaultValue: '',
    sort: form.value.searchFields.length,
    refLovCode: ''
  });
  sfVisible.value = true;
}

function confirmSearchField() {
  const data = { ...sfForm };
  if (sfIndex.value >= 0) form.value.searchFields[sfIndex.value] = data;
  else form.value.searchFields.push(data);
  sfVisible.value = false;
}

/* ---------------- 表格列 ---------------- */
const tcVisible = ref(false);
const tcIndex = ref(-1);
const tcForm = reactive<TableColumn>({
  id: 0,
  lovDefId: '',
  field: '',
  title: '',
  width: 120,
  align: 'left',
  sortable: false,
  refLovCode: '',
  formatType: '',
  sort: 0
});

function editTableColumn(index: number) {
  tcIndex.value = index;
  Object.assign(tcForm, form.value.tableColumns[index]);
  tcVisible.value = true;
}

function addTableColumn() {
  tcIndex.value = -1;
  Object.assign(tcForm, {
    id: 0,
    lovDefId: form.value.id,
    field: '',
    title: '',
    width: 120,
    align: 'left',
    sortable: false,
    refLovCode: '',
    formatType: '',
    sort: form.value.tableColumns.length
  });
  tcVisible.value = true;
}

function confirmTableColumn() {
  const data = { ...tcForm };
  if (tcIndex.value >= 0) form.value.tableColumns[tcIndex.value] = data;
  else form.value.tableColumns.push(data);
  tcVisible.value = false;
}

/* ---------------- 枚举值 ---------------- */
function addEnumItem() {
  form.value.enumItems.push({
    id: 0,
    lovDefId: props.lovCode,
    value: '',
    label: '',
    sort: form.value.enumItems.length,
    enabled: true,
    extra: null
  });
}

function removeItem(key: 'enumItems' | 'searchFields' | 'tableColumns', index: number) {
  form.value[key].splice(index, 1);
}

async function loadConfig() {
  if (!props.modelValue || !props.lovCode) return;
  loading.value = true;
  try {
    // GetConfig 直接返回配置对象；部分实现会包一层 ApiResponse，两种都兼容
    const raw = await cubeApi.page.getAction<Record<string, unknown>>('Admin/Lov', 'GetConfig', { id: props.lovCode });
    const config = ((raw as { data?: Record<string, unknown> })?.data ?? raw) as Record<string, unknown> | undefined;
    if (!config) return;

    if (config.type === 'LIST' && !config.listConfig) config.listConfig = emptyListConfig();

    form.value = {
      id: String(config.id ?? props.lovCode),
      lovCode: String(config.lovCode ?? ''),
      name: String(config.name ?? ''),
      type: String(config.type ?? 'ENUM'),
      source: String(config.source ?? 'MANUAL'),
      valueField: String(config.valueField ?? ''),
      labelField: String(config.labelField ?? ''),
      enabled: config.enabled !== false,
      enumItems: Array.isArray(config.enumItems) ? (config.enumItems as EnumItem[]) : [],
      listConfig: (config.listConfig as ListConfig) ?? emptyListConfig(),
      searchFields: Array.isArray(config.searchFields) ? (config.searchFields as SearchField[]) : [],
      tableColumns: Array.isArray(config.tableColumns) ? (config.tableColumns as TableColumn[]) : []
    };

    activeTab.value =
      form.value.type === 'ENUM'
        ? 'enumItems'
        : form.value.searchFields.length > 0
          ? 'searchFields'
          : 'listConfig';
  } catch {
    ElMessage.error($t('lovConfig.loadFailed'));
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const body = { ...form.value, id: props.lovCode };
    await cubeApi.page.postAction('Admin/Lov', 'SaveConfig', body);
    ElMessage.success($t('lovConfig.saveSuccess'));
    emit('saved');
    close();
  } catch {
    ElMessage.error($t('lovConfig.saveFailed'));
  } finally {
    saving.value = false;
  }
}

function close() {
  emit('update:modelValue', false);
}

watch(
  () => props.modelValue,
  value => {
    if (value) loadConfig();
  }
);
</script>

<template>
  <ElDialog
    :model-value="modelValue"
    :title="$t('lovConfig.title', { code: form.lovCode || '...' })"
    width="90%"
    top="4vh"
    destroy-on-close
    :close-on-click-modal="false"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div v-loading="loading" class="lov-config">
      <div class="lov-config__header">
        <ElTag :type="form.type === 'ENUM' ? 'primary' : 'success'" size="small">{{ form.type }}</ElTag>
        <code class="lov-config__code">{{ form.lovCode }}</code>
        <span class="lov-config__name">{{ form.name }}</span>
        <ElTag v-if="form.source === 'AUTO'" type="info" size="small">{{ $t('lovAdmin.sourceAuto') }}</ElTag>
        <ElTag v-else size="small">{{ $t('lovAdmin.sourceManual') }}</ElTag>
      </div>

      <ElTabs v-model="activeTab">
        <ElTabPane v-if="form.type === 'ENUM'" :label="$t('lovConfig.enumItemsTab')" name="enumItems">
          <ElAlert
            v-if="form.source === 'AUTO'"
            :title="$t('lovConfig.autoManagedTip')"
            type="warning"
            :closable="false"
            show-icon
            class="lov-config__tip"
          />
          <ElTable :data="form.enumItems" border max-height="420">
            <ElTableColumn :label="$t('common.index')" width="70" align="center">
              <template #default="{ $index }">{{ $index + 1 }}</template>
            </ElTableColumn>
            <ElTableColumn :label="$t('lovConfig.sortLabel')" width="120">
              <template #default="{ row }">
                <ElInputNumber
                  v-model="row.sort"
                  :disabled="form.source === 'AUTO'"
                  size="small"
                  :min="0"
                  controls-position="right"
                />
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('lovConfig.valueColumn')" min-width="140">
              <template #default="{ row }">
                <ElInput v-model="row.value" :disabled="form.source === 'AUTO'" size="small" />
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('lovConfig.labelColumn')" min-width="180">
              <template #default="{ row }">
                <ElInput v-model="row.label" :disabled="form.source === 'AUTO'" size="small" />
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('lovConfig.enabledLabel')" width="80" align="center">
              <template #default="{ row }">
                <ElSwitch v-model="row.enabled" :disabled="form.source === 'AUTO'" />
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('common.action')" width="90" align="center">
              <template #default="{ $index }">
                <ElButton
                  v-if="form.source !== 'AUTO'"
                  size="small"
                  type="danger"
                  text
                  @click="removeItem('enumItems', $index)"
                >
                  {{ $t('common.delete') }}
                </ElButton>
              </template>
            </ElTableColumn>
            <template #empty>
              <ElEmpty :description="$t('common.noData')" :image-size="70" />
            </template>
          </ElTable>
          <ElButton v-if="form.source !== 'AUTO'" type="primary" class="lov-config__add" @click="addEnumItem">
            {{ $t('lovConfig.addEnumItem') }}
          </ElButton>
        </ElTabPane>

        <ElTabPane v-if="form.type === 'LIST'" :label="$t('lovAdmin.listConfig')" name="listConfig">
          <ElForm :model="form.listConfig" label-width="140px" class="lov-config__form">
            <ElFormItem :label="$t('lovConfig.requestUrl')">
              <ElInput v-model="form.listConfig.requestUrl" placeholder="/api/v1/xxx" />
              <div class="lov-config__form-tip">{{ $t('lovConfig.requestUrlTip') }}</div>
            </ElFormItem>
            <ElFormItem :label="$t('lovConfig.method')">
              <ElRadioGroup v-model="form.listConfig.method">
                <ElRadio value="GET">GET</ElRadio>
                <ElRadio value="POST">POST</ElRadio>
              </ElRadioGroup>
            </ElFormItem>
            <ElFormItem :label="$t('lovConfig.proxyRequest')">
              <ElSwitch v-model="form.listConfig.proxyRequest" :disabled="isSameAppUrl" />
              <div class="lov-config__form-tip">
                {{ $t('lovConfig.proxyRequestTip') }}
                <span v-if="isSameAppUrl" class="lov-config__warn">{{ $t('lovConfig.sameAppUrlTip') }}</span>
              </div>
            </ElFormItem>
            <ElFormItem :label="$t('lovConfig.pageable')">
              <ElSwitch v-model="form.listConfig.pageable" />
            </ElFormItem>
            <template v-if="form.listConfig.pageable">
              <ElFormItem :label="$t('lovConfig.pageNumField')">
                <ElInput v-model="form.listConfig.pageNumField" placeholder="pageNo" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.pageSizeField')">
                <ElInput v-model="form.listConfig.pageSizeField" placeholder="pageSize" />
              </ElFormItem>
            </template>
            <ElFormItem :label="$t('lovConfig.dataPath')">
              <ElInput v-model="form.listConfig.dataPath" placeholder="data" />
              <div class="lov-config__form-tip">{{ $t('lovConfig.dataPathTip') }}</div>
            </ElFormItem>
            <ElFormItem :label="$t('lovConfig.totalPath')">
              <ElInput v-model="form.listConfig.totalPath" placeholder="page.totalCount" />
              <div class="lov-config__form-tip">{{ $t('lovConfig.totalPathTip') }}</div>
            </ElFormItem>
            <ElFormItem :label="$t('lovConfig.fixedParams')">
              <ElInput
                v-model="form.listConfig.fixedParams"
                type="textarea"
                :rows="3"
                placeholder='{ "status": "active" }'
              />
            </ElFormItem>
          </ElForm>
        </ElTabPane>

        <ElTabPane v-if="form.type === 'LIST'" :label="$t('lovConfig.searchFieldsTab')" name="searchFields">
          <ElTable :data="form.searchFields" border max-height="420">
            <ElTableColumn :label="$t('lovConfig.sortLabel')" width="110">
              <template #default="{ row }">
                <ElInputNumber v-model="row.sort" size="small" :min="0" controls-position="right" />
              </template>
            </ElTableColumn>
            <ElTableColumn prop="field" :label="$t('lovConfig.fieldName')" min-width="120" />
            <ElTableColumn prop="title" :label="$t('lovConfig.displayTitle')" min-width="140" />
            <ElTableColumn :label="$t('lovConfig.controlType')" width="100" align="center">
              <template #default="{ row }">
                <ElTag size="small">{{ row.componentType }}</ElTag>
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('lovConfig.refLov')" min-width="150">
              <template #default="{ row }">
                <code v-if="row.refLovCode">{{ row.refLovCode }}</code>
                <span v-else class="lov-config__muted">—</span>
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('common.action')" width="120" align="center">
              <template #default="{ $index }">
                <ElButton size="small" @click="editSearchField($index)">{{ $t('common.edit') }}</ElButton>
                <ElButton size="small" type="danger" text @click="removeItem('searchFields', $index)">
                  {{ $t('common.delete') }}
                </ElButton>
              </template>
            </ElTableColumn>
            <template #empty>
              <ElEmpty :description="$t('common.noData')" :image-size="70" />
            </template>
          </ElTable>
          <ElButton type="primary" class="lov-config__add" @click="addSearchField">
            {{ $t('lovConfig.addSearchField') }}
          </ElButton>

          <ElDialog v-model="sfVisible" :title="$t('lovConfig.editSearchField')" width="520px" append-to-body>
            <ElForm :model="sfForm" label-width="110px">
              <ElFormItem :label="$t('lovConfig.fieldName')" required>
                <ElInput v-model="sfForm.field" :placeholder="$t('lovConfig.fieldNameHint')" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.displayTitle')">
                <ElInput v-model="sfForm.title" :placeholder="$t('lovConfig.titleHint')" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.controlType')">
                <ElSelect v-model="sfForm.componentType" @change="sfForm.refLovCode = ''">
                  <ElOption :label="$t('lovConfig.controlInput')" value="input" />
                  <ElOption :label="$t('lovConfig.controlSelect')" value="select" />
                  <ElOption :label="$t('lovConfig.controlLov')" value="lov" />
                </ElSelect>
              </ElFormItem>
              <ElFormItem
                v-if="sfForm.componentType === 'select' || sfForm.componentType === 'lov'"
                :label="$t('lovConfig.refLov')"
              >
                <ElInput v-model="sfForm.refLovCode" :placeholder="$t('lovConfig.refLovHint')" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.paramType')">
                <ElRadioGroup v-model="sfForm.paramType">
                  <ElRadio value="BODY">BODY</ElRadio>
                  <ElRadio value="QUERY">QUERY</ElRadio>
                </ElRadioGroup>
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.required')">
                <ElSwitch v-model="sfForm.required" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.defaultValue')">
                <ElInput v-model="sfForm.defaultValue" />
              </ElFormItem>
            </ElForm>
            <template #footer>
              <ElButton @click="sfVisible = false">{{ $t('common.cancel') }}</ElButton>
              <ElButton type="primary" @click="confirmSearchField">{{ $t('common.confirm') }}</ElButton>
            </template>
          </ElDialog>
        </ElTabPane>

        <ElTabPane v-if="form.type === 'LIST'" :label="$t('lovConfig.tableColumnsTab')" name="tableColumns">
          <ElTable :data="form.tableColumns" border max-height="420">
            <ElTableColumn :label="$t('lovConfig.sortLabel')" width="110">
              <template #default="{ row }">
                <ElInputNumber v-model="row.sort" size="small" :min="0" controls-position="right" />
              </template>
            </ElTableColumn>
            <ElTableColumn prop="field" :label="$t('lovConfig.fieldName')" min-width="120" />
            <ElTableColumn prop="title" :label="$t('lovConfig.columnTitle')" min-width="140" />
            <ElTableColumn prop="width" :label="$t('lovConfig.columnWidth')" width="80" align="center" />
            <ElTableColumn prop="align" :label="$t('lovConfig.columnAlign')" width="80" align="center" />
            <ElTableColumn :label="$t('lovConfig.refLov')" min-width="150">
              <template #default="{ row }">
                <code v-if="row.refLovCode">{{ row.refLovCode }}</code>
                <span v-else class="lov-config__muted">—</span>
              </template>
            </ElTableColumn>
            <ElTableColumn :label="$t('common.action')" width="120" align="center">
              <template #default="{ $index }">
                <ElButton size="small" @click="editTableColumn($index)">{{ $t('common.edit') }}</ElButton>
                <ElButton size="small" type="danger" text @click="removeItem('tableColumns', $index)">
                  {{ $t('common.delete') }}
                </ElButton>
              </template>
            </ElTableColumn>
            <template #empty>
              <ElEmpty :description="$t('common.noData')" :image-size="70" />
            </template>
          </ElTable>
          <ElButton type="primary" class="lov-config__add" @click="addTableColumn">
            {{ $t('lovConfig.addColumn') }}
          </ElButton>

          <ElDialog v-model="tcVisible" :title="$t('lovConfig.editColumn')" width="520px" append-to-body>
            <ElForm :model="tcForm" label-width="110px">
              <ElFormItem :label="$t('lovConfig.fieldName')" required>
                <ElInput v-model="tcForm.field" :placeholder="$t('lovConfig.fieldNameHintStatus')" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.displayTitle')">
                <ElInput v-model="tcForm.title" :placeholder="$t('lovConfig.titleHintStatus')" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.columnWidthPx')">
                <ElInputNumber v-model="tcForm.width" :min="0" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.align')">
                <ElRadioGroup v-model="tcForm.align">
                  <ElRadio value="left">{{ $t('lovConfig.alignLeft') }}</ElRadio>
                  <ElRadio value="center">{{ $t('lovConfig.alignCenter') }}</ElRadio>
                  <ElRadio value="right">{{ $t('lovConfig.alignRight') }}</ElRadio>
                </ElRadioGroup>
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.sortable')">
                <ElSwitch v-model="tcForm.sortable" />
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.refLov')">
                <ElInput
                  v-model="tcForm.refLovCode"
                  :placeholder="$t('lovConfig.refLovHintStatus')"
                  @input="tcForm.formatType = ''"
                />
                <div class="lov-config__form-tip">{{ $t('lovConfig.refLovTip') }}</div>
              </ElFormItem>
              <ElFormItem :label="$t('lovConfig.formatType')">
                <ElInput
                  v-model="tcForm.formatType"
                  :placeholder="$t('lovConfig.formatTypeHint')"
                  @input="tcForm.refLovCode = ''"
                />
              </ElFormItem>
            </ElForm>
            <template #footer>
              <ElButton @click="tcVisible = false">{{ $t('common.cancel') }}</ElButton>
              <ElButton type="primary" @click="confirmTableColumn">{{ $t('common.confirm') }}</ElButton>
            </template>
          </ElDialog>
        </ElTabPane>
      </ElTabs>
    </div>

    <template #footer>
      <ElButton @click="close">{{ $t('common.cancel') }}</ElButton>
      <ElButton type="primary" :loading="saving" @click="save">{{ $t('lovConfig.saveConfig') }}</ElButton>
    </template>
  </ElDialog>
</template>

<style scoped>
.lov-config__header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  margin-bottom: 16px;
}

.lov-config__code {
  font-family: var(--el-font-family-mono, monospace);
}

.lov-config__name {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.lov-config__tip {
  margin-bottom: 16px;
}

.lov-config__add {
  margin-top: 16px;
}

.lov-config__form {
  max-width: 680px;
}

.lov-config__form-tip {
  width: 100%;
  margin-top: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.lov-config__warn {
  color: var(--el-color-warning);
}

.lov-config__muted {
  color: var(--el-text-color-placeholder);
}
</style>
