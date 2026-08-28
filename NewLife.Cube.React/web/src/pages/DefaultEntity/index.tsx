/**
 * 动态实体页（catch-all）
 *
 * 对齐 Vue 皮肤 DefaultEntity.vue：根据当前路径在菜单树中匹配，
 * 命中 → 渲染通用列表页；表单类 URL（含 ?id= 或 /Edit 等后缀）→ 渲染表单页；未命中 → 404。
 */
import { useLocation, useSearchParams } from 'react-router-dom';
import { useMenuStore } from '@/stores/menu';
import DefaultListPage from '@/views/list';
import FormPage from '@/views/form/FormPage';
import NotFoundPage from '@/pages/NotFound';

/** 是否表单类路径（含 ?id= 或 /Edit /Add /New /Detail 后缀） */
function isFormPath(pathname: string, search: string): boolean {
  if (search.includes('id=')) return true;
  return /\/+(edit|add|new|detail)$/i.test(pathname);
}

export default function DefaultEntityPage() {
  const location = useLocation();
  const [params] = useSearchParams();
  const findMenu = useMenuStore((s) => s.findMenu);

  const matched = findMenu(location.pathname);

  if (!matched) {
    return <NotFoundPage />;
  }

  const title = matched.title || matched.name;

  // 表单类路径（编辑/新增/详情）→ 表单页
  if (isFormPath(location.pathname, location.search)) {
    return <FormPage key={location.pathname + location.search} title={title} />;
  }

  // 列表页：key=type 保证切换实体时整体重建（store 缓存按 type 区分）
  return <DefaultListPage key={matched.path || location.pathname} title={title} />;
}
