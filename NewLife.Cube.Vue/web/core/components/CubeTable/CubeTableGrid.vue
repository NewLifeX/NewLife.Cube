<script setup lang="ts">
/**
 * CubeTable 表格区（内置默认实现）
 *
 * 两种列来源，同一时间只用一种（与 MES ListPage 的取舍一致）：
 *   - 消费方写 `#default`（自写 `el-table-column`）→ 本组件不再按元数据生成列，
 *     并把 `visibleColumns` / `resetColumnVisibility` 放进插槽作用域供 `v-if` 用；
 *   - 不写 → 按 `GetPage` 的 list 字段生成列。
 * 勾选列 / 索引列 / 操作列在两种模式下都按 `pageSetting`（或显式 props）渲染。
 *
 * 渲染口径沿用 `core/views/components/ListTableContent.vue`（值集翻译、图片/附件、开关、
 * 色块、图标、Json/富文本折叠），但「怎么算」全部在 `cubeTable.logic.ts` 与 `useCubeTableLov`，
 * 本组件只做绑定与分支渲染。
 *
 * 高度默认 `100%`：外层 `.table-wrap` 给出确定高度后，表头固定、只有数据区滚动，
 * 页面本身不出现滚动条（MES 列表页的滚动模型）。
 *
 * 插槽：
 *   - `#default`：自写列定义（整块接管数据列）；
 *   - `#row-actions="{ row, index }"`：整列操作按钮替换（默认 查看 / 编辑 / 删除）；
 *   - 动态 `#col-<字段名>`（宿主 CubeTable 转发）：单列单元格替换，优先于内置渲染；
 *   - `#empty`：空态替换。
 * 未声明的属性（`row-key`、`stripe`、`size` 等）透传给内部 `el-table`。
 */
import { computed, useSlots } from 'vue';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import { Delete, Edit, View } from '@element-plus/icons-vue';
import { getValueByKey, resolveAssetUrl } from '../../utils/url';
import { isBlankCell } from '../../engine/logic';
import {
  buildColumnViews,
  cellBoolean,
  cellParts,
  cellText,
  isExpandableCell,
  isOverflowTooltipCell,
  resolveGridFlags,
} from './cubeTable.logic';
import { useCubeTableLov } from './useCubeTableLov';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeColumnView, CubeTableActions } from './types';

defineOptions({ inheritAttrs: false });

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActions;
    /** 列显隐（宿主「设置列」的结果；null 表示未启用） */
    visibility?: Record<string, boolean> | null;
    /** 恢复默认列显隐（放进 #default 作用域供业务列 `v-if` 配套使用） */
    resetVisibility?: (() => void) | null;
    /** 表格高度：默认撑满 .table-wrap，交给 el-table 内部滚动 */
    height?: string | number;
    /** 勾选列 / 索引列 / 操作列 / 双击编辑：不传则跟随后端 pageSetting */
    selection?: boolean;
    indexColumn?: boolean;
    rowActions?: boolean;
    doubleClickEdit?: boolean;
    /** 操作列宽 */
    actionWidth?: number;
  }>(),
  {
    visibility: null,
    resetVisibility: null,
    height: '100%',
    actionWidth: 168,
    selection: undefined,
    indexColumn: undefined,
    rowActions: undefined,
    doubleClickEdit: undefined,
  },
);

const slots = useSlots();
/** 业务页自写了列定义，元数据生成列就整块让位 */
const customColumns = computed(() => Boolean(slots.default));

const columns = computed(() =>
  buildColumnViews(props.ctx.listFields.value, props.visibility, {
    showKeyColumn: props.ctx.pageSetting.value.enableKey !== false,
  }),
);
const rows = computed(() => props.ctx.list.value);
const flags = computed(() =>
  resolveGridFlags(
    {
      selection: props.selection,
      indexColumn: props.indexColumn,
      rowActions: props.rowActions,
      doubleClickEdit: props.doubleClickEdit,
    },
    props.ctx.pageSetting.value,
  ),
);
const lov = useCubeTableLov(
  () => columns.value,
  () => rows.value,
);

const raw = (row: Record<string, unknown>, column: CubeColumnView) => getValueByKey(row, column.key);
const text = (row: Record<string, unknown>, column: CubeColumnView) => cellText(column, raw(row, column), lov);
const parts = (row: Record<string, unknown>, column: CubeColumnView) => cellParts(column, raw(row, column), lov);
const blank = (row: Record<string, unknown>, column: CubeColumnView) => isBlankCell(raw(row, column));
const boolOf = (row: Record<string, unknown>, column: CubeColumnView) => cellBoolean(raw(row, column));
const hrefOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  resolveAssetUrl(String(raw(row, column) ?? ''));
/** 值集多值才铺标签，单值走文本（一个孤立标签的观感比重复文本更差） */
const tagsOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  column.control === 'lov' && parts(row, column).length > 1 ? parts(row, column) : [];
const iconOf = (name: string) => (ElementPlusIconsVue as Record<string, unknown>)[name] ?? null;
const slotName = (column: CubeColumnView) => `col-${column.key}`;
</script>

