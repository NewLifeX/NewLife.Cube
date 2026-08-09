/**
 * 统一图标注册表（OSC-0017）——业务图标名的唯一事实源。
 *
 * 所有业务图标统一走全局 `<icon-park :type="kebab-case名" />` 渲染（@icon-park/vue-next）。
 * 组件按需引入于 iconComponents.ts（仅打包用到的图标，避免全量 install 膨胀 bundle），
 * main.ts 注册自定义 `<icon-park>` 聚合组件按 `type` 动态渲染。
 * 图标名必须经 IconPark `IconType` 校验（见 iconRegistry.spec.ts 单测），无效名在
 * menuIcon/fieldIcon 的兜底分支保证不抛异常。
 */
import type { Appearance } from './userProfile';
import type { ViewKind } from './viewMapping';
import type { FieldMeta } from '@/core/types/field';

/** 视图类型 → 图标（6 视图；kanban 用 blackboard 语义替代） */
export const VIEW_KIND_ICONS: Record<ViewKind, string> = {
  table: 'list-checkbox',
  tree: 'tree-list',
  card: 'pic',
  kanban: 'blackboard',
  calendar: 'calendar',
  gantt: 'timeline',
};

/** 外观 → 图标（右上角主题按钮） */
export const APPEARANCE_ICONS: Record<Appearance, string> = {
  light: 'sun',
  dark: 'moon',
  system: 'computer',
};

/** 数值类型集合（fieldIcon 分支） */
const NUMBER_TYPE_NAMES = new Set([
  'Int16',
  'Int32',
  'Int64',
  'Double',
  'Decimal',
  'Single',
  'UInt16',
  'UInt32',
  'UInt64',
  'Byte',
  'SByte',
]);
/** 日期/时间类型集合（fieldIcon 分支） */
const DATETIME_TYPE_NAMES = new Set(['DateTime', 'DateTimeOffset', 'Date', 'TimeSpan', 'Time']);
/** 邮件 itemType（fieldIcon 分支） */
const MAIL_ITEM_TYPES = new Set(['mail', 'email']);
/** 手机 itemType（fieldIcon 分支） */
const PHONE_ITEM_TYPES = new Set(['mobile', 'phone']);

function itemTypeOf(field: FieldMeta): string {
  return (field.itemType ?? '').trim().toLowerCase();
}

/**
 * 字段类型 → 图标。
 *
 * 判定优先级：itemType 特殊字段 > Map 外键/主键 > typeName 常规类型 > 默认。
 * Map 外键（lovCode 非 Enum. 前缀，与 filterBuilder 判定一致）→ link；主键/Guid → key。
 */
export function fieldIcon(field: FieldMeta): string {
  const it = itemTypeOf(field);
  if (it === 'image' || it === 'avatar') return 'pic';
  if (it === 'file' || it === 'attachment' || it === 'upload') return 'file-text';
  if (it === 'url' || it === 'link') return 'link';
  if (MAIL_ITEM_TYPES.has(it)) return 'mail';
  if (PHONE_ITEM_TYPES.has(it)) return 'phone';
  // Map 外键：lovCode 存在且非枚举（Enum. 前缀）→ 关联图标
  if (field.lovCode && !field.lovCode.startsWith('Enum.')) return 'link';
  if (field.primaryKey) return 'key';
  if (field.typeName === 'Boolean') return 'switch';
  if (DATETIME_TYPE_NAMES.has(field.typeName)) return 'time';
  if (
    field.typeName === 'Enum' ||
    field.lovCode?.startsWith('Enum.') ||
    (field.dataSource && Object.keys(field.dataSource).length > 0)
  )
    return 'tag';
  if (NUMBER_TYPE_NAMES.has(field.typeName)) return 'list-numbers';
  if (field.typeName === 'Guid') return 'key';
  // 其它 String / 默认：文本
  return 'font-size';
}

