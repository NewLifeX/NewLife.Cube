declare module 'slash2';
declare module '*.css';
declare module '*.less';
declare module '*.scss';
declare module '*.sass';
declare module '*.svg';
declare module '*.png';
declare module '*.jpg';
declare module '*.jpeg';
declare module '*.gif';
declare module '*.bmp';
declare module '*.tiff';
declare module 'omit.js';
declare module 'numeral';
declare module '@antv/data-set';
declare module 'mockjs';
declare module 'react-fittext';
declare module 'bizcharts-plugin-slider';

/**
 * GetColumns
 */
declare interface CubeColumn {
  tableName: string;
  id: number;
  tableId: number;
  name: string;
  displayName: string;
  enable: boolean;
  dataType: string;
  itemType: string;
  primaryKey: boolean;
  master: boolean;
  length: number;
  nullable: boolean;
  isDataObjectField: boolean;
  description: string;
  showInList: boolean;
  showInAddForm: boolean;
  showInEditForm: boolean;
  showInDetailForm: boolean;
  showInSearch: boolean;
  sort: number;
  width: string;
  cellText: string;
  cellTitle: string;
  cellUrl: string;
  headerText: string;
  headerTitle: string;
  headerUrl: string;
  dataAction: string;
  dataSource: string;
  createUserId: number;
  createTime: string;
  createIP: string;
  updateUserId: number;
  updateTime: string;
  updateIP: string;
}

/**
 * Pager
 */
type Pager = {
  currentPage: number;
  limit: number;
  offset: number;
  orderBy?: string;
  pageCount: number;
  pageIndex: number;
  pageSize: number;
  totalCount: number;
};

/**
 * 通用的请求返回结构
 */
declare interface ResponseStructure<T = any> {
  data: T;
  code: number;
  message?: string;
  pager?: Pager;
}

declare const REACT_APP_ENV: 'test' | 'dev' | 'pre' | false;
// 以下变量声明对应config.[env].ts文件内define的变量
declare const API_URL: string;
