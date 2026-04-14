/**
 * @cube/api-core — 魔方前端公共 API 调用层
 *
 * 框架无关的 HTTP 客户端封装，统一 Token 管理、错误处理、请求/响应拦截。
 * 所有皮肤包共享此模块，避免重复实现。
 */

// 核心工厂
export { createCubeApi, type CubeApi, type CubeApiOptions } from './cube';

// 底层构建块（高级用法）
export { createApiClient, createRequest, type ApiClientOptions } from './client';
export { TokenManager, type TokenStorage } from './token';
export { createUserApi, createMenuApi, createPageApi, createConfigApi } from './api';

// 类型
export type {
  ApiResponse,
  PageInfo,
  PageParams,
  DataField,
  UserInfo,
  LoginResult,
  LoginConfig,
  ChallengeResult,
  ResetPasswordModel,
  SiteInfo,
  OAuthProvider,
  MenuItem,
  RegisterModel,
  OAuthPendingInfo,
} from './types';
export { FieldKind, Auth, RegisterCategory } from './types';

// 密码安全工具（RSA-OAEP 加密，配合 /Auth/Challenge 接口）
export { encryptPassword } from './crypto';
