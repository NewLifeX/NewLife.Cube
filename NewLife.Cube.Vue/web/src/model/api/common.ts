export interface ApiResult<T> {
  code: number;
  data: T,
  message: string;
  page: Page;
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
