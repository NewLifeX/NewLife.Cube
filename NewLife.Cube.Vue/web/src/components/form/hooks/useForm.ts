import { FormInstance } from "element-plus";
import { computed, ref } from "vue";
import { FormEmits, FormProps } from "../model/form";

export default function useForm (props: FormProps, emits: FormEmits) {
  const formEl = ref<FormInstance>();
  const formValue = computed({
    get () {
      return props.modelValue;
    },
    set (val) {
      emits('update:modelValue', val);
    }
  });
  const onChange = (...props: any[]) => {
    emits('change', ...props);
  }
  const getColBind = (col?: number | Col) => {
    if (typeof col === 'number') {
      return {
        span: col
      }
    }
    return col
  }
  return {
    formEl,
    formValue,
    onChange,
    getColBind,
  }
}