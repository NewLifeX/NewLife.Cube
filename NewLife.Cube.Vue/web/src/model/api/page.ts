export interface Column {
  name: string;
  displayName: string;
  description: string;
  category: string;
  typeName: string;
  itemType?: any;
  length: number;
  precision: number;
  scale: number;
  nullable: boolean;
  primaryKey: boolean;
  readonly: boolean;
  mapField: string;
  groupView?: any;
  /** 单元格链接。支持 {Id}/{Name} 变量替换 */
  url?: string;
  /** 链接目标。_blank/_self/_parent/_top */
  target?: string;
  /** 数据动作。action 走ajax请求 */
  dataAction?: string;
  /** 头部文字 */
  header?: string;
  /** 最大宽度 */
  maxWidth?: number;
  /** 文本对齐方式 */
  textAlign?: string;
  /** 是否必填 */
  required?: boolean;
  /** 是否可见 */
  visible?: boolean;
}