const TOKEN_KEY = 'token';

export interface TokenStorage {
  getToken(): string | null;
  setToken(token: string): void;
  clearToken(): void;
}

/** 基于 Cookie 的 Token 存储（与 Cube.Vue 兼容） */
class CookieTokenStorage implements TokenStorage {
  getToken(): string | null {
    const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${TOKEN_KEY}=([^;]*)`));
    return match ? decodeURIComponent(match[1]) : null;
  }
  setToken(token: string): void {
    document.cookie = `${TOKEN_KEY}=${encodeURIComponent(token)}; path=/; SameSite=Lax`;
  }
  clearToken(): void {
    document.cookie = `${TOKEN_KEY}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
  }
}

/** 基于 localStorage 的 Token 存储 */
class LocalTokenStorage implements TokenStorage {
  private key: string;
  constructor(key: string = TOKEN_KEY) { this.key = key; }
  getToken(): string | null { return localStorage.getItem(this.key); }
  setToken(token: string): void { localStorage.setItem(this.key, token); }
  clearToken(): void { localStorage.removeItem(this.key); }
}

/**
 * Token 管理器 — 统一管理访问令牌的存取与清除
 *
 * 默认使用 Cookie 存储（与 Cube.Vue 行为一致），可选 localStorage。
 */
export class TokenManager {
  private storage: TokenStorage;

  constructor(storage?: TokenStorage | 'cookie' | 'localStorage') {
    if (!storage || storage === 'cookie') {
      this.storage = new CookieTokenStorage();
    } else if (storage === 'localStorage') {
      this.storage = new LocalTokenStorage();
    } else {
      this.storage = storage;
    }
  }

  /** 获取当前 Token */
  getToken(): string | null { return this.storage.getToken(); }

  /** 设置 Token */
  setToken(token: string): void { this.storage.setToken(token); }

  /** 清除 Token */
  clearToken(): void { this.storage.clearToken(); }
}
