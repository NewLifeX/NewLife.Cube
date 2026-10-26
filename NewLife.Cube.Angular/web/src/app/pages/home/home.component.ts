import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzPageHeaderModule } from 'ng-zorro-antd/page-header';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import {
  appendMonitorPoint,
  buildMonitorChart,
  buildWorkbenchView,
  createWorkbenchLoader,
  MONITOR_POLL_INTERVAL,
  type MonitorPoint,
  type WorkbenchData,
  type WorkbenchView,
  type WorkbenchWidgetView,
} from '@newlifex/page-logic';
import { ApiService } from '../../services/api.service';

/** 个人信息卡行标签，顺序与共享视图模型一致 */
const PROFILE_LABELS = ['登录次数', '最后登录', '登录地址', '注册时间'];

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    NzCardModule,
    NzStatisticModule,
    NzGridModule,
    NzPageHeaderModule,
    NzButtonModule,
    NzTagModule,
    NzAlertModule,
    NzSpinModule,
    NzEmptyModule,
    NzDescriptionsModule,
  ],
  template: `
    <nz-spin [nzSpinning]="loading && !view">
      @if (error && !view) {
        <nz-alert nzType="error" [nzMessage]="'工作台加载失败：' + error" nzShowIcon></nz-alert>
        <button nz-button nzType="primary" style="margin-top: 16px" (click)="load()">重试</button>
      } @else {
        <div class="banner">
          <div>
            <h2>{{ greeting }}，{{ view?.displayName || '-' }}</h2>
            <p class="banner-sub">{{ today }}@if (view?.roleText) { · {{ view!.roleText }} }</p>
          </div>
          <button nz-button [nzLoading]="loading" (click)="reload()">刷新</button>
        </div>

        @if (error) {
          <nz-alert nzType="warning" [nzMessage]="error" nzShowIcon style="margin-bottom: 16px"></nz-alert>
        }

        @if (view?.kpis?.length) {
          <div nz-row [nzGutter]="16" style="margin-bottom: 16px">
            @for (kpi of view!.kpis; track kpi.name) {
              <div nz-col [nzXs]="24" [nzSm]="12" [nzMd]="6">
                <nz-card class="kpi" [nzHoverable]="!!kpi.url" (click)="openKpi(kpi.url)">
                  <nz-statistic [nzTitle]="kpi.label" [nzValue]="kpi.value"></nz-statistic>
                  @if (kpi.trend) {
                    <p class="kpi-trend">{{ kpi.trend }}</p>
                  }
                </nz-card>
              </div>
            }
          </div>
        }

        @if (view?.widgets?.length) {
          <div nz-row [nzGutter]="16">
            @for (widget of view!.widgets; track widget.name) {
              <div nz-col [nzXs]="24" [nzMd]="widget.cols">
                <nz-card [nzTitle]="widget.title" [attr.data-widget]="widget.name">
                  <ng-container *ngTemplateOutlet="widgetBody; context: { $implicit: widget }"></ng-container>
                </nz-card>
              </div>
            }
          </div>
        }

        @if (view && !view.hasContent) {
          <nz-empty nzNotFoundContent="暂无工作台数据"></nz-empty>
        }
      }
    </nz-spin>

    <ng-template #widgetBody let-widget>
      @if (widget.kind === 'monitor') {
        @if (monitorPoints.length) {
          <svg [attr.viewBox]="'0 0 ' + chart.width + ' ' + chart.height" class="monitor-chart" preserveAspectRatio="none">
            <polyline [attr.points]="chart.cpuPoints" fill="none" stroke="#1677ff" stroke-width="2" />
            <polyline [attr.points]="chart.memPoints" fill="none" stroke="#52c41a" stroke-width="2" />
          </svg>
          <div class="monitor-legend">
            <span>CPU {{ latestMonitor?.cpu ?? 0 }}%</span>
            <span>内存 {{ latestMonitor?.memory ?? 0 }}%</span>
            @if (latestMonitor?.memUsed != null && latestMonitor?.memTotal) {
              <span>{{ latestMonitor!.memUsed }} / {{ latestMonitor!.memTotal }} MB</span>
            }
          </div>
        } @else {
          <nz-empty nzNotFoundContent="正在采集监控数据"></nz-empty>
        }
      } @else if (widget.kind === 'profile' && widget.profile) {
        <div class="profile-head">
          <div class="profile-avatar">{{ widget.profile.initial }}</div>
          <div>
            <div class="profile-name">{{ widget.profile.displayName }}</div>
            <div class="profile-tags">
              <nz-tag [nzColor]="widget.profile.online ? 'success' : 'default'">
                {{ widget.profile.online ? '在线' : '离线' }}
              </nz-tag>
              @for (role of widget.profile.roles; track role) {
                <nz-tag>{{ role }}</nz-tag>
              }
            </div>
          </div>
        </div>
        <nz-descriptions nzSize="small" [nzColumn]="2" nzBordered>
          @for (row of widget.profile.rows; track row.key; let i = $index) {
            <nz-descriptions-item [nzTitle]="profileLabels[i] || row.key">{{ row.text }}</nz-descriptions-item>
          }
        </nz-descriptions>
      } @else if (widget.kind === 'quickLinks') {
        @if (widget.links?.length) {
          <div class="links">
            @for (link of widget.links; track link.url) {
              <button nz-button (click)="navigate(link.url)">{{ link.name }}</button>
            }
          </div>
        } @else {
          <nz-empty nzNotFoundContent="暂无快捷入口"></nz-empty>
        }
      } @else if (widget.kind === 'loginLog') {
        <div nz-row [nzGutter]="16">
          <div nz-col [nzXs]="24" [nzSm]="12">
            <div class="list-title">最近登录</div>
            @if (widget.logins?.length) {
              <ul class="plain-list">
                @for (item of widget.logins; track $index) {
                  <li>
                    <span>{{ item.title }}</span>
                    <span class="muted">{{ item.subtitle }} · {{ item.time }}</span>
                  </li>
                }
              </ul>
            } @else {
              <nz-empty nzNotFoundContent="暂无登录记录"></nz-empty>
            }
          </div>
          <div nz-col [nzXs]="24" [nzSm]="12">
            <div class="list-title">在线用户</div>
            @if (widget.onlines?.length) {
              <ul class="plain-list">
                @for (item of widget.onlines; track $index) {
                  <li>
                    <span>{{ item.title }}</span>
                    <span class="muted">{{ item.subtitle }} · {{ item.time }}</span>
                  </li>
                }
              </ul>
            } @else {
              <nz-empty nzNotFoundContent="暂无在线用户"></nz-empty>
            }
          </div>
        </div>
      } @else if (widget.kind === 'kv' && widget.rows?.length) {
        <nz-descriptions nzSize="small" [nzColumn]="1" nzBordered>
          @for (row of widget.rows; track row.key) {
            <nz-descriptions-item [nzTitle]="row.key">
              @if (row.link) {
                <a [href]="row.link.href" [attr.target]="row.link.external ? '_blank' : null" rel="noopener noreferrer">
                  {{ row.link.text }}
                </a>
              } @else {
                {{ row.text }}
              }
            </nz-descriptions-item>
          }
        </nz-descriptions>
      } @else if (widget.kind === 'text') {
        <p>{{ widget.text }}</p>
      } @else {
        <nz-empty nzNotFoundContent="暂无数据"></nz-empty>
      }
    </ng-template>
  `,
  styles: [
    `
      .banner {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
        margin-bottom: 16px;
      }
      .banner h2 {
        margin: 0;
      }
      .banner-sub {
        margin: 4px 0 0;
        color: rgba(0, 0, 0, 0.45);
        font-size: 13px;
      }
      .kpi {
        height: 100%;
      }
      .kpi-trend {
        margin: 4px 0 0;
        color: rgba(0, 0, 0, 0.45);
        font-size: 12px;
      }
      .monitor-chart {
        width: 100%;
        height: 120px;
      }
      .monitor-legend {
        display: flex;
        gap: 16px;
        font-size: 12px;
        color: rgba(0, 0, 0, 0.45);
      }
      .profile-head {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 12px;
      }
      .profile-avatar {
        width: 44px;
        height: 44px;
        border-radius: 50%;
        background: #1677ff;
        color: #fff;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 600;
      }
      .profile-name {
        font-weight: 600;
      }
      .profile-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
        margin-top: 4px;
      }
      .links {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .list-title {
        margin-bottom: 4px;
        font-size: 12px;
        color: rgba(0, 0, 0, 0.45);
      }
      .plain-list {
        margin: 0;
        padding: 0;
        list-style: none;
      }
      .plain-list li {
        display: flex;
        justify-content: space-between;
        gap: 8px;
        padding: 4px 0;
        font-size: 13px;
        border-bottom: 1px dashed rgba(0, 0, 0, 0.06);
      }
      .muted {
        color: rgba(0, 0, 0, 0.45);
      }
    `,
  ],
})
export class HomeComponent implements OnInit, OnDestroy {
  view: WorkbenchView | null = null;
  loading = false;
  error = '';
  monitorPoints: MonitorPoint[] = [];
  chart = buildMonitorChart([], 320, 120);
  latestMonitor: MonitorPoint | null = null;
  profileLabels = PROFILE_LABELS;
  today = new Date().toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  });
  greeting = this.resolveGreeting();

  private loader;
  private timer: ReturnType<typeof setInterval> | null = null;
  private polling = false;

  constructor(
    private apiService: ApiService,
    private router: Router,
  ) {
    this.loader = createWorkbenchLoader(this.apiService.api);
  }

  ngOnInit(): void {
    void this.load();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const result = await this.loader.load();
      if (result.data) {
        this.view = buildWorkbenchView(result.data as WorkbenchData);
        this.syncPolling();
      } else {
        this.error = result.error || '工作台加载失败';
      }
    } catch (ex) {
      this.error = ex instanceof Error ? ex.message : String(ex);
    } finally {
      this.loading = false;
    }
  }

  async reload(): Promise<void> {
    await this.load();
  }

  openKpi(url?: string): void {
    if (url) void this.router.navigateByUrl(url);
  }

  navigate(url: string): void {
    if (url) void this.router.navigateByUrl(url);
  }

  private syncPolling(): void {
    if (this.view?.hasChartWidget) this.startPolling();
    else this.stopPolling();
  }

  private startPolling(): void {
    if (this.timer) return;
    void this.pollMonitor();
    this.timer = setInterval(() => void this.pollMonitor(), MONITOR_POLL_INTERVAL);
  }

  private stopPolling(): void {
    if (!this.timer) return;
    clearInterval(this.timer);
    this.timer = null;
  }

  private async pollMonitor(): Promise<void> {
    if (this.polling) return;
    this.polling = true;
    try {
      this.monitorPoints = appendMonitorPoint(this.monitorPoints, await this.loader.pollMonitor());
      this.chart = buildMonitorChart(this.monitorPoints, 320, 120);
      this.latestMonitor = this.monitorPoints[this.monitorPoints.length - 1] ?? null;
    } catch {
      // 监控曲线属于增强信息，单次失败不打断工作台主流程
    } finally {
      this.polling = false;
    }
  }

  private resolveGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 6) return '夜深了';
    if (hour < 12) return '早上好';
    if (hour < 14) return '中午好';
    if (hour < 18) return '下午好';
    return '晚上好';
  }
}

