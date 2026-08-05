// 聚合各组件的 story 变体：供 gallery 渲染、Playwright CT 截图。
//
// 为什么用「显式静态导入」而不是 import.meta.glob 自动收集？
//   glob 在 CT 的 vite（HMR 活跃）环境下会为 story 模块附加 ?t= 查询串，
//   导致模块图不稳定、偶发漏注册（表现为部分 story 的 id 不在 gallery 的 registry 中，
//   mountStory 报「未知 story」）。该 flaky 在沙箱/CI 极难排查。
//   显式导入确定性最高；新增组件 story 只需在此处追加一行 import 即可。
import { stories as lovSelectStories } from '../core/components/LovSelect/LovSelect.story';
import { stories as lovSelectTableStories } from '../core/components/LovSelectTable/LovSelectTable.story';

export const stories: Array<{
  id: string;
  component: unknown;
  props?: Record<string, unknown>;
}> = [...lovSelectStories, ...lovSelectTableStories];
