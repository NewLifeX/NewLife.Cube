<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { ElTooltip } from 'element-plus';

defineOptions({
  name: 'TextOverflow',
});

interface Props {
  /**
   * 要显示的文本内容
   */
  text: string;
  /**
   * tooltip显示位置
   * @default 'top'
   */
  tooltipPlacement?:
    | 'top'
    | 'top-start'
    | 'top-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'left'
    | 'left-start'
    | 'left-end'
    | 'right'
    | 'right-start'
    | 'right-end';
  /**
   * 是否始终显示tooltip，无论是否溢出
   * @default false
   */
  alwaysShowTooltip?: boolean;
  /**
   * 自定义class
   */
  class?: string;
  /**
   * 检测溢出的延迟时间（毫秒）
   * @default 0
   */
  checkDelay?: number;
}

const props = withDefaults(defineProps<Props>(), {
  tooltipPlacement: 'top',
  alwaysShowTooltip: false,
  customClass: '',
  checkDelay: 0,
});

const textRef = ref<HTMLElement | null>(null);
const isTextOverflow = ref(false);

/**
 * 检查文本是否溢出
 */
const checkTextOverflow = () => {
  if (!textRef.value) return;

  const element = textRef.value;
  // 通过比较scrollWidth和clientWidth判断是否溢出
  isTextOverflow.value = element.scrollWidth > element.clientWidth;
};

/**
 * 初始化时检查是否溢出
 */
onMounted(async () => {
  // 添加延迟，确保DOM已完全渲染
  await nextTick();

  if (props.checkDelay > 0) {
    setTimeout(checkTextOverflow, props.checkDelay);
  } else {
    checkTextOverflow();
  }

  // 监听窗口大小变化，重新检测溢出状态
  window.addEventListener('resize', checkTextOverflow);
});

/**
 * 判断是否需要显示tooltip
 */
const shouldShowTooltip = () => {
  return props.alwaysShowTooltip || isTextOverflow.value;
};
</script>

<template>
  <div class="text-overflow-container" :class="props.class">
    <ElTooltip
      :content="props.text"
      :disabled="!shouldShowTooltip()"
      :placement="props.tooltipPlacement"
      :enterable="false"
      popper-class="text-overflow-tooltip"
    >
      <div ref="textRef" class="text-overflow-content">
        {{ props.text }}
      </div>
    </ElTooltip>
  </div>
</template>

<style lang="scss" scoped>
.text-overflow-container {
  width: 100%;
  display: inline-block;
}

.text-overflow-content {
  width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

:deep(.text-overflow-tooltip) {
  max-width: 300px;
  word-break: break-word;
}
</style>
