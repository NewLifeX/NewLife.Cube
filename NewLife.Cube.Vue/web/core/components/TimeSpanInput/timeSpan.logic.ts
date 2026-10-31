/**
 * 时长换算（ItemType=TimeSpan）
 *
 * 实现放在共享包 `@newlifex/cube`（`field/index.ts` 的 timeSpan* 系列）：
 * 所有皮肤（Vue/NaiveUI/MUI/Shadcn/ArcoVue/TDesign/Vuetify/Svelte/Angular）与列表格式化
 * 共用同一份单位推断与换算，避免出现「列表显示 6 小时、编辑框按秒回显」的漂移。
 *
 * 本文件只做重新导出，保住 Vue 皮肤内部的既有导入路径。
 */
export {
  timeSpanUnitOfDescription,
  timeSpanToSeconds,
  secondsToTimeSpan,
  splitTimeSpan,
  joinTimeSpan,
  type TimeSpanUnit,
  type TimeSpanParts,
} from '@newlifex/cube';
