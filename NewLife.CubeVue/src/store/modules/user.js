import { getToken, setToken, removeToken } from '@/utils/token'
import * as userInfoStorage from '@/utils/user'

const user = {
  state: {
    userInfo: undefined, //userInfoStorage.getUserInfo(),
    permission: undefined, // 权限集合
    token: getToken(),
    hasPermission,
  },

  mutations: {
    SET_USERINFO: (state, userInfo) => {
      userInfoStorage.setUserInfo(userInfo)
      state.userInfo = userInfo
    },
    REMOVE_USERINFO: (state) => {
      userInfoStorage.removeUserInfo()
      state.userInfo = undefined
    },
    SET_TOKEN: (state, token) => {
      setToken(token)
      state.token = token
    },
    REMOVE_TOKEN: (state) => {
      removeToken()
      state.token = undefined
    },
    SET_PERMISSION: (state, permission) => {
      state.permission = permission
    },
  },

  actions: {
    setToken({ commit }, token) {
      commit('SET_TOKEN', token)
    },
    // 设置用户信息
    setUserInfo({ commit }, userInfo) {
      commit('SET_USERINFO', userInfo)
    },
    // 登出
    logout({ commit, state }) {
      // 移除token
      commit('REMOVE_TOKEN')
      // 移除用户信息
      commit('REMOVE_USERINFO')
    },
  },
}

// 判断当前用户是否拥有某个操作的权限
function hasPermission(store, { menuId, actionId, permissions }) {
  let userStore = store.state.user
  if (!userStore.permission) {
    if (!userStore.userInfo || !userStore.userInfo.permission) return false
    // permission格式: 1#255,2#255。#前为菜单id，#后为权限值
    // 处理成以菜单id为key的对象
    let permission = userStore.userInfo.permission
    let pObj = {}
    let mlist = permission.split(',')
    for (const key in mlist) {
      const m = mlist[key]
      const p = m.split('#')
      pObj[p[0]] = p[1]
    }
    store.commit('SET_PERMISSION', pObj)
  }

  // 没有这个菜单
  let permissionFlags = userStore.permission[menuId]
  if (permissionFlags === undefined) return false

  // 只传了菜单id，没有传权限，并且菜单存在，说明有只读权限
  if (actionId === undefined || actionId === null || actionId < 1) return true

  // 菜单没有这个权限
  if (permissions.findIndex((f) => f.k == actionId) < 0) return false

  return (permissionFlags & actionId) > 0
}

export default user
