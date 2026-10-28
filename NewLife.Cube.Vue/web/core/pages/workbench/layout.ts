/**
 * 工作台卡片布局（拖动排序 / 隐藏 / 持久化）
 *
 * 纯逻辑已收敛到 `@newlifex/cube` 的 `widget-layout`，与各 SPA 皮肤共用同一套口径；
 * 这里只做转出，保持 Vue 皮肤内既有引用路径不变。
 */

export { mergeWidgetsWithLayout, buildWidgetLayout, moveWidget, createWidgetLayoutLoader } from '@newlifex/cube';
export type { WidgetLayout, WidgetLayoutMap, LayoutWidget, RankedWidgets } from '@newlifex/cube';

import type { WorkbenchWidget } from '../../types/workbench';
import type { RankedWidgets } from '@newlifex/cube';

/** 本皮肤卡片类型下的应用布局结果 */
export type RankedWidgetView = RankedWidgets<WorkbenchWidget>;
