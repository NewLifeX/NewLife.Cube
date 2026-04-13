import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import type { DataField } from '@cube/api-core';
import { inferWidgetType, WidgetType } from '@cube/field-mapping';

interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-field-input',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    NzInputModule, NzInputNumberModule, NzSelectModule,
    NzSwitchModule, NzDatePickerModule,
  ],
  template: `
    @switch (widgetType) {
      @case (WidgetType.Switch) {
        <nz-switch [ngModel]="!!value" (ngModelChange)="valueChange.emit($event)"></nz-switch>
      }
      @case (WidgetType.Select) {
        <nz-select [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请选择' + (field.displayName || field.name)" nzAllowClear style="width: 100%">
          @for (opt of options; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label"></nz-option>
          }
        </nz-select>
      }
      @case (WidgetType.TagList) {
        <nz-select [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请选择' + (field.displayName || field.name)" nzAllowClear style="width: 100%">
          @for (opt of options; track opt.value) {
            <nz-option [nzValue]="opt.value" [nzLabel]="opt.label"></nz-option>
          }
        </nz-select>
      }
      @case (WidgetType.Number) {
        <nz-input-number [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请输入' + (field.displayName || field.name)" style="width: 100%"></nz-input-number>
      }
      @case (WidgetType.Decimal) {
        <nz-input-number [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPrecision]="field.precision || 2"
          [nzPlaceHolder]="'请输入' + (field.displayName || field.name)" style="width: 100%"></nz-input-number>
      }
      @case (WidgetType.DateTime) {
        <nz-date-picker [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          nzShowTime [nzPlaceHolder]="'请选择' + (field.displayName || field.name)" style="width: 100%"></nz-date-picker>
      }
      @case (WidgetType.Date) {
        <nz-date-picker [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [nzPlaceHolder]="'请选择' + (field.displayName || field.name)" style="width: 100%"></nz-date-picker>
      }
      @case (WidgetType.TextArea) {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + (field.displayName || field.name)" [nzAutosize]="{ minRows: 3 }"></textarea>
      }
      @case (WidgetType.RichText) {
        <textarea nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + (field.displayName || field.name)" [nzAutosize]="{ minRows: 3 }"></textarea>
      }
      @case (WidgetType.Password) {
        <input nz-input type="password" [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + (field.displayName || field.name)" />
      }
      @case (WidgetType.ReadOnly) {
        <span>{{ value ?? '-' }}</span>
      }
      @default {
        <input nz-input [ngModel]="value" (ngModelChange)="valueChange.emit($event)"
          [placeholder]="'请输入' + (field.displayName || field.name)" />
      }
    }
  `,
})
export class FieldInputComponent {
  @Input() field!: DataField;
  @Input() value: any;
  @Output() valueChange = new EventEmitter<any>();

  WidgetType = WidgetType;

  get widgetType(): WidgetType {
    return inferWidgetType(this.field);
  }

  get options(): SelectOption[] {
    if (!this.field.dataSource) return [];
    try {
      const map = JSON.parse(this.field.dataSource) as Record<string, string>;
      return Object.entries(map).map(([value, label]) => ({ value, label }));
    } catch {
      return [];
    }
  }
}
