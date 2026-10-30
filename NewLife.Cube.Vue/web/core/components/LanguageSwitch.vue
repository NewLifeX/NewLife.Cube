<template>
  <div v-if="!locked" class="language-switch">
    <el-dropdown @command="handleCommand">
      <span class="el-dropdown-link">
        {{ currentLanguageLabel }}
        <el-icon class="el-icon--right">
          <arrow-down />
        </el-icon>
      </span>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item
            v-for="item in languages"
            :key="item.value"
            :command="item.value"
            :class="{ active: currentLanguage === item.value }"
          >
            {{ item.label }}
          </el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { ArrowDown } from '@element-plus/icons-vue';
import { useSiteStore } from '../stores/site';
import {
  getCurrentLanguage,
  resolveSelectableLanguages,
  setLanguage,
  type CubeLocale,
} from '../i18n';

defineOptions({
  name: 'LanguageSwitch',
});

const emit = defineEmits<{
  /** 语言切换完成事件，供调用方刷新服务端本地化文本 */
  change: [language: string];
}>();

const siteStore = useSiteStore();

/** 站点锁定语言时隐藏切换入口（与 /Cube/Site 的 language.locked 保持一致） */
const locked = computed(() => siteStore.config?.language?.locked === true);

/** 可选语言 = 前端 11 语 ∩ 站点/租户白名单 */
const languages = computed(() => resolveSelectableLanguages(siteStore.config?.language));

const currentLanguage = computed(() => getCurrentLanguage());

const currentLanguageLabel = computed(() => {
  const lang = languages.value.find((item) => item.value === currentLanguage.value);
  return lang ? lang.label : currentLanguage.value;
});

// 处理语言切换
function handleCommand(command: CubeLocale) {
  if (command === currentLanguage.value) return;

  setLanguage(command);
  emit('change', command);
}
</script>

<style scoped>
.language-switch {
  cursor: pointer;
  user-select: none;
}

.el-dropdown-link {
  display: flex;
  align-items: center;
}

:deep(.el-dropdown-menu__item.active) {
  color: var(--el-color-primary);
  background-color: var(--el-dropdown-menuItem-hover-fill);
}
</style>
