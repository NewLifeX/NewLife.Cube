export function getItem(key: string): string {
  return localStorage.getItem(key) || '';
}

export function setItem(key: string, value: string) {
  return localStorage.setItem(key, value);
}

export function removeItem(key: string) {
  return localStorage.removeItem(key);
}

export default {
  getItem,
  setItem,
  removeItem,
};
