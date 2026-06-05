// v1 API 相关类型定义

// SSO Token 模型
export interface SsoTokenModel {
  client_id?: string;
  client_secret?: string;
  userName?: string;
  password?: string;
  grant_type?: string;
}

// Cube 相关接口类型
export interface CubeInfo {
  state?: string;
}

export interface UserSearchParams {
  roleId?: number;
  departmentId?: number;
  key?: string;
}

export interface DepartmentSearchParams {
  parentid?: number;
  key?: string;
}

export interface AreaParams {
  id?: number;
  isContainSelf?: boolean;
}

export interface SaveLayoutParams {
  userid?: number;
  category?: string;
  name?: string;
  value?: string;
}

export interface PageConfigParams {
  kind?: string;
  page?: string;
}

// SSO 相关接口类型
export interface SsoLoginParams {
  name?: string;
}

export interface SsoLoginInfoParams {
  id?: string;
  code?: string;
  state?: string;
}

export interface SsoAuthorizeParams {
  client_id?: string;
  redirect_uri?: string;
  response_type?: string;
  scope?: string;
  state?: string;
  loginUrl?: string;
}

export interface SsoTokenParams {
  client_id?: string;
  client_secret?: string;
  code?: string;
  grant_type?: string;
  username?: string;
  password?: string;
  refresh_token?: string;
}

export interface SsoUserInfoParams {
  access_token?: string;
}

export interface SsoAuthParams {
  access_token?: string;
  redirect_uri?: string;
}

export interface SsoVerifyParams {
  access_token?: string;
  redirect_uri?: string;
}

// 通用响应类型
export interface ApiResponse<T = unknown> {
  code?: number;
  message?: string;
  data?: T;
  success?: boolean;
}

// 分页响应类型
export interface PagedResponse<T = unknown> {
  data?: T[];
  total?: number;
  pageIndex?: number;
  pageSize?: number;
}
