/**
 * 动态实体页（catch-all）
 *
 * 对齐 Vue 皮肤 DefaultEntity.vue：根据当前路径在菜单树中匹配，
 * 命中 → 渲染通用列表页；未命中 → 404。通用列表页在 LIST 阶段实现。
 */
import { useLocation } from 'react-router-dom';
import { useMenuStore } from '@/stores/menu';
import NotFoundPage from '@/pages/NotFound';

export default function DefaultEntityPage() {
  const location = useLocation();
  const findMenu = useMenuStore((s) => s.findMenu);

  const matched = findMenu(location.pathname);

  if (!matched) {
    return <NotFoundPage />;
  }

  // TODO(LIST-1): 渲染通用列表页（createPageStore + GetPage 驱动）
  return <div>通用列表页（建设中）：{matched.title ?? matched.name}</div>;
}
