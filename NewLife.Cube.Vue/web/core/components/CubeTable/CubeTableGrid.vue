<script setup lang="ts">
/**
 * CubeTable 表格区（内置默认实现）
 *
 * 两种列来源，同一时间只用一种（与 MES ListPage 的取舍一致）：
 *   - 消费方写 `#default`（自写 `el-table-column`）→ 本组件不再按元数据生成列，
 *     并把 `visibleColumns` / `resetColumnVisibility` 放进插槽作用域供 `v-if` 用；
 *   - 不写 → 按 `meta` 的 list 字段生成列。
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
 *
 * 操作列宽度按行动作数自适应（`layoutRowActions`）：显式传 `actionWidth` 则覆盖；
 * 单行动作超过 4 个时只露前 3 个，第 4 个位置是「更多」下拉，后加的操作一律收进去。
 */
import { computed, useSlots } from 'vue';
import { ElMessage } from 'element-plus';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import { ArrowDown, Delete, Edit, View } from '@element-plus/icons-vue';
import request from '../../utils/request';
import { resolveAssetUrl } from '../../utils/url';
import { isBlankCell } from '../../engine/logic';
import {
  buildColumnViews,
  cellBoolean,
  cellLinkUrl,
  cellParts,
  cellRawValue,
  cellText,
  cellTitle,
  isExpandableCell,
  linkTemplateHasValue,
  isOverflowTooltipCell,
  layoutRowActions,
  resolveGridFlags,
  splitRowActions,
} from './cubeTable.logic';
import { useCubeTableLov } from './useCubeTableLov';
import { usePagePermissions } from '../../composables/usePagePermissions';
import { resolveFieldRenderer } from '../../engine/fieldRenderers';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeColumnView, CubeRowAction, CubeTableActions } from './types';

defineOptions({ inheritAttrs: false });

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActions;
    /** 列显隐（宿主「设置列」的结果；null 表示未启用） */
    visibility?: Record<string, boolean> | null;
    /** 用户列序（宿主「设置列」的结果；空表示元数据顺序） */
    columnOrder?: string[] | null;
    /** 恢复默认列显隐（放进 #default 作用域供业务列 `v-if` 配套使用） */
    resetVisibility?: (() => void) | null;
    /** 表格高度：默认撑满 .table-wrap，交给 el-table 内部滚动 */
    height?: string | number;
    /** 勾选列 / 索引列 / 操作列 / 双击编辑：不传则跟随后端 pageSetting */
    selection?: boolean;
    indexColumn?: boolean;
    rowActions?: boolean;
    doubleClickEdit?: boolean;
    /** 操作列宽：不传按行动作数自适应 */
    actionWidth?: number;
    /** 操作列追加动作：排在内置 查看/编辑/删除 之后，一起参与自适应与「更多」收起 */
    extraRowActions?: CubeRowAction[];
  }>(),
  {
    visibility: null,
    columnOrder: null,
    resetVisibility: null,
    height: '100%',
    actionWidth: undefined,
    extraRowActions: () => [],
    selection: undefined,
    indexColumn: undefined,
    rowActions: undefined,
    doubleClickEdit: undefined,
  },
);

const slots = useSlots();
const permissions = usePagePermissions(props.ctx);
/** 业务页自写了列定义，元数据生成列就整块让位 */
const customColumns = computed(() => Boolean(slots.default));

const columns = computed(() =>
  buildColumnViews(props.ctx.listFields.value, props.visibility, { order: props.columnOrder }),
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
    {
      update: permissions.canUpdate.value,
      delete: permissions.canDelete.value,
    },
  ),
);
const lov = useCubeTableLov(
  () => columns.value,
  () => rows.value,
);

