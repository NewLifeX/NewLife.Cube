import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet, RouterLink } from '@angular/router';
import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar, type TabItem } from '@newlifex/cube';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzTagModule } from 'ng-zorro-antd/tag';
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
    NzSwitchModule, NzIconModule, NzDrawerModule, NzTagModule,
  ],
  template: `
    <nz-layout style="min-height: 100vh">
      @if (!isNarrow) {
        <nz-sider nzCollapsible [nzCollapsed]="appService.collapsed()" [nzTrigger]="null" [nzWidth]="220" [nzCollapsedWidth]="48">
          <div style="height: 48px; display: flex; align-items: center; justify-content: center; color: #fff; font-weight: bold; font-size: 16px;">
            {{ appService.collapsed() ? '魔' : (appService.siteConfig()?.name || '魔方管理平台') }}
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
      }

      <nz-drawer [nzVisible]="mobileOpen" nzPlacement="left" [nzWidth]="220" [nzTitle]="appService.siteConfig()?.name || '魔方管理平台'" (nzOnClose)="mobileOpen = false">
        <ng-container *nzDrawerContent>
          <ul nz-menu nzMode="inline">
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
        </ng-container>
      </nz-drawer>

      <nz-layout>
        <nz-header style="background: #fff; padding: 0 16px; display: flex; align-items: center; justify-content: space-between;">
          <div style="display: flex; align-items: center; gap: 12px;">
            <button nz-button nzType="text" (click)="isNarrow ? (mobileOpen = true) : appService.toggleCollapsed()">
              <span nz-icon [nzType]="!isNarrow && !appService.collapsed() ? 'menu-fold' : 'menu-unfold'"></span>
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

        <nz-content class="layout-content">
          @if (tabs.length > 1) {
            <div class="tab-strip">
              @for (t of tabs; track t.id) {
                <nz-tag
                  [nzMode]="t.closable ? 'closeable' : 'default'"
                  [nzColor]="t.id === activeTabId ? 'blue' : undefined"
                  (click)="activateTab(t)"
                  (nzOnClose)="closeTabAndNavigate(t.id)"
                >
                  {{ t.title }}
                </nz-tag>
              }
              <button nz-button nzType="link" nzSize="small" (click)="closeAll()">关闭全部</button>
            </div>
          }
          <router-outlet />
        </nz-content>
      </nz-layout>
    </nz-layout>
  `,
  styles: [
    `
      .layout-content {
        padding: 16px;
      }
      @media (max-width: 767px) {
        .layout-content {
          padding: 8px;
        }
      }
    .tab-strip {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 12px;
    }
    `,
  ],
})
export class LayoutComponent implements OnInit {
  constructor(
    public appService: AppService,
    public userService: UserService,
    private router: Router,
  ) {}

  syncTab() {
    const url = this.router.url.split('#')[0];
    const path = url.split('?')[0];
    const tab = createTab(path, url, path);
    this.tabs = openTab(this.tabs, tab);
    this.activeTabId = tab.id;
  }

  activateTab(tab: TabItem) {
    if (tab.id === this.activeTabId) return;
    void this.router.navigateByUrl(tab.fullPath);
  }

  closeTabAndNavigate(id: string) {
    const result = closeTab(this.tabs, id, this.activeTabId);
    this.tabs = result.tabs;
    if (result.nextActiveId) {
      this.activeTabId = result.nextActiveId;
      const next = result.tabs.find((t) => t.id === result.nextActiveId);
      if (next) void this.router.navigateByUrl(next.fullPath);
    }
  }

  closeAll() {
    this.tabs = closeAllTabs(this.tabs);
    const home = this.tabs[0];
    if (home) {
      this.activeTabId = home.id;
      void this.router.navigateByUrl(home.fullPath);
    }
  }

  /** 窄屏是否由本组件自动折叠，宽屏时据此恢复展开 */
  private autoCollapsed = false;
  /** 窄屏状态：使用抽屉菜单 */
  isNarrow = false;
  /** 窄屏抽屉是否打开 */
  mobileOpen = false;

  // --- 多标签页 ---
  tabs: TabItem[] = [];
  activeTabId = '';

  ngOnInit() {
    this.syncTab();
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) this.syncTab();
    });
    this.appService.fetchSiteConfig();
    if (!this.userService.isLoggedIn()) {
      this.userService.fetchUserInfo().then(() => {
        if (this.userService.isLoggedIn()) this.userService.fetchMenus();
        else this.router.navigate(['/login']);
      });
    }
    this.syncResponsive();
  }

  /** 窄屏自动折叠侧边栏，宽屏恢复用户手动展开 */
  @HostListener('window:resize')
  syncResponsive() {
    const narrow = shouldAutoCollapseSidebar(window.innerWidth);
    this.isNarrow = narrow;
    if (narrow) {
      if (!this.appService.collapsed()) {
        this.appService.toggleCollapsed();
        this.autoCollapsed = true;
      }
    } else {
      this.mobileOpen = false;
      if (this.autoCollapsed) {
        if (this.appService.collapsed()) this.appService.toggleCollapsed();
        this.autoCollapsed = false;
      }
    }
  }

  navigateTo(url?: string) {
    this.mobileOpen = false;
    if (url) this.router.navigateByUrl(url);
  }

  async handleLogout() {
    await this.userService.logout();
    this.router.navigate(['/login']);
  }
}
