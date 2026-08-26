import type { AiChatPage } from './aiChatContext';

export type AiWelcomeTab = 'recommend' | 'ask' | 'analyze';

export interface AiQuickItem {
  tab: 'recommend' | 'analyze';
  label: string;
  message: string;
}

const MSG_ANALYZE = '分析当前列表数据';
const MSG_FILL = '帮我填写当前表单';
const MSG_DIAG = '检查系统运行状态';

/** 问候称呼：trim 后空则「管理员」 */
export function aiGreetingName(displayName: string | undefined | null): string {
  const n = (displayName || '').trim();
  return n || '管理员';
}

export function aiWelcomeHello(displayName: string | undefined | null): string {
  return `👋 Hi，${aiGreetingName(displayName)}`;
}

export function aiWelcomeSubtitle(page: AiChatPage): string {
  if (page === 'list') return '我能帮你分析当前列表或检查系统状态。';
  if (page === 'form' || page === 'object') return '我能帮你填写当前表单或检查系统状态。';
  return '我能帮你检查系统运行状态。';
}

/** 快捷行；提问 Tab 永远无项 */
export function aiQuickItems(page: AiChatPage): AiQuickItem[] {
  const diag: AiQuickItem = { tab: 'recommend', label: '系统诊断', message: MSG_DIAG };
  const diagAnalyze: AiQuickItem = { tab: 'analyze', label: '系统诊断', message: MSG_DIAG };
  if (page === 'list') {
    return [
      { tab: 'recommend', label: '分析当前数据', message: MSG_ANALYZE },
      diag,
      { tab: 'analyze', label: '分析当前数据', message: MSG_ANALYZE },
      diagAnalyze,
    ];
  }
  if (page === 'form' || page === 'object') {
    return [{ tab: 'recommend', label: '帮我填表', message: MSG_FILL }, diag, diagAnalyze];
  }
  return [diag, diagAnalyze];
}

export function aiQuickItemsForTab(page: AiChatPage, tab: AiWelcomeTab): AiQuickItem[] {
  if (tab === 'ask') return [];
  return aiQuickItems(page).filter((x) => x.tab === tab);
}
