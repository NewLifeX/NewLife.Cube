// @ts-ignore
/* eslint-disable */

declare namespace API {
  type MenuInfo = {
    displayName: string;
    icon: string;
    id: number;
    name: string;
    newWindow: boolean;
    parentID: number;
    permissions: {
      [key: int]: string;
    };
    url: string;
    visible: boolean;
    children: MenuInfo[];
  };
  type UserInfo = {
    id: number;
    name: string;
    displayName: string;
    sex: number;
    mail: string;
    mobile: string;
    code: string;
    avatar: string;
    roleID: number;
    roleIds: string;
    roleName: string;
    roleNames: string;
    departmentID: number;
    online: boolean;
    enable: boolean;
    logins: 13;
    lastLogin: string;
    lastLoginIP: string;
    registerTime: string;
    registerIP: string;
    updateUser: string;
    updateUserID: 85;
    updateIP: string;
    updateTime: string;
    remark: string;
    permission: string;
  };
  type CurrentUser = {
    name?: string;
    avatar?: string;
    userid?: string;
    email?: string;
    signature?: string;
    title?: string;
    group?: string;
    tags?: { key?: string; label?: string }[];
    notifyCount?: number;
    unreadCount?: number;
    country?: string;
    access?: string;
    geographic?: {
      province?: { label?: string; key?: string };
      city?: { label?: string; key?: string };
    };
    address?: string;
    phone?: string;
  };

  type LoginResult = {
    status?: string;
    type?: string;
    currentAuthority?: string;
  };

  type PageParams = {
    current?: number;
    pageSize?: number;
  };

  type RuleListItem = {
    key?: number;
    disabled?: boolean;
    href?: string;
    avatar?: string;
    name?: string;
    owner?: string;
    desc?: string;
    callNo?: number;
    status?: number;
    updatedAt?: string;
    createdAt?: string;
    progress?: number;
  };

  type RuleList = {
    data?: RuleListItem[];
    /** 列表的内容总数 */
    total?: number;
    success?: boolean;
  };

  type FakeCaptcha = {
    code?: number;
    status?: string;
  };

  type LoginParams = {
    username?: string;
    password?: string;
    autoLogin?: boolean;
    type?: string;
  };

  type ErrorResponse = {
    /** 业务约定的错误码 */
    errorCode: string;
    /** 业务上的错误信息 */
    errorMessage?: string;
    /** 业务上的请求是否成功 */
    success?: boolean;
  };

  type NoticeIconList = {
    data?: NoticeIconItem[];
    /** 列表的内容总数 */
    total?: number;
    success?: boolean;
  };

  type NoticeIconItemType = 'notification' | 'message' | 'event';

  type NoticeIconItem = {
    id?: string;
    extra?: string;
    key?: string;
    read?: boolean;
    avatar?: string;
    title?: string;
    status?: string;
    datetime?: string;
    description?: string;
    type?: NoticeIconItemType;
  };

  /** 扩展相关属性 */
  type ExItem = {
    ex1: number;
    ex2: number;
    ex3: number;
    ex4?: string;
    ex5?: string;
    ex6?: string;
  };
  /** 创建相关属性 */
  type CreateItem = {
    createIP: string;
    createTime: string;
    createUser?: string;
    createUserID: number;
  };
  /** 更新相关属性 */
  type UpdateItem = {
    updateIP: string;
    updateTime: string;
    updateUser?: string;
    updateUserID: number;
  };
}
