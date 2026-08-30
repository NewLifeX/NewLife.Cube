/**
 * 动态实体页（catch-all）
 *
 * 对齐 Vue 皮肤 DefaultEntity.vue：根据当前路径在菜单树中匹配，
 * 命中 → 渲染通用列表页；表单类 URL（含 ?id= 或 /Edit 等后缀）→ 渲染表单页；未命中 → 404。
 */
import { useMemo } from 'react';
import { useLocation } from 'react-router-dom';
import { useMenuStore } from '@/stores/menu';
import DefaultListPage from '@/views/list';
import FormPage from '@/views/form/FormPage';
import NotFoundPage from '@/pages/NotFound';
import ConfigNav, { findConfigCenter } from '@/views/config/ConfigNav';

/** 是否表单类路径（含 ?id= 或 /Edit /Add /New /Detail 后缀） */
function isFormPath(pathname: string, search: string): boolean {
  if (search.includes('id=')) return true;
  return /\/+(edit|add|new|detail)$/i.test(pathname);
}

export default function DefaultEntityPage() {
  const location = useLocation();
  // 订阅 flatMenus 而非 findMenu：findMenu 引用稳定不会触发重渲染，
  // 若菜单异步加载晚于本组件首次渲染，需在菜单就绪后重新匹配（否则误判 404）
  const flatMenus = useMenuStore((s) => s.flatMenus);
  const findMenu = useMenuStore((s) => s.findMenu);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const matched = useMemo(() => findMenu(location.pathname), [findMenu, flatMenus, location.pathname]);
  // 配置中心路径：配置控制器在左侧菜单隐藏，菜单树中不存在，需按静态清单特判
  const config = useMemo(() => findConfigCenter(location.pathname), [location.pathname]);

  if (!matched && !config) {
    return <NotFoundPage />;
  }

  const title = config?.label || matched?.title || matched?.name || '';

  // 表单类路径（编辑/新增/详情）→ 表单页；列表/配置页 → 通用列表页
  const content = isFormPath(location.pathname, location.search) ? (
    <FormPage key={location.pathname + location.search} title={title} />
  ) : (
    <DefaultListPage key={matched?.path || config?.path || location.pathname} />
  );

  // 配置中心页：顶部渲染切换器（基本设置/系统设置/星尘设置/数据中间件/魔方设置 + 更多配置）
  if (config) {
    return (
      <>
        <ConfigNav currentPath={location.pathname} />
        {content}
      </>
    );
  }

  return content;
}
