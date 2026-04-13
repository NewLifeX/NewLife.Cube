import { Component, OnInit, OnDestroy, ElementRef, ViewChildren, QueryList, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzUploadModule } from 'ng-zorro-antd/upload';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { FieldKind, Auth, type DataField } from '@cube/api-core';
import { ApiService } from '../../services/api.service';
import { UserService } from '../../services/user.service';
import { FieldInputComponent } from '../../components/field-input.component';
import * as echarts from 'echarts';

@Component({
  selector: 'app-dynamic-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    NzTableModule, NzButtonModule, NzCardModule, NzFormModule,
    NzModalModule, NzDescriptionsModule, NzPopconfirmModule,
    NzUploadModule, NzSpaceModule, NzIconModule, NzDividerModule,
    NzDropDownModule, FieldInputComponent,
  ],
  template: `
    <!-- ECharts 图表 -->
    @if (chartList.length) {
      <nz-card style="margin-bottom: 16px;">
        @for (opt of chartList; track $index) {
          <div #chartContainer style="width: 100%; height: 400px;"></div>
        }
      </nz-card>
    }

    <!-- 搜索 -->
    @if (searchFields.length) {
      <nz-card style="margin-bottom: 16px;">
        <form nz-form nzLayout="inline" (ngSubmit)="handleSearch()">
          @for (field of searchFields; track field.name) {
            <nz-form-item>
              <nz-form-label>{{ field.displayName || field.name }}</nz-form-label>
              <nz-form-control>
                <app-field-input [field]="field" [value]="searchForm[field.name]"
                  (valueChange)="searchForm[field.name] = $event"></app-field-input>
              </nz-form-control>
            </nz-form-item>
          }
          <nz-form-item>
            <nz-space>
              <button *nzSpaceItem nz-button nzType="primary" type="submit">
                <span nz-icon nzType="search"></span>搜索
              </button>
              <button *nzSpaceItem nz-button (click)="handleReset()">重置</button>
            </nz-space>
          </nz-form-item>
        </form>
      </nz-card>
    }

    <!-- 操作栏 -->
    <div style="display: flex; justify-content: space-between; margin-bottom: 12px;">
      <nz-space>
        @if (canAdd) {
          <button *nzSpaceItem nz-button nzType="primary" (click)="handleAdd()">
            <span nz-icon nzType="plus"></span>新增
          </button>
        }
        @if (canDelete) {
          <button *nzSpaceItem nz-button nzDanger [disabled]="!checkedIds.size" (click)="handleBatchDelete()">
            <span nz-icon nzType="delete"></span>批量删除
          </button>
        }
      </nz-space>
      <nz-space>
        @if (canExport) {
          <button *nzSpaceItem nz-button nz-dropdown [nzDropdownMenu]="exportMenu">
            <span nz-icon nzType="download"></span>导出 <span nz-icon nzType="down"></span>
          </button>
          <nz-dropdown-menu #exportMenu="nzDropdownMenu">
            <ul nz-menu>
              @for (opt of exportOptions; track opt.value) {
                <li nz-menu-item (click)="handleExport(opt.value)">{{ opt.label }}</li>
              }
            </ul>
          </nz-dropdown-menu>
        }
        @if (canImport) {
          <nz-upload *nzSpaceItem [nzAction]="typePath + '/ImportFile'" [nzShowUploadList]="false" (nzChange)="onImportChange($event)">
            <button nz-button><span nz-icon nzType="upload"></span>导入</button>
          </nz-upload>
        }
      </nz-space>
    </div>

    <!-- 数据表格 -->
    <nz-table
      #basicTable
      [nzData]="tableData"
      [nzLoading]="loading"
      [nzPageIndex]="pageIndex"
      [nzPageSize]="pageSize"
      [nzTotal]="total"
      [nzFrontPagination]="false"
      nzShowSizeChanger
      (nzPageIndexChange)="onPageChange($event)"
      (nzPageSizeChange)="onPageSizeChange($event)"
    >
      <thead>
        <tr>
          <th nzShowCheckbox [(nzChecked)]="allChecked" (nzCheckedChange)="onAllChecked($event)"></th>
          @for (field of listFields; track field.name) {
            @if (field.visible !== false) {
              <th>{{ field.displayName || field.name }}</th>
            }
          }
          <th nzWidth="180px">操作</th>
        </tr>
      </thead>
      <tbody>
        @for (row of basicTable.data; track row['id']) {
          <tr>
            <td nzShowCheckbox [nzChecked]="checkedIds.has(row['id'])" (nzCheckedChange)="onItemChecked(row['id'], $event)"></td>
            @for (field of listFields; track field.name) {
              @if (field.visible !== false) {
                <td>{{ row[field.name] }}</td>
              }
            }
            <td>
              <a (click)="handleDetail(row)">详情</a>
              @if (canEdit) {
                <nz-divider nzType="vertical"></nz-divider>
                <a (click)="handleEdit(row)">编辑</a>
              }
              @if (canDelete) {
                <nz-divider nzType="vertical"></nz-divider>
                <a nz-popconfirm nzPopconfirmTitle="确认删除？" (nzOnConfirm)="handleDelete(row)" class="text-danger">删除</a>
              }
            </td>
          </tr>
        }
        <!-- 统计行 -->
        @if (statData) {
          <tr style="background: #fafafa; font-weight: bold;">
            <td></td>
            @for (field of listFields; track field.name; let idx = $index) {
              @if (field.visible !== false) {
                <td>{{ idx === 0 ? '合计' : (statData[field.name] != null ? statData[field.name] : '') }}</td>
              }
            }
            <td></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <!-- 新增/编辑弹窗 -->
    <nz-modal [(nzVisible)]="formVisible" [nzTitle]="isEdit ? '编辑' : '新增'" nzWidth="640" (nzOnOk)="handleFormSubmit()" [nzOkLoading]="submitLoading">
      <ng-container *nzModalContent>
        <form nz-form nzLayout="vertical">
          @for (field of formFields; track field.name) {
            <nz-form-item>
              <nz-form-label [nzRequired]="!!field.required">{{ field.displayName || field.name }}</nz-form-label>
              <nz-form-control>
                <app-field-input [field]="field" [value]="formData[field.name]"
                  (valueChange)="formData[field.name] = $event"></app-field-input>
              </nz-form-control>
            </nz-form-item>
          }
        </form>
      </ng-container>
    </nz-modal>

    <!-- 详情弹窗 -->
    <nz-modal [(nzVisible)]="detailVisible" nzTitle="详情" nzWidth="640" [nzFooter]="null">
      <ng-container *nzModalContent>
        <nz-descriptions nzBordered [nzColumn]="1">
          @for (field of detailFields; track field.name) {
            <nz-descriptions-item [nzTitle]="field.displayName || field.name">
              {{ detailData[field.name] ?? '-' }}
            </nz-descriptions-item>
          }
        </nz-descriptions>
      </ng-container>
    </nz-modal>
  `,
  styles: [`.text-danger { color: #ff4d4f; }`],
})
export class DynamicPageComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChildren('chartContainer') chartContainers!: QueryList<ElementRef<HTMLDivElement>>;

  typePath = '';
  listFields: DataField[] = [];
  searchFields: DataField[] = [];
  addFields: DataField[] = [];
  editFields: DataField[] = [];
  detailFields: DataField[] = [];

  tableData: Record<string, any>[] = [];
  loading = false;
  pageIndex = 1;
  pageSize = 20;
  total = 0;
  statData: Record<string, any> | null = null;

  searchForm: Record<string, any> = {};
  checkedIds = new Set<number>();
  allChecked = false;

  formVisible = false;
  isEdit = false;
  formData: Record<string, any> = {};
  submitLoading = false;

  detailVisible = false;
  detailData: Record<string, any> = {};

  // 权限
  canAdd = false;
  canEdit = false;
  canDelete = false;
  canExport = false;
  canImport = false;

  // ECharts
  chartList: any[] = [];
  private chartInstances: any[] = [];
  private chartsInitialized = false;
  private resizeHandler = () => { for (const inst of this.chartInstances) { inst?.resize(); } };

  exportOptions = [
    { label: '导出 Excel', value: 'Excel' },
    { label: '导出 CSV', value: 'Csv' },
    { label: '导出 JSON', value: 'Json' },
    { label: '导出 XML', value: 'Xml' },
    { label: '导出模板', value: 'ExcelTemplate' },
  ];

  private routeSub?: Subscription;

  get formFields(): DataField[] {
    return this.isEdit ? this.editFields : this.addFields;
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private userService: UserService,
    private message: NzMessageService,
  ) {}

  ngOnInit() {
    this.routeSub = this.route.url.subscribe((segments) => {
      this.typePath = '/' + segments.map(s => s.path).join('/');
      this.pageIndex = 1;
      this.updatePermissions();
      this.loadFields().then(() => this.loadData());
      this.loadChartData();
    });
    window.addEventListener('resize', this.resizeHandler);
  }

  ngOnDestroy() {
    this.routeSub?.unsubscribe();
    window.removeEventListener('resize', this.resizeHandler);
    this.disposeCharts();
  }

  ngAfterViewChecked() {
    if (this.chartList.length && this.chartContainers?.length && !this.chartsInitialized) {
      this.initCharts();
      this.chartsInitialized = true;
    }
  }

  private updatePermissions() {
    const perms = this.userService.getMenuPermission(this.typePath);
    this.canAdd = String(Auth.ADD) in perms;
    this.canEdit = String(Auth.EDIT) in perms;
    this.canDelete = String(Auth.DELETE) in perms;
    this.canExport = String(Auth.EXPORT) in perms;
    this.canImport = String(Auth.IMPORT) in perms;
  }

  private disposeCharts() {
    for (const inst of this.chartInstances) { inst?.dispose(); }
    this.chartInstances = [];
    this.chartsInitialized = false;
  }

  private initCharts() {
    this.disposeCharts();
    this.chartContainers.forEach((ref, idx) => {
      const opt = this.chartList[idx];
      if (!opt) return;
      const instance = echarts.init(ref.nativeElement);
      instance.setOption(opt);
      this.chartInstances.push(instance);
    });
  }

  async loadChartData() {
    try {
      const res = await this.api.page.getChartData(this.typePath);
      this.chartList = Array.isArray(res.data) && res.data.length > 0 ? res.data : [];
      this.chartsInitialized = false;
    } catch {
      this.chartList = [];
    }
  }

  async loadFields() {
    const path = this.typePath;
    const [list, search, add, edit, detail] = await Promise.all([
      this.api.page.getFields(path, FieldKind.List),
      this.api.page.getFields(path, FieldKind.Search),
      this.api.page.getFields(path, FieldKind.Add),
      this.api.page.getFields(path, FieldKind.Edit),
      this.api.page.getFields(path, FieldKind.Detail),
    ]);
    this.listFields = list.data || [];
    this.searchFields = search.data || [];
    this.addFields = add.data || [];
    this.editFields = edit.data || [];
    this.detailFields = detail.data || [];
  }

  async loadData() {
    this.loading = true;
    try {
      const res = await this.api.page.getList(this.typePath, {
        pageIndex: this.pageIndex - 1,
        pageSize: this.pageSize,
        ...this.searchForm,
      });
      this.tableData = res.data || [];
      this.statData = (res as any).stat ?? null;
      if (res.page) this.total = res.page.totalCount || 0;
    } finally {
      this.loading = false;
    }
  }

  onPageChange(page: number) {
    this.pageIndex = page;
    this.loadData();
  }

  onPageSizeChange(size: number) {
    this.pageSize = size;
    this.pageIndex = 1;
    this.loadData();
  }

  onAllChecked(checked: boolean) {
    if (checked) {
      this.tableData.forEach(r => this.checkedIds.add(r['id']));
    } else {
      this.checkedIds.clear();
    }
  }

  onItemChecked(id: number, checked: boolean) {
    if (checked) this.checkedIds.add(id);
    else this.checkedIds.delete(id);
    this.allChecked = this.tableData.length > 0 && this.tableData.every(r => this.checkedIds.has(r['id']));
  }

  handleSearch() {
    this.pageIndex = 1;
    this.loadData();
  }

  handleReset() {
    this.searchForm = {};
    this.pageIndex = 1;
    this.loadData();
  }

  handleAdd() {
    this.isEdit = false;
    this.formData = {};
    this.formVisible = true;
  }

  handleEdit(row: Record<string, any>) {
    this.isEdit = true;
    this.formData = { ...row };
    this.formVisible = true;
  }

  async handleFormSubmit() {
    this.submitLoading = true;
    try {
      if (this.isEdit) {
        await this.api.page.update(this.typePath, this.formData);
      } else {
        await this.api.page.add(this.typePath, this.formData);
      }
      this.message.success(this.isEdit ? '编辑成功' : '新增成功');
      this.formVisible = false;
      this.loadData();
    } catch {
      this.message.error('操作失败');
    } finally {
      this.submitLoading = false;
    }
  }

  handleDetail(row: Record<string, any>) {
    this.detailData = { ...row };
    this.detailVisible = true;
  }

  async handleDelete(row: Record<string, any>) {
    await this.api.page.remove(this.typePath, row['id']);
    this.message.success('删除成功');
    this.loadData();
  }

  async handleBatchDelete() {
    if (!this.checkedIds.size) return;
    await this.api.page.deleteSelect(this.typePath, Array.from(this.checkedIds));
    this.message.success('批量删除成功');
    this.checkedIds.clear();
    this.allChecked = false;
    this.loadData();
  }

  handleExport(fmt: string) {
    window.open(this.api.page.getExportUrl(this.typePath, fmt), '_blank');
  }

  onImportChange(event: any) {
    if (event.type === 'success') {
      this.message.success('导入成功');
      this.loadData();
    }
  }
}
