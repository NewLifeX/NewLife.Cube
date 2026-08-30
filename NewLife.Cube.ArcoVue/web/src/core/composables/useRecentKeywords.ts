import { ref } from 'vue';

/** 最近搜索关键字：跨页面复用（localStorage 持久化，上限 7 条）。 */
const STORAGE_KEY = 'cube:recentKeywords';
const MAX = 7;

function load(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const arr = raw ? (JSON.parse(raw) as unknown) : [];
    return Array.isArray(arr)
      ? arr.filter((s): s is string => typeof s === 'string' && s.trim().length > 0).slice(0, MAX)
      : [];
  } catch {
    return [];
  }
}

const keywords = ref<string[]>(load());

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(keywords.value));
  } catch {
    /* ignore：localStorage 不可用（隐私模式/受限）时仅会话内存 */
  }
}

/**
 * 最近搜索关键字管理（OSC-260830a1b2）：记录用户最近 7 次使用过的关键字，供查询组合框快速复用。
 * 带 `search` 图标与 `▾` 下拉的组合输入框消费；关键字去重、最新在前、去空白。
 */
export function useRecentKeywords() {
  /** 当前最近关键字列表（最新在前） */
  const recentKeywords = keywords;

  /** 记录一次关键字（去重、最新在前、裁剪到 7 条） */
  function add(kw: string) {
    const s = (kw ?? '').trim();
    if (!s) return;
    recentKeywords.value = [s, ...recentKeywords.value.filter((k) => k !== s)].slice(0, MAX);
    persist();
  }

  /** 移除一条 */
  function remove(kw: string) {
    recentKeywords.value = recentKeywords.value.filter((k) => k !== kw);
    persist();
  }

  /** 清空 */
  function clear() {
    recentKeywords.value = [];
    persist();
  }

  return { recentKeywords, add, remove, clear };
}
