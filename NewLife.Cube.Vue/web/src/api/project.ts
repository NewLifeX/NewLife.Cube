import request from '/@/utils/request';

/**
 * （不建议写成 request.post(xxx)，因为这样 post 时，无法 params 与 data 同时传参）
 *
 * 登录api接口集合
 * @method signIn 用户登录
 * @method signOut 用户退出登录
 */
export function useProjectApi() {
	return {
    workExamine: (data: { id: number; examine: 0 | 1 | 2 }) => {
			return request({
				url: '/Projects/Work/WorkExamine',
				method: 'post',
				data,
			});
		},
	};
}