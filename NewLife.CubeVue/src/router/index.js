import beforeEach from './beforeEach'

const Layout = () => import('@/views/layout/index.vue')

export const constantRouterMap = [
  // {
  //   path: '/redirect',
  //   component: Layout,
  //   hidden: true,
  //   children: [
  //     {
  //       path: '/redirect/:path*',
  //       component: () => import('src/views/redirect'),
  //     },
  //   ],
  // },
  {
    path: '/login',
    component: () => import('@/views/account/login'),
    hidden: true
  },
  {
    path: '/auth-redirect',
    component: () => import('@/views/account/authRedirect'),
    hidden: true
  },
  {
    path: '',
    redirect: '/Admin/User/Info',
    component: Layout,
    children: [
      {
        path: '/Admin/User/Info',
        component: () => import('@/views/Admin/User/info'),
        name: 'UserInfo',
        meta: {
          title: '个人信息',
          noCache: true
        }
      }
    ]
  },
  // {
  //   path: '/404',
  //   component: () => import('src/views/errorPage/404'),
  //   hidden: true,
  // },
  // {
  //   path: '/401',
  //   component: () => import('src/views/errorPage/401'),
  //   hidden: true,
  // },
  {
    path: '',
    component: Layout,
    // redirect: 'dashboard',
    children: [
      {
        path: 'dashboard',
        component: () => import('@/views/Admin/Index/Main.vue'),
        name: 'Dashboard',
        meta: {
          title: '首页',
          icon: 'dashboard',
          noCache: true
        }
      }
    ]
  },
  {
    path: '/Admin/Index/Main',
    redirect: 'dashboard'
  }
]

export const asyncRouterMap = [
  {
    path: '*',
    redirect: '/404',
    hidden: true
  }
]

const routerOptions = {
  // history: VueRouter.createWebHashHistory(),
  scrollBehavior: () => ({
    top: 0
  }),
  routes: constantRouterMap
}

export default { routerOptions, beforeEach }
