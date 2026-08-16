<template>
  <a-drawer
    :visible="visible"
    :width="width"
    unmount-on-close
    placement="right"
    class="auto-center-drawer"
    :footer="editing"
    @update:visible="onVisible"
  >
    <template #title>
      <div class="auto-center-title">
        <a-button v-if="editing" type="text" size="mini" class="auto-center-back" @click="backToList">
          <template #icon><icon-park type="left" /></template>
        </a-button>
        <icon-park v-else type="robot-one" class="auto-center-title__icon" />
        <span>{{ title }}</span>
      </div>
    </template>

    <AutomationEditor
      v-if="editing && editorId != null"
      ref="editorRef"
      :type-path="typePath"
      :fields="fields"
      :edit-id="editorId"
      @saved="backToList"
      @back="backToList"
      @footer-change="onEditorFooter"
    />

    <a-spin v-else :loading="loading" class="auto-center-spin">
      <div class="auto-center-section">
        <div class="auto-center-section__head">流程</div>
        <div class="auto-card-grid">
          <button type="button" class="auto-card auto-card--create" @click="createNew">
            <icon-park type="plus" class="auto-card__plus" />
            <span>创建自定义流程</span>
          </button>

          <div
            v-for="row in list"
            :key="row.id"
            class="auto-card"
            @click="openEdit(row)"
          >
            <div class="auto-card__flow">
              <icon-park :type="TRIGGER_ICON[row.triggerKind] || 'lightning'" class="auto-card__ico" />
              <span class="auto-card__arrow">→</span>
              <icon-park type="message" class="auto-card__ico auto-card__ico--act" />
            </div>
            <div class="auto-card__title">{{ cardTitle(row) }}</div>
            <div class="auto-card__desc">{{ cardDesc(row) }}</div>
            <div class="auto-card__last">{{ lastRunText(row) }}</div>
            <div class="auto-card__foot" @click.stop>
              <a-switch
                size="small"
                :model-value="row.enable"
                @change="(v: string | number | boolean) => onToggleEnable(row, v)"
              />
              <a-button type="text" status="danger" size="mini" @click="removeRow(row)">删除</a-button>
            </div>
          </div>
        </div>
      </div>
    </a-spin>

    <!-- 与默认「取消/确定」同位置；语义上由「仅保存/保存并启用」承担，故去掉后者 -->
    <template v-if="editing" #footer>
      <div class="auto-drawer-foot">
        <div class="auto-drawer-foot__summary">{{ editSummary }}</div>
        <a-space>
          <a-button :loading="editSaving" @click="footerSaveOnly">仅保存</a-button>
          <a-button type="primary" :loading="editSaving" @click="footerSaveAndEnable">保存并启用</a-button>
        </a-space>
      </div>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import AutomationEditor from './AutomationEditor.vue';
import { useAutomationDrawer } from './useAutomationDrawer';

const props = defineProps<{
  visible: boolean;
  typePath: string;
  fields: FieldMeta[];
}>();

const emit = defineEmits<{ 'update:visible': [boolean] }>();

const {
  list,
  loading,
  editorId,
  editing,
  width,
  title,
  editorRef,
  editSaving,
  editSummary,
  footerSaveOnly,
  footerSaveAndEnable,
  onEditorFooter,
  onVisible,
  createNew,
  openEdit,
  backToList,
  onToggleEnable,
  removeRow,
  cardTitle,
  cardDesc,
  lastRunText,
  TRIGGER_ICON,
} = useAutomationDrawer(props, emit);
</script>

<style scoped>
/* 字号 / 字重 / 圆角消费 theme/tokens.ts（OSC-0007），勿硬编码 px 字号与品牌色 */
.auto-center-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: var(--cube-font-size-title);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.auto-center-spin {
  display: block;
  width: 100%;
  max-width: 100%;
  min-width: 0;
}
.auto-center-title__icon {
  color: rgb(var(--primary-6));
}
.auto-center-back {
  margin-left: calc(var(--cube-density-gap) / -2);
}
.auto-center-section {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  box-sizing: border-box;
}
.auto-center-section__head {
  margin-bottom: var(--cube-density-gap);
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.auto-card-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--cube-density-gap);
  width: 100%;
  max-width: 100%;
  min-width: 0;
  box-sizing: border-box;
}
@media (max-width: 560px) {
  .auto-card-grid {
    grid-template-columns: 1fr;
  }
}
.auto-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  max-width: 100%;
  width: auto;
  min-height: 148px;
  padding: var(--cube-density-gap);
  box-sizing: border-box;
  overflow: hidden;
  text-align: left;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-medium);
  cursor: pointer;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.auto-card:hover {
  border-color: rgb(var(--primary-6));
  box-shadow: 0 2px 8px color-mix(in srgb, var(--color-text-1) 8%, transparent);
}
.auto-card--create {
  align-items: center;
  justify-content: center;
  color: rgb(var(--primary-6));
  border-style: dashed;
  background: var(--color-fill-1);
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  white-space: normal;
}
.auto-card__plus {
  font-size: var(--cube-font-size-title);
  margin-bottom: 4px;
  transform: scale(1.5);
  transform-origin: center;
}
.auto-card__flow {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--color-text-2);
}
.auto-card__ico {
  font-size: var(--cube-font-size-title);
}
.auto-card__ico--act {
  color: rgb(var(--primary-6));
}
.auto-card__arrow {
  color: var(--color-text-3);
  font-size: var(--cube-font-size-meta);
}
.auto-card__title {
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  line-height: 1.4;
  overflow: hidden;
  word-break: break-word;
}
.auto-card__desc,
.auto-card__last {
  overflow: hidden;
  word-break: break-word;
}
.auto-card__desc {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  line-height: 1.5;
}
.auto-card__last {
  flex: 1;
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  line-height: 1.4;
}
.auto-card__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-top: 4px;
  min-width: 0;
}
.auto-drawer-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--cube-density-gap);
  width: 100%;
}
.auto-drawer-foot__summary {
  flex: 1;
  min-width: 0;
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  line-height: 1.5;
  text-align: left;
}
</style>

<style>
/* drawer 挂到 body：与 RecordDrawer 一致，用全局类约束 teleported 容器 */
.auto-center-drawer .arco-drawer-body {
  overflow-x: hidden;
  max-width: 100%;
  box-sizing: border-box;
  background: var(--color-fill-2);
}
.auto-center-drawer .arco-drawer-body > .arco-spin {
  display: block;
  width: 100%;
  max-width: 100%;
  min-width: 0;
}
</style>
