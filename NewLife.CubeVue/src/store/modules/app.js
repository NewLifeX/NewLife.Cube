import Storage from '@/utils/storage'
import urls from '@/api/constant'

const app = {
  state: {
    sidebar: {
      opened: true, //!+Storage.getItem('sidebarStatus'),
      withoutAnimation: false
    },
    device: 'desktop',
    size: Storage.getItem('size') || 'medium',
    urls: urls,
    // 系统配置
    sysConfig: undefined,
    // 登录页面配置
    loginConfig: JSON.parse(Storage.getItem('loginConfig')),
    // 是否隐藏布局
    hiddenLayout: false,
    // 信息弹窗
    message: undefined,
    // 确认框弹窗
    messageBox: undefined
  },
  mutations: {
    TOGGLE_SIDEBAR: (state) => {
      if (state.sidebar.opened) {
        Storage.setItem('sidebarStatus', 1)
      } else {
        Storage.setItem('sidebarStatus', 0)
      }
      state.sidebar.opened = !state.sidebar.opened
      state.sidebar.withoutAnimation = false
    },
    CLOSE_SIDEBAR: (state, withoutAnimation) => {
      Storage.setItem('sidebarStatus', 1)
      state.sidebar.opened = false
      state.sidebar.withoutAnimation = withoutAnimation
    },
    TOGGLE_DEVICE: (state, device) => {
      state.device = device
    },
    SET_SIZE: (state, size) => {
      state.size = size
      Storage.setItem('size', size)
    },
    SET_URLS: (state, urls) => {
      Object.assign(state.urls, urls)
    },
    SET_SYSCONFIG: (state, cfg) => {
      state.sysConfig = cfg
    },
    SET_LOGINCONFIG: (state, cfg) => {
      state.loginConfig = cfg
      Storage.setItem('loginConfig', JSON.stringify(cfg))
    },
    SET_HIDDENLAYOUT: (state, hidden) => {
      state.hiddenLayout = hidden
    },
    SET_MESSAGE: (state, message) => {
      state.message = message
    },
    SET_MESSAGEBOX: (state, messageBox) => {
      state.messageBox = messageBox
    }
  },
  actions: {
    toggleSideBar({ commit }) {
      commit('TOGGLE_SIDEBAR')
    },
    closeSideBar({ commit }, { withoutAnimation }) {
      commit('CLOSE_SIDEBAR', withoutAnimation)
    },
    toggleDevice({ commit }, device) {
      commit('TOGGLE_DEVICE', device)
    },
    setSize({ commit }, size) {
      commit('SET_SIZE', size)
    },
    setUrls({ commit }, urls) {
      commit('SET_URLS', urls)
    },
    setHiddenLayout({ commit }, hidden) {
      commit('SET_HIDDENLAYOUT', hidden)
    },
    setSysConfig({ commit }, cfg) {
      commit('SET_SYSCONFIG', cfg)
    },
    setLoginConfig({ commit }, cfg) {
      commit('SET_LOGINCONFIG', cfg)
    },
    setMessage({ commit }, message) {
      commit('SET_MESSAGE', message)
    },
    setMessageBox({ commit }, messageBox) {
      commit('SET_MESSAGEBOX', messageBox)
    }
  }
}

export default app
