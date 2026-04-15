import { useUserInfo } from '/@/stores/userInfo';
import { judementSameArr } from '/@/utils/arrayOperation';

/**
 * 单个权限验证
 * @param value 权限值
 * @returns 有权限，返回 `true`，反之则反
 */
export function auth(value: string): boolean {
	const stores = useUserInfo();
	const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
	if (authBtnList.some((v: string) => v.endsWith('#255'))) return true;
	return authBtnList.some((v: string) => v === value);
}

/**
 * 多个权限验证，满足一个则为 true
 * @param value 权限值
 * @returns 有权限，返回 `true`，反之则反
 */
export function auths(value: Array<string>): boolean {
	const stores = useUserInfo();
	const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
	if (authBtnList.some((v: string) => v.endsWith('#255'))) return true;
	if (!value || value.length === 0) return false;
	return authBtnList.some((val: string) => value.some((v: string) => v === val));
}

/**
 * 多个权限验证，全部满足则为 true
 * @param value 权限值
 * @returns 有权限，返回 `true`，反之则反
 */
export function authAll(value: Array<string>): boolean {
	const stores = useUserInfo();
	const authBtnList = Array.isArray(stores.userInfos?.authBtnList) ? stores.userInfos.authBtnList : [];
	if (authBtnList.some((v: string) => v.endsWith('#255'))) return true;
	return judementSameArr(value, authBtnList);
}
