import { ref, toRaw, type UnwrapRef } from 'vue';
import { type AxiosRequestConfig, type AxiosInstance } from 'axios';
import axios from '../../utils/request';
import { generateResponseData, getDataByKey } from '../../utils/common';
import type { VNode } from 'vue';

/**
 * 数据集配置选项接口
 * @template T 数据类型
 * @template Q 查询参数类型
 */
export type TransportHookProps = {
  data?: object;
  params?: object;
  dataSet?: DataSet;
  [key: string]: object | undefined;
};

export type TransportType = (props: TransportHookProps) => AxiosRequestConfig;

/**
 * 表格列配置
 */

export type ColumnConfig<T, K extends keyof T = keyof T> = {
  /** 字段名，必须是T的key */
  prop?: K | string;
  /** 列标题 */
  label?: string;
  /** 列类型 */
  type?: 'selection' | 'index' | 'expand' | string;
  /** 列宽度 */
  width?: string | number;
  /** 最小宽度 */
  minWidth?: string | number;
  /** 是否可排序 */
  sortable?: boolean | string;
  /** 排序方法 */
  sortMethod?: (a: T, b: T) => number;
  /** 排序依据 */
  sortBy?: string | ((row: T, index: number) => string) | string[];
  /** 排序顺序 */
  sortOrders?: Array<'ascending' | 'descending' | null>;
  /** 是否可拖动宽度 */
  resizable?: boolean;
  /** 对齐方式 */
  align?: 'left' | 'center' | 'right' | string;
  /** 表头对齐方式 */
  headerAlign?: 'left' | 'center' | 'right' | string;
  /** 固定列 */
  fixed?: boolean | 'left' | 'right' | string;
  /** 列的 class 名称 */
  className?: string;
  /** 列头的 class 名称 */
  labelClassName?: string;
  /** 是否显示 tooltip */
  showOverflowTooltip?: boolean | string;
  /** tooltip 格式化 */
  tooltipFormatter?: (row: T, column: ColumnConfig<T>, cellValue: unknown, index: number) => string;
  /** 格式化函数 */
  formatter?: (
    row: T,
    column: ColumnConfig<T>,
    cellValue: unknown,
    index: number,
  ) => VNode | string;
  /** 索引列自定义内容 */
  index?: number | ((index: number) => number);
  /** 是否多选列可用 */
  selectable?: (row: T, index: number) => boolean;
  /** 多选列数据是否保留 */
  reserveSelection?: boolean;
  /** column key */
  columnKey?: string;
  /** 筛选数据 */
  filters?: Array<{ text: string; value: string }>;
  /** 筛选方法 */
  filterMethod?: (value: unknown, row: T, column: ColumnConfig<T>) => boolean;
  /** 筛选多选 */
  filterMultiple?: boolean;
  /** 筛选列的默认值 */
  filteredValue?: string[];
  /** 筛选弹窗位置 */
  filterPlacement?: string;
  /** 筛选弹窗 class */
  filterClassName?: string;
  /** render slot */
  render?: (value: T[K], row: T, rowIndex: number) => VNode | string;
};

interface DataSetOptions<T, Q> {
  axios?: AxiosInstance;
  /** 初始数据数组 */
  data?: T[];
  /** 数据项唯一标识字段名，默认为'id' */
  idField?: string;
  /** 可查询字段配置 */
  queryFields?: {
    /** 字段名，必须是Q的key */
    name: keyof Q;
    /** 字段类型 */
    type: 'string' | 'number' | 'boolean' | 'date';
    /** 是否支持模糊查询 */
    fuzzy?: boolean;
  }[];
  /** 字段配置 */
  fields?: {
    /** 字段名，必须是T的key */
    name: keyof T;
    /** 字段标签 */
    label?: string;
    /** 字段类型 */
    type: 'string' | 'number' | 'boolean' | 'date' | 'object';
    /** 是否必填 */
    required?: boolean;
    /** 默认值 */
    defaultValue?: T[keyof T];
    /** 校验规则 */
    validator?: (value: T[keyof T]) => boolean | string;
  }[];
  /** 列配置 */
  columns?: ColumnConfig<T>[];
  /** 传输配置 */
  transport?: {
    /** 创建记录 */
    create?: TransportType;
    /** 读取数据 */
    read?: TransportType;
    /** 更新记录 */
    update?: TransportType;
    /** 删除记录 */
    destroy?: TransportType;
    /** 数据校验 */
    validate?: TransportType;
    /** 表单提交 */
    submit?: TransportType;
  };
  /** 是否初始化后自动查询远程数据 */
  autoQuery?: boolean;
  /** 默认分页大小 */
  pageSize?: number;
  /** 数据属性名，默认为'data' */
  dataKey?: string;
  /** 数据总数属性名，默认为'total' */
  totalCountKey?: string;
}

/**
 * 响应式数据集管理类
 * 使用Vue的响应式系统管理数据集合，支持增删改查等操作
 * @template T 数据类型
 * @template Q 查询参数类型
 */
export class DataSet<T, Q> {
  /** 响应式数据数组 */
  private readonly _data = ref<Array<T>>([]);
  /** 当前选中项的引用 */
  private readonly _current = ref<T | null>(null);
  /** 数据项唯一标识字段名 */
  private readonly _idField: string;
  /** 可查询字段配置 */
  private readonly _queryFields: DataSetOptions<T, Q>['queryFields'];
  /** 字段配置 */
  private readonly _fields: DataSetOptions<T, Q>['fields'];
  /** 传输配置 */
  private readonly _transport: DataSetOptions<T, Q>['transport'];
  /** 加载状态 */
  private readonly _loading = ref(false);
  /** 分页大小 */
  private readonly _pageSize = ref(10);
  /** 当前页码 */
  private readonly _currentPage = ref(1);
  /** 数据总数 */
  private readonly _totalCount = ref(0);
  /** 自定义axios实例 */
  private readonly _axios?: AxiosInstance;

  private readonly _options: DataSetOptions<T, Q>;

  get pageSize() {
    return this._pageSize.value;
  }

  set pageSize(value: number) {
    this._pageSize.value = value;
  }

  get currentPage() {
    return this._currentPage.value;
  }

  set currentPage(value: number) {
    this._currentPage.value = value;
  }

  get totalCount() {
    return this._totalCount.value;
  }

  set totalCount(value: number) {
    this._totalCount.value = value;
  }

  /**
   * 创建数据集实例
   * @param options 数据集配置选项
   */
  constructor(options: DataSetOptions<T, Q> = {}) {
    this._idField = options.idField || 'id';
    this._queryFields = options.queryFields;
    this._fields = options.fields;
    this._transport = options.transport;
    this._pageSize.value = options.pageSize || 10;
    this._axios = options.axios;

    this._options = options;

    if (options.data) {
      const value = this._data.value as T[];
      value.push(...options.data);
      this._totalCount.value = value.length;
    }

    if (options.autoQuery && this._transport?.read) {
      this.read();
    }
  }

  get axios(): AxiosInstance {
    return this._axios || axios;
  }

  /**
   *
   * 获取数据集中的所有数据
   * @returns 数据数组
   */
  get data(): T[] {
    return this._data.value as T[];
  }

  /**
   * 获取当前选中的数据项
   * @returns 当前选中项或null
   */
  get current(): T | null {
    return this._current.value;
  }

  /**
   * 设置当前选中的数据项
   * @param value 要设置为当前项的数据
   */
  set current(value: T | null) {
    this._current.value = value;
  }

  /**
   * 向数据集添加新数据项
   * @param item 要添加的数据项
   * @example
   * dataset.add({id: 1, name: 'John'});
   */
  add(item: T): void {
    this._data.value.push(item);
    this._totalCount.value++;
  }

  /**
   * 从数据集中移除指定数据项
   * @param item 要移除的数据项
   * @returns 是否成功移除
   * @example
   * const success = dataset.remove(itemToRemove);
   */
  remove(item: T): boolean {
    const index = this._data.value.findIndex((i) => i[this._idField] === item[this._idField]);
    if (index >= 0) {
      this._data.value.splice(index, 1);
      this._totalCount.value = Math.max(0, this._totalCount.value - 1);
      if (this.current && this.current[this._idField] === item[this._idField]) {
        this.current = null;
      }
      return true;
    }
    return false;
  }

  /**
   * 更新数据集中的指定数据项
   * @param item 包含更新数据的数据项
   * @returns 是否成功更新
   * @example
   * const success = dataset.update(updatedItem);
   */
  update(item: T): boolean {
    const index = this._data.value.findIndex((i) => i[this._idField] === item[this._idField]);
    if (index >= 0) {
      this._data.value[index] = item;
      return true;
    }
    return false;
  }

  /**
   * 查询符合条件的数据项
   * @param fn 过滤函数，接收数据项和可选查询参数，返回是否匹配
   * @returns 符合条件的数据项数组
   * @example
   * const adults = dataset.query((user) => user.age >= 18);
   * // 使用查询参数
   * const results = dataset.query((user, query) => {
   *   return query?.minAge ? user.age >= query.minAge : true;
   * }, { minAge: 18 });
   */
  /**
   * 查询数据项
   * @param fnOrQuery 过滤函数或查询对象
   * @param query 可选查询参数(当第一个参数是函数时使用)
   * @returns 符合条件的数据项数组
   * @example
   * // 使用过滤函数
   * dataset.query((user) => user.age >= 18);
   * // 使用查询对象(需配置queryFields)
   * dataset.query({ name: 'John', age: 18 });
   */
  query(fnOrQuery: ((item: T, query?: Q) => boolean) | Record<string, any>, query?: Q): T[] {
    if (typeof fnOrQuery === 'function') {
      return this._data.value.filter((item) => fnOrQuery(item, query));
    }

    if (!this._queryFields) {
      console.warn('Query fields not configured, falling back to full scan');
      return this._data.value.filter((item) =>
        Object.entries(fnOrQuery).every(([key, value]) => item[key as keyof T] === value),
      );
    }

    return this._data.value.filter((item) => {
      return Object.entries(fnOrQuery).every(([key, value]) => {
        const fieldConfig = this._queryFields?.find((f) => f.name === key);
        if (!fieldConfig) return true;

        if (fieldConfig.fuzzy && fieldConfig.type === 'string') {
          return String(item[key as keyof Q]).includes(String(value));
        }
        return item[key as keyof Q] === value;
      });
    });
  }

  /**
   * 根据ID查找数据项
   * @param id 要查找的数据项ID
   * @returns 找到的数据项或undefined
   * @example
   * const user = dataset.findById(1);
   */
  findById(id: unknown): T | undefined {
    return this._data.value.find((item) => item[this._idField] === id);
  }

  /**
   * 获取字段配置
   * @returns 字段配置数组
   */
  getFields() {
    return this._fields;
  }

  /**
   * 获取列配置
   * @returns 列配置数组
   */
  getColumns() {
    return this._options.columns;
  }

  /**
   * 获取加载状态
   * @returns 是否正在加载
   */
  get loading() {
    return this._loading.value;
  }

  /**
   * 设置加载状态
   */
  set loading(value: boolean) {
    this._loading.value = value;
  }

