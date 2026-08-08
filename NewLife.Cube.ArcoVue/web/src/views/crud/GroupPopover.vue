<template>
  <a-popover
    :popup-visible="visible"
    position="br"
    trigger="click"
    class="group-popover"
    @popup-visible-change="onVisibleChange"
  >
    <template #content>
      <div class="group-picker">
        <div class="gp-head">
          <span class="gp-title">分组</span>
          <a-typography-text v-if="draft.length" type="secondary" class="gp-hint">
            按 {{ draft.length }} 个字段分组
          </a-typography-text>
        </div>

        <div class="gp-list">
          <div v-for="(f, i) in draft" :key="f" class="gp-item">
            <span class="gp-item-name">{{ labelOf(f) }}</span>
            <a-space :size="2" class="gp-item-ops">
              <a-button type="text" size="mini" :disabled="i === 0" @click="move(i, -1)">
                <icon-park type="up" />
              </a-button>
              <a-button type="text" size="mini" :disabled="i === draft.length - 1" @click="move(i, 1)">
                <icon-park type="down" />
              </a-button>
              <a-button type="text" size="mini" status="danger" @click="removeAt(i)">
                <icon-park type="close" />
              </a-button>
            </a-space>
          </div>

          <a-select
            v-if="candidateFields.length && draft.length < 3"
            :model-value="''"
            placeholder="+ 添加分组字段"
            allow-clear
            size="small"
            class="gp-add"
            @change="addField"
          >
            <a-option
              v-for="cf in candidateFields"
              :key="cf.name"
              :value="cf.name"
              :label="cf.displayName || cf.name"
            />
          </a-select>
          <a-empty v-if="!draft.length" description="未设置分组" :image="false" class="gp-empty" />
        </div>

        <div class="gp-foot">
          <a-button size="small" :disabled="!draft.length" @click="clearDraft">清除</a-button>
          <a-button size="small" :disabled="!canSave" @click="emitSave">保存到此视图</a-button>
          <a-space class="gp-foot-right">
            <a-button size="small" @click="close">取消</a-button>
            <a-button size="small" type="primary" @click="emitApply">应用</a-button>
          </a-space>
        </div>
      </div>
    </template>

    <template #default>
      <slot />
    </template>
  </a-popover>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import {
  groupFieldCandidates,
  pushGroupField,
  removeGroupField,
  moveGroupField,
} from '@/core/utils/viewMapping';
import { normalizeGroup, type ViewGroup } from '@/core/utils/viewProfile';

const props = defineProps<{
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewGroup;
  canSave: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  apply: [ViewGroup];
  save: [ViewGroup];
}>();

const draft = ref<ViewGroup>([]);

const labelOf = (name: string): string => {
  const f = props.fields.find((x) => x.name === name);
  return f?.displayName || name;
};

const candidateFields = computed(() => {
  const used = new Set(draft.value);
  return groupFieldCandidates(props.fields).filter((f) => !used.has(f.name));
});

function syncDraftFromProps() {
  draft.value = normalizeGroup(props.modelValue);
}

function addField(name: unknown) {
  if (typeof name !== 'string' || !name) return;
  draft.value = pushGroupField(draft.value, name);
}

function removeAt(i: number) {
  draft.value = removeGroupField(draft.value, i);
}

function move(i: number, delta: -1 | 1) {
  draft.value = moveGroupField(draft.value, i, delta);
}

function clearDraft() {
  draft.value = [];
}

function emitApply() {
  emit('apply', normalizeGroup(draft.value));
  close();
}

function emitSave() {
  emit('save', normalizeGroup(draft.value));
}

function close() {
  emit('update:visible', false);
}

function onVisibleChange(v: boolean) {
  if (v) syncDraftFromProps();
  emit('update:visible', v);
}

watch(
  () => props.visible,
  (v) => {
    if (v) syncDraftFromProps();
  },
);
</script>

<style scoped>
.group-picker {
  width: 340px;
  padding: 4px;
}
.gp-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.gp-title {
  font-weight: 600;
  font-size: 14px;
}
.gp-hint {
  font-size: 12px;
}
.gp-list {
  max-height: 280px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 8px;
}
.gp-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 8px;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
}
.gp-item-name {
  font-size: 13px;
}
.gp-item-ops {
  flex: 0 0 auto;
}
.gp-add {
  width: 100%;
}
.gp-empty {
  padding: 8px 0;
}
.gp-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  border-top: 1px solid var(--color-border-2);
  padding-top: 8px;
}
.gp-foot-right {
  margin-left: auto;
}
</style>
