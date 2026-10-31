import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzTimePickerModule } from 'ng-zorro-antd/time-picker';
import type { AreaOption, DataField, TimeSpanParts, TimeSpanUnit } from '@newlifex/cube';
import {
  createAreaCascade,
  joinTimeSpan,
  resolveWidget,
  splitTimeSpan,
  timeSpanUnitOfDescription,
} from '@newlifex/cube';
import { ApiService } from '../services/api.service';

interface SelectOption {
  value: string;
  label: string;
}

/**
 * 字段渲染组件（Angular + ng-zorro 皮肤）
 *
 * 控件类型一律由共享规则引擎 `resolveWidget` 决定，本组件只把抽象 widget 映射到 ng-zorro 控件：
 * 与 Vue/React/Svelte 皮肤共用同一套 ItemType 口径（密码/颜色/时长/地区/映射下拉/多选/日期…）。
 */
@Component({
  selector: 'app-field-input',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    NzInputModule, NzInputNumberModule, NzSelectModule,
    NzSwitchModule, NzDatePickerModule, NzTimePickerModule,
  ],
  template: `
    @if (isSelect) {
      <nz-select [ngModel]="selectValue" (ngModelChange)="onSelectChange($event)"
        [nzMode]="isMulti ? 'multiple' : 'default'"
        [nzPlaceHolder]="'请选择' + label" nzAllowClear style="width: 100%">
        @for (opt of options; track opt.value) {
          <nz-option [nzValue]="opt.value" [nzLabel]="opt.label"></nz-option>
        }
      </nz-select>
    } @else {
    @switch (widgetType) {
      @case ('area') {
        <div class="field-area">
          @for (levelOptions of areaOptions; track $index; let level = $index) {
            <nz-select
              [ngModel]="areaPath[level] ?? null"
              (ngModelChange)="onAreaChange(level, $event)"
              [nzPlaceHolder]="level === 0 ? '请选择省份' : '请选择下级'"
              nzAllowClear
              style="width: 100%; margin-bottom: 4px">
              @for (opt of levelOptions; track opt.id) {
                <nz-option [nzValue]="opt.id" [nzLabel]="opt.name"></nz-option>
              }
            </nz-select>
          }
        </div>
      }
      @case ('timeSpan') {
        <div class="field-timespan">
          @for (part of spanPartsList; track part.key) {
            <nz-input-number
              [ngModel]="part.value"
              (ngModelChange)="onSpanChange(part.key, $event)"
              [nzMin]="0"
              [nzMax]="part.max"
              [nzStep]="part.step"
              [nzPlaceHolder]="part.hint"
              style="width: 88px"></nz-input-number>
          }
        </div>
      }
      @case ('switch') {
        <nz-switch [ngModel]="!!value" (ngModelChange)="valueChange.emit($event)"></nz-switch>
      }
      @case ('number') {
        <nz-input-number [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请输入' + label" style="width: 100%"></nz-input-number>
      }
      @case ('mapInput') {
        <nz-input-number [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请输入' + label" style="width: 100%"></nz-input-number>
      }
      @case ('date') {
        <nz-date-picker [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请选择' + label" style="width: 100%"></nz-date-picker>
      }
      @case ('datetime') {
        <nz-date-picker [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          nzShowTime [nzPlaceHolder]="'请选择' + label" style="width: 100%"></nz-date-picker>
      }
      @case ('time') {
        <nz-time-picker [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请选择' + label" style="width: 100%"></nz-time-picker>
      }
      @case ('textarea') {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" [nzAutosize]="{ minRows: 3 }"></textarea>
      }
      @case ('html') {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" [nzAutosize]="{ minRows: 3 }"></textarea>
      }
      @case ('markdown') {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" [nzAutosize]="{ minRows: 6 }"></textarea>
      }
      @case ('json') {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" [nzAutosize]="{ minRows: 6 }" style="font-family: monospace"></textarea>
      }
      @case ('code') {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" [nzAutosize]="{ minRows: 8 }" style="font-family: monospace"></textarea>
      }
      @case ('password') {
        <input nz-input type="password" [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + label" autocomplete="new-password" />
      }
      @case ('color') {
        <div class="field-color">
          <span class="field-color__dot" [style.background]="value || '#000000'"></span>
          <input nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
            placeholder="请输入颜色值，如 #1677ff" />
        </div>
      }
      @case ('image') {
        <input nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          placeholder="图片地址" />
      }
      @case ('file') {
        <input nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          placeholder="附件地址" />
      }
      @case ('readonly') {
        <span>{{ value ?? '-' }}</span>
      }
      @default {
        <input nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [type]="inputType" [placeholder]="'请输入' + label" />
      }
    }
    }
  `,
  styles: [`
    .field-timespan { display: flex; gap: 6px; }
    .field-color { display: flex; align-items: center; gap: 8px; }
    .field-color__dot { width: 14px; height: 14px; border-radius: 3px; border: 1px solid rgba(0, 0, 0, 0.24); }
  `],
})
export class FieldInputComponent implements OnChanges {
  @Input() field!: DataField;
  @Input() value: any;
  @Output() valueChange = new EventEmitter<any>();

