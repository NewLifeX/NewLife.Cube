import type { InjectionKey, Component } from 'vue';

export const DefaultListPageKey: InjectionKey<Component> = Symbol('DefaultListPage');
export const PageNotFoundKey: InjectionKey<Component> = Symbol('PageNotFound');
export const ListPageHeaderKey: InjectionKey<Component> = Symbol('ListPageHeader');
export const ListSearchBarKey: InjectionKey<Component> = Symbol('ListSearchBar');
export const ListToolbarKey: InjectionKey<Component> = Symbol('ListToolbar');
export const ListTableContentKey: InjectionKey<Component> = Symbol('ListTableContent');
export const ListPaginationKey: InjectionKey<Component> = Symbol('ListPagination');
export const ListPageFooterKey: InjectionKey<Component> = Symbol('ListPageFooter');
export const FormPageHeaderKey: InjectionKey<Component> = Symbol('FormPageHeader');
export const FormContentKey: InjectionKey<Component> = Symbol('FormContent');
export const FormActionsKey: InjectionKey<Component> = Symbol('FormActions');

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

/** typePath → sectionName → lazy loader */
export type PageSectionRegistry = Record<
  string,
  Record<string, () => Promise<{ default: unknown }>>
>;

export const PageSectionRegistryKey: InjectionKey<PageSectionRegistry> = Symbol('PageSectionRegistry');

const registry: PageSectionRegistry = {};

export function getPageSectionRegistry(): PageSectionRegistry {
  return registry;
}

/**
 * 约定扫描 apps 下 PascalCase Section 组件，
 * 路径段映射到 typePath（如 admin/user → /Admin/User）
 */
export function registerPageSectionsFromGlob(
  modules: Record<string, () => Promise<unknown>>,
): void {
  for (const [filePath, loader] of Object.entries(modules)) {
    const norm = filePath.replace(/\\/g, '/');
    const m = norm.match(/\/views\/(.+)\/([A-Z][A-Za-z0-9]+)\.vue$/);
    if (!m) continue;
    const [, viewDir, sectionName] = m;
    if (!SectionKeyMap[sectionName]) continue;
    if (sectionName === 'index') continue;

    const segments = viewDir.split('/').filter(Boolean);
    // admin/user → /Admin/User
    const typePath =
      '/' +
      segments
        .map((s) =>
          s
            .split('-')
            .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
            .join(''),
        )
        .join('/');

    if (!registry[typePath]) registry[typePath] = {};
    registry[typePath][sectionName] = loader as () => Promise<{ default: unknown }>;
  }
}

export function getSectionLoader(
  typePath: string,
  sectionName: string,
): (() => Promise<{ default: unknown }>) | undefined {
  return registry[typePath]?.[sectionName];
}
