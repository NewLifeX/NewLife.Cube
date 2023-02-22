import Storage from '@/utils/storage'

const Key = 'userInfo'

export function getUserInfo() {
  return JSON.parse(Storage.getItem(Key))
}

export function setUserInfo(value) {
  return Storage.setItem(Key, JSON.stringify(value))
}

export function removeUserInfo() {
  return Storage.removeItem(Key)
}
