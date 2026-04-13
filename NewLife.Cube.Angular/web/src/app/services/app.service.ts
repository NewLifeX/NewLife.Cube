import { Injectable, signal } from '@angular/core';
import type { SiteInfo } from '@cube/api-core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AppService {
  private _collapsed = signal(false);
  private _darkMode = signal(false);
  private _siteInfo = signal<SiteInfo | null>(null);

  readonly collapsed = this._collapsed.asReadonly();
  readonly darkMode = this._darkMode.asReadonly();
  readonly siteInfo = this._siteInfo.asReadonly();

  constructor(private api: ApiService) {}

  toggleCollapsed() {
    this._collapsed.update(v => !v);
  }

  toggleDarkMode() {
    this._darkMode.update(v => !v);
  }

  async fetchSiteInfo() {
    const res = await this.api.config.getSiteInfo();
    if (res.data) this._siteInfo.set(res.data);
  }
}
