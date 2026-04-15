import type { App } from 'vue';
import { useUserInfo } from '/@/stores/userInfo';
import { judementSameArr } from '/@/utils/arrayOperation';

/**
 * 用户权限指令
 * @directive 单个权限验证（v-auth="xxx"）
 * @directive 多个权限验证，满足一个则显示（v-auths="[xxx,xxx]"）
 * @directive 多个权限验证，全部满足则显示（v-auth-all="[xxx,xxx]"）
 */
export function authDirective(app: App) {
	const setVisible = (el: HTMLElement, visible: boolean) => {
		el.style.display = visible ? '' : 'none';
	};

	const hasAnyAuth = (values: Array<string>) => {
		if (!values || values.length === 0) return false;
		const stores = useUserInfo();
		const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
		return authBtnList.some((val: string) => values.some((v) => v === val));
	};

	const hasSingleAuth = (value: string) => {
		if (!value) return false;
		const stores = useUserInfo();
		const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
		return authBtnList.some((v: string) => v === value);
	};

	// 单个权限验证（v-auth="xxx"）
	app.directive('auth', {
		mounted(el, binding) {
			setVisible(el as HTMLElement, hasSingleAuth(binding.value));
		},
		updated(el, binding) {
			setVisible(el as HTMLElement, hasSingleAuth(binding.value));
		},
	});
	// 多个权限验证，满足一个则显示（v-auths="[xxx,xxx]"）
	app.directive('auths', {
		mounted(el, binding) {
			setVisible(el as HTMLElement, hasAnyAuth(binding.value));
		},
		updated(el, binding) {
			setVisible(el as HTMLElement, hasAnyAuth(binding.value));
		},
	});
	// 多个权限验证，全部满足则显示（v-auth-all="[xxx,xxx]"）
	app.directive('auth-all', {
		mounted(el, binding) {
			const stores = useUserInfo();
			const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
			const flag = judementSameArr(binding.value, authBtnList);
			setVisible(el as HTMLElement, flag);
		},
		updated(el, binding) {
			const stores = useUserInfo();
			const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
			const flag = judementSameArr(binding.value, authBtnList);
			setVisible(el as HTMLElement, flag);
		},
	});
}
