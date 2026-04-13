import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzPageHeaderModule } from 'ng-zorro-antd/page-header';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, NzCardModule, NzStatisticModule, NzGridModule, NzPageHeaderModule],
  template: `
    <nz-page-header nzTitle="首页"></nz-page-header>
    <div nz-row [nzGutter]="16">
      @for (card of stats; track card.title) {
        <div nz-col [nzXs]="24" [nzSm]="12" [nzMd]="6">
          <nz-card>
            <nz-statistic [nzTitle]="card.title" [nzValue]="card.value"></nz-statistic>
          </nz-card>
        </div>
      }
    </div>
  `,
})
export class HomeComponent {
  stats = [
    { title: '用户总数', value: 0 },
    { title: '今日访问', value: 0 },
    { title: '在线用户', value: 0 },
    { title: '系统消息', value: 0 },
  ];
}
