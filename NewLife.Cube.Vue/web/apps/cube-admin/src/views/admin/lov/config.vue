<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="t('lovConfig.title', { code: form.lovCode || '...' })"
    width="90%"
    top="3vh"
    :close-on-click-modal="false"
    @open="loadConfig"
  >
    <!-- 值集基本信息头 -->
    <div class="config-header">
      <el-tag :type="form.type === 'ENUM' ? 'primary' : 'success'" size="small">
        {{ form.type }}
      </el-tag>
      <code>{{ form.lovCode }}</code>
      <span class="header-name">{{ form.name }}</span>
      <el-tag v-if="form.source === 'AUTO'" type="info" size="small">{{ t('lovAdmin.sourceAuto') }}</el-tag>
      <el-tag v-else size="small">{{ t('lovAdmin.sourceManual') }}</el-tag>
    </div>

    <el-divider />

    <!-- 动态 Tab -->
    <el-tabs v-model="activeTab">
      <!-- 枚举值 (ENUM 类型) -->
      <el-tab-pane v-if="form.type === 'ENUM'" :label="t('lovConfig.enumItemsTab')" name="enumItems">
        <el-alert
          v-if="form.source === 'AUTO'"
          :title="t('lovConfig.autoManagedTip')"
          type="warning"
          :closable="false"
          show-icon
          style="margin-bottom: 16px"
        />
        <el-table :data="form.enumItems" max-height="400" border>
          <el-table-column :label="t('common.sort')" width="120">
            <template #default="{ row }">
              <el-input-number
                v-model="row.sort"
                :disabled="form.source === 'AUTO'"
                size="small"
                :min="0"
                controls-position="right"
              />
            </template>
          </el-table-column>
          <el-table-column :label="t('lovConfig.valueColumn')" min-width="120">
            <template #default="{ row }">
              <el-input v-model="row.value" :disabled="form.source === 'AUTO'" size="small" />
            </template>
          </el-table-column>
          <el-table-column :label="t('lovConfig.labelColumn')" min-width="160">
            <template #default="{ row }">
              <el-input v-model="row.label" :disabled="form.source === 'AUTO'" size="small" />
            </template>
          </el-table-column>
          <el-table-column :label="t('common.enable')" width="70">
            <template #default="{ row }">
              <el-switch v-model="row.enabled" :disabled="form.source === 'AUTO'" />
            </template>
          </el-table-column>
          <el-table-column :label="t('common.actions')" width="100" fixed="right">
            <template #default="{ $index }">
              <el-button
                v-if="form.source !== 'AUTO'"
                size="small"
                type="danger"
                text
                @click="removeItem('enumItems', $index)"
                >{{ t('button.delete') }}</el-button
              >
            </template>
          </el-table-column>
        </el-table>
        <el-button v-if="form.source !== 'AUTO'" type="primary" class="mt-2" @click="addEnumItem"
          >{{ t('lovConfig.addEnumItem') }}</el-button
        >
      </el-tab-pane>

      <!-- 列表配置 (LIST 类型) -->
      <el-tab-pane v-if="form.type === 'LIST'" :label="t('lovAdmin.listConfig')" name="listConfig">
        <el-form :model="form.listConfig" label-width="120px" class="config-form">
          <el-form-item :label="t('lovConfig.requestUrl')">
            <el-input v-model="form.listConfig.requestUrl" placeholder="/api/v1/xxx" />
            <div class="form-tip">{{ t('lovConfig.requestUrlTip') }}</div>
          </el-form-item>
          <el-form-item :label="t('lovConfig.method')">
            <el-radio-group v-model="form.listConfig.method">
              <el-radio value="GET">GET</el-radio>
              <el-radio value="POST">POST</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item :label="t('lovConfig.proxyRequest')">
            <el-switch v-model="form.listConfig.proxyRequest" :disabled="isSameAppUrl" />
            <div class="form-tip">
              {{ t('lovConfig.proxyRequestTip') }}
              <span v-if="isSameAppUrl" style="color: #e6a23c">
                {{ t('lovConfig.sameAppUrlTip') }}
              </span>
            </div>
          </el-form-item>
          <el-form-item :label="t('lovConfig.pageable')">
            <el-switch v-model="form.listConfig.pageable" />
          </el-form-item>
          <template v-if="form.listConfig.pageable">
            <el-form-item :label="t('lovConfig.pageNumField')">
              <el-input v-model="form.listConfig.pageNumField" placeholder="pageNo" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.pageSizeField')">
              <el-input v-model="form.listConfig.pageSizeField" placeholder="pageSize" />
            </el-form-item>
          </template>
          <el-form-item :label="t('lovConfig.dataPath')">
            <el-input v-model="form.listConfig.dataPath" placeholder="data" />
            <div class="form-tip">{{ t('lovConfig.dataPathTip') }}</div>
          </el-form-item>
          <el-form-item :label="t('lovConfig.totalPath')">
            <el-input v-model="form.listConfig.totalPath" placeholder="page.totalCount" />
            <div class="form-tip">{{ t('lovConfig.totalPathTip') }}</div>
          </el-form-item>
          <el-form-item :label="t('lovConfig.fixedParams')">
            <el-input
              v-model="form.listConfig.fixedParams"
              type="textarea"
              :rows="3"
              placeholder='{ "status": "active" }'
            />
          </el-form-item>
        </el-form>
      </el-tab-pane>

      <!-- 搜索字段 (LIST 类型) -->
      <el-tab-pane v-if="form.type === 'LIST'" :label="t('lovConfig.searchFieldsTab')" name="searchFields">
        <el-table :data="form.searchFields" max-height="400" border>
          <el-table-column :label="t('common.sort')" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.sort" size="small" :min="0" controls-position="right" />
            </template>
          </el-table-column>
          <el-table-column :label="t('lovConfig.fieldName')" prop="field" min-width="120" />
          <el-table-column :label="t('lovConfig.displayTitle')" prop="title" min-width="140" />
          <el-table-column :label="t('lovConfig.controlType')" width="100">
            <template #default="{ row }">
              <el-tag size="small">{{ row.componentType }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="t('lovConfig.refLov')" min-width="160">
            <template #default="{ row }">
              <code v-if="row.refLovCode">{{ row.refLovCode }}</code>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('common.actions')" width="130" fixed="right">
            <template #default="{ $index }">
              <el-button size="small" @click="editSearchField($index)">{{ t('button.edit') }}</el-button>
              <el-button size="small" type="danger" text @click="removeItem('searchFields', $index)"
                >{{ t('button.delete') }}</el-button
              >
            </template>
          </el-table-column>
        </el-table>
        <el-button type="primary" class="mt-2" @click="addSearchField">{{ t('lovConfig.addSearchField') }}</el-button>

        <!-- 搜索字段编辑弹窗 -->
        <el-dialog v-model="sfVisible" :title="t('lovConfig.editSearchField')" width="500px" append-to-body>
          <el-form :model="sfForm" label-width="100px">
            <el-form-item :label="t('lovConfig.fieldName')" required>
              <el-input v-model="sfForm.field" :placeholder="t('lovConfig.fieldNameHint')" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.displayTitle')">
              <el-input v-model="sfForm.title" :placeholder="t('lovConfig.titleHint')" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.controlType')">
              <el-select v-model="sfForm.componentType" @change="sfForm.refLovCode = ''">
                <el-option :label="t('lovConfig.controlInput')" value="input" />
                <el-option :label="t('lovConfig.controlSelect')" value="select" />
                <el-option :label="t('lovConfig.controlLov')" value="lov" />
              </el-select>
            </el-form-item>
            <el-form-item
              v-if="sfForm.componentType === 'select' || sfForm.componentType === 'lov'"
              :label="t('lovConfig.refLov')"
            >
              <el-input v-model="sfForm.refLovCode" :placeholder="t('lovConfig.refLovHint')" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.paramType')">
              <el-radio-group v-model="sfForm.paramType">
                <el-radio value="BODY">BODY</el-radio>
                <el-radio value="QUERY">QUERY</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item :label="t('lovConfig.required')">
              <el-switch v-model="sfForm.required" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.defaultValue')">
              <el-input v-model="sfForm.defaultValue" />
            </el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="sfVisible = false">{{ t('button.cancel') }}</el-button>
            <el-button type="primary" @click="confirmSearchField">{{ t('button.confirm') }}</el-button>
          </template>
        </el-dialog>
      </el-tab-pane>

      <!-- 表格列 (LIST 类型) -->
      <el-tab-pane v-if="form.type === 'LIST'" :label="t('lovConfig.tableColumnsTab')" name="tableColumns">
        <el-table :data="form.tableColumns" max-height="400" border>
          <el-table-column :label="t('common.sort')" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.sort" size="small" :min="0" controls-position="right" />
            </template>
          </el-table-column>
          <el-table-column :label="t('lovConfig.fieldName')" prop="field" min-width="120" />
          <el-table-column :label="t('lovConfig.columnTitle')" prop="title" min-width="140" />
          <el-table-column :label="t('lovConfig.columnWidth')" width="70" prop="width" />
          <el-table-column :label="t('lovConfig.columnAlign')" width="70" prop="align" />
          <el-table-column :label="t('lovConfig.refLov')" min-width="160">
            <template #default="{ row }">
              <code v-if="row.refLovCode">{{ row.refLovCode }}</code>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('common.actions')" width="130" fixed="right">
            <template #default="{ $index }">
              <el-button size="small" @click="editTableColumn($index)">{{ t('button.edit') }}</el-button>
              <el-button size="small" type="danger" text @click="removeItem('tableColumns', $index)"
                >{{ t('button.delete') }}</el-button
              >
            </template>
          </el-table-column>
        </el-table>
        <el-button type="primary" class="mt-2" @click="addTableColumn">{{ t('lovConfig.addColumn') }}</el-button>

        <!-- 表格列编辑弹窗 -->
        <el-dialog v-model="tcVisible" :title="t('lovConfig.editColumn')" width="500px" append-to-body>
          <el-form :model="tcForm" label-width="100px">
            <el-form-item :label="t('lovConfig.fieldName')" required>
              <el-input v-model="tcForm.field" :placeholder="t('lovConfig.fieldNameHintStatus')" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.displayTitle')">
              <el-input v-model="tcForm.title" :placeholder="t('lovConfig.titleHintStatus')" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.columnWidthPx')">
              <el-input-number v-model="tcForm.width" :min="0" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.align')">
              <el-radio-group v-model="tcForm.align">
                <el-radio value="left">{{ t('lovConfig.alignLeft') }}</el-radio>
                <el-radio value="center">{{ t('lovConfig.alignCenter') }}</el-radio>
                <el-radio value="right">{{ t('lovConfig.alignRight') }}</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item :label="t('lovConfig.sortable')">
              <el-switch v-model="tcForm.sortable" />
            </el-form-item>
            <el-form-item :label="t('lovConfig.refLov')">
              <el-input
                v-model="tcForm.refLovCode"
                :placeholder="t('lovConfig.refLovHintStatus')"
                @input="tcForm.formatType = ''"
              />
              <div class="form-tip">{{ t('lovConfig.refLovTip') }}</div>
            </el-form-item>
            <el-form-item :label="t('lovConfig.formatType')">
              <el-input
                v-model="tcForm.formatType"
                @input="tcForm.refLovCode = ''"
                :placeholder="t('lovConfig.formatTypeHint')"
              />
            </el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="tcVisible = false">{{ t('button.cancel') }}</el-button>
            <el-button type="primary" @click="confirmTableColumn">{{ t('button.confirm') }}</el-button>
          </template>
        </el-dialog>
      </el-tab-pane>
    </el-tabs>

    <template #footer>
      <el-button @click="close">{{ t('button.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="save">{{ t('lovConfig.saveConfig') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import cubeApi, { usePageApi } from '@newlifex/cube-vue/core/composables/useCubeApi';

interface EnumItem {
  id: number;
  lovDefId: number;
  value: string;
  label: string;
  sort: number;
  enabled: boolean;
  extra?: string | null;
}

interface ListConfig {
  id: number;
  lovDefId: number;
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
  id: number;
  lovDefId: number;
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
  id: number;
  lovDefId: number;
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
  id: number;
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
  lovDefId: number;
}>();

const emit = defineEmits<{
  'update:modelValue': [val: boolean];
  saved: [];
}>();

const api = usePageApi('Admin', 'Lov');
const activeTab = ref('enumItems');
const saving = ref(false);
const { t } = useI18n();
const loading = ref(false);

const emptyListConfig = (): ListConfig => ({
  id: 0,
  lovDefId: 0,
  requestUrl: '',
  method: 'GET',
  pageable: false,
  pageNumField: 'pageNo',
  pageSizeField: 'pageSize',
  dataPath: 'data.list',
  totalPath: 'data.total',
  fixedParams: '',
  proxyRequest: false,
});

// 请求地址以 / 开头（同源同应用）→ 强制前端直连，禁止代理
const isSameAppUrl = ref(false);
watch(
  () => form.value.listConfig?.requestUrl,
  (url) => {
    isSameAppUrl.value = !!url && url.startsWith('/');
    if (isSameAppUrl.value) form.value.listConfig.proxyRequest = false;
  },
  { immediate: true },
);

const form = ref<LovConfigForm>({
  id: 0,
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
  tableColumns: [],
});

// 搜索字段编辑
const sfVisible = ref(false);
const sfIndex = ref(-1);
const sfForm = reactive<SearchField>({
  id: 0,
  lovDefId: 0,
  field: '',
  title: '',
  componentType: 'input',
  paramType: 'BODY',
  required: false,
  defaultValue: '',
  sort: 0,
  refLovCode: '',
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
    refLovCode: '',
  });
  sfVisible.value = true;
}
function confirmSearchField() {
  const data = { ...sfForm };
  if (sfIndex.value >= 0) {
    form.value.searchFields[sfIndex.value] = data;
  } else {
    form.value.searchFields.push(data);
  }
  sfVisible.value = false;
}

// 表格列编辑
const tcVisible = ref(false);
const tcIndex = ref(-1);
const tcForm = reactive<TableColumn>({
  id: 0,
  lovDefId: 0,
  field: '',
  title: '',
  width: 120,
  align: 'left',
  sortable: false,
  refLovCode: '',
  formatType: '',
  sort: 0,
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
    sort: form.value.tableColumns.length,
  });
  tcVisible.value = true;
}
function confirmTableColumn() {
  const data = { ...tcForm };
  if (tcIndex.value >= 0) {
    form.value.tableColumns[tcIndex.value] = data;
  } else {
    form.value.tableColumns.push(data);
  }
  tcVisible.value = false;
}

// 枚举值
function addEnumItem() {
  form.value.enumItems.push({
    id: 0,
    lovDefId: props.lovDefId,
    value: '',
    label: '',
    sort: form.value.enumItems.length,
    enabled: true,
    extra: null,
  });
}

function removeItem(key: 'enumItems' | 'searchFields' | 'tableColumns', index: number) {
  form.value[key].splice(index, 1);
}

async function loadConfig() {
  if (!props.modelValue || !props.lovDefId) return;
  loading.value = true;
  try {
    // 优先调用 GetConfig 获取完整配置
    let config: any;
    try {
      config = await api.getAction('GetConfig?id=' + props.lovDefId);
      // getAction 返回 { code, data, traceId }，取 data
      if (config && config.data) config = config.data;
    } catch {
      // 回退到 getDetail
      const res = await api.getDetail(props.lovDefId);
      config = res.data ?? res;
    }

    if (!config) {
      loading.value = false;
      return;
    }

    if (config.type === 'LIST' && !config.listConfig) {
      config.listConfig = emptyListConfig();
    }

    form.value = {
      id: config.id,
      lovCode: config.lovCode ?? '',
      name: config.name ?? '',
      type: config.type ?? 'ENUM',
      source: config.source ?? 'MANUAL',
      valueField: config.valueField ?? '',
      labelField: config.labelField ?? '',
      enabled: config.enabled ?? true,
      enumItems: config.enumItems ?? [],
      listConfig: config.listConfig ?? emptyListConfig(),
      searchFields: config.searchFields ?? [],
      tableColumns: config.tableColumns ?? [],
    };

    activeTab.value =
      form.value.type === 'ENUM'
        ? 'enumItems'
        : form.value.searchFields.length > 0
          ? 'searchFields'
          : 'listConfig';
  } catch (err: any) {
    ElMessage.error(err?.message || t('lovConfig.loadFailed'));
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const body = { ...form.value, id: props.lovDefId };
    await cubeApi.client.request({ url: '/Admin/Lov/SaveConfig', method: 'post', data: body });
    ElMessage.success(t('lovConfig.saveSuccess'));
    emit('saved');
    close();
  } catch (err: any) {
    ElMessage.error(err?.message || t('lovConfig.saveFailed'));
  } finally {
    saving.value = false;
  }
}

function close() {
  emit('update:modelValue', false);
}

watch(
  () => props.modelValue,
  (val) => {
    if (val) loadConfig();
  },
);
</script>

<style scoped>
.config-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.header-name {
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.config-form {
  max-width: 640px;
}

.form-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

.text-muted {
  color: var(--el-text-color-placeholder);
}

.mt-2 {
  margin-top: 12px;
}
</style>
