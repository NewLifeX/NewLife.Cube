<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import PageHeader from '../components/page-header.vue';

defineOptions({ name: 'WidgetAdminPage' });

interface WidgetItem {
  name: string;
  title: string;
  icon?: string;
  cols: number;
  category?: string;
  permission?: string | null;
  adminOnly: boolean;
  sort: number;
  enable: boolean;
}

const loading = ref(false);
const saving = ref(false);
const widgets = ref<WidgetItem[]>([]);
const groupOrder = ref<string[]>([]);
const groupItems = ref<Record<string, string[]>>({});

/**
 * 分组视图：后端 groupOrder/groupItems 用原始分组名（如 System），
 * 而部件 category 是本地化名称（如 系统），这里以原始名做键、本地化名做标题。
 */
interface GroupView {
  key: string;
  title: string;
  names: string[];
}

const groupViews = computed<GroupView[]>(() => {
  const byName = new Map(widgets.value.map(item => [item.name, item]));
  const keys = groupOrder.value.length > 0 ? groupOrder.value : Object.keys(groupItems.value);

  if (keys.length === 0) {
    // 没有分组配置时按部件的本地化分组名归并
    const map = new Map<string, string[]>();
    for (const item of widgets.value) {
      const category = item.category || '';
      if (!category) continue;
      const names = map.get(category) ?? [];
      names.push(item.name);
      map.set(category, names);
    }
    return [...map].map(([title, names]) => ({ key: title, title, names }));
  }

  return keys.map(key => {
    const names = (groupItems.value[key] ?? []).filter(name => byName.has(name));
    const title = names.map(name => byName.get(name)?.category).find(Boolean) || key;
    return { key, title, names };
  });
});

/** 部件名 → 所属分组原始名 */
const groupKeyOf = computed(() => {
  const map = new Map<string, string>();
  for (const group of groupViews.value) {
    for (const name of group.names) map.set(name, group.key);
  }
  return map;
});

type WidgetRow = WidgetItem & { groupKey: string };

/** 表格行：按「分组顺序 + 组内顺序」展开，未分组的部件排到最后 */
const rows = computed<WidgetRow[]>(() => {
  const byName = new Map(widgets.value.map(item => [item.name, item]));
  const ordered: WidgetRow[] = [];
  const seen = new Set<string>();

  for (const group of groupViews.value) {
    for (const name of group.names) {
      const item = byName.get(name);
      if (!item || seen.has(name)) continue;
      ordered.push({ ...item, groupKey: group.key });
      seen.add(name);
    }
  }
  for (const item of widgets.value) {
    if (seen.has(item.name)) continue;
    ordered.push({ ...item, groupKey: '' });
    seen.add(item.name);
  }
  return ordered;
});

async function load() {
  loading.value = true;
  try {
    const res = await cubeApi.client.get<{
      data?: { widgets?: WidgetItem[]; groupOrder?: string[]; groupItems?: Record<string, string[]> };
      widgets?: WidgetItem[];
      groupOrder?: string[];
      groupItems?: Record<string, string[]>;
    }>('/Cube/Widget');
    const body = res.data;
    const data = body?.data ?? body;
    widgets.value = Array.isArray(data?.widgets) ? data.widgets : [];
    groupOrder.value = Array.isArray(data?.groupOrder) ? data.groupOrder : [];
    groupItems.value = data?.groupItems ?? {};
  } catch {
    ElMessage.error($t('page.list.loadFailed'));
  } finally {
    loading.value = false;
  }
}

/** 启用/禁用部件（后端写入全局 Parameter，工作台按它过滤） */
async function toggleEnable(item: WidgetItem, enable: boolean) {
  saving.value = true;
  try {
    await cubeApi.client.post('/Cube/Widget/Enable', null, { params: { name: item.name, enable } });
    item.enable = enable;
    ElMessage.success($t('common.updateSuccess'));
  } catch {
    // 请求层已提示错误，这里回滚开关状态，避免界面与后端不一致
    item.enable = !enable;
  } finally {
    saving.value = false;
  }
}

/** 调整分组顺序 */
async function moveGroup(index: number, delta: number) {
  const target = index + delta;
  const list = groupViews.value.map(group => group.key);
  if (target < 0 || target >= list.length) return;

  [list[index], list[target]] = [list[target], list[index]];
  saving.value = true;
  try {
    await cubeApi.client.post('/Cube/Widget/SaveGroupOrder', null, { params: { groups: list.join(',') } });
    groupOrder.value = list;
    ElMessage.success($t('common.updateSuccess'));
  } catch {
    // 失败不落本地顺序
  } finally {
    saving.value = false;
  }
}

