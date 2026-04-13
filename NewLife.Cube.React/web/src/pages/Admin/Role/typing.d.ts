declare namespace API {
  /**
   * 角色列表
   */
  type RoleListItem = {
    id: number;
    name: string;
    remark?: string;
    sort: number;
    permission?: string;
    isSystem: boolean;
    enable: boolean;
  } & Partial<CreateItem> &
    Partial<UpdateItem> &
    Partial<ExItem>;
}