<template>
  <el-table
    v-bind="$attrs"
    v-loading="ctx.loading.value"
    :data="rows"
    :height="height"
    border
    class="ctg"
    @selection-change="(sel: Record<string, unknown>[]) => ctx.setSelected(sel)"
    @row-dblclick="(row: Record<string, unknown>) => flags.doubleClickEdit && void ctx.openEdit(row)"
  >
    <template #empty>
      <slot name="empty">
        <!-- 取数失败时把原因就地说明，而不是留一片空白让人以为没数据 -->
        <span class="ctg-empty">{{ ctx.error.value || '暂无数据' }}</span>
      </slot>
    </template>

    <el-table-column v-if="flags.selection" type="selection" width="46" align="center" />
    <el-table-column v-if="flags.indexColumn" type="index" label="#" width="60" align="center" />

    <!-- 业务自写列：与 MES ListPage 同一用法，列宽/渲染/操作按钮都由页面决定。
         visibleColumns / resetColumnVisibility 进作用域，供自写列做 v-if -->
    <slot
      v-if="customColumns"
      name="default"
      :visibleColumns="visibility ?? {}"
      :resetColumnVisibility="resetVisibility"
    />

    <template v-else>
      <el-table-column
        v-for="column in columns"
        :key="column.key"
        :label="column.label"
        :prop="column.key"
        :width="column.width === 'auto' ? undefined : column.width"
        :min-width="column.width === 'auto' ? 120 : undefined"
        :align="column.align"
        :show-overflow-tooltip="isOverflowTooltipCell(column.control)"
      >
        <template v-if="$slots[slotName(column)]" #default="scope">
          <slot
            :name="slotName(column)"
            :row="scope.row"
            :column="column"
            :value="raw(scope.row, column)"
            :index="scope.$index"
          />
        </template>

        <template v-else #default="scope">
          <span v-if="blank(scope.row, column)" class="ctg-blank">—</span>

          <el-image
            v-else-if="column.control === 'image'"
            class="ctg-image"
            :src="hrefOf(scope.row, column)"
            :preview-src-list="[hrefOf(scope.row, column)]"
            preview-teleported
            fit="cover"
          />

          <el-link
            v-else-if="column.control === 'file'"
            type="primary"
            :href="hrefOf(scope.row, column)"
            target="_blank"
          >
            查看文件
          </el-link>

          <el-switch
            v-else-if="column.control === 'boolean'"
            :model-value="boolOf(scope.row, column)"
            disabled
          />

          <span
            v-else-if="column.control === 'color'"
            class="ctg-color"
            :style="{ background: text(scope.row, column) || 'transparent' }"
            :title="text(scope.row, column)"
          />

          <span v-else-if="column.control === 'icon'" class="ctg-icon">
            <el-icon v-if="iconOf(text(scope.row, column))">
              <component :is="iconOf(text(scope.row, column))" />
            </el-icon>
            <template v-else>{{ text(scope.row, column) }}</template>
          </span>

          <el-popover v-else-if="isExpandableCell(column.control)" :width="360" trigger="click" placement="top">
            <template #reference>
              <span class="ctg-expandable">{{ text(scope.row, column) }}</span>
            </template>
            <div class="ctg-full">
              <span
                v-if="column.control === 'html' && column.field.itemType === 'html'"
                v-html="String(raw(scope.row, column))"
              />
              <pre v-else>{{ raw(scope.row, column) }}</pre>
            </div>
          </el-popover>

          <span v-else-if="tagsOf(scope.row, column).length" class="ctg-tags">
            <el-tag
              v-for="tag in tagsOf(scope.row, column)"
              :key="tag"
              effect="plain"
              round
              size="small"
            >
              {{ tag }}
            </el-tag>
          </span>

          <span v-else>{{ text(scope.row, column) }}</span>
        </template>
      </el-table-column>
    </template>

    <el-table-column v-if="flags.rowActions" label="操作" :width="actionWidth" fixed="right" align="center">
      <template v-if="$slots['row-actions']" #default="scope">
        <slot name="row-actions" :row="scope.row" :index="scope.$index" />
      </template>
      <template v-else #default="scope">
        <el-button link type="primary" :icon="View" @click="ctx.openView(scope.row)">查看</el-button>
        <el-button link type="primary" :icon="Edit" @click="ctx.openEdit(scope.row)">编辑</el-button>
        <el-button link type="danger" :icon="Delete" @click="actions.removeRow(scope.row)">删除</el-button>
      </template>
    </el-table-column>
  </el-table>
</template>

<style scoped>
.ctg-blank,
.ctg-empty {
  color: var(--el-text-color-secondary);
}
.ctg-image {
  width: 32px;
  height: 32px;
  border-radius: var(--el-border-radius-small);
}
.ctg-color {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-small);
  vertical-align: middle;
}
.ctg-icon {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.ctg-expandable {
  cursor: pointer;
  color: var(--el-color-primary);
}
.ctg-full {
  max-height: 260px;
  overflow: auto;
  font-size: 12px;
}
.ctg-tags {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 4px;
}
</style>
