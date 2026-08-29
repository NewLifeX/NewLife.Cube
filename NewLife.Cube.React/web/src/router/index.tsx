/**
 * 路由系统（对齐 Vue 皮肤 routes/index.ts + DefaultEntity 模式）
 *
 * - 静态路由表 + catch-all `*` → DefaultEntity（按菜单匹配动态实体页）
 * - 每个路由通过 handle 声明元数据：auth（是否需登录）、layout（是否用主布局）、title
 */
import { createBrowserRouter } from 'react-router-dom';
import RootLayout from '@/layouts/RootLayout';
import LoginPage from '@/pages/Login';
import RegisterPage from '@/pages/Register';
import ActivatePage from '@/pages/Activate';
import ForgotPasswordPage from '@/pages/ForgotPassword';
import ProfileSecurityPage from '@/pages/ProfileSecurity';
import HomePage from '@/pages/Home';
import NotFoundPage from '@/pages/NotFound';
import UnauthorizedPage from '@/pages/Unauthorized';
import LoadingPage from '@/pages/Loading';
import DefaultEntityPage from '@/pages/DefaultEntity';

/** 路由元数据 */
export interface RouteMeta {
  /** 标题 */
  title?: string;
  /** 是否需登录（默认 true） */
  auth?: boolean;
  /** 是否使用主布局（默认 true；登录/激活等公开页 false） */
  layout?: boolean;
  /** 是否加入多标签（默认 true） */
  tab?: boolean;
  /** 是否固定标签（首页等）：不可关闭，且作为“关闭全部”后的归宿 */
  fixed?: boolean;
  /** 动态实体页（catch-all），标签标题需从菜单解析 */
  dynamic?: boolean;
}

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        index: true,
        element: <HomePage />,
        handle: { title: '首页', auth: true, tab: true, fixed: true } as RouteMeta,
      },
      {
        path: 'login',
        element: <LoginPage />,
        handle: { title: '登录', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: 'register',
        element: <RegisterPage />,
        handle: { title: '注册', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: 'activate',
        element: <ActivatePage />,
        handle: { title: '账号激活', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: 'forgot-password',
        element: <ForgotPasswordPage />,
        handle: { title: '忘记密码', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: 'profile/security',
        element: <ProfileSecurityPage />,
        handle: { title: '安全中心', auth: true, tab: true } as RouteMeta,
      },
      {
        path: 'unauthorized',
        element: <UnauthorizedPage />,
        handle: { title: '未授权', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: 'loading',
        element: <LoadingPage />,
        handle: { title: '加载中', auth: false, layout: false, tab: false } as RouteMeta,
      },
      {
        path: '*',
        element: <DefaultEntityPage />,
        handle: { title: '默认页面', auth: true, tab: true, dynamic: true } as RouteMeta,
      },
    ],
  },
]);

export default router;
