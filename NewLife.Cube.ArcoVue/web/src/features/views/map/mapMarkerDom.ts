/**
 * 地图标记 DOM（OSC-261004d7f4）
 *
 * 标记视觉 = IconPark 图标（分类规则命中）+ 分类颜色，供三端覆盖物 content 复用。
 * 样式类 `cube-map-marker` 定义在 MapView.vue 的全局样式中（SDK 管理的 DOM 在组件树外）。
 */
import { h, render } from 'vue';
import { FALLBACK_ICON, ICON_COMPONENTS } from '@/core/utils/iconComponents';
import { MARKER_SIZE } from './mapAdapter';
import type { MapMarkerStyle } from './mapPoints';

/** 创建标记元素（IconPark 图标 + 颜色；未知图标名回落默认图标） */
export function createMarkerElement(style: MapMarkerStyle): HTMLElement {
  const el = document.createElement('div');
  el.className = 'cube-map-marker';
  el.style.width = `${MARKER_SIZE}px`;
  el.style.height = `${MARKER_SIZE}px`;
  if (style.color) el.style.color = style.color;
  const icon = ICON_COMPONENTS[style.icon] ?? FALLBACK_ICON;
  render(h(icon, { size: MARKER_SIZE - 8, strokeWidth: 3 }), el);
  return el;
}
