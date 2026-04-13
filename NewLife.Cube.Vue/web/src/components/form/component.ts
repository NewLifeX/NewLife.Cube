import { ElAutocomplete, ElCheckbox, ElColorPicker, ElDatePicker, ElDivider, ElInput, ElInputNumber, ElRadio, ElRate, ElSlider, ElSwitch, ElTimePicker, ElTimeSelect } from "element-plus";
import Select from '../select/index.vue'
import CheckboxGroup from '../checkboxGroup/index.vue'
import RadioGroup from '../radioGroup/index.vue'
import Editor from '../editor/index.vue'
import Upload from '../upload/index.vue'
import Cascader from '../cascader/index.vue'
export const forms = {
  autocomplete: ElAutocomplete,
  checkbox: ElCheckbox,
  checkboxGroup: CheckboxGroup,
  colorPicker: ElColorPicker,
  datePicker: ElDatePicker,
  input: ElInput,
  inputNumber: ElInputNumber,
  radioGroup: RadioGroup,
  radio: ElRadio,
  rate: ElRate,
  select: Select,
  slider: ElSlider,
  switch: ElSwitch,
  timePicker: ElTimePicker,
  timeSelect: ElTimeSelect,
  editor: Editor,
  divider: ElDivider,
  upload: Upload,
  cascader: Cascader,
}
