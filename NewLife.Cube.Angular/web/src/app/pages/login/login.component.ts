import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzMessageService } from 'ng-zorro-antd/message';
import type { OAuthProvider } from '@cube/api-core';
import { ApiService } from '../../services/api.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, NzCardModule, NzFormModule, NzInputModule, NzButtonModule, NzDividerModule],
  template: `
    <div style="display: flex; justify-content: center; align-items: center; min-height: 100vh; background: linear-gradient(135deg, #1890ff 0%, #722ed1 100%);">
      <nz-card style="width: 400px;" [nzBordered]="false">
        <div style="text-align: center; font-size: 20px; font-weight: bold; margin-bottom: 24px;">登录</div>
        <form nz-form (ngSubmit)="handleLogin()">
          <nz-form-item>
            <nz-form-label>用户名</nz-form-label>
            <nz-form-control>
              <input nz-input [(ngModel)]="username" name="username" placeholder="请输入用户名" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label>密码</nz-form-label>
            <nz-form-control>
              <input nz-input type="password" [(ngModel)]="password" name="password" placeholder="请输入密码" />
            </nz-form-control>
          </nz-form-item>
          <button nz-button nzType="primary" nzBlock nzSize="large" [nzLoading]="loading">登录</button>
        </form>
        @if (oauthProviders.length) {
          <nz-divider nzText="第三方登录"></nz-divider>
          <div style="display: flex; gap: 8px; justify-content: center;">
            @for (p of oauthProviders; track p.name) {
              <button nz-button (click)="oauthLogin(p.name)">{{ p.nickName || p.name }}</button>
            }
          </div>
        }
      </nz-card>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  username = '';
  password = '';
  loading = false;
  oauthProviders: OAuthProvider[] = [];

  constructor(
    private api: ApiService,
    private userService: UserService,
    private router: Router,
    private message: NzMessageService,
  ) {}

  async ngOnInit() {
    const res = await this.api.user.getLoginConfig();
    if (res.data?.providers) this.oauthProviders = res.data.providers;
  }

  async handleLogin() {
    if (!this.username || !this.password) {
      this.message.warning('请输入用户名和密码');
      return;
    }
    this.loading = true;
    try {
      const res = await this.userService.login(this.username, this.password);
      if (res.data) {
        this.message.success('登录成功');
        this.router.navigate(['/home']);
      } else {
        this.message.error(res.message || '登录失败');
      }
    } finally {
      this.loading = false;
    }
  }

  oauthLogin(name: string) {
    window.location.href = `/Sso/LoginExternal?provider=${name}`;
  }
}
