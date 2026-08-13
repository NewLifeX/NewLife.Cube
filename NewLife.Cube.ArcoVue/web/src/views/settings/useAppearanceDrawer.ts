import { reactive, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import { useUserProfileStore } from '@/stores/userProfile';
import { cloneProfile, type UserProfilePrefs } from '@/core/utils/userProfile';
import { PRESET_THEME_COLORS, type PresetThemeColor } from '@/core/utils/presetColors';

/** AppearanceDrawer 组件全部业务 TS：外观偏好编辑与同步（自 AppearanceDrawer.vue script setup 原样搬移） */
export function useAppearanceDrawer() {
  const profileStore = useUserProfileStore();
  const form = reactive(cloneProfile(profileStore.prefs)) as UserProfilePrefs;

  watch(
    () => profileStore.prefs,
    (p) => {
      Object.assign(form.layout, p.layout);
      Object.assign(form.theme, p.theme);
      Object.assign(form.workspace, p.workspace);
    },
    { deep: true },
  );

  function onLayoutChange() {
    profileStore.patchLayout({ ...form.layout });
  }

  function onThemeChange() {
    profileStore.patchTheme({ ...form.theme });
  }

  function onPrimaryInput(e: Event) {
    const v = (e.target as HTMLInputElement).value;
    form.theme.primaryColor = v;
    onThemeChange();
  }

  /** 当前主色是否为该预置色（大小写不敏感比对） */
  function isPresetSelected(c: PresetThemeColor): boolean {
    return form.theme.primaryColor.toLowerCase() === c.color.toLowerCase();
  }

  /** 当前主色是否命中任一预置色（命中时自定义徽标不显示选中态，避免与预置色选中二义） */
  function isPresetColorActive(): boolean {
    return PRESET_THEME_COLORS.some((c) => isPresetSelected(c));
  }

  /** 点击预置色 → 写 primaryColor 并走统一主题变更链路（patchTheme + debounce 持久化） */
  function pickPresetColor(c: PresetThemeColor) {
    form.theme.primaryColor = c.color;
    onThemeChange();
  }

  /** 隐藏的原生颜色选择器（由自定义主色徽标触发） */
  const customColorRef = ref<HTMLInputElement | null>(null);
  /** 点击自定义主色徽标 → 唤起原生颜色选择器 */
  function openCustomColorPicker() {
    customColorRef.value?.click();
  }

  async function reset() {
    await profileStore.resetToDefaults();
    Object.assign(form.layout, profileStore.prefs.layout);
    Object.assign(form.theme, profileStore.prefs.theme);
    Object.assign(form.workspace, profileStore.prefs.workspace);
    if (!profileStore.saveError) Message.success('已恢复默认');
  }

  return {
    PRESET_THEME_COLORS,
    profileStore,
    form,
    customColorRef,
    onLayoutChange,
    onThemeChange,
    onPrimaryInput,
    isPresetSelected,
    isPresetColorActive,
    pickPresetColor,
    openCustomColorPicker,
    reset,
  };
}
