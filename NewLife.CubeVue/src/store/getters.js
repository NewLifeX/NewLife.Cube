const getters = {
  token: (state) => state.user.token,
  userInfo: (state) => state.user.userInfo,
  menuRouters: (state) => state.route.menuRouters,
  addRouters: (state) => state.route.addRouters,
  files: (state) => state.route.files,
  sysConfig: (state) => state.app.sysConfig,
  loginConfig: (state) => state.app.loginConfig,
  sidebar: (state) => state.app.sidebar,
  app: (state) => state.app,
  urls: (state) => state.app.urls,
  request: (state) => state.api.request,
  apis: (state) => state.api.apis,
  message: (state) => state.app.message,
  messageBox: (state) => state.app.messageBox,
}
export default getters