/** fa-xxx（后端菜单 Icon 类名）→ IconPark（覆盖 Cube 内置控制器常见菜单图标） */
export const FA_ICON_MAP: Record<string, string> = {
  'fa-user': 'people',
  'fa-users': 'peoples',
  'fa-user-plus': 'add-user',
  'fa-user-circle': 'user',
  'fa-user-secret': 'facial-mask',
  'fa-table': 'list-checkbox',
  'fa-list': 'list',
  'fa-navicon': 'list',
  'fa-wrench': 'tool',
  'fa-cog': 'setting',
  'fa-gear': 'setting',
  'fa-database': 'data',
  'fa-history': 'history',
  'fa-clock-o': 'big-clock',
  'fa-tasks': 'checklist',
  'fa-star': 'star',
  'fa-home': 'home',
  'fa-file': 'file-text',
  'fa-file-text': 'file-text',
  'fa-search': 'search',
  'fa-desktop': 'computer',
  'fa-tachometer': 'dashboard',
  'fa-area-chart': 'chart-line',
  'fa-shopping-cart': 'shopping-bag',
  'fa-bomb': 'death-star',
  // 其它内置菜单（无 fa- 前缀的也纳入）
  list: 'list',
  grid: 'grid-four',
};

/** 菜单显示名 → 图标（精确匹配，优先级最高；用于 fa 语义与产品命名不符的场景） */
const MENU_NAME_ICONS: Record<string, string> = {
  // 魔方管理顶级菜单：后端 Icon=fa-tachometer（仪表盘），产品命名宜用立方体图标
  魔方管理: 'cube-three',
};

/** 名称关键词兜底（menuIcon 未命中 FA_ICON_MAP 时按 displayName/name 匹配） */
const MENU_KEYWORD_FALLBACK: ReadonlyArray<readonly [RegExp, string]> = [
  [/用户|成员|账户|账号|个人/, 'people'],
  [/角色|权限|授权/, 'permissions'],
  [/菜单|导航/, 'list'],
  [/日志|审计|历史/, 'history'],
  [/设置|配置|参数|系统/, 'setting'],
  [/数据|数据库|模型|表/, 'data'],
  [/文件|附件|上传/, 'file-text'],
  [/统计|报表|图表|分析/, 'chart-line'],
  [/任务|计划|调度|定时/, 'timer'],
  [/流程|审批|工作流/, 'send'],
  [/消息|通知|提醒/, 'message'],
  [/订单|交易|支付/, 'shopping-bag'],
  [/部门|组织|机构/, 'building-one'],
  [/客户|联系人/, 'user'],
  [/商品|产品|物料/, 'box'],
  [/仓库|库存/, 'inbox'],
];

/** 菜单图标默认兜底 */
export const DEFAULT_MENU_ICON = 'application';

/**
 * 菜单图标解析（三态兜底）：FA_ICON_MAP 命中 → 名称关键词兜底 → 默认图标。
 *
 * @param item 菜单项（icon 为后端 fa-xxx 类名；displayName/name 用于关键词兜底）
 */
export function menuIcon(item: { icon?: string; displayName?: string; name: string }): string {
  // 显示名精确匹配优先（产品命名与 fa 语义不符时用专用映射）
  const nameKey = (item.displayName || item.name || '').trim();
  const nameHit = MENU_NAME_ICONS[nameKey];
  if (nameHit) return nameHit;

  const icon = (item.icon ?? '').trim().toLowerCase();
  if (icon) {
    // 兼容 'fa fa-user' 空格多类名：取首个 token 查表（如 fa-user / fa-user-o）
    const first = icon.split(/\s+/)[0] ?? '';
    const hit = FA_ICON_MAP[first] ?? FA_ICON_MAP[first.replace(/^fa-/, '')];
    if (hit) return hit;
  }
  const title = item.displayName || item.name;
  if (title) {
    for (const [re, name] of MENU_KEYWORD_FALLBACK) {
      if (re.test(title)) return name;
    }
  }
  return DEFAULT_MENU_ICON;
}
