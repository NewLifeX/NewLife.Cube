export default function(stroe) {
  //获取可用数据库连接列表
  function getDbList(path) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.getDbList,
      method: 'post',
    })
  }

  return {
    getDbList,
  }
}
