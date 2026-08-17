<template>
  <div class="shell-toolbar">
    <a-space>
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

      <a-dropdown trigger="hover" position="br">
        <a-button type="text" class="shell-toolbar__user">
          <a-avatar :size="28">{{ userStore.displayName?.charAt(0) || 'U' }}</a-avatar>
          <span class="shell-toolbar__user-name">{{ userStore.displayName }}</span>
        </a-button>
        <template #content>
          <a-dgroup v-if="tenantStore.enabled" :title="`租户 · ${tenantStore.currentLabel}`">
            <a-doption
              v-for="t in tenantStore.items"
              :key="t.id"
              :class="{ 'is-active-tenant': t.id === tenantStore.currentId }"
              @click="onSwitchTenant(t.id)"
            >
              <span class="user-menu-item">
                <icon-park
                  :type="t.id === tenantStore.currentId ? 'check' : 'building-one'"
                  class="user-menu-icon"
                />
                <span class="user-menu-text">{{ tenantOptionLabel(t) }}</span>
              </span>
            </a-doption>
          </a-dgroup>

          <a-dgroup :title="`账号 · ${userStore.displayName || '当前用户'}`">
            <a-doption @click="goSecurity">
              <span class="user-menu-item">
                <icon-park type="permissions" class="user-menu-icon" />
                <span class="user-menu-text">账号安全</span>
              </span>
            </a-doption>
            <a-doption @click="goAppearance">
              <span class="user-menu-item">
                <icon-park type="setting" class="user-menu-icon" />
                <span class="user-menu-text">外观设置</span>
              </span>
            </a-doption>
          </a-dgroup>

          <a-doption @click="handleLogout">
            <span class="user-menu-item">
              <icon-park type="logout" class="user-menu-icon" />
              <span class="user-menu-text">退出登录</span>
            </span>
          </a-doption>
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
  tenantOptionLabel,
  onSwitchTenant,
  handleLogout,
} = useShellToolbar();
</script>

<style scoped>
.shell-toolbar {
  display: flex;
  align-items: center;
}
.shell-toolbar__user {
  display: inline-flex;
  align-items: center;
}
.shell-toolbar__user-name {
  margin-left: 8px;
}
.user-menu-item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}
.user-menu-icon {
  flex-shrink: 0;
  font-size: 14px;
}
.user-menu-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.is-active-tenant {
  color: rgb(var(--primary-6));
}
</style>
