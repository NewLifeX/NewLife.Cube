export interface UserInfo {
  id: number;
  name: string;
  password: string;
  displayName: string;
  sex: string;
  mail: string;
  mobile: string;
  code?: any;
  avatar: string;
  roleID: number;
  roleIds?: any;
  roleName: string;
  roleNames: string;
  departmentID: number;
  online: boolean;
  enable: boolean;
  logins: number;
  lastLogin: string;
  lastLoginIP: string;
  registerTime: string;
  registerIP?: any;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4?: any;
  ex5?: any;
  ex6?: any;
  updateUser?: any;
  updateUserID: number;
  updateIP: string;
  updateTime: string;
  remark?: any;
  permission: string;
}

export type Login = {
  token: string;
}

export interface GetMenuTreeItem {
  children: GetMenuTreeItem[]
  displayName: string;
  icon: string;
  id: number;
  name: string;
  newWindow: boolean;
  parentID: number;
  permissions: Object;
  url: string;
  visible: boolean;
}
