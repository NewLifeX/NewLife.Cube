<template>
  <div class="wbr">
    <a-alert type="info" show-icon class="wbr-alert">
      保存角色模板后，仅未个性化的用户会继承。已自定义工作台的用户不会被覆盖。空部件保存会清除模板（继承系统默认），避免空墙阻断种子。
    </a-alert>
    <div class="wbr-body">
      <a-card title="角色" size="small" class="wbr-roles">
        <a-empty v-if="!roles.length" description="暂无角色" />
        <a-list v-else :bordered="false" size="small">
          <a-list-item
            v-for="r in roles"
            :key="r.id"
            class="wbr-role"
            :class="{ active: r.id === selectedId }"
            @click="selectRole(r.id)"
          >
            {{ r.name }}
            <a-tag v-if="r.isSystem" size="small" color="orangered">系统</a-tag>
          </a-list-item>
        </a-list>
      </a-card>
      <div class="wbr-preview">
        <div class="wbr-preview-bar">
          <span>{{ selected?.name || '请选择角色' }}</span>
          <a-space>
            <a-tag v-if="!hasTemplate" color="gray">尚未配置</a-tag>
            <a-tooltip content="清除后未个性化用户继承系统默认">
              <a-button
                :loading="saving"
                :disabled="!selectedId || !hasTemplate"
                @click="clearTemplate"
              >
                清除模板
              </a-button>
            </a-tooltip>
            <a-button type="primary" :loading="saving" :disabled="!selectedId" @click="save">保存</a-button>
          </a-space>
        </div>
        <a-spin :loading="loading">
          <WidgetHost />
        </a-spin>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import WidgetHost from '@/features/widget/WidgetHost.vue';
import { useWorkbenchRole } from './useWorkbenchRole';

defineOptions({ name: 'WorkbenchRole' });

const {
  roles,
  selectedId,
  selected,
  loading,
  saving,
  hasTemplate,
  selectRole,
  save,
  clearTemplate,
} = useWorkbenchRole();
</script>

<style scoped>
.wbr {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.wbr-body {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  gap: 12px;
  min-height: 480px;
}
.wbr-roles {
  min-width: 0;
}
.wbr-role {
  cursor: pointer;
}
.wbr-role.active {
  background: var(--color-fill-2);
  color: rgb(var(--primary-6));
}
.wbr-preview-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
@media (max-width: 800px) {
  .wbr-body {
    grid-template-columns: 1fr;
  }
}
</style>
