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
}