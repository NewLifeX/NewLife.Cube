<template>
  <Form :config="config" v-model="formData" v-model:visible="layoutVisible" :wrapper="wrapper" :title="myIsUpdate?'修改':'添加'" @submit="submit">
    <template v-for="item in config.filter(item => item.slot)" :key="item.prop.toString()" #[`${item.slot!}`]="data">
      <slot :name="item.slot" :model="data.model" :prop="data.prop"></slot>
    </template>
  </Form>
</template>

<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue';
import { usePageApi } from '/@/api/page';
import Form from '/@/components/form/index.vue';
import { ColumnConfig } from '../form/model/form';
import { ElMessage } from 'element-plus';
import { EditWrapper, UsePageProps } from './model';
import providePageKey from './provide/key';
interface Props {
  type?: string;
  wrapper?: EditWrapper;
  visible: boolean;
  modelValue?: EmptyObjectType;
  config: ColumnConfig[];
  isUpdate?: boolean;
}
interface Emits {
  (e: 'update:visible', val: boolean): void;
  (e: 'update:modelValue', val: EmptyObjectType): void;
  (e: 'submitSuccess'): void;
}
const props = withDefaults(defineProps<Props>(), {
  modelValue: () => ({})
});
const emits = defineEmits<Emits>();
const layoutVisible = computed({
  get () {
    return props.visible
  },
  set (val) {
    emits('update:visible', val)
  }
})
const loading = ref(false);
const formData = computed({
  get () {
    return props.modelValue || {}
  },
  set (val) {
    emits('update:modelValue', val)
  }
});
const myIsUpdate = ref(props.isUpdate);
watch(() => props.isUpdate, (val) => {
  myIsUpdate.value = val
})
const pageApi = usePageApi();
const providePage = inject(providePageKey)
const { onAddBefore, onAddAfter, onEditBefore, onEditAfter } = providePage?.pageProps || {}
let { detailConfig, addConfig, editConfig } = providePage?.pageProps || ({} as UsePageProps)

const handleAdd = () => {
  if (myIsUpdate.value) {
    formData.value = {}
  }
  myIsUpdate.value = false;
  layoutVisible.value = true;
}
const handleEdit = async (row: EmptyObjectType) => {
  formData.value = {}
  myIsUpdate.value = true;
  layoutVisible.value = true;
  loading.value = true
  let res;
  try {
    const requestProps = typeof detailConfig?.requestProps === 'function' ? detailConfig.requestProps(row) : detailConfig?.requestProps;
    if (detailConfig?.api) {
      res = await detailConfig?.api({ id: row.id, ...requestProps })
    } else if (detailConfig?.url) {
      res = await pageApi.getTableDetailByUrl(detailConfig?.url, row.id, requestProps)
    } else {
      res = await pageApi.getTableDetail(props.type!, row.id, requestProps)
    }
    loading.value = false
    formData.value = res.data
  } catch (error) {
    loading.value = false
  }
}
const submit = () => {
  async function submitFun () {
    if (myIsUpdate.value) {
      const requestProps = typeof editConfig?.requestProps === 'function' ? editConfig.requestProps(formData.value) : editConfig?.requestProps;
      if (editConfig?.api) {
        await editConfig?.api({ ...formData.value, ...requestProps })
      } else if (editConfig?.url) {
        await pageApi.setTableItem(editConfig?.url, { ...formData.value, ...requestProps })
      } else {
        await pageApi.setTableItem(props.type!, { ...formData.value, ...requestProps })
      }
      onEditAfter && onEditAfter(formData);
    } else {
      const requestProps = typeof addConfig?.requestProps === 'function' ? addConfig.requestProps(formData.value) : addConfig?.requestProps;
      if (addConfig?.api) {
        await addConfig?.api({ ...formData.value, ...requestProps })
      } else if (addConfig?.url) {
        await pageApi.addTableItem(addConfig?.url, { ...formData.value, ...requestProps })
      } else {
        await pageApi.addTableItem(props.type!, { ...formData.value, ...requestProps })
      }
      onAddAfter && onAddAfter(formData);
    }
    emits('submitSuccess')
  }
  if (myIsUpdate.value) {
    if (onEditBefore) onEditBefore(formData, submitFun)
    else submitFun()
  } else {
    if (onAddBefore) onAddBefore(formData, submitFun)
    else submitFun()
  }
}
defineExpose({
  handleAdd,
  handleEdit
})
</script>

<style scoped>

</style>
