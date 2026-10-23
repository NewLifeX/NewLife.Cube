import { Injectable, signal } from '@angular/core';
import type { SiteConfig } from '@newlifex/api-core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AppService {
  private _collapsed = signal(false);
  private _darkMode = signal(false);
  private _siteConfig = signal<SiteConfig | null>(null);

  readonly collapsed = this._collapsed.asReadonly();
  readonly darkMode = this._darkMode.asReadonly();
  readonly siteConfig = this._siteConfig.asReadonly();

  constructor(private api: ApiService) {}

  toggleCollapsed() {
    this._collapsed.update(v => !v);
  }

  toggleDarkMode() {
    this._darkMode.update(v => !v);
  }

  async fetchSiteConfig() {
    const res = await this.api.site.getSite();
    if (res.data) this._siteConfig.set(res.data);
  }
}
