<template>
  <div class="record-card" @dblclick="$emit('detail', record)">
    <div class="record-card-title">{{ title }}</div>
    <div v-if="imageUrl" class="record-card-image">
      <img :src="imageUrl" alt="" />
    </div>
    <div class="record-card-fields">
      <div v-for="item in bodyFields" :key="item.key" class="record-card-field">
        <span class="label">{{ item.label }}</span>
        <span class="value">{{ item.value }}</span>
      </div>
    </div>
    <div class="record-card-ops">
      <a-button v-if="canViewDetail" size="mini" @click.stop="$emit('detail', record)">详情</a-button>
      <a-button v-if="canEdit" size="mini" @click.stop="$emit('edit', record)">编辑</a-button>
      <a-button
        v-if="canDelete"
        size="mini"
        status="danger"
        @click.stop="$emit('delete', record)"
      >
        删除
      </a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  record: Record<string, unknown>;
  title: string;
  imageUrl?: string;
  bodyFields: { key: string; label: string; value: string }[];
  canViewDetail: boolean;
  canEdit: boolean;
  canDelete: boolean;
}>();

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
}>();
</script>

<style scoped>
.record-card {
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  padding: 12px;
  background: var(--color-bg-2);
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}
.record-card-title {
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text-1);
  word-break: break-all;
}
.record-card-image {
  width: 100%;
  max-height: 140px;
  overflow: hidden;
  border-radius: 6px;
  background: var(--color-fill-1);
}
.record-card-image img {
  width: 100%;
  height: 140px;
  object-fit: cover;
  display: block;
}
.record-card-fields {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px 12px;
  font-size: 12px;
}
.record-card-field {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.record-card-field .label {
  color: var(--color-text-3);
}
.record-card-field .value {
  color: var(--color-text-1);
  word-break: break-all;
}
.record-card-ops {
  display: flex;
  gap: 6px;
  margin-top: 4px;
}
</style>
