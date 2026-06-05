import type { InjectionKey, Component } from 'vue';

// ─── 页面级默认组件 Keys ───────────────────────────────────────
export const DefaultListPageKey: InjectionKey<Component> = Symbol('DefaultListPage');
export const PageNotFoundKey: InjectionKey<Component> = Symbol('PageNotFound');

// ─── 列表页 Section Keys ──────────────────────────────────────────
export const ListPageHeaderKey: InjectionKey<Component> = Symbol('ListPageHeader');
export const ListSearchBarKey: InjectionKey<Component> = Symbol('ListSearchBar');
export const ListToolbarKey: InjectionKey<Component> = Symbol('ListToolbar');
export const ListTableContentKey: InjectionKey<Component> = Symbol('ListTableContent');
export const ListPaginationKey: InjectionKey<Component> = Symbol('ListPagination');
export const ListPageFooterKey: InjectionKey<Component> = Symbol('ListPageFooter');

// ─── 表单页 Section Keys ──────────────────────────────────────────
export const FormPageHeaderKey: InjectionKey<Component> = Symbol('FormPageHeader');
export const FormContentKey: InjectionKey<Component> = Symbol('FormContent');
export const FormActionsKey: InjectionKey<Component> = Symbol('FormActions');

// ─── 可被约定式自动发现的名称 → InjectionKey 映射 ───────────────
export const SectionKeyMap: Record<string, InjectionKey<Component>> = {
  DefaultListPage: DefaultListPageKey,
  PageNotFound: PageNotFoundKey,
  ListPageHeader: ListPageHeaderKey,
  ListSearchBar: ListSearchBarKey,
  ListToolbar: ListToolbarKey,
  ListTableContent: ListTableContentKey,
  ListPagination: ListPaginationKey,
  ListPageFooter: ListPageFooterKey,
  FormPageHeader: FormPageHeaderKey,
  FormContent: FormContentKey,
  FormActions: FormActionsKey,
};

// ─── 全局页面 Section 注册表（约定式自动发现用）──────────────────
export const PageSectionRegistryKey: InjectionKey<
  Record<string, Record<string, () => Promise<{ default: unknown }>>>
> = Symbol('PageSectionRegistry');
