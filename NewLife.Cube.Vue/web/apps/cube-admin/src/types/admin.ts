// XCode 相关类型定义
export interface XCodeSetting {
  isNew: boolean;
  debug: boolean;
  showSQL: boolean;
  sqlPath: string;
  traceSQLTime: number;
  sqlMaxLength: number;
  useParameter: boolean;
  batchSize: number;
  batchInterval: number;
  commandTimeout: number;
  retryOnFailure: number;
  migration?: Migration;
  checkComment: boolean;
  checkDeleteIndex: boolean;
  checkAddIndex: boolean;
  checkDuplicateIndex: boolean;
}

export interface Migration {
  enable: boolean;
  mode: number;
  nameFormat: string;
  onlyCreateTable: boolean;
  backupDatabase: boolean;
  initData: boolean;
}
