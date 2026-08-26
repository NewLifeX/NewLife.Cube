import { defineStore } from 'pinia';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import {
  SYSTEM_DEFAULT_PROFILE,
  cloneProfile,
  clearLocalProfile,
  loadLocalProfile,
  mergeProfile,
  prefsFromWire,
  prefsToWirePayload,
  saveLocalProfile,
  type LayoutPrefs,
  type ThemePrefs,
  type UserProfilePrefs,
  type WorkspacePrefs,
} from '@/core/utils/userProfile';
import { applyTheme, watchSystemAppearance } from '@/theme/applyTheme';

const SAVE_DEBOUNCE_MS = 400;

export const useUserProfileStore = defineStore('userProfile', {
  state: () => ({
    prefs: cloneProfile(SYSTEM_DEFAULT_PROFILE) as UserProfilePrefs,
    loaded: false,
    dirty: false,
    saving: false,
    loadError: '' as string,
    saveError: '' as string,
    _saveTimer: null as ReturnType<typeof setTimeout> | null,
    _stopWatch: null as null | (() => void),
  }),
  getters: {
    layout: (s) => s.prefs.layout,
    theme: (s) => s.prefs.theme,
    workspace: (s) => s.prefs.workspace,
  },
  actions: {
    /** 先读本地，再可选拉服务端覆盖 */
    bootstrapLocal() {
      const local = loadLocalProfile();
      this.prefs = local ? mergeProfile(local) : cloneProfile(SYSTEM_DEFAULT_PROFILE);
      this.applyVisual();
      this.ensureSystemWatch();
    },

    applyVisual() {
      applyTheme(this.prefs.theme);
    },

    ensureSystemWatch() {
      if (this._stopWatch) return;
      this._stopWatch = watchSystemAppearance(() => this.prefs.theme);
    },

    persistLocal() {
      saveLocalProfile(this.prefs);
    },

    async loadFromServer() {
      this.loadError = '';
      try {
        const res = await cubeApi.profile.getUserProfile();
        this.prefs = prefsFromWire(res);
        this.dirty = false;
        this.loaded = true;
        this.persistLocal();
        this.applyVisual();
        this.ensureSystemWatch();
      } catch (err) {
        this.loadError = formatApiError(err, '加载用户偏好失败');
        // 保留本地 / 默认，不阻断壳
        const local = loadLocalProfile();
        if (local) this.prefs = local;
        this.loaded = true;
        this.applyVisual();
      }
    },

    patchLayout(partial: Partial<LayoutPrefs>, opts?: { immediate?: boolean }) {
      this.prefs.layout = { ...this.prefs.layout, ...partial };
      this.markDirtyAndSchedule(opts?.immediate);
    },

    patchTheme(partial: Partial<ThemePrefs>, opts?: { immediate?: boolean }) {
      this.prefs.theme = { ...this.prefs.theme, ...partial };
      this.applyVisual();
      this.markDirtyAndSchedule(opts?.immediate);
    },

    patchWorkspace(partial: Partial<WorkspacePrefs>, opts?: { immediate?: boolean }) {
      this.prefs.workspace = { ...this.prefs.workspace, ...partial };
      this.markDirtyAndSchedule(opts?.immediate);
    },

    setPrefs(prefs: UserProfilePrefs, opts?: { immediate?: boolean }) {
      this.prefs = mergeProfile(prefs);
      this.applyVisual();
      this.markDirtyAndSchedule(opts?.immediate);
    },

    markDirtyAndSchedule(immediate?: boolean) {
      this.dirty = true;
      this.persistLocal();
      if (immediate) {
        void this.saveNow();
        return;
      }
      if (this._saveTimer) clearTimeout(this._saveTimer);
      this._saveTimer = setTimeout(() => {
        this._saveTimer = null;
        void this.saveNow();
      }, SAVE_DEBOUNCE_MS);
    },

    async saveNow() {
      if (this._saveTimer) {
        clearTimeout(this._saveTimer);
        this._saveTimer = null;
      }
      this.saving = true;
      this.saveError = '';
      const payload = prefsToWirePayload(this.prefs);
      try {
        await cubeApi.profile.putUserProfile(payload);
        this.dirty = false;
        this.persistLocal();
      } catch (err) {
        this.saveError = formatApiError(err, '保存外观设置失败');
        Message.error(this.saveError);
      } finally {
        this.saving = false;
      }
    },

    async resetToDefaults() {
      this.prefs = cloneProfile(SYSTEM_DEFAULT_PROFILE);
      this.applyVisual();
      this.dirty = true;
      this.persistLocal();
      await this.saveNow();
    },

    resetSession() {
      if (this._saveTimer) {
        clearTimeout(this._saveTimer);
        this._saveTimer = null;
      }
      if (this._stopWatch) {
        this._stopWatch();
        this._stopWatch = null;
      }
      this.prefs = cloneProfile(SYSTEM_DEFAULT_PROFILE);
      this.loaded = false;
      this.dirty = false;
      this.saveError = '';
      this.loadError = '';
      clearLocalProfile();
      this.applyVisual();
    },
  },
});
