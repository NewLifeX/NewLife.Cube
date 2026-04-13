import Storage from '@/utils/storage';

const TokenKey = 'cube_antd_token';

export function getToken(): string {
  return Storage.getItem(TokenKey);
}

export function setToken(token: string) {
  return Storage.setItem(TokenKey, token);
}

export function removeToken() {
  return Storage.removeItem(TokenKey);
}
