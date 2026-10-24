/**
 * parameter-kind.ts — 参数类别枚举映射
 *
 * 提取为纯函数，便于单元测试和组件复用。
 * 从后端返回的 Int32 数字映射为标签展示文案和 Element Plus tag 类型。
 */
import i18n from '@newlifex/cube-vue/core/i18n';

export interface KindOption {
  text: string;
  type: 'info' | 'warning' | '' | 'success' | 'danger';
}

/** 标签文案跟随语言：取用时才翻译 */
const t = (key: string): string => i18n.global.t(key);

/** kind 枚举映射表（text 为词条 key，展示时翻译） */
const kindMap: Record<string, { key: string; type: KindOption['type'] }> = {
  '0': { key: 'param.kindNormal', type: '' },
  '1': { key: 'param.kindSystem', type: 'warning' },
  '2': { key: 'param.kindUser', type: 'info' },
};

/**
 * 将后端 kind 数字转为标签展示
 * @param kind 后端返回的 kind 值（0/1/2 或其它）
 * @returns 包含显示文本和标签类型的对象
 */
export function getKind(kind: unknown): KindOption {
  const hit = kindMap[String(kind)];
  return hit ? { text: t(hit.key), type: hit.type } : { text: t('param.kindUnknown'), type: 'info' };
}