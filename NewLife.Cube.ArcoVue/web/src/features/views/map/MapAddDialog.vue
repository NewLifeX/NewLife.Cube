<template>
  <a-modal
    :visible="visible"
    :hide-title="true"
    :width="440"
    :mask="false"
    modal-class="map-add-dialog"
    :ok-loading="saving"
    ok-text="保存"
    :body-style="{ maxHeight: '480px', overflowY: 'auto' }"
    @open="drag.onOpen"
    @close="drag.onClose"
    @ok="emit('submit')"
    @cancel="emit('update:visible', false)"
  >
    <a-form :model="model" layout="vertical">
      <!-- 必填项（轻量表单仅展示必填字段，不含坐标字段） -->
      <a-form-item
        v-for="f in fields"
        :key="f.name"
        :field="f.name"
        :label="f.displayName || f.name"
        required
      >
        <FieldInput
          :field="f"
          :model-value="model[f.name]"
          :type-path="typePath"
          :record-id="0"
          @update:model-value="(v: unknown) => (model[f.name] = v)"
        />
      </a-form-item>
      <!-- 位置信息：单输入框「经度,纬度」（必填，未选择/无效不允许保存）；支持在地图上点击拾取自动回填 -->
      <template v-if="coordEnabled">
        <a-form-item label="位置信息（经纬度）" required class="map-add__coord">
          <div class="map-add__hint">在地图上点击即可选取位置（自动回填坐标）</div>
          <a-input
            :model-value="coordText"
            placeholder="经度,纬度（如 114.5,23.5）"
            allow-clear
            @update:model-value="(v: string) => emit('update:coordText', v)"
          />
        </a-form-item>
      </template>
      <div v-if="!fields.length && !coordEnabled" class="map-add__hint">
        该实体没有必填项，可直接保存
      </div>
    </a-form>
  </a-modal>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import FieldInput from '@/components/FieldInput.vue';
import { useMapAddDrag } from './useMapAddDrag';

defineProps<{
  visible: boolean;
  /** 保存中（禁用确定按钮） */
  saving?: boolean;
  /** 必填字段（不含坐标字段） */
  fields: FieldMeta[];
  /** 是否显示位置信息区（实体有坐标字段） */
  coordEnabled: boolean;
  /** 位置信息单输入框文本（「经度,纬度」；由父级维护，拾取回填也写入此处） */
  coordText: string;
  /** 表单模型（子组件直接写回其属性） */
  model: Record<string, unknown>;
  /** 上传类字段的实体路径 */
  typePath?: string;
}>();

// 任意位置按住拖动（去标题栏后 Arco draggable 失效，自实现；打开重置、关闭清理）
const drag = useMapAddDrag();

const emit = defineEmits<{
  'update:visible': [v: boolean];
  'update:coordText': [v: string];
  submit: [];
}>();
</script>

<style scoped>
/* 位置提示行（标签下方、输入框上方） */
.map-add__hint {
  margin: 0 0 6px;
  font-size: 12px;
  line-height: 18px;
  color: var(--color-text-3);
}
/* Arco 对无 field 的表单项内容区加 -flex（并排、禁止换行），会把提示行与输入框压缩在同一行；
   改为块布局，使提示信息与输入框各占整行 */
.map-add__coord :deep(.arco-form-item-content-flex) {
  display: block;
}
/* 位置区为表单最后一项：压缩其下边距，减小输入框与底部分隔线的间隙 */
.map-add__coord {
  margin-bottom: 8px;
}
</style>

<style>
/* 无遮罩弹层的容器/包裹层仍会全屏拦截地图点击（拾取坐标失效）；
   依次放行：container → wrapper 置 none，仅弹窗本体可交互 */
.arco-modal-container:has(.map-add-dialog),
.arco-modal-wrapper:has(.map-add-dialog) {
  pointer-events: none;
}
.arco-modal-wrapper:has(.map-add-dialog) .map-add-dialog {
  pointer-events: auto;
  /* 任意位置拖动：--map-add-dx/dy 由 useMapAddDrag 写入（transform 平移，不破坏 wrapper 居中） */
  transform: translate(var(--map-add-dx, 0px), var(--map-add-dy, 0px));
}
/* 弹层去标题栏后：压缩底部留白与分隔线上下间距 */
.map-add-dialog .arco-modal-body {
  padding-bottom: 8px;
}
.map-add-dialog .arco-modal-footer {
  padding-top: 12px;
}
</style>
