<template>
  <div class="shell-toolbar">
    <a-space>
      <a-dropdown v-if="tenantStore.enabled" trigger="click">
        <a-button type="text" size="small">
          {{ tenantStore.currentLabel }}
          <icon-park type="down" style="margin-left: 4px" />
        </a-button>
        <template #content>
          <a-doption
            v-for="t in tenantStore.items"
            :key="t.id"
            :value="t.id"
            @click="onSwitchTenant(t.id)"
          >
            {{ t.name || t.code || (t.id === 0 ? '平台' : t.id) }}
          </a-doption>
        </template>
      </a-dropdown>

      <a-tooltip content="站内通知">
        <a-badge :count="inboxUnreadCount" :max-count="99">
          <a-button type="text" size="small" @click="goInbox">
            <icon-park type="remind" />
          </a-button>
        </a-badge>
      </a-tooltip>

      <a-tooltip :content="appearanceLabel">
        <a-button type="text" size="small" @click="cycleAppearance">
          <icon-park :type="APPEARANCE_ICONS[profileStore.theme.appearance]" />
        </a-button>
      </a-tooltip>
      <a-dropdown>
        <a-button type="text">
          <a-avatar :size="28">{{ userStore.displayName?.charAt(0) || 'U' }}</a-avatar>
          <span style="margin-left: 8px;">{{ userStore.displayName }}</span>
        </a-button>
        <template #content>
          <a-doption @click="goSecurity">账号安全</a-doption>
          <a-doption @click="goAppearance">外观设置</a-doption>
          <a-doption @click="handleLogout">退出登录</a-doption>
        </template>
      </a-dropdown>
    </a-space>
  </div>
</template>

<script setup lang="ts">
import { useShellToolbar } from './useShellToolbar';

const {
  tenantStore,
  userStore,
  profileStore,
  appearanceLabel,
  APPEARANCE_ICONS,
  inboxUnreadCount,
  cycleAppearance,
  goAppearance,
  goInbox,
  goSecurity,
  onSwitchTenant,
  handleLogout,
} = useShellToolbar();
</script>

<style scoped>
.shell-toolbar {
  display: flex;
  align-items: center;
}
</style>
