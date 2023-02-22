const app = {
  state: {
    // api对象，根据方法名区分、替换
    apis: {},
    // http请求封装
    request: () => {
      throw 'request方法没有实现'
    },
  },
  mutations: {
    SET_APIS: (state, apis) => {
      Object.assign(state.apis, apis)
    },
    SET_REQUEST: (state, request) => {
      state.request = request
    },
  },
  actions: {
    addApis({ commit }, apis) {
      commit('SET_APIS', apis)
    },
    setRequest({ commit }, request) {
      commit('SET_REQUEST', request)
    },
  },
}

export default app
