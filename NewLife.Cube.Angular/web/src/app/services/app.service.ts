import { Injectable, signal } from '@angular/core';
import type { LoginConfig } from '@cube/api-core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AppService {
  private _collapsed = signal(false);
  private _darkMode = signal(false);
  private _loginConfig = signal<LoginConfig | null>(null);

  readonly collapsed = this._collapsed.asReadonly();
  readonly darkMode = this._darkMode.asReadonly();
  readonly loginConfig = this._loginConfig.asReadonly();

  constructor(private api: ApiService) {}

  toggleCollapsed() {
    this._collapsed.update(v => !v);
  }

  toggleDarkMode() {
    this._darkMode.update(v => !v);
  }

  async fetchLoginConfig() {
    const res = await this.api.user.getLoginConfig();
    if (res.data) this._loginConfig.set(res.data);
  }
}
