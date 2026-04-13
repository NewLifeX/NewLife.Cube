<template>
  <el-cascader class="w-full" :props="caProps" v-model="caValue" />
</template>

<script setup lang="ts" name="cascader">
import { useVModel } from '@vueuse/core';
import type { CascaderProps } from 'element-plus'
import { computed, ref, useAttrs, watch } from 'vue';
import { usePageApi } from '/@/api/page';
import { deepMerge } from '/@/utils/other';
type PropApi = (...arr: any) => Promise<Array<EmptyObjectType>>
interface Props {
  // 读取store缓存
  storeKey?: string;
  // 值字段名
  valueKey?: string;
  // 名称字段名
  labelKey?: string;
  // 子集字段名
  childrenKey?: string;
  // 请求返回值字段名
  resultKey?: string;
  // 父级地段名
  parentKey?: string;
  // 用与将数组拆分赋值给多个字段
  modelKeys?: string[];
  // 绑定的值，可绑定表单对象或者属性值，当绑定表单对象时，需要搭配modelKeys一起使用
  modelValue?: any;
  // 请求方法
  api?: PropApi | Array<PropApi>
  // 请求地址
  url?: string | Array<string>
  // 总层级
  level?: number;
  // 最后以及标识字段
  leafKey?: string;
  // 请求携带的参数
  requestProps?: EmptyObjectType;
}
interface Emits {
  (e: 'update:model-value', val: any): void;
  (e: 'optionRequestAfter', val: EmptyArrayType, level: number): void;
}
const props = withDefaults(defineProps<Props>(), {
  valueKey: 'id',
  labelKey: 'name',
  resultKey: 'data',
  childrenKey: 'children',
  parentKey: 'parentId'
})
const emits = defineEmits<Emits>()

const vModel = useVModel(props, 'modelValue', emits)

const caValue = computed({
  get () {
    if (props.modelKeys) {
      let val = props.modelKeys.map(key => props.modelValue[key])
      for (let i = val.length - 1; i >= 0; i--) {
        if (!val[i]) val.splice(i, 1)
        else break;
      }
      return val
    }
    return props.modelValue
  },
  set (val: Array<string | number>) {
    let value = val || []
    if (props.modelKeys) {
      props.modelKeys.forEach((key, i) => {
        vModel.value[key] = value[i]
      })
    } else {
      vModel.value = value
    }
  }
})

const attrs = useAttrs();
const caProps = (attrs.props || {}) as CascaderProps;
if (props.api || props.url) {
  caProps.lazy = true;
  caProps.value = props.valueKey!;
  caProps.label = props.labelKey!;
  caProps.children = props.childrenKey!;
  const { getTableData } = usePageApi()
  caProps.lazyLoad = async (node, resolve) => {
    const { level, value } = node
    let res
    const requestProps = deepMerge(props.requestProps, level ? {[props.parentKey]: value} : {})
    if (props.api) {
      res = await Array.isArray(props.api) ? props.api[level](requestProps) : (props.api as PropApi)(requestProps)
    } else {
      res = await getTableData(Array.isArray(props.url) ? props.url[level] : props.url!, requestProps)
    }
    emits('optionRequestAfter', res[props.resultKey], level)
    const nodes = res[props.resultKey].map((item: EmptyObjectType) => ({
      ...item,
      leaf: props.leafKey ? item[props.leafKey] : props.level ? level >= props.level : true
    }))
    resolve(nodes)
  }
}
</script>

<style scoped>

</style>