/** 调整组内部件顺序 */
async function moveItem(item: WidgetRow, delta: number) {
  const groupKey = item.groupKey;
  if (!groupKey) return;

  const names = rows.value.filter(row => row.groupKey === groupKey).map(row => row.name);
  const index = names.indexOf(item.name);
  const target = index + delta;
  if (index < 0 || target < 0 || target >= names.length) return;

  [names[index], names[target]] = [names[target], names[index]];
  saving.value = true;
  try {
    await cubeApi.client.post('/Cube/Widget/SaveGroupItemOrder', null, {
      params: { group: groupKey, names: names.join(',') }
    });
    groupItems.value = { ...groupItems.value, [groupKey]: names };
    ElMessage.success($t('common.updateSuccess'));
  } catch {
    // 失败不落本地顺序
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="widget-admin">
    <PageHeader :title="$t('widgetAdmin.title')" :subtitle="$t('widgetAdmin.subtitle')">
      <template #extra>
        <ElButton :loading="loading" @click="load">{{ $t('common.refresh') }}</ElButton>
      </template>
    </PageHeader>

    <ElCard shadow="never">
      <template #header>
        <span class="widget-admin__card-title">{{ $t('widgetAdmin.groupOrder') }}</span>
      </template>
      <div class="widget-admin__groups">
        <ElTag v-for="(group, index) in groupViews" :key="group.key" size="large" class="widget-admin__group">
          {{ group.title }}
          <ElButton
            link
            :icon="ArrowUp"
            :disabled="index === 0 || saving"
            :aria-label="$t('widgetAdmin.moveUp')"
            @click="moveGroup(index, -1)"
          />
          <ElButton
            link
            :icon="ArrowDown"
            :disabled="index === groupViews.length - 1 || saving"
            :aria-label="$t('widgetAdmin.moveDown')"
            @click="moveGroup(index, 1)"
          />
        </ElTag>
        <span v-if="!groupViews.length" class="widget-admin__empty">{{ $t('common.noData') }}</span>
      </div>
    </ElCard>

    <ElCard shadow="never">
      <ElTable v-loading="loading" :data="rows" border row-key="name">
        <ElTableColumn prop="title" :label="$t('widgetAdmin.widgetName')" min-width="200">
          <template #default="{ row }">
            <span class="widget-admin__title">{{ row.title }}</span>
            <ElTag v-if="row.adminOnly" size="small" type="warning" effect="plain" class="widget-admin__badge">
              {{ $t('widgetAdmin.adminOnly') }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn prop="category" :label="$t('widgetAdmin.group')" width="160" />
        <ElTableColumn prop="cols" :label="$t('widgetAdmin.cols')" width="100" align="center" />
        <ElTableColumn prop="sort" :label="$t('widgetAdmin.sort')" width="100" align="center" />
        <ElTableColumn :label="$t('widgetAdmin.enable')" width="100" align="center">
          <template #default="{ row }">
            <ElSwitch
              :model-value="row.enable"
              :disabled="saving"
              @update:model-value="(value: string | number | boolean) => toggleEnable(row, Boolean(value))"
            />
          </template>
        </ElTableColumn>
        <ElTableColumn :label="$t('widgetAdmin.itemOrder')" width="120" align="center">
          <template #default="{ row }">
            <ElButton
              link
              :icon="ArrowUp"
              :aria-label="$t('widgetAdmin.moveUp')"
              :disabled="saving || !row.groupKey"
              @click="moveItem(row, -1)"
            />
            <ElButton
              link
              :icon="ArrowDown"
              :aria-label="$t('widgetAdmin.moveDown')"
              :disabled="saving || !row.groupKey"
              @click="moveItem(row, 1)"
            />
          </template>
        </ElTableColumn>
        <template #empty>
          <ElEmpty :description="$t('common.noData')" :image-size="90" />
        </template>
      </ElTable>
    </ElCard>
  </div>
</template>

<style scoped>
.widget-admin {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.widget-admin :deep(.el-card__header) {
  padding-top: 16px;
}

.widget-admin__card-title {
  font-weight: 700;
}

.widget-admin__groups {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.widget-admin__group {
  display: inline-flex;
  align-items: center;
  gap: 2px;
}

.widget-admin__title {
  font-weight: 600;
}

.widget-admin__badge {
  margin-left: 8px;
}

.widget-admin__empty {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
