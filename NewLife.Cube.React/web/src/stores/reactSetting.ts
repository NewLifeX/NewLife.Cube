/**
 * React 皮肤设置 Store（对齐后端 ReactSetting 配置类）
 *
 * 后端 ReactController（ConfigController<ReactSetting>）把配置存数据库参数字典表
 * （DbConfigProvider Category=React），前端通过 GET /Admin/React 读取并缓存。
 *
 * 当前配置项：
 * - formStyle    表单风格：inline-三栏同排（对齐 MVC），vertical-标签一行控件一行（antd6 风格）
 * - descMode     字段注释显示：1-标签后小字，2-标签后问号图标悬浮，0-不显示
 * - configNavFlat 配置导航排开：true-魔方设置等配置页一字排开，false-核心+更多下拉
 *
 * 读取失败（未登录/无权限/网络）时回退默认值，不阻塞页面渲染。
 */
import { useEffect } from 'react';
import { create } from 'zustand';
import { api } from '@/api';

/** React 皮肤设置 */
export interface ReactSetting {
  /** 表单风格：inline-三栏同排（对齐 MVC）/ vertical-标签一行控件一行（antd6 风格） */
  formStyle: 'inline' | 'vertical';
  /** 字段注释显示：1-标签后小字，2-标签后问号图标悬浮，0-不显示 */
  descMode: number;
  /** 配置导航排开：true-一字排开 / false-核心+更多下拉 */
  configNavFlat: boolean;
  /** 文本框清除图标：true-显示清空叉叉 / false-隐藏（对齐 MVC 与主流表单输入框） */
  inputClear: boolean;
}

/** 默认设置（读取失败时回退，保证任何环境下页面可用） */
export const DEFAULT_REACT_SETTING: ReactSetting = {
  formStyle: 'inline',
  descMode: 1,
  configNavFlat: true,
  inputClear: false,
};

interface ReactSettingState {
  setting: ReactSetting;
  /** 是否已尝试加载（成功或失败都置 true，避免重复请求） */
  loaded: boolean;
  /** 加载后端配置（幂等，只拉取一次） */
  load: () => Promise<ReactSetting>;
}

export const useReactSettingStore = create<ReactSettingState>((set, get) => ({
  setting: DEFAULT_REACT_SETTING,
  loaded: false,
  load: async () => {
    if (get().loaded) return get().setting;
    try {
      // ConfigController GET {type} 返回配置对象（{ code, data: {...} }，兼容解包/不解包）
      const res = await api.client.get('/Admin/React');
      const body = res.data as { data?: Partial<ReactSetting> };
      const data = body?.data ?? (res.data as Partial<ReactSetting> | undefined) ?? {};
      const setting = { ...DEFAULT_REACT_SETTING, ...data };
      set({ setting, loaded: true });
      return setting;
    } catch {
      set({ loaded: true });
      return get().setting;
    }
  },
}));

/**
 * 读取 React 皮肤设置（Hook）
 *
 * 首次调用触发后端加载（幂等），失败回退默认值。
 *
 * @returns 当前皮肤设置
 */
export function useReactSetting(): ReactSetting {
  const setting = useReactSettingStore((s) => s.setting);
  useEffect(() => {
    void useReactSettingStore.getState().load();
  }, []);
  return setting;
}
