import { ElDialog, ElDrawer } from "element-plus";
import { computed } from "vue";
import { WrapperEmits, WrapperProps } from "../model/wrapper";


export default function useFormWrapper (props: WrapperProps, emits: WrapperEmits) {
  const layoutComponent = computed(() => {
    const is = {
      div: 'div',
      dialog: ElDialog,
      drawer: ElDrawer,
    }
    return is[props.wrapper]
  })
  const layoutBind = computed(() => {
    return props.wrapper === 'div' ? {} : {
      title: props.title,
      [props.wrapper === 'dialog' ? 'width' : 'size']: '50%',
    }
  });
  const layoutVisible = computed({
    get () {
      return props.visible
    },
    set (val) {
      emits('update:visible', val)
    }
  })
  return {
    layoutComponent,
    layoutBind,
    layoutVisible
  }
}
