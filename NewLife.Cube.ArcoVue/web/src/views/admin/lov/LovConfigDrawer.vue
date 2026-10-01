<template>
  <a-drawer
    :visible="visible"
    :title="title"
    :width="width"
    placement="right"
    unmount-on-close
    @cancel="close"
    @update:visible="(v: boolean) => $emit('update:visible', v)"
  >
    <a-spin :loading="loading" style="display: block">
      <a-alert v-if="error" type="warning" show-icon>{{ error }}</a-alert>
      <a-empty
        v-else-if="!loading && !canSaveConfigType"
        description="该类型不能在此配置"
      />
      <template v-else-if="!loading && !error">
        <div class="lov-cfg-header">
          <a-tag :color="type === 'ENUM' ? 'arcoblue' : 'green'" size="small">
            {{ typeLabel }}
          </a-tag>
          <code>{{ lovCode }}</code>
          <span class="lov-cfg-name">{{ name }}</span>
          <a-tag v-if="source === 'AUTO'" color="gray" size="small">自动</a-tag>
          <a-tag v-else size="small">手工</a-tag>
        </div>

        <!-- ENUM -->
        <template v-if="type === 'ENUM'">
          <a-alert
            v-if="source === 'AUTO'"
            type="warning"
            show-icon
            class="lov-cfg-alert"
            :closable="false"
          >
            该值集由代码自动管理(C#枚举)，枚举值不可手工编辑
          </a-alert>
          <a-table :data="enumItems" :pagination="false" row-key="sort" size="small">
            <template #columns>
              <a-table-column title="排序" :width="88">
                <template #cell="{ record }">
                  <a-input-number
                    v-model="record.sort"
                    :disabled="!canEdit || source === 'AUTO'"
                    :min="0"
                    size="mini"
                  />
                </template>
              </a-table-column>
              <a-table-column title="值(Value)" :width="140">
                <template #cell="{ record }">
                  <a-input
                    v-model="record.value"
                    :disabled="!canEdit || source === 'AUTO'"
                    placeholder="value"
                    size="mini"
                  />
                </template>
              </a-table-column>
              <a-table-column title="标签(Label)">
                <template #cell="{ record }">
                  <a-input
                    v-model="record.label"
                    :disabled="!canEdit || source === 'AUTO'"
                    placeholder="label"
                    size="mini"
                  />
                </template>
              </a-table-column>
              <a-table-column title="启用" :width="70">
                <template #cell="{ record }">
                  <a-switch v-model="record.enabled" :disabled="!canEdit || source === 'AUTO'" />
                </template>
              </a-table-column>
              <a-table-column v-if="canEdit && source !== 'AUTO'" title="操作" :width="72">
                <template #cell="{ rowIndex }">
                  <a-button type="text" status="danger" size="mini" @click="removeEnumRow(rowIndex)">
                    删除
                  </a-button>
                </template>
              </a-table-column>
            </template>
          </a-table>
          <a-button
            v-if="canEdit && source !== 'AUTO'"
            type="outline"
            class="lov-cfg-add"
            @click="addEnumRow"
          >
            + 新增枚举值
          </a-button>
        </template>

        <!-- LIST：三 Tab，对齐 Cube.Vue -->
        <a-tabs v-else-if="type === 'LIST'" v-model:active-key="activeTab">
          <a-tab-pane key="listConfig" title="列表配置">
            <a-form :model="listConfig" layout="vertical" class="lov-cfg-form">
              <a-form-item>
                <template #label>
                  <div class="lov-cfg-field-label">
                    <span>请求地址</span>
                    <div class="lov-cfg-tip">仅后端可见，不下发到前端</div>
                  </div>
                </template>
                <a-input
                  v-model="listConfig.requestUrl"
                  :disabled="!canEdit"
                  placeholder="/api/v1/xxx"
                  allow-clear
                />
              </a-form-item>
              <a-form-item label="请求方式">
                <a-radio-group v-model="listConfig.method" :disabled="!canEdit">
                  <a-radio value="GET">GET</a-radio>
                  <a-radio value="POST">POST</a-radio>
                </a-radio-group>
              </a-form-item>
              <a-form-item>
                <template #label>
                  <div class="lov-cfg-field-label">
                    <span>是否代理请求</span>
                    <div class="lov-cfg-tip">
                      开启后由后端 <code>/Admin/Lov/ListData</code> 代理转发外部数据源（地址对前端不可见、规避跨域）；关闭则由前端直接请求上方「请求地址」。
                      <span v-if="sameAppUrl" class="lov-cfg-warn">
                        请求地址以 / 开头（同源同应用），已强制前端直连，不可开启代理。
                      </span>
                    </div>
                  </div>
                </template>
                <a-switch
                  v-model="listConfig.proxyRequest"
                  :disabled="!canEdit || sameAppUrl"
                />
              </a-form-item>
              <a-form-item label="是否分页">
                <a-switch v-model="listConfig.pageable" :disabled="!canEdit" />
              </a-form-item>
              <template v-if="listConfig.pageable">
                <a-form-item label="页码参数名">
                  <a-input
                    v-model="listConfig.pageNumField"
                    :disabled="!canEdit"
                    placeholder="pageNo"
                  />
                </a-form-item>
                <a-form-item label="每页条数参数名">
                  <a-input
                    v-model="listConfig.pageSizeField"
                    :disabled="!canEdit"
                    placeholder="pageSize"
                  />
                </a-form-item>
              </template>
              <a-form-item>
                <template #label>
                  <div class="lov-cfg-field-label">
                    <span>数据路径</span>
                    <div class="lov-cfg-tip">数据列表所在的 JSON 路径，如 data.list 或直接 data</div>
                  </div>
                </template>
                <a-input v-model="listConfig.dataPath" :disabled="!canEdit" placeholder="data" />
              </a-form-item>
              <a-form-item>
                <template #label>
                  <div class="lov-cfg-field-label">
                    <span>总数路径</span>
                    <div class="lov-cfg-tip">总记录数所在的 JSON 路径，如 page.totalCount</div>
                  </div>
                </template>
                <a-input
                  v-model="listConfig.totalPath"
                  :disabled="!canEdit"
                  placeholder="page.totalCount"
                />
              </a-form-item>
              <a-form-item label="固定参数(JSON)">
                <a-textarea
                  v-model="listConfig.fixedParams"
                  :disabled="!canEdit"
                  :auto-size="{ minRows: 3, maxRows: 6 }"
                  placeholder='{ "status": "active" }'
                />
              </a-form-item>
            </a-form>
          </a-tab-pane>

          <a-tab-pane key="searchFields" title="搜索字段">
            <a-table
              class="lov-cfg-grid"
              :data="searchFields"
              :pagination="false"
              row-key="sort"
              size="small"
              :scroll="{ x: 720 }"
              :bordered="false"
            >
              <template #columns>
                <a-table-column title="排序" :width="88">
                  <template #cell="{ record }">
                    <a-input-number v-model="record.sort" :disabled="!canEdit" :min="0" size="mini" />
                  </template>
                </a-table-column>
                <a-table-column title="字段名" data-index="field" :width="110" :ellipsis="true" />
                <a-table-column title="显示标题" data-index="title" :width="100" :ellipsis="true" />
                <a-table-column title="控件类型" :width="100">
                  <template #cell="{ record }">
                    <a-tag size="small">{{ record.componentType }}</a-tag>
                  </template>
                </a-table-column>
                <a-table-column title="关联值集" :width="140" :ellipsis="true">
                  <template #cell="{ record }">
                    <code v-if="record.refLovCode">{{ record.refLovCode }}</code>
                    <span v-else class="lov-cfg-muted">—</span>
                  </template>
                </a-table-column>
                <a-table-column v-if="canEdit" title="操作" :width="120" :fixed="'right'">
                  <template #cell="{ rowIndex }">
                    <a-button type="text" size="mini" @click="openSearchField(rowIndex)">编辑</a-button>
                    <a-button
                      type="text"
                      status="danger"
                      size="mini"
                      @click="removeSearchField(rowIndex)"
                    >
                      删除
                    </a-button>
                  </template>
                </a-table-column>
              </template>
            </a-table>
            <a-button v-if="canEdit" type="outline" class="lov-cfg-add" @click="addSearchField">
              + 新增搜索字段
            </a-button>
          </a-tab-pane>

          <a-tab-pane key="tableColumns" title="表格列">
            <a-table
              class="lov-cfg-grid"
              :data="tableColumns"
              :pagination="false"
              row-key="sort"
              size="small"
              :scroll="{ x: 780 }"
              :bordered="false"
            >
              <template #columns>
                <a-table-column title="排序" :width="88">
                  <template #cell="{ record }">
                    <a-input-number v-model="record.sort" :disabled="!canEdit" :min="0" size="mini" />
                  </template>
                </a-table-column>
                <a-table-column title="字段名" data-index="field" :width="110" :ellipsis="true" />
                <a-table-column title="标题" data-index="title" :width="90" :ellipsis="true" />
                <a-table-column title="宽度" data-index="width" :width="72" />
                <a-table-column title="对齐" data-index="align" :width="80" />
                <a-table-column title="关联值集" :width="140" :ellipsis="true">
                  <template #cell="{ record }">
                    <code v-if="record.refLovCode">{{ record.refLovCode }}</code>
                    <span v-else class="lov-cfg-muted">—</span>
                  </template>
                </a-table-column>
                <a-table-column v-if="canEdit" title="操作" :width="120" :fixed="'right'">
                  <template #cell="{ rowIndex }">
                    <a-button type="text" size="mini" @click="openTableColumn(rowIndex)">编辑</a-button>
                    <a-button
                      type="text"
                      status="danger"
                      size="mini"
                      @click="removeTableColumn(rowIndex)"
                    >
                      删除
                    </a-button>
                  </template>
                </a-table-column>
              </template>
            </a-table>
            <a-button v-if="canEdit" type="outline" class="lov-cfg-add" @click="addTableColumn">
              + 新增列
            </a-button>
          </a-tab-pane>
        </a-tabs>
      </template>
    </a-spin>

    <template #footer>
      <a-space>
        <a-button @click="close">取消</a-button>
        <a-button type="primary" :loading="saving" :disabled="!saveEnabled" @click="save">
          保存配置
        </a-button>
      </a-space>
    </template>
  </a-drawer>

  <!-- 搜索字段编辑 -->
  <a-modal
    v-model:visible="sfVisible"
    title="编辑搜索字段"
    :width="480"
    unmount-on-close
    @ok="confirmSearchField"
  >
    <a-form :model="sfForm" layout="vertical" class="lov-cfg-form">
      <a-form-item label="字段名" required>
        <a-input v-model="sfForm.field" placeholder="如 deptName" />
      </a-form-item>
      <a-form-item label="显示标题">
        <a-input v-model="sfForm.title" placeholder="如 部门名称" />
      </a-form-item>
      <a-form-item>
        <template #label>
          <div class="lov-cfg-field-label">
            <span>控件类型</span>
            <div class="lov-cfg-tip">select / lov 时需填写关联值集</div>
          </div>
        </template>
        <a-select
          v-model="sfForm.componentType"
          @change="() => { sfForm.refLovCode = ''; }"
        >
          <a-option value="input">文本输入 (input)</a-option>
          <a-option value="select">下拉选择 (select)</a-option>
          <a-option value="lov">值集选择 (lov)</a-option>
        </a-select>
      </a-form-item>
      <a-form-item
        v-if="sfForm.componentType === 'select' || sfForm.componentType === 'lov'"
      >
        <template #label>
          <div class="lov-cfg-field-label">
            <span>关联值集</span>
            <div class="lov-cfg-tip">如 Enum.Department，用于渲染下拉选项</div>
          </div>
        </template>
        <a-input v-model="sfForm.refLovCode" placeholder="如 Enum.Department" />
      </a-form-item>
      <a-form-item label="传参方式">
        <a-radio-group v-model="sfForm.paramType">
          <a-radio value="BODY">BODY</a-radio>
          <a-radio value="QUERY">QUERY</a-radio>
        </a-radio-group>
      </a-form-item>
      <a-form-item label="必填">
        <a-switch v-model="sfForm.required" />
      </a-form-item>
      <a-form-item label="默认值">
        <a-input v-model="sfForm.defaultValue" />
      </a-form-item>
    </a-form>
  </a-modal>

  <!-- 表格列编辑 -->
  <a-modal
    v-model:visible="tcVisible"
    title="编辑表格列"
    :width="480"
    unmount-on-close
    @ok="confirmTableColumn"
  >
    <a-form :model="tcForm" layout="vertical" class="lov-cfg-form">
      <a-form-item label="字段名" required>
        <a-input v-model="tcForm.field" placeholder="如 status" />
      </a-form-item>
      <a-form-item label="显示标题">
        <a-input v-model="tcForm.title" placeholder="如 状态" />
      </a-form-item>
      <a-form-item label="列宽(px)">
        <a-input-number v-model="tcForm.width" :min="0" />
      </a-form-item>
      <a-form-item label="对齐方式">
        <a-radio-group v-model="tcForm.align">
          <a-radio value="left">左</a-radio>
          <a-radio value="center">中</a-radio>
          <a-radio value="right">右</a-radio>
        </a-radio-group>
      </a-form-item>
      <a-form-item label="可排序">
        <a-switch v-model="tcForm.sortable" />
      </a-form-item>
      <a-form-item>
        <template #label>
          <div class="lov-cfg-field-label">
            <span>关联值集</span>
            <div class="lov-cfg-tip">选中后自动对列值做翻译</div>
          </div>
        </template>
        <a-input
          v-model="tcForm.refLovCode"
          placeholder="如 Enum.Status"
          @input="() => { tcForm.formatType = ''; }"
        />
      </a-form-item>
      <a-form-item>
        <template #label>
          <div class="lov-cfg-field-label">
            <span>格式化类型</span>
            <div class="lov-cfg-tip">与关联值集互斥，如 date / amount</div>
          </div>
        </template>
        <a-input
          v-model="tcForm.formatType"
          placeholder="如 date"
          @input="() => { tcForm.refLovCode = ''; }"
        />
      </a-form-item>
    </a-form>
  </a-modal>
</template>

<script setup lang="ts">
/**
 * 值集配置抽屉（OSC-2610019c9d）：ENUM 表 / LIST 三 Tab；对齐 Cube.Vue lov/config.vue。
 */
import { computed } from 'vue';
import { canSaveConfig, lovTypeLabel } from '@/core/utils/lovAdmin';
import { useLovConfig } from './useLovConfig';

const props = defineProps<{
  visible: boolean;
  lovId: string;
  title: string;
  width: number;
  canEdit: boolean;
}>();

const emit = defineEmits<{ 'update:visible': [boolean] }>();

const {
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
} = useLovConfig(props, (e, v) => emit(e, v));

const canSaveConfigType = computed(() => canSaveConfig(type.value));
const typeLabel = computed(() => lovTypeLabel(type.value));
</script>

<style scoped>
.lov-cfg-header {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
  font-size: 14px;
}
.lov-cfg-name {
  font-weight: 600;
}
.lov-cfg-alert {
  margin-bottom: 12px;
}
.lov-cfg-form {
  max-width: 640px;
}
.lov-cfg-add {
  margin-top: 8px;
}
/* 说明文字在标签下方、与标签左对齐（不挤在控件右侧） */
.lov-cfg-field-label {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  text-align: left;
  line-height: 1.4;
}
.lov-cfg-tip {
  margin: 0;
  font-size: 12px;
  font-weight: 400;
  color: var(--color-text-3);
  line-height: 1.45;
  white-space: normal;
}
.lov-cfg-warn {
  color: rgb(var(--orange-6));
}
.lov-cfg-muted {
  color: var(--color-text-4);
}
/* 搜索字段 / 表格列：表头与单元格不折行 */
.lov-cfg-grid :deep(.arco-table-th),
.lov-cfg-grid :deep(.arco-table-td) {
  white-space: nowrap;
}
.lov-cfg-grid :deep(.arco-table-th .arco-table-cell),
.lov-cfg-grid :deep(.arco-table-td .arco-table-cell) {
  white-space: nowrap;
}
</style>
