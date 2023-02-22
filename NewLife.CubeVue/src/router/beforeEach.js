// import { Message } from 'element-ui'
import { getToken } from '@/utils/token' // getToken from cookie

const whiteList = ['/login', '/auth-redirect'] // no redirect whitelist

export default (store, router) => {
  return (to, from, next) => {
    if (getToken()) {
      if (to.path === '/login') {
        next({
          path: '/',
        })
      } else {
        if (!store.getters.userInfo) {
          // TODO 暂时在这里拉取系统配置信息，
          // 应该在登陆完之后拉取初始化需要的信息

          // 拉取系统配置信息
          store.getters.apis
            .getObject(store.getters.urls.getSysConfig)
            .then((res) => {
              let cfg = res.data.data.value
              store.dispatch('setSysConfig', cfg)
            })

          // 拉取user_info信息
          store.getters.apis
            .getUserInfo()
            .then((response) => {
              const data = response.data.data
              // 设置用户信息
              store.dispatch('setUserInfo', data)

              // 将请求回来的菜单转化成路由以及菜单信息
              store.getters.apis.getMenu().then((routeRes) => {
                let accessedRouters = routeRes.data.data

                // 设置路由信息
                store.dispatch('generateRoutes', accessedRouters)

                // 添加路由信息
                let addRouters = store.getters.addRouters
                if (addRouters) {
                  addRouters.forEach((e) => {
                    router.addRoute(e) // 动态添加可访问路由表
                  })
                }
                next({
                  ...to,
                  replace: true,
                }) // hack方法 确保addRoutes已完成 ,set the replace: true so the navigation will not leave a history record
              })
            })
            .catch((err) => {
              // TODO 登录跳转时请求出错，自动注销，待定
              console.log('请求错误，注销并跳转登录', err)
              store.getters.apis.logout().then(() => {
                store.dispatch('logout')
                //   Message.error(err || 'Verification failed, please login again')
                next({
                  path: '/',
                })
              })
            })

          // 拉取登录配置
          store.getters.apis.getLoginConfig().then((res) => {
            let cfg = res.data.data
            store.dispatch('setLoginConfig', cfg)
          })
        } else {
          next()
        }
      }
    } else {
      /* has no token*/
      if (whiteList.indexOf(to.path) !== -1) {
        // 在免登录白名单，直接进入
        next()
      } else {
        next(`/login?redirect=${to.path}`) // 否则全部重定向到登录页
      }
    }
  }
}
