/**
 * 实体列表刷新入口门禁（OSC-261004e6ee）：
 * bootstrap 仅首次挂载与 typePath 变化；其余普通刷新只走 loadData。
 */
export type ListRefreshTrigger =
  | 'mount'
  | 'typePath'
  | 'toolbar'
  | 'page'
  | 'search'
  | 'filter'
  | 'sort'
  | 'viewSwitch';

export type ListRefreshKind = 'bootstrap' | 'loadData';

export function resolveListRefreshKind(trigger: ListRefreshTrigger): ListRefreshKind {
  return trigger === 'mount' || trigger === 'typePath' ? 'bootstrap' : 'loadData';
}
