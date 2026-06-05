import { forIn as _forIn} from "lodash";
import Cookies from 'universal-cookie';
const cookies = new Cookies();

/**
 * 存储sessionStorage
 */
export function setSession(key: string, value: any) {
  if (value === undefined || value === null) {
    window.sessionStorage.removeItem(key);
    return;
  }
  const formatValue = JSON.stringify(value);
  window.sessionStorage.setItem(key, formatValue);
  return true;
}

/**
 * 获取sessionStorage
 */
export function getSession(key: string) {
  const value = sessionStorage.getItem(key);
  if (value) {
    return JSON.parse(value);
  }
  return null;
}

export function removeAllCookie() {
  _forIn(cookies.getAll(), (_: any, key: string) => {
    cookies.remove(key);
  });
}

export function getCookie(key: string) {
  return cookies.get(key);
}