  /**
   * 读取数据
   * @param params 查询参数
   * @returns Promise包含查询结果
   */
  async read(params?: object): Promise<T[]> {
    if (!this._transport?.read) {
      throw new Error('Read transport not configured');
    }

    this._loading.value = true;
    try {
      const config = this._transport.read({
        params: {
          ...params,
          pageSize: this._pageSize.value,
          pageIndex: this._currentPage.value,
        },
        dataSet: this,
      });

      // response有可能是原始的响应对象，也可能是经过处理的配置对象
      const response = await this.axios(config);

      let res = response as object;

      // 首先判断response是否是AxiosResponse对象
      if (
        'data' in res &&
        'status' in res &&
        'statusText' in res &&
        'headers' in res &&
        'config' in res &&
        'request' in res
      ) {
        res = response.data;
      }

      // 处理响应数据

      const { dataKey = 'data', totalCountKey = 'page.totalCount' } = this._options;

      // 使用generateResponseData处理响应数据
      const processedData = generateResponseData(res, dataKey) as T[];
      const totalCount = getDataByKey(res, totalCountKey) as number;

      // 默认情况
      this._data.value = processedData;
      this._totalCount.value = totalCount;
      return processedData;
    } finally {
      this._loading.value = false;
    }
  }

  /**
   * 创建记录
   * @param data 要创建的数据
   * @returns Promise包含创建结果
   */
  async create(data: T): Promise<T> {
    if (!this._transport?.create) {
      throw new Error('Create transport not configured');
    }

    this._loading.value = true;
    try {
      const config = this._transport.create({ data, dataSet: this });
      const response = await this.axios(config);

      this._data.value.push(response.data);
      this._totalCount.value++;
      return response.data;
    } finally {
      this._loading.value = false;
    }
  }

  /**
   * 远程更新记录
   * @param data 要更新的数据
   * @returns Promise包含更新结果
   */
  async remoteUpdate(data: T): Promise<T> {
    if (!this._transport?.update) {
      throw new Error('Update transport not configured');
    }

    this._loading.value = true;
    try {
      const config = this._transport.update({ data, dataSet: this });
      const response = await this.axios(config);

      const index = this._data.value.findIndex(
        (item) => item[this._idField] === data[this._idField],
      );
      if (index >= 0) {
        this._data.value[index] = response.data;
      }
      return response.data;
    } finally {
      this._loading.value = false;
    }
  }

  /**
   * 删除记录
   * @param data 要删除的数据
   * @returns Promise包含删除结果
   */
  async destroy(data: T): Promise<boolean> {
    if (!this._transport?.destroy) {
      throw new Error('Destroy transport not configured');
    }

    this._loading.value = true;
    try {
      const config = this._transport.destroy({ data, dataSet: this });
      await this.axios(config);

      const index = this._data.value.findIndex(
        (item) => item[this._idField] === data[this._idField],
      );
      if (index >= 0) {
        this._data.value.splice(index, 1);
        this._totalCount.value = Math.max(0, this._totalCount.value - 1);
      }
      return true;
    } finally {
      this._loading.value = false;
    }
  }

  /**
   * 根据字段名获取字段配置
   * @param name 字段名
   * @returns 字段配置或undefined
   */
  getField(name: keyof T) {
    return this._fields?.find((f) => f.name === name);
  }

  /**
   * 验证数据项是否符合字段配置
   * @param item 数据项
   * @returns 验证结果
   */
  validate(item: T) {
    if (!this._fields) return { valid: true, errors: [] };

    const errors: { field: keyof T; message: string }[] = [];

    this._fields.forEach((field) => {
      const value = item[field.name];

      // 检查必填字段
      if (field.required && (value === undefined || value === null || value === '')) {
        errors.push({
          field: field.name,
          message: `${field.label || String(field.name)} 是必填字段`,
        });
        return;
      }

      // 执行自定义校验
      if (field.validator) {
        const result = field.validator(value);
        if (result !== true) {
          errors.push({
            field: field.name,
            message: result || `${field.label || String(field.name)} 校验失败`,
          });
        }
      }
    });

    return {
      valid: errors.length === 0,
      errors,
    };
  }
}
