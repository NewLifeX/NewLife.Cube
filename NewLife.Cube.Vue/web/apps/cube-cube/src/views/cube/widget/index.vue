<script setup lang="ts">
/**
 * 工作台部件管理（M3 自定义页）。
 *
 * 后端 `WidgetController` 是自定义 JSON 接口（`GET /api/Cube/Widget` + `Enable` /
 * `SaveGroupOrder` / `SaveGroupItemOrder`），没有实体元数据可用；MVC 侧同样是自定义视图，
 * 所以这一页保留手写：部件列表 + 启用开关 + 分组/组内顺序调整，全部走真实接口。
 */
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue';
import request from '@newlifex/cube-vue/core/utils/request';
import ListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';

interface WidgetItem {
  name: string;
  title: string;
  icon?: string;
  cols: number;
  category?: string;
  permission?: string;
  adminOnly: boolean;
  sort: number;
  enable: boolean;
}

const { t } = useI18n();

const api = '/api/Cube/Widget';
const loading = ref(false);
const saving = ref(false);
const widgets = ref<WidgetItem[]>([]);
const groupOrder = ref<string[]>([]);
const groupItems = ref<Record<string, string[]>>({});

/** 分组列表：以后端下发的顺序为准，缺省按部件的首次出现顺序 */
const groups = computed(() => {
  if (groupOrder.value.length > 0) return groupOrder.value;
  const names: string[] = [];
  for (const item of widgets.value) {
    const category = item.category || '';
    if (category && !names.includes(category)) names.push(category);
  }
  return names;
});

/** 表格行：按「分组顺序 + 组内顺序」展开，新部件补在所在组末尾 */
const rows = computed(() => {
  const byName = new Map(widgets.value.map((item) => [item.name, item]));
  const ordered: WidgetItem[] = [];
  const seen = new Set<string>();

  const push = (name: string, category: string) => {
    const item = byName.get(name);
    if (!item || seen.has(name) || (item.category || '') !== category) return;
    ordered.push(item);
    seen.add(name);
  };

  for (const group of groups.value) {
    const names = groupItems.value[group] ?? widgets.value
      .filter((item) => (item.category || '') === group)
      .sort((a, b) => a.sort - b.sort)
      .map((item) => item.name);
    for (const name of names) push(name, group);
    for (const item of widgets.value) push(item.name, group);
  }
  // 没有分组的部件兜底排到最后，避免整行丢失
  for (const item of widgets.value) {
    if (!seen.has(item.name)) {
      ordered.push(item);
      seen.add(item.name);
    }
  }
  return ordered;
});

async function load() {
  loading.value = true;
  try {
    const res: any = await request({ url: api, method: 'get' });
    const data = res?.data ?? res;
    widgets.value = Array.isArray(data?.widgets) ? data.widgets : [];
    groupOrder.value = Array.isArray(data?.groupOrder) ? data.groupOrder : [];
    groupItems.value = data?.groupItems ?? {};
  } catch {
    ElMessage.error(t('common.loadFailed'));
  } finally {
    loading.value = false;
  }
}

/** 启用/禁用部件（后端写入全局 Parameter，工作台按它过滤） */
async function toggleEnable(item: WidgetItem, enable: boolean) {
  saving.value = true;
  try {
    await request({ url: `${api}/Enable`, method: 'post', data: { name: item.name, enable } });
    item.enable = enable;
    ElMessage.success(t('common.saveSuccess'));
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
  const list = [...groups.value];
  if (target < 0 || target >= list.length) return;

  [list[index], list[target]] = [list[target], list[index]];
  saving.value = true;
  try {
    await request({ url: `${api}/SaveGroupOrder`, method: 'post', data: { groups: list.join(',') } });
    groupOrder.value = list;
    ElMessage.success(t('common.saveSuccess'));
  } catch {
    // 失败不落本地顺序
  } finally {
    saving.value = false;
  }
}

/** 调整组内部件顺序 */
async function moveItem(item: WidgetItem, delta: number) {
  const category = item.category || '';
  const names = rows.value.filter((row) => (row.category || '') === category).map((row) => row.name);
  const index = names.indexOf(item.name);
  const target = index + delta;
  if (index < 0 || target < 0 || target >= names.length) return;

  [names[index], names[target]] = [names[target], names[index]];
  saving.value = true;
  try {
    await request({
      url: `${api}/SaveGroupItemOrder`,
      method: 'post',
      data: { group: category, names: names.join(',') },
    });
    groupItems.value = { ...groupItems.value, [category]: names };
    ElMessage.success(t('common.saveSuccess'));
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
    <ListPageHeader />

    <el-card class="wa-card" shadow="never">
      <template #header>
        <span>{{ t('widgetAdmin.groupOrder') }}</span>
      </template>
      <div class="wa-groups">
        <el-tag v-for="(group, index) in groups" :key="group" class="wa-group" size="large">
          {{ group }}
          <el-button
            link
            :icon="ArrowUp"
            :disabled="index === 0 || saving"
            :aria-label="t('widgetAdmin.moveUp')"
            @click="moveGroup(index, -1)"
          />
          <el-button
            link
            :icon="ArrowDown"
            :disabled="index === groups.length - 1 || saving"
            :aria-label="t('widgetAdmin.moveDown')"
            @click="moveGroup(index, 1)"
          />
        </el-tag>
      </div>
    </el-card>

    <el-card class="wa-card" shadow="never">
      <el-table v-loading="loading" :data="rows" border row-key="name" class="wa-table">
        <el-table-column prop="title" :label="t('common.name')" min-width="180">
          <template #default="{ row }">
            <span class="wa-title">{{ row.title }}</span>
            <el-tag v-if="row.adminOnly" size="small" type="warning" effect="plain" class="wa-badge">
              {{ t('widgetAdmin.adminOnly') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="category" :label="t('widgetAdmin.group')" width="160" />
        <el-table-column prop="cols" :label="t('widgetAdmin.cols')" width="100" align="center" />
        <el-table-column prop="sort" :label="t('common.sort')" width="100" align="center" />
        <el-table-column :label="t('common.enable')" width="100" align="center">
          <template #default="{ row }">
            <el-switch
              :model-value="row.enable"
              :disabled="saving"
              @update:model-value="(value: string | number | boolean) => toggleEnable(row, Boolean(value))"
            />
          </template>
        </el-table-column>
        <el-table-column :label="t('widgetAdmin.itemOrder')" width="120" align="center">
          <template #default="{ row }">
            <el-button
              link
              :icon="ArrowUp"
              :aria-label="t('widgetAdmin.moveUp')"
              :disabled="saving"
              @click="moveItem(row, -1)"
            />
            <el-button
              link
              :icon="ArrowDown"
              :aria-label="t('widgetAdmin.moveDown')"
              :disabled="saving"
              @click="moveItem(row, 1)"
            />
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<style scoped>
.wa-card + .wa-card {
  margin-top: 12px;
}

.wa-groups {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.wa-group {
  display: inline-flex;
  align-items: center;
  gap: 2px;
}

.wa-title {
  font-weight: 600;
}

.wa-badge {
  margin-left: 8px;
}
</style>
