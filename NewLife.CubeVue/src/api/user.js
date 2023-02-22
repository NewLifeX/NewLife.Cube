export default function(stroe) {
  function login(loginForm) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls

    return request({
      url: urls.login,
      method: 'post',
      data: loginForm
      // params: data,
    })
  }

  function logout() {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: urls.logout,
      method: 'get'
    })
  }

  function getUserInfo() {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: urls.getUserInfo,
      method: 'get'
    })
  }

  function updateUserInfo(userInfo) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: urls.getUserInfo,
      method: 'post',
      data: userInfo
    })
  }

  function changePassword(data) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: urls.changePassword,
      method: 'post',
      data
    })
  }

  return {
    login,
    logout,
    getUserInfo,
    updateUserInfo,
    changePassword
  }
}
