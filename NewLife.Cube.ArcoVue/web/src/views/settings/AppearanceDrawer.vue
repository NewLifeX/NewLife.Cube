<template>
  <a-drawer
    :visible="visible"
    :width="480"
    unmount-on-close
    title="外观设置"
    placement="right"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <p class="drawer-sub">布局、主题与密度偏好将同步到当前账号</p>

    <a-card title="布局" :bordered="false" class="block" size="small">
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

    <a-card title="主题" :bordered="false" class="block" size="small">
      <a-form :model="form.theme" layout="vertical">
        <a-form-item label="外观">
          <a-radio-group v-model="form.theme.appearance" @change="onThemeChange">
            <a-radio value="light">亮色</a-radio>
            <a-radio value="dark">暗色</a-radio>
            <a-radio value="system">跟随系统</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item label="主色">
          <div class="preset-swatches">
            <button
              v-for="c in PRESET_THEME_COLORS"
              :key="c.key"
              type="button"
              class="preset-swatch"
              :class="{ selected: isPresetSelected(c) }"
              :style="{ background: c.color }"
              :title="c.name"
              @click="pickPresetColor(c)"
            >
              <icon-park v-if="isPresetSelected(c)" type="check" class="preset-swatch__check" />
            </button>
            <div class="custom-color-block">
              <span class="custom-label">自定义主色</span>
              <div class="custom-color-row">
                <button
                  type="button"
                  class="preset-swatch custom-swatch"
                  :class="{ selected: !isPresetColorActive() }"
                  :style="{ background: form.theme.primaryColor }"
                  title="选择自定义主色"
                  @click="openCustomColorPicker"
                >
                  <icon-park v-if="!isPresetColorActive()" type="check" class="preset-swatch__check" />
                </button>
                <input
                  ref="customColorRef"
                  type="color"
                  :value="form.theme.primaryColor"
                  class="color-input-hidden"
                  @input="onPrimaryInput"
                />
                <span class="color-text">{{ form.theme.primaryColor }}</span>
              </div>
            </div>
          </div>
        </a-form-item>
        <a-form-item label="圆角">
          <a-slider
            v-model="form.theme.radius"
            :min="0"
            :max="16"
            :step="1"
            @change="onThemeChange"
          />
        </a-form-item>
        <a-form-item label="字号比例">
          <a-slider
            v-model="form.theme.fontScale"
            :min="0.875"
            :max="1.25"
            :step="0.025"
            :format-tooltip="(v: number) => `${Math.round(v * 100)}%`"
            @change="onThemeChange"
          />
        </a-form-item>
      </a-form>
    </a-card>

    <template #footer>
      <a-space>
        <a-button v-if="showRoleWorkbench" @click="goRoleWorkbench">角色工作台</a-button>
        <a-button :loading="profileStore.saving" @click="reset">恢复默认</a-button>
        <a-tag v-if="profileStore.dirty" color="orangered">有未同步更改</a-tag>
        <a-tag v-else-if="profileStore.saving" color="arcoblue">同步中…</a-tag>
        <a-tag v-else color="green">已同步</a-tag>
      </a-space>
      <p v-if="profileStore.saveError" class="err">{{ profileStore.saveError }}</p>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAppearanceDrawer } from './useAppearanceDrawer';
import { useUserStore } from '@/stores/user';

defineOptions({ name: 'AppearanceDrawer' });

defineProps<{ visible: boolean }>();
const emit = defineEmits<{ 'update:visible': [boolean] }>();

const {
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
} = useAppearanceDrawer();

const router = useRouter();
const userStore = useUserStore();
const showRoleWorkbench = computed(() => userStore.userInfo?.isSystem === true);
function goRoleWorkbench() {
  emit('update:visible', false);
  void router.push('/settings/workbench-role');
}
</script>

<style scoped>
.drawer-sub {
  margin: -4px 0 16px;
  color: var(--color-text-3);
  font-size: 13px;
}
.block {
  margin-bottom: 12px;
}
.err {
  color: rgb(var(--red-6));
  margin-top: 8px;
}
.color-input-hidden {
  position: absolute;
  opacity: 0;
  width: 1px;
  height: 1px;
  pointer-events: none;
}
.color-text {
  margin-left: 8px;
  color: var(--color-text-2);
  font-size: 13px;
}
/* 自定义主色（OSC-0017 细化）：并入预置色板网格第三行，徽标置于标签下方 */
.custom-color-block {
  grid-column: 1 / -1;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  width: 100%;
  margin-top: 2px;
}
.custom-label {
  color: var(--color-text-3);
  font-size: 13px;
}
.custom-color-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
/* 预置色板（OSC-0017）：13 官方品牌色 swatch-grid，选中主题主色描边 + check 角标 */
.preset-swatches {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
  margin-bottom: 12px;
  width: 100%;
}
.preset-swatch {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid var(--color-border-2);
  padding: 0;
  cursor: pointer;
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
}
.preset-swatch:hover {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.preset-swatch.selected {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.preset-swatch__check {
  color: #fff;
  font-size: 14px;
  filter: drop-shadow(0 0 1px rgba(0, 0, 0, 0.45));
}

</style>
