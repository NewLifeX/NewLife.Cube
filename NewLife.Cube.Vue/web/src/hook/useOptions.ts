import { storeToRefs } from "pinia";
import { Ref, ref, toRef, toRefs, watch } from "vue";
import { usePageApi } from "../api/page";
import { useEnumOptions } from "../stores/enumOptions";
import { OptionEmits, OptionProps } from "../utils/optionProps";

export default function useOptions (props: OptionProps, emits: OptionEmits) {
  const enumOptions = useEnumOptions()
  // console.log('enumOptions', toRef(enumOptions.options, props.storeKey), props.storeKey)
  const myOptions = props.storeKey ? (toRef(enumOptions.options, props.storeKey) as Ref<EmptyObjectType[] | undefined>) : ref(props.options);
  watch(() => props.options, (val: EmptyArrayType) => {
    myOptions.value = val
  })
  if (props.storeKey) {
    enumOptions.setOptions(props.storeKey).then((res: EmptyObjectType) => {
      emits('optionRequestAfter', myOptions)
    })
  } else if (props.api) {
    props.api().then((res: EmptyObjectType) => {
      myOptions.value = props.resultKey ? getByKey(res, props.resultKey) : res
      emits('optionRequestAfter', myOptions)
    })
  } else if (props.url) {
    usePageApi().getTableData(props.url, { pageIndex: 0 }).then(res => {
      myOptions.value = props.resultKey ? getByKey(res, props.resultKey) : res
      emits('optionRequestAfter', myOptions)
    })
  }
  return {
    myOptions
  }
}

function getByKey (data: {[k in string]: any}, key: string): any {
  return key.split('.').reduce((total, item) => total[item], data)
}