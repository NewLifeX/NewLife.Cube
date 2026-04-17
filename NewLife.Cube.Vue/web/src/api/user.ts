/**
 * 用户认证 API — 薄包装层（委托到 @cube/api-core cubeApi，消除本地重复封装）
 *
 * 保持原有 useUserApi() 调用形式不变，方便存量代码零改动迁移。
 */
import cubeApi from './index';

export function useUserApi() {
	return {
		signIn: (data: object) => cubeApi.user.login(data as Parameters<typeof cubeApi.user.login>[0]),
		signOut: (_data?: object) => cubeApi.user.logout(),
		info: () => cubeApi.user.info(),
		/** 发送验证码，channel: Sms（短信）或 Mail（邮箱） */
		sendCode: (data: Parameters<typeof cubeApi.user.sendCode>[0]) => cubeApi.user.sendCode(data),
		/** 验证码登录 */
		loginByCode: (data: Parameters<typeof cubeApi.user.loginByCode>[0]) => cubeApi.user.loginByCode(data),
		/** 用户注册 */
		register: (data: Parameters<typeof cubeApi.user.register>[0]) => cubeApi.user.register(data),
		/** 获取OAuth回跳待注册预填信息 */
		getOAuthPendingInfo: (token: string) => cubeApi.user.getOAuthPendingInfo(token),
		/** 获取 RSA 挑战公钥，用于密码加密登录 */
		getChallenge: () => cubeApi.user.getChallenge(),
		/** 忘记密码—重置密码 */
		resetPassword: (data: Parameters<typeof cubeApi.user.resetPassword>[0]) => cubeApi.user.resetPassword(data),
		/** 获取登录页配置 */
		getLoginConfig: () => cubeApi.user.getLoginConfig(),
		/** 获取站点信息 */
		getSiteInfo: () => cubeApi.user.getSiteInfo(),
	};
}
