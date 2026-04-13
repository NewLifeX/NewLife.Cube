import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink } from '@angular/router';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { FormsModule } from '@angular/forms';
import { AppService } from '../services/app.service';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule, RouterOutlet, RouterLink, FormsModule,
    NzLayoutModule, NzMenuModule, NzBreadCrumbModule,
    NzButtonModule, NzDropDownModule, NzAvatarModule,
    NzSwitchModule, NzIconModule,
  ],
  template: `
    <nz-layout style="min-height: 100vh">
      <nz-sider nzCollapsible [nzCollapsed]="appService.collapsed()" [nzTrigger]="null" nzWidth="220" nzCollapsedWidth="48">
        <div style="height: 48px; display: flex; align-items: center; justify-content: center; color: #fff; font-weight: bold; font-size: 16px;">
          {{ appService.collapsed() ? '魔' : (appService.siteInfo()?.name || '魔方管理平台') }}
        </div>
        <ul nz-menu nzTheme="dark" nzMode="inline">
          @for (item of userService.menus(); track item.id) {
            @if (item.children?.length) {
              <li nz-submenu [nzTitle]="item.displayName || item.name" nzIcon="appstore">
                @for (child of item.children; track child.id) {
                  <li nz-menu-item (click)="navigateTo(child.url)">{{ child.displayName || child.name }}</li>
                }
              </li>
            } @else {
              <li nz-menu-item nzIcon="file" (click)="navigateTo(item.url)">{{ item.displayName || item.name }}</li>
            }
          }
        </ul>
      </nz-sider>

      <nz-layout>
        <nz-header style="background: #fff; padding: 0 16px; display: flex; align-items: center; justify-content: space-between;">
          <div style="display: flex; align-items: center; gap: 12px;">
            <button nz-button nzType="text" (click)="appService.toggleCollapsed()">
              <span nz-icon [nzType]="appService.collapsed() ? 'menu-unfold' : 'menu-fold'"></span>
            </button>
            <nz-breadcrumb>
              <nz-breadcrumb-item><a routerLink="/home">首页</a></nz-breadcrumb-item>
            </nz-breadcrumb>
          </div>
          <div style="display: flex; align-items: center; gap: 12px;">
            <nz-switch [ngModel]="appService.darkMode()" (ngModelChange)="appService.toggleDarkMode()"></nz-switch>
            <a nz-dropdown [nzDropdownMenu]="userMenu" nzTrigger="click" style="cursor: pointer; display: flex; align-items: center; gap: 8px;">
              <nz-avatar [nzText]="userService.displayName()?.charAt(0) || 'U'" nzSize="small"></nz-avatar>
              <span>{{ userService.displayName() }}</span>
            </a>
            <nz-dropdown-menu #userMenu="nzDropdownMenu">
              <ul nz-menu>
                <li nz-menu-item (click)="handleLogout()">退出登录</li>
              </ul>
            </nz-dropdown-menu>
          </div>
        </nz-header>

        <nz-content style="padding: 16px;">
          <router-outlet />
        </nz-content>
      </nz-layout>
    </nz-layout>
  `,
})
export class LayoutComponent implements OnInit {
  constructor(
    public appService: AppService,
    public userService: UserService,
    private router: Router,
  ) {}

  ngOnInit() {
    this.appService.fetchSiteInfo();
    if (!this.userService.isLoggedIn()) {
      this.userService.fetchUserInfo().then(() => {
        if (this.userService.isLoggedIn()) this.userService.fetchMenus();
        else this.router.navigate(['/login']);
      });
    }
  }

  navigateTo(url?: string) {
    if (url) this.router.navigateByUrl(url);
  }

  async handleLogout() {
    await this.userService.logout();
    this.router.navigate(['/login']);
  }
}
