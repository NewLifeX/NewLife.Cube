<template>
  <div class="appearance-page">
    <a-page-header title="外观设置" subtitle="布局、主题与密度偏好将同步到当前账号" />

    <a-card title="布局" :bordered="false" class="block">
      <a-form :model="form.layout" layout="vertical">
        <a-form-item label="布局模式">
          <a-radio-group v-model="form.layout.mode" @change="onLayoutChange">
            <a-radio value="side">侧栏</a-radio>
            <a-radio value="top">顶栏</a-radio>
            <a-radio value="mix">混合</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item label="侧栏宽度">
          <a-input-number
            v-model="form.layout.siderWidth"
            :min="160"
            :max="360"
            :step="10"
            @change="onLayoutChange"
          />
        </a-form-item>
        <a-form-item label="多页签">
          <a-switch v-model="form.layout.showTabs" @change="onLayoutChange" />
        </a-form-item>
        <a-form-item label="内容区宽度">
          <a-radio-group v-model="form.layout.contentWidth" @change="onLayoutChange">
            <a-radio value="standard">标准</a-radio>
            <a-radio value="wide">较宽</a-radio>
            <a-radio value="fluid">流式</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item label="密度">
          <a-radio-group v-model="form.theme.density" @change="onThemeChange">
            <a-radio value="default">默认</a-radio>
            <a-radio value="compact">紧凑</a-radio>
          </a-radio-group>
        </a-form-item>
      </a-form>
    </a-card>

    <a-card title="主题" :bordered="false" class="block">
      <a-form :model="form.theme" layout="vertical">
        <a-form-item label="外观">
          <a-radio-group v-model="form.theme.appearance" @change="onThemeChange">
            <a-radio value="light">亮色</a-radio>
            <a-radio value="dark">暗色</a-radio>
            <a-radio value="system">跟随系统</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item label="主色">
          <input
            type="color"
            :value="form.theme.primaryColor"
            class="color-input"
            @input="onPrimaryInput"
          />
          <span class="color-text">{{ form.theme.primaryColor }}</span>
        </a-form-item>
        <a-form-item label="圆角">
          <a-slider
            v-model="form.theme.radius"
            :min="0"
            :max="16"
            :step="1"
            style="max-width: 320px"
            @change="onThemeChange"
          />
        </a-form-item>
        <a-form-item label="字号比例">
          <a-slider
            v-model="form.theme.fontScale"
            :min="0.875"
            :max="1.25"
            :step="0.025"
            :format-tooltip="(v: number) => v.toFixed(3)"
            style="max-width: 320px"
            @change="onThemeChange"
          />
        </a-form-item>
      </a-form>
    </a-card>

    <div class="actions">
      <a-space>
        <a-button type="primary" :loading="profileStore.saving" @click="saveNow">立即保存</a-button>
        <a-button :loading="profileStore.saving" @click="reset">恢复默认</a-button>
        <a-tag v-if="profileStore.dirty" color="orangered">有未同步更改</a-tag>
        <a-tag v-else-if="profileStore.saving" color="arcoblue">同步中…</a-tag>
        <a-tag v-else color="green">已同步</a-tag>
      </a-space>
      <p v-if="profileStore.saveError" class="err">{{ profileStore.saveError }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import { useUserProfileStore } from '@/stores/userProfile';
import { cloneProfile, type UserProfilePrefs } from '@/core/utils/userProfile';

defineOptions({ name: 'AppearanceSettings' });

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

async function saveNow() {
  await profileStore.saveNow();
  if (!profileStore.saveError) Message.success('已保存');
}

async function reset() {
  await profileStore.resetToDefaults();
  Object.assign(form.layout, profileStore.prefs.layout);
  Object.assign(form.theme, profileStore.prefs.theme);
  Object.assign(form.workspace, profileStore.prefs.workspace);
  if (!profileStore.saveError) Message.success('已恢复默认');
}
</script>

<style scoped>
.appearance-page {
  max-width: 720px;
}
.block {
  margin-bottom: 16px;
}
.actions {
  margin-top: 8px;
}
.err {
  color: rgb(var(--red-6));
  margin-top: 8px;
}
.color-input {
  width: 42px;
  height: 32px;
  padding: 0;
  border: none;
  background: transparent;
  vertical-align: middle;
  cursor: pointer;
}
.color-text {
  margin-left: 8px;
  color: var(--color-text-2);
  font-size: 13px;
}
</style>
