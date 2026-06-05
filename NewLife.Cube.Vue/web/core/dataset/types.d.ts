import { DataSet } from './data-set/DataSet';

declare module './data-set/DataSet' {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  interface DataSet<T = any, Q = any> {
    /**
     * 获取数据集长度
     */
    readonly length: number;
    /**
     * 清空数据集
     */
    clear(): void;
    /**
     * 批量添加数据
     */
    addAll(items: T[]): void;
  }
}

export {};