/** 当前页面查询参数，供链接 {page:参数名} 占位符使用 */
const pageParams = computed<Record<string, unknown>>(() => ({
  ...props.ctx.searchModel,
  q: props.ctx.keyword.value,
}));
const raw = (row: Record<string, unknown>, column: CubeColumnView) => cellRawValue(row, column);
const text = (row: Record<string, unknown>, column: CubeColumnView) =>
  cellText(column, raw(row, column), { lovLabel: lov.lovLabel, row, page: pageParams.value });
const parts = (row: Record<string, unknown>, column: CubeColumnView) =>
  cellParts(column, raw(row, column), { lovLabel: lov.lovLabel });
const blank = (row: Record<string, unknown>, column: CubeColumnView) => isBlankCell(raw(row, column));
const boolOf = (row: Record<string, unknown>, column: CubeColumnView) => cellBoolean(raw(row, column));
const hrefOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  resolveAssetUrl(String(raw(row, column) ?? ''));
const linkOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  cellLinkUrl(column, row, pageParams.value);
const titleOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  cellTitle(column, row, pageParams.value);
/** 链接模板没有有效行值时（如空 TraceId）回退普通单元格 */
const linkHasValue = (row: Record<string, unknown>, column: CubeColumnView) =>
  linkTemplateHasValue(column.field.url ?? '', row);

/** 单元格 AJAX 动作：dataAction 存在时阻止跳转，先请求再刷新列表 */
async function onCellClick(
  event: MouseEvent,
  row: Record<string, unknown>,
  column: CubeColumnView,
): Promise<void> {
  if (!column.field.dataAction) return;
  event.preventDefault();
  const url = linkOf(row, column);
  if (!url) return;

  try {
    const method = column.field.dataAction.toLowerCase() === 'post' ? 'post' : 'get';
    const res = (await request({ url, method })) as
      | { message?: string; data?: { message?: string } }
      | undefined;
    // 兼容裸对象与 ApiResponse 包裹两种返回形态
    const message = res?.message ?? res?.data?.message;
    if (message) ElMessage.success(message);
    await props.ctx.refresh();
  } catch (err) {
    // 请求层已统一提示错误，这里兜底避免未捕获 rejection
    console.error('[CubeTable] 单元格动作失败:', err);
  }
}
/** 值集多值才铺标签，单值走文本（一个孤立标签的观感比重复文本更差） */
const tagsOf = (row: Record<string, unknown>, column: CubeColumnView) =>
  column.control === 'lov' && parts(row, column).length > 1 ? parts(row, column) : [];
const iconOf = (name: string) => (ElementPlusIconsVue as Record<string, unknown>)[name] ?? null;
const slotName = (column: CubeColumnView) => `col-${column.key}`;
/** 后端下发 view 名时优先交给已注册的字段渲染器，未注册则回退内置控件 */
const fieldRendererOf = (column: CubeColumnView) => resolveFieldRenderer(column.field);

/** 内置行操作：与旧 Section 组件同口径（查看 / 编辑 / 删除），删除走确认层不直连引擎 */
const builtinRowActions = computed<CubeRowAction[]>(() => {
  const actions: CubeRowAction[] = [
    { key: '__view', label: '查看', icon: View, type: 'primary', onClick: (row) => void props.ctx.openView(row) },
  ];
  if (permissions.canUpdate.value) {
    actions.push({ key: '__edit', label: '编辑', icon: Edit, type: 'primary', onClick: (row) => void props.ctx.openEdit(row) });
  }
  if (permissions.canDelete.value) {
    actions.push({ key: '__remove', label: '删除', icon: Delete, type: 'danger', onClick: (row) => void props.actions.removeRow(row) });
  }
  return actions;
});

/** 某一行的完整动作列表：内置在前、业务追加在后，行级不可见的剔除 */
const rowActionsOf = (row: Record<string, unknown>): CubeRowAction[] => [
  ...builtinRowActions.value,
  ...(props.extraRowActions ?? []).filter((action) => !action.visible || action.visible(row)),
];

