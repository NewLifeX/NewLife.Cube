export default function(stroe) {
  function getEntityFields(path, kind) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    let params = {
      kind
    }
    return request({
      url: path + urls.getEntityFields,
      method: 'get',
      params
    })
  }

  function getDetailFields(path) {
    return getEntityFields(path, 'Detail')
  }
  function getEditFormFields(path) {
    return getEntityFields(path, 'EditForm')
  }
  function getAddFormFields(path) {
    return getEntityFields(path, 'AddForm')
  }
  function getListFields(path) {
    return getEntityFields(path, 'List')
  }

  /**
   * 获取表对应的列
   * @param {*} path 基础请求路径
   * @returns
   */
  function getColumns(path) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.getColumns,
      method: 'get'
    })
  }

  function getDataList(path, page) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.getDataList,
      method: 'post',
      data: page
    })
  }

  function getData(path, id) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    let params = {
      id
    }
    return request({
      url: path + urls.getData,
      method: 'get',
      params
    })
  }

  function getDetailData(path, id) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    let params = {
      id
    }
    return request({
      url: path + urls.getDetailData,
      method: 'get',
      params
    })
  }

  function deleteById(path, id) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    let params = {
      id
    }
    return request({
      url: path + urls.deleteById,
      method: 'get',
      params
    })
  }

  function add(path, entity) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.add,
      method: 'post',
      data: entity
    })
  }

  function edit(path, entity) {
    const request = stroe.getters.request
    const urls = stroe.getters.urls
    return request({
      url: path + urls.edit,
      method: 'post',
      data: entity
    })
  }

  return {
    getColumns,
    getDetailFields,
    getEditFormFields,
    getAddFormFields,
    getListFields,
    getDataList,
    getData,
    getDetailData,
    add,
    edit,
    deleteById
  }
}
