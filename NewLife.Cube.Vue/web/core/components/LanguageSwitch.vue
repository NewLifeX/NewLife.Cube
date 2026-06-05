<template>
  <div class="language-switch">
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
import { computed, ref } from 'vue';
import { ArrowDown } from '@element-plus/icons-vue';
import { getCurrentLanguage, setLanguage } from '../i18n';

defineOptions({
  name: 'LanguageSwitch',
});

const languages = [
  { label: '中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' },
];

const currentLanguage = ref(getCurrentLanguage());

const currentLanguageLabel = computed(() => {
  const lang = languages.find((item) => item.value === currentLanguage.value);
  return lang ? lang.label : '中文';
});

// 处理语言切换
function handleCommand(command: typeof currentLanguage.value) {
  if (command !== currentLanguage.value) {
    currentLanguage.value = command;
    setLanguage(command);
  }
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