  constructor(private readonly apiService: ApiService) {}

  get mapping() {
    return resolveWidget(this.field);
  }

  get widgetType(): string {
    return this.mapping.widget;
  }

  get label(): string {
    return this.field?.displayName || this.field?.name || '';
  }

  get isMulti(): boolean {
    return this.widgetType === 'lovMulti' || !!this.mapping.props?.['multiple'];
  }

  /** 下拉类控件（select / lov / lovMulti / mapSelect）统一走同一个 nz-select */
  get isSelect(): boolean {
    return ['select', 'lov', 'lovMulti', 'mapSelect'].includes(this.widgetType);
  }

  get inputType(): string {
    switch (this.widgetType) {
      case 'email':
        return 'email';
      case 'phone':
        return 'tel';
      case 'url':
        return 'url';
      case 'icon':
        return 'text';
      default:
        return 'text';
    }
  }

  /** 下拉选项：后端 dataSource（引擎已随 props 透传） */
  get options(): SelectOption[] {
    const src = (this.mapping.props?.['dataSource'] ?? this.field?.dataSource) as unknown;
    if (!src) return [];
    try {
      const map = (typeof src === 'string' ? JSON.parse(src) : src) as Record<string, string>;
      return Object.entries(map).map(([value, label]) => ({ value, label }));
    } catch {
      return [];
    }
  }

  get selectValue(): any {
    return this.isMulti ? FieldInputComponent.toMultiArray(this.value) : this.value;
  }

  /** 多选值归一：数组 / 逗号串 / JSON 串 */
  private static toMultiArray(v: any): string[] {
    if (v == null || v === '') return [];
    if (Array.isArray(v)) return v.map(String);
    const text = String(v).trim();
    if (!text) return [];
    try {
      const parsed = JSON.parse(text);
      if (Array.isArray(parsed)) return parsed.map(String);
    } catch {
      /* 非 JSON，按逗号串处理 */
    }
    return text.split(',').filter(Boolean);
  }

  onSelectChange(v: any): void {
    if (!this.isMulti) {
      this.valueChange.emit(v ?? '');
      return;
    }
    const array = Array.isArray(v) ? v : FieldInputComponent.toMultiArray(v);
    this.valueChange.emit(array.join(','));
  }

  // ── 时长 ──
  get spanUnit(): TimeSpanUnit {
    return (this.mapping.props?.['unit'] as TimeSpanUnit) ?? timeSpanUnitOfDescription(this.field?.description);
  }

  spanParts: TimeSpanParts = { hours: 0, minutes: 0, seconds: 0 };

  get spanPartsList() {
    return [
      { key: 'hours' as const, value: this.spanParts.hours, max: undefined, step: 1, hint: '时' },
      { key: 'minutes' as const, value: this.spanParts.minutes, max: 59, step: 1, hint: '分' },
      {
        key: 'seconds' as const,
        value: this.spanParts.seconds,
        max: 59,
        step: this.spanUnit === 'milliseconds' ? 0.001 : 1,
        hint: '秒',
      },
    ];
  }

  onSpanChange(key: keyof TimeSpanParts, v: any): void {
    this.spanParts = { ...this.spanParts, [key]: Number(v ?? 0) || 0 };
    this.valueChange.emit(joinTimeSpan(this.spanParts, this.spanUnit));
  }

  // ── 地区级联 ──
  areaOptions: AreaOption[][] = [];
  areaPath: number[] = [];
  private areaCascade: ReturnType<typeof createAreaCascade> | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.field) return;

    if (this.widgetType === 'timeSpan') {
      this.spanParts = splitTimeSpan(this.value, this.spanUnit);
    }

    if (this.widgetType === 'area') {
      void this.syncArea(changes['field'] ? true : false);
    }
  }

  private async syncArea(reset: boolean): Promise<void> {
    if (reset || !this.areaCascade) {
      const level = Number(this.mapping.props?.['areaLevel'] ?? 3) || 3;
      this.areaCascade = createAreaCascade(this.apiService.api.area, level);
      const state = await this.areaCascade.init(Number(this.value ?? 0) || 0);
      this.areaOptions = state.options;
      this.areaPath = state.path;
      return;
    }

    const current = this.areaCascade.snapshot.value;
    if (Number(this.value ?? 0) !== Number(current ?? 0)) {
      const state = await this.areaCascade.init(Number(this.value ?? 0) || 0);
      this.areaOptions = state.options;
      this.areaPath = state.path;
    }
  }

  async onAreaChange(level: number, v: any): Promise<void> {
    if (!this.areaCascade) return;
    const state = await this.areaCascade.selectLevel(level, v === '' || v == null ? null : Number(v));
    this.areaOptions = state.options;
    this.areaPath = state.path;
    this.valueChange.emit(state.value ?? '');
  }
}
