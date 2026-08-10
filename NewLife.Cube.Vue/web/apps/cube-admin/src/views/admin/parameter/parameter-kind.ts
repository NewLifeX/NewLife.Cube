/**
 * parameter-kind.ts — 参数类别枚举映射
 *
 * 提取为纯函数，便于单元测试和组件复用。
 * 从后端返回的 Int32 数字映射为中文标签和 Element Plus tag 类型。
 */

export interface KindOption {
  text: string;
  type: 'info' | 'warning' | '' | 'success' | 'danger';
}

/** kind 枚举映射表 */
const kindMap: Record<string, KindOption> = {
  '0': { text: '普通', type: '' },
  '1': { text: '系统', type: 'warning' },
  '2': { text: '用户', type: 'info' },
};

/**
 * 将后端 kind 数字转为标签展示
 * @param kind 后端返回的 kind 值（0/1/2 或其它）
 * @returns 包含显示文本和标签类型的对象
 */
export function getKind(kind: unknown): KindOption {
  return kindMap[String(kind)] ?? { text: '未知', type: 'info' };
}