/**
 * Vitest 专用 stub：@flowgram.ai/fixed-semi-materials 的传递依赖（semi-ui/icons CJS）
 * 在 node 环境解析报语法错；设计器纯函数单测不实际渲染画布，无需真实材料。
 * 仅 vitest.config alias 引用，不影响 vite dev/build（各自独立 config）。
 */
export const defaultFixedSemiMaterials: Record<string, unknown> = {};
