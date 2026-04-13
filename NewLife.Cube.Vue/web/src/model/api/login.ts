/** OAuth 提供者 */
export interface OAuthProvider {
  name: string;
  logo: string;
  nickName: string;
}

/** 登录配置（来自 /Admin/Cube/GetLoginConfig） */
export interface LoginConfig {
  displayName: string;
  logo: string;
  allowLogin: boolean;
  allowRegister: boolean;
  enableSms: boolean;
  enableMail: boolean;
  loginTip: string;
  providers: OAuthProvider[];
}

/** 站点信息（来自 /Admin/Cube/GetSiteInfo） */
export interface SiteInfo {
  displayName: string;
  copyright: string;
  registration: string;
  loginTip: string;
  logo: string;
}