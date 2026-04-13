export interface ApiResult<T> {
  code: number;
  data: T,
  message: string;
  page: Page;
  /** 统计行数据 */
  stat: any;
}

// export interface ApiPagerResult<T> extends ApiResult<T> {
//   pager: Pager;
// }

export interface PageProps {
  pageIndex?: number;
  pageSize?: number;
}

export interface Page {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  longTotalCount: string;
}
