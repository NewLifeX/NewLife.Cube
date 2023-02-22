export default function getApis(stroe) {
  function getObject(path) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.getObject,
      method: 'get',
    })
  }

  function updateObject(path, obj) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.updateObject,
      method: 'post',
      data: obj,
    })
  }

  function getLoginConfig() {
    let request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: urls.getLoginConfig,
      method: 'get',
    })
  }

  return {
    getObject,
    updateObject,
    getLoginConfig,
  }
}
