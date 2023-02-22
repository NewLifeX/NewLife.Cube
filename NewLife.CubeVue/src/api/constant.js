export default {
  baseUrl: '',
  getBaseUrl() {
    // 获取基础请求路径，可重载
    return this.baseUrl
  },
  ssoUrl: 'https://sso.newlifex.com',
  login: '/Admin/User/Login',
  getToken: '/Sso/LoginInfo',
  getUserInfo: '/Admin/User/Info/',
  logout: '/Admin/User/Logout',
  changePassword: '/Admin/User/ChangePassword',
  getMenu: '/Admin/Index/GetMenuTree',
  getEntityFields: '/GetFields',
  getColumns: '/GetColumns',
  getDataList: '/Index',
  getData: '/Edit',
  getDetailData: '/Detail',
  deleteById: '/Delete',
  add: '/Add',
  edit: '/Edit',
  getObject: '/Index',
  getSysConfig: '/Admin/Sys',
  updateObject: '/Update',
  getLoginConfig: '/Admin/Cube/GetLoginConfig'
}