// 列宽取当前页各行动作数的最大值：个别行藏了动作不至于把「更多」挤变形；
// 列表为空时按内置三键给宽。
const actionLayout = computed(() => {
  const counts = rows.value.map((row) => rowActionsOf(row).length);
  return layoutRowActions(counts.length ? Math.max(...counts) : builtinRowActions.value.length);
});
const resolvedActionWidth = computed(() => props.actionWidth ?? actionLayout.value.width);

/** 单行拆成「直接按钮」与「收进更多」两段（按行判定，不足 4 个不硬塞空的「更多」） */
const splitOf = (row: Record<string, unknown>) => splitRowActions(rowActionsOf(row));
const onAction = (action: CubeRowAction, row: Record<string, unknown>) => void action.onClick(row);
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
        :fixed="column.fixed"
        :class-name="column.field.class"
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
          <!-- 字段级渲染器：后端声明 view，前端按名注册；未注册时回退内置控件 -->
          <component
            v-if="fieldRendererOf(column)"
            :is="fieldRendererOf(column)?.component"
            v-bind="fieldRendererOf(column)?.props"
            :field="column.field"
            :column="column"
            :value="raw(scope.row, column)"
            :row="scope.row"
            :ctx="ctx"
          />

          <!-- 列表链接：支持 {字段} 与 {page:参数} 占位符，dataAction 时走 AJAX 后刷新 -->
          <el-link
            v-else-if="column.field.url && linkHasValue(scope.row, column)"
            type="primary"
            :href="linkOf(scope.row, column) || undefined"
            :target="column.field.target === '_blank' ? '_blank' : '_self'"
            :title="titleOf(scope.row, column) || undefined"
            @click="onCellClick($event, scope.row, column)"
          >
            {{ text(scope.row, column) || column.label }}
          </el-link>

          <span v-else-if="blank(scope.row, column)" class="ctg-blank">—</span>

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

          <span v-else :title="titleOf(scope.row, column) || undefined">{{ text(scope.row, column) }}</span>
        </template>
      </el-table-column>
    </template>

    <el-table-column v-if="flags.rowActions" label="操作" :width="resolvedActionWidth" fixed="right" align="center">
      <template v-if="$slots['row-actions']" #default="scope">
        <slot name="row-actions" :row="scope.row" :index="scope.$index" />
      </template>
      <template v-else #default="scope">
        <!-- 单一元素根：el-table-column 会用空行对象试渲染一次本插槽做列注册，
             裸 v-for 的片段会被它采纳进 hidden-columns 多挂一份按钮（元素根则不参与） -->
        <span class="ctg-actions">
          <el-button
            v-for="action in splitOf(scope.row).direct"
            :key="action.key"
            link
            :type="action.type ?? 'primary'"
            :icon="action.icon"
            :disabled="action.disabled ? action.disabled(scope.row) : false"
            @click="onAction(action, scope.row)"
          >
            {{ action.label }}
          </el-button>
          <el-dropdown
            v-if="splitOf(scope.row).overflow.length"
            trigger="click"
            class="ctg-more"
            @command="(index: number) => onAction(splitOf(scope.row).overflow[index], scope.row)"
          >
            <el-button link type="primary">
              更多<el-icon><ArrowDown /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item
                  v-for="(action, index) in splitOf(scope.row).overflow"
                  :key="action.key"
                  :command="index"
                  :disabled="action.disabled ? action.disabled(scope.row) : false"
                >
                  {{ action.label }}
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </span>
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
/* 操作列单元格：按钮与「更多」同排流式布局 */
.ctg-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
/* 按钮间距统一由 flex gap 提供：取消 el-button 默认 margin-left，避免双倍间距
   把「删除」顶出列宽（与工具栏 .cta 同一口径） */
.ctg-actions :deep(.el-button + .el-button) {
  margin-left: 0;
}
/* 「更多」与直接按钮同排：下拉是行内元素，去掉它自带的基线偏移 */
.ctg-more {
  vertical-align: middle;
}
</style>
