/** 注销确认：输入须与当前用户名全字匹配（区分大小写） */
export function canConfirmCloseAccount(
  input: string,
  userName: string | null | undefined,
): boolean {
  return !!userName && input === userName;
}
