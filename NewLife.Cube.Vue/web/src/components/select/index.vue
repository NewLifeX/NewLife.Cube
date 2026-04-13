<template>
  <el-select v-bind="$attrs">
    <el-option v-for="item in myOptions"
      :key="item[valueKey]"
      :label="item[labelKey]"
      :disabled="item.disabled"
      :value="item[valueKey]">
    </el-option>
  </el-select>
</template>

<script setup lang="ts">
import useOptions from '/@/hook/useOptions';
import { OptionEmits, optionProps } from '/@/utils/optionProps';

const props = defineProps(optionProps);
const emits = defineEmits<OptionEmits>()
const { myOptions } = useOptions(props, emits)

</script>

<style scoped>

</style>