import { create } from 'zustand';

interface AppState {
  darkMode: boolean;
  collapsed: boolean;
  siteName: string;
  logo: string;
  toggleDark: () => void;
  toggleCollapse: () => void;
  setSiteInfo: (info: { name?: string; logo?: string }) => void;
}

export const useAppStore = create<AppState>((set) => ({
  darkMode: false,
  collapsed: false,
  siteName: '魔方管理平台',
  logo: '',
  toggleDark: () => set((s) => ({ darkMode: !s.darkMode })),
  toggleCollapse: () => set((s) => ({ collapsed: !s.collapsed })),
  setSiteInfo: (info) =>
    set((s) => ({
      siteName: info.name ?? s.siteName,
      logo: info.logo ?? s.logo,
    })),
}));
