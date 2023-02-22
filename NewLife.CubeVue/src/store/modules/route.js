import { formatRoutes } from '@/utils/route'

const route = {
  state: {
    // 将展示在侧边栏的菜单
    // menuRouters: [], // constantRouterMap,
    menuRouters: [],
    // 将要添加到路由系统中的新路由
    addRouters: [],
    // src/views 文件夹下的文件组件
    files: function(req) {
      console.log('no module')
      return null
    },
  },
  mutations: {
    SET_ROUTERS: (state, routers) => {
      state.addRouters = routers
      state.menuRouters = /* constantRouterMap.concat*/ routers
    },
    ADD_ROUTERS: (state, routers) => {
      state.addRouters = state.addRouters.concat(routers)
    },
    SET_FILES: (state, files) => {
      let map = state.files.map || {}
      files.keys().forEach((key) => {
        map[key] = files(key)
      })
      state.files = function(req) {
        return map[req]
      }
      state.files.map = map
      state.files.keys = function() {
        return Object.keys(map)
      }
    },
  },
  actions: {
    generateRoutes({ commit, state }, accessedRouters) {
      // 将请求回来的菜单生成为路由
      let addRouters = formatRoutes(state.files, accessedRouters)
      commit('SET_ROUTERS', addRouters)
    },
    setRouters({ commit }, addRouters) {
      commit('SET_ROUTERS', addRouters)
    },
    setFiles({ commit }, files) {
      commit('SET_FILES', files)
    },
  },
}

export default route
