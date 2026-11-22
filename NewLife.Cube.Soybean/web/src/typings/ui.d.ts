declare namespace UI {
  type ThemeColor = 'danger' | 'primary' | 'info' | 'success' | 'warning';

  type DataTableBaseColumn<T> = Partial<import('element-plus').TableColumnCtx<T extends object ? T : never>>;

  type TableColumnCheck = import('@sa/hooks').TableColumnCheck;

  type SetTableColumnKey<C, T> = Omit<C, 'prop'> & { prop?: keyof T | (string & {}) };

  type TableColumnWithKey<T> = SetTableColumnKey<DataTableBaseColumn<T>, T>;

  type TableColumn<T> = DataTableBaseColumn<T>;

  /**
   * the type of table operation
   *
   * - add: add table item
   * - edit: edit table item
   */
  type TableOperateType = 'add' | 'edit';
}

// ======================================== element-plus ========================================

declare module 'element-plus/dist/locale/zh-cn.mjs' {
  const locale: any;
  export default locale;
}

declare module 'element-plus/dist/locale/en.mjs' {
  const locale: any;
  export default locale;
}
