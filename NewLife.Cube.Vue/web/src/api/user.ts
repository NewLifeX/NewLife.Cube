import { Login, UserInfo } from '../model/api/user';
import { LoginConfig, SiteInfo } from '../model/api/login';
import request from '/@/utils/request';

/**
 * （不建议写成 request.post(xxx)，因为这样 post 时，无法 params 与 data 同时传参）
 *
 * 登录api接口集合
 * @method signIn 用户登录
 * @method signOut 用户退出登录
 * @method getLoginConfig 获取登录配置
 * @method getSiteInfo 获取站点信息
 */
export function useUserApi() {
	return {
    signIn: (data: object) => {
			return request<Login>({
				url: '/admin/user/login',
				method: 'post',
				data,
			});
		},
		signOut: (data: object) => {
			return request({
				url: '/user/signOut',
				method: 'post',
				data,
			});
		},
		info: () => {
			return request<UserInfo>({
				url: '/Admin/User/Info',
				method: 'get'
			});
		},
		getLoginConfig: () => {
			return request<LoginConfig>({
				url: '/Admin/Cube/GetLoginConfig',
				method: 'get'
			});
		},
		getSiteInfo: () => {
			return request<SiteInfo>({
				url: '/Admin/Cube/GetSiteInfo',
				method: 'get'
			});
		},
	};
}