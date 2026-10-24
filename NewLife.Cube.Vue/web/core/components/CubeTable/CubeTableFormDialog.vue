<script setup lang="ts">
/**
 * CubeTable 新增/编辑/查看弹窗（内置默认实现）
 *
 * 与命令式 `openListFormDialog` 的区别：**声明式**，弹窗就在 `CubeTable` 组件树内，
 * 直接消费 `ctx.dialogVisible / ctx.formModel / ctx.submitForm`，
 * 不再需要 `Teleport` + 手工取数那套补偿（`docs/architecture/cube-engine.md` §8.3）。
 *
 * 表单体复用 `FormContent.vue`（字段 → 控件的唯一分发点），本组件只管容器与动作。
 */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import FormContent from '../../views/components/FormContent.vue';
import { dialogShapeFor } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';

const props = defineProps<{ ctx: CubeEngineContext }>();
const { t } = useI18n();

/**
 * 直接取引擎上下文里的 Ref 来绑（`docs/architecture/cube-engine.md` §8.7 的意图）。
 * 模板里写 `v-model="ctx.dialogVisible.value"` 会被 `vue/no-mutating-props` 拦下 ——
 * 它确实是在改 prop 对象上的属性；这里换成局部 Ref，写入照样落到引擎，依赖方向也更干净。
 * 不用 `toRefs(props.ctx)`：上下文是普通对象而不是 reactive，toRefs 会告警。
 */
const dialogVisible = props.ctx.dialogVisible;
const dialogTitle = props.ctx.dialogTitle;
const formLoading = props.ctx.formLoading;
const formFields = props.ctx.formFields;
const submitting = props.ctx.submitting;

const readonly = computed(() => props.ctx.formMode.value === 'view');
/** 页面预设的表单布局；未配置时按字段数自适应 */
const formPreset = computed(() => props.ctx.preset?.value?.form);
const shape = computed(() => {
  const base = dialogShapeFor(props.ctx.formFields.value.length);
  const columns = formPreset.value?.columns;
  return columns ? { ...base, columns } : base;
});
/** 骨架行数：字段多时不必等量渲染，8 行足够表示「在加载」 */
const skeletonRows = computed(() => Math.min(props.ctx.formFields.value.length || 4, 8));

function onModelInput(value: Record<string, unknown>): void {
  // FormContent 聚合 emit 整个对象，回写同一个响应式模型（口径同 openListFormDialog）
  Object.assign(props.ctx.formModel, value);
}
</script>

<template>
  <el-dialog
    v-model="dialogVisible"
    :title="dialogTitle"
    :width="shape.width"
    :close-on-click-modal="false"
    append-to-body
    class="ctfd"
  >
    <!-- 详情回源中：此时渲染表单会让用户先敲进默认值、再被回包覆盖（静默丢数据） -->
    <el-skeleton v-if="formLoading" :rows="skeletonRows" animated />
    <FormContent
      v-else
      :fields="formFields"
      :model-value="ctx.formModel"
      :api-prefix="ctx.apiPrefix.value"
      :columns="shape.columns"
      :group-by="formPreset?.groupBy ?? 'auto'"
      :disabled="readonly"
      @update:model-value="onModelInput"
    />

    <template #footer>
      <el-button @click="ctx.closeDialog()">{{ t('button.cancel') }}</el-button>
      <el-button v-if="readonly" type="primary" @click="ctx.closeDialog()">{{ t('button.close') }}</el-button>
      <el-button
        v-else
        type="primary"
        :loading="submitting"
        @click="ctx.submitForm()"
      >
        {{ t('button.save') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.ctfd :deep(.el-dialog__body) {
  padding-top: 8px;
}
</style>
