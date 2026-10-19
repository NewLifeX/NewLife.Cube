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
import { computed, toRefs } from 'vue';
import FormContent from '../../views/components/FormContent.vue';
import { dialogShapeFor } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';

const props = defineProps<{ ctx: CubeEngineContext }>();

/**
 * 上下文里的 Ref 先解包再绑（`docs/architecture/cube-engine.md` §8.7）：
 * 模板里直接写 `v-model="ctx.dialogVisible.value"` 是在改 prop 上的属性，
 * 会被 `vue/no-mutating-props` 拦下；解包后本地 ref 仍指向同一个 Ref，
 * 写入照样落到引擎，语义不变而依赖方向干净。
 */
const { dialogVisible, dialogTitle, formLoading, formFields, submitting } = toRefs(props.ctx);

const readonly = computed(() => props.ctx.formMode.value === 'view');
const shape = computed(() => dialogShapeFor(props.ctx.formFields.value.length));
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
      :disabled="readonly"
      @update:model-value="onModelInput"
    />

    <template #footer>
      <el-button @click="ctx.closeDialog()">取消</el-button>
      <el-button v-if="readonly" type="primary" @click="ctx.closeDialog()">关闭</el-button>
      <el-button
        v-else
        type="primary"
        :loading="submitting"
        @click="ctx.submitForm()"
      >
        保存
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.ctfd :deep(.el-dialog__body) {
  padding-top: 8px;
}
</style>
