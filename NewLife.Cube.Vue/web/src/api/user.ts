import { Login, UserInfo } from '../model/api/user';
import { LoginConfig, SiteInfo, SendCodeModel, LoginByCodeModel, RegisterModel } from '../model/api/login';
import request from '/@/utils/request';

/**
 * （不建议写成 request.post(xxx)，因为这样 post 时，无法 params 与 data 同时传参）
 *
 * 登录api接口集合
 * @method signIn 用户登录（密码）
 * @method signOut 用户退出登录
 * @method sendCode 发送验证码（Sms/Mail）
 * @method loginByCode 验证码登录
 * @method register 用户注册
 * @method getLoginConfig 获取登录配置
 * @method getSiteInfo 获取站点信息
 */
export function useUserApi() {
	return {
		signIn: (data: object) => {
			return request<Login>({
				url: '/Auth/Login',
				method: 'post',
				data,
			});
		},
		signOut: (data: object) => {
			return request({
				url: '/Auth/Logout',
				method: 'post',
				data,
			});
		},
		info: () => {
			return request<UserInfo>({
				url: '/Auth/Info',
				method: 'get',
			});
		},
		/** 发送验证码，channel: Sms（短信）或 Mail（邮箱） */
		sendCode: (data: SendCodeModel) => {
			return request<number>({
				url: '/Auth/SendCode',
				method: 'post',
				data,
			});
		},
		/** 验证码登录，loginCategory: 1=手机 2=邮箱 */
		loginByCode: (data: LoginByCodeModel) => {
			return request<Login>({
				url: '/Auth/LoginByCode',
				method: 'post',
				data,
			});
		},
		/** 用户注册 */
		register: (data: RegisterModel) => {
			return request({
				url: '/Admin/User/Register',
				method: 'post',
				data,
			});
		},
		getLoginConfig: () => {
			return request<LoginConfig>({
				url: '/Auth/LoginConfig',
				method: 'get',
			});
		},
		getSiteInfo: () => {
			return request<SiteInfo>({
				url: '/Cube/SiteInfo',
				method: 'get',
			});
		},
	};
}