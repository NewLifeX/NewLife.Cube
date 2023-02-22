import axios from 'axios'
// import JSONbig from 'json-bigint'

export default function getRequest(store) {
  const service = axios.create({
    timeout: 50000,
    // 响应拦截处理大数值
    transformResponse: [
      function(data) {
        try {
          // 使用正则将长整数替换为字符串
          data = data.replace(/(\d{16,})/gi, '"$1"')
          return JSON.parse(data)
          // // 使用json-bigint将大数值转成
          // data = JSONbig.parse(data)
          // return data
        } catch (err) {
          return data
        }
      }
    ]
  })

  // 是否正在弹窗
  let isLoginTimeout = false

  service.interceptors.request.use(
    (config) => {
      // api 的 base_url
      // 在此处设置baseURL，避免直接依赖store，如果放在上面create方法，store为空
      config.baseURL = store.getters.urls.getBaseUrl()

      // 所有请求默认是json格式，除了上传文件，不用表单
      config.headers['Content-Type'] = 'application/json; charset=UTF-8'

      const token = store.getters.token
      if (token) {
        // 让每个请求携带token-- ['Authorization']为自定义key 请根据实际情况自行修改
        config.headers['Authorization'] = token
      }

      return config
    },
    (error) => {
      console.log(error)
      store.getters.message({
        message: error,
        type: 'error',
        duration: 5 * 1000
      })
      Promise.reject(error)
    }
  )

  service.interceptors.response.use(
    (response) => {
      // console.log(response)
      // http状态不是200不会到这里
      const res = response.data
      if (res.code !== undefined && res.code !== null) {
        if (res.code >= 500) {
          store.getters.message({
            message: res.message,
            type: 'error',
            duration: 5 * 1000
          })
          return Promise.reject('后端服务错误')
        } else if (res.code === 401) {
          handle401()
        } else if (res.code === 403) {
          handle403(res)
        } else {
          return response
        }
      } else {
        console.log('格式错误', res) // for debug
        store.getters.message({
          message: '服务端返回格式不正确!!!请联系管理员',
          type: 'error',
          duration: 5 * 1000
        })
        return Promise.reject('服务端返回格式不正确')
      }
    },
    (error) => {
      if (error.message === 'Request failed with status code 401') {
        handle401()
      } else if (error.message === 'Request failed with status code 403') {
        handle403({ message: '没有权限！' })
      } else {
        console.log('err', error, JSON.stringify(error)) // for debug
        store.getters.message({
          message: '服务请求出错',
          type: 'error',
          duration: 5 * 1000
        })
      }

      return Promise.reject(error)
    }
  )

  function handle401() {
    // 如果已弹窗，不重复弹窗
    if (!isLoginTimeout) {
      isLoginTimeout = true
    } else {
      console.log('登录超时，已弹窗，尝试关闭已打开的弹窗')
      try {
        // 关闭所有弹窗
        store.getters.messageBox.close()
      } catch (error) {}
    }

    store.getters.messageBox
      .confirm('登陆超时，可以取消继续留在该页面，或者重新登录', '确定登出', {
        confirmButtonText: '重新登录',
        cancelButtonText: '取消',
        type: 'warning'
      })
      .then(() => {
        isLoginTimeout = false
        store.getters.apis.logout().then(() => {
          store.dispatch('logout')
          isLoginTimeout = false
          location.reload() // 为了重新实例化vue-router对象 避免bug
        })
      })
  }

  function handle403(res) {
    store.getters.message({
      message: res.message,
      type: 'warning',
      duration: 5 * 1000
    })
    return Promise.reject('没有权限')
  }

  return service
}
