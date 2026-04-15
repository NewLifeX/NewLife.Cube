// @ts-ignore
/* eslint-disable */
import { request } from '@umijs/max';

/** 获取当前的用户 GET /Auth/Info */
export async function currentUser(options?: { [key: string]: any }) {
  return request<ResponseStructure<API.UserInfo>>('/Auth/Info', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 退出登录接口 POST /Auth/Logout */
export async function outLogin(options?: { [key: string]: any }) {
  return request<Record<string, any>>('/Auth/Logout', {
    method: 'POST',
    ...(options || {}),
  });
}

/** 登录接口 POST /Auth/Login */
export async function login(body: API.LoginParams, options?: { [key: string]: any }) {
  return request<API.LoginResult>('/Auth/Login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    data: body,
    ...(options || {}),
  });
}

/** 此处后端没有提供注释 GET /api/notices */
export async function getNotices(options?: { [key: string]: any }) {
  return request<API.NoticeIconList>('/api/notices', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 获取规则列表 GET /api/rule */
export async function rule(
  params: {
    // query
    /** 当前的页码 */
    current?: number;
    /** 页面的容量 */
    pageSize?: number;
  },
  options?: { [key: string]: any },
) {
  return request<API.RuleList>('/api/rule', {
    method: 'GET',
    params: {
      ...params,
    },
    ...(options || {}),
  });
}

/** 新建规则 PUT /api/rule */
export async function updateRule(options?: { [key: string]: any }) {
  return request<API.RuleListItem>('/api/rule', {
    method: 'PUT',
    ...(options || {}),
  });
}

/** 新建规则 POST /api/rule */
export async function addRule(options?: { [key: string]: any }) {
  return request<API.RuleListItem>('/api/rule', {
    method: 'POST',
    ...(options || {}),
  });
}

/** 删除规则 DELETE /api/rule */
export async function removeRule(options?: { [key: string]: any }) {
  return request<Record<string, any>>('/api/rule', {
    method: 'DELETE',
    ...(options || {}),
  });
}

/** 查询菜单 GET /Cube/MenuTree */
export async function queryMenus(options?: { [key: string]: any }) {
  return request<ResponseStructure<API.MenuInfo[]>>('/Cube/MenuTree', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 查询菜单 GET /Admin/Index/GetMenuTree */
export async function queryIndex(options?: { [key: string]: any }) {
  return request<ResponseStructure<any>>('/Admin/Cube', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 获取登录配置 GET /Auth/LoginConfig */
export async function getLoginConfig(options?: { [key: string]: any }) {
  return request<ResponseStructure<API.LoginConfig>>('/Auth/LoginConfig', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 获取站点信息 GET /Cube/SiteInfo */
export async function getSiteInfo(options?: { [key: string]: any }) {
  return request<ResponseStructure<API.SiteInfo>>('/Cube/SiteInfo', {
    method: 'GET',
    ...(options || {}),
  });
}

/** 发送验证码 POST /Auth/SendCode */
export async function sendCode(body: API.SendCodeParams, options?: { [key: string]: any }) {
  return request<ResponseStructure<number>>('/Auth/SendCode', {
    method: 'POST',
    data: body,
    ...(options || {}),
  });
}

/** 验证码登录 POST /Auth/LoginByCode */
export async function loginByCode(body: API.LoginByCodeParams, options?: { [key: string]: any }) {
  return request<ResponseStructure<API.LoginResult>>('/Auth/LoginByCode', {
    method: 'POST',
    data: body,
    ...(options || {}),
  });
}

/** 注册新用户 POST /Admin/User/Register */
export async function register(body: API.RegisterParams, options?: { [key: string]: any }) {
  return request<ResponseStructure<void>>('/Admin/User/Register', {
    method: 'POST',
    data: body,
    ...(options || {}),
  });
}

/** 重置密码 POST /Auth/ResetPassword */
export async function resetPassword(body: API.ResetPasswordParams, options?: { [key: string]: any }) {
  return request<ResponseStructure<boolean>>('/Auth/ResetPassword', {
    method: 'POST',
    data: body,
    ...(options || {}),
  });
}
