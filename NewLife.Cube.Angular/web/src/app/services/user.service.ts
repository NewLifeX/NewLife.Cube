import { Injectable, signal, computed } from '@angular/core';
import type { UserInfo, MenuItem } from '@cube/api-core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class UserService {
  private _user = signal<UserInfo | null>(null);
  private _menus = signal<MenuItem[]>([]);

  readonly user = this._user.asReadonly();
  readonly menus = this._menus.asReadonly();
  readonly isLoggedIn = computed(() => !!this._user());
  readonly displayName = computed(() => this._user()?.displayName || this._user()?.name || '');

  constructor(private api: ApiService) {}

  async login(username: string, password: string) {
    const res = await this.api.user.login({ username, password });
    if (res.data) {
      await this.fetchUserInfo();
      await this.fetchMenus();
    }
    return res;
  }

  async logout() {
    await this.api.user.logout();
    this._user.set(null);
    this._menus.set([]);
  }

  async fetchUserInfo() {
    const res = await this.api.user.info();
    if (res.data) this._user.set(res.data);
  }

  async fetchMenus() {
    const res = await this.api.menu.getMenuTree();
    if (res.data) this._menus.set(res.data);
  }

  /** 递归查找菜单树中 URL 匹配的节点 */
  private findMenu(menus: MenuItem[], path: string): MenuItem | undefined {
    for (const m of menus) {
      if (m.url && path.toLowerCase().endsWith(m.url.toLowerCase())) return m;
      if (m.children?.length) {
        const found = this.findMenu(m.children, path);
        if (found) return found;
      }
    }
    return undefined;
  }

  /** 获取指定路径的菜单权限 */
  getMenuPermission(path: string): Record<string, string> {
    const item = this.findMenu(this._menus(), path);
    return item?.permissions ?? {};
  }
}
