<template>
  <a-popover
    :popup-visible="visible"
    position="bottom"
    trigger="click"
    class="sort-popover"
    @popup-visible-change="onVisibleChange"
  >
    <template #content>
      <div class="sort-picker">
        <div class="sp-head">
          <span class="sp-title">排序</span>
          <a-typography-text type="secondary" class="sp-hint">最多 3 列</a-typography-text>
        </div>
        <div class="sp-list">
          <div v-for="(row, i) in draft" :key="`${row.field}-${i}`" class="sp-item">
            <span class="sp-item-name">{{ labelOf(row.field) }}</span>
            <a-space :size="2" class="sp-item-ops">
              <a-button
                type="text"
                size="mini"
                :title="row.desc ? '降序' : '升序'"
                @click="toggleDir(i)"
              >
                <icon-park :type="row.desc ? 'alphabetical-sorting-two' : 'alphabetical-sorting'" />
              </a-button>
              <a-button type="text" size="mini" status="danger" title="删除" @click="removeAt(i)">
                <icon-park type="close" />
              </a-button>
            </a-space>
          </div>
          <a-select
            :model-value="''"
            placeholder="添加排序"
            size="small"
            class="sp-add"
            :disabled="!canAdd"
            :trigger-props="{ contentClass: 'sp-add-popup' }"
            @change="addField"
          >
            <a-option
              v-for="cf in candidateFields"
              :key="cf.name"
              :value="cf.name"
              :label="cf.displayName || cf.name"
            >
              <span class="sp-opt">
                <span class="sp-opt-name">{{ cf.displayName || cf.name }}</span>
                <span v-if="cf.indexed || cf.primaryKey" class="sp-opt-index" title="排序可走索引">
                  <icon-park type="key" :size="14" />
                </span>
              </span>
            </a-option>
          </a-select>
        </div>
        <div class="sp-foot">
          <a-button size="small" @click="close">取消</a-button>
          <a-button size="small" type="primary" @click="emitApply">应用</a-button>
        </div>
      </div>
    </template>
    <template #default>
      <slot />
    </template>
  </a-popover>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ViewSort } from '@/core/utils/viewProfile';
import { useSortPopover } from './useSortPopover';

const props = defineProps<{
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewSort[];
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  apply: [ViewSort[]];
}>();

const {
  draft,
  candidateFields,
  canAdd,
  labelOf,
  addField,
  toggleDir,
  removeAt,
  close,
  emitApply,
  onVisibleChange,
} = useSortPopover(props, emit);
</script>

<style scoped>
.sort-picker {
  width: 340px;
  padding: 4px;
}
.sp-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.sp-title {
  font-weight: 600;
  font-size: 14px;
}
.sp-hint {
  font-size: 12px;
}
.sp-list {
  max-height: 280px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 8px;
}
.sp-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 8px;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
}
.sp-item-name {
  font-size: 13px;
  min-width: 0;
}
.sp-item-ops {
  flex: 0 0 auto;
}
.sp-add {
  width: 100%;
}
.sp-opt {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}
.sp-opt-name {
  min-width: 0;
}
.sp-opt-index {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  color: var(--color-text-3);
  line-height: 0;
}
.sp-foot {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  border-top: 1px solid var(--color-border-2);
  padding-top: 8px;
}
</style>

<style>
.sp-add-popup .arco-select-option-content {
  flex: 1;
  min-width: 0;
}
</style>
