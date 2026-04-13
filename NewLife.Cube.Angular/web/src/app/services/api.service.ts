import { Injectable } from '@angular/core';
import { createCubeApi, type CubeApi } from '@cube/api-core';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private _api: CubeApi;

  constructor() {
    this._api = createCubeApi({ baseURL: '' });
  }

  get api(): CubeApi {
    return this._api;
  }

  get user() { return this._api.user; }
  get menu() { return this._api.menu; }
  get page() { return this._api.page; }
  get config() { return this._api.config; }
}
