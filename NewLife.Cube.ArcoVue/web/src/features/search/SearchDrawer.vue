<template>
  <a-drawer
    :visible="visible"
    :width="300"
    placement="right"
    :closable="false"
    :mask-closable="true"
    :footer="false"
    unmount-on-close
    class="search-drawer"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <!-- 标题：查询组合按钮绝对定位在抽屉右上角（不依赖 Arco title 容器宽度） -->
    <template #title>
      <span class="sd-title-text">高级搜索</span>
      <div class="sd-actions">
        <QueryComboButton
          :queries="queries"
          :active-query-id="activeQueryId"
          :params-dirty="paramsDirty"
          :can-save="canSave"
          :has-more-fields="false"
          :more-field-count="0"
          :expanded="false"
          @search="$emit('search')"
          @reset="$emit('reset')"
          @apply="(id: string) => $emit('apply', id)"
          @save="(name: string) => $emit('saveQuery', name)"
          @rename="(id: string, name: string) => $emit('renameQuery', id, name)"
          @delete="(id: string) => $emit('deleteQuery', id)"
        />
      </div>
    </template>

    <a-form :model="model" layout="vertical" @submit.prevent="$emit('search')">
      <!-- Q 关键字作为第一个查询条件 -->
      <a-form-item v-if="enableKey !== false" label="关键字">
        <a-input
          :model-value="String(model.Q ?? '')"
          placeholder="全字段模糊搜索"
          allow-clear
          @update:model-value="(v: unknown) => (model.Q = v)"
          @press-enter="$emit('search')"
        />
      </a-form-item>

      <!-- 主时间范围（特殊条件，紧跟 Q 之后） -->
      <a-form-item v-if="masterTimeName" :label="masterTimeDisplayName || '时间范围'">
        <a-range-picker
          :model-value="masterTimeRange"
          show-time
          value-format="YYYY-MM-DDTHH:mm:ss"
          style="width: 100%"
          @update:model-value="onMasterTimeChange"
        />
      </a-form-item>

      <!-- 其余查询条件：按 GetPage Search 列表顺序依次排列（主时间字段不重复渲染） -->
      <a-form-item
        v-for="field in fieldItems"
        :key="field.name"
        :label="field.displayName || field.name"
      >
        <SearchFieldInput
          :field="field"
          :model-value="model[field.name]"
          :form="model"
          @update:model-value="(v) => (model[field.name] = v)"
          @update:key="(k, v) => (model[k] = v)"
          @search="$emit('search')"
        />
      </a-form-item>
    </a-form>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import SearchFieldInput from '@/components/SearchFieldInput.vue';
import QueryComboButton from './QueryComboButton.vue';
import type { FieldMeta } from '@/core/types/field';
import type { SavedQuery } from '@/core/utils/viewProfile';

defineOptions({ name: 'SearchDrawer' });

const props = defineProps<{
  /** 抽屉可见性 */
  visible: boolean;
  /** search 分区字段（GetPage Search 列表顺序） */
  fields: FieldMeta[];
  /** 搜索表单对象（父组件 reactive，直接读写其属性） */
  model: Record<string, unknown>;
  /** 主时间字段名（OSC-0016）；无 MasterTime 时不渲染主时间范围 */
  masterTimeName?: string | null;
  /** 主时间字段显示名（OSC-0016） */
  masterTimeDisplayName?: string | null;
  /** 关键字 Q 是否启用（OSC-0016）；false 时不渲染关键字框 */
  enableKey?: boolean;
  /** 预定义查询列表（OSC-0016） */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前参数与 activeQuery 是否不一致（条目 ✓ 标记控制） */
  paramsDirty: boolean;
  /** 当前参数是否可保存（非空） */
  canSave: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  search: [];
  reset: [];
  /** 应用预定义查询（OSC-0016） */
  apply: [id: string];
  /** 保存当前查询为预定义（OSC-0016） */
  saveQuery: [name: string];
  /** 重命名当前查询（OSC-0016） */
  renameQuery: [id: string, name: string];
  /** 删除预定义查询（OSC-0016） */
  deleteQuery: [id: string];
}>();

/** 其余查询条件：主时间字段不重复渲染（单独特殊控件），其余按 GetPage Search 顺序 */
const fieldItems = computed(() =>
  props.fields.filter((f) => f.name !== props.masterTimeName),
);

/** 主时间范围值：dtStart/dtEnd 两键映射 [start, end] */
const masterTimeRange = computed(() => {
  const s = props.model?.dtStart;
  const e = props.model?.dtEnd;
  return s && e ? [String(s), String(e)] : undefined;
});

/** 主时间范围变更：写 dtStart/dtEnd，清空时删除两键 */
function onMasterTimeChange(val: unknown) {
  const arr = Array.isArray(val) ? val : [];
  if (arr.length >= 2) {
    props.model.dtStart = arr[0] ?? '';
    props.model.dtEnd = arr[1] ?? '';
  } else {
    delete props.model.dtStart;
    delete props.model.dtEnd;
  }
}
</script>

<style scoped>
/* 查询组合按钮绝对定位在抽屉右上角：Arco 的 .arco-drawer-title 宽度只随内容自适应（不占满），
   用 header 相对定位 + 按钮绝对定位，保证「高级搜索」标题在左、按钮恒在右上角 */
.sd-actions {
  position: absolute;
  right: 16px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
}
.sd-title-text {
  font-size: 16px;
  font-weight: 600;
  white-space: nowrap;
}
</style>

<style>
/* class 透传到 .arco-drawer-container（无 scoped data-v 属性，:deep 无法命中），
   故用非 scoped 样式限定 .search-drawer 下的 header 相对定位，作为按钮绝对定位的包含块 */
.search-drawer .arco-drawer-header {
  position: relative;
}
</style>
