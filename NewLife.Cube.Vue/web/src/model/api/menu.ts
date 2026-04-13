export interface Menu {
  id: number;
  name: string;
  displayName: string;
  parentID: number;
  url: string;
  icon?: string;
  visible: boolean;
  newWindow: boolean;
  permissions: Permissions;
  children: Menu[];
}

interface Permissions {
  '1'?: string;
  '2'?: string;
  '4'?: string;
  '8'?: string;
  '16'?: string;
  '32'?: string;
}
