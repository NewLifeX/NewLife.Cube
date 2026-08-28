import { computed } from 'vue';
import type { WidgetCardProps } from './context';

export function useProfileWidget(props: WidgetCardProps) {
  const rows = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    const display = String(r.displayName ?? r.DisplayName ?? r.name ?? r.Name ?? '');
    return [
      { label: '账号', value: String(r.name ?? r.Name ?? '') },
      { label: '昵称', value: display },
      { label: '角色', value: String(r.roleNames ?? r.RoleNames ?? '') },
      { label: '在线', value: r.online ?? r.Online ? '是' : '否' },
      { label: '登录次数', value: String(r.logins ?? r.Logins ?? '') },
      { label: '最后登录', value: String(r.lastLogin ?? r.LastLogin ?? '') },
      { label: '登录 IP', value: String(r.lastLoginIP ?? r.LastLoginIP ?? '') },
      { label: '注册时间', value: String(r.registerTime ?? r.RegisterTime ?? '') },
    ];
  });
  return { rows };
}
