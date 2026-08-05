<template>
  <a-drawer
    :visible="visible"
    :title="`管理模板 · ${typePath}`"
    :width="480"
    @cancel="emit('update:visible', false)"
  >
    <a-alert type="info" style="margin-bottom: 16px">
      模板为系统级配置（仅管理员可管理）。普通用户基于模板个性化，个人配置优先于模板。
    </a-alert>

    <a-spin :loading="loading" style="width: 100%">
      <div class="domain-section">
        <div class="domain-head">
          <h4>视图模板</h4>
          <a-tag size="small" :color="hasViews ? 'green' : 'gray'">
            {{ hasViews ? '已发布' : '未发布' }}
          </a-tag>
        </div>
        <a-space wrap>
          <a-button size="small" type="primary" @click="publishViews">
            当前视图存为模板
          </a-button>
          <a-button size="small" status="danger" :disabled="!hasViews" @click="clearViews">
            清除视图模板
          </a-button>
        </a-space>
        <p v-if="hasViews" class="domain-tip">
          视图模板对所有用户生效；已个人化视图的用户不受影响。
        </p>
      </div>

      <a-divider />

      <div class="domain-section">
        <div class="domain-head">
          <h4>搜索模板</h4>
          <a-tag size="small" :color="hasFilters ? 'green' : 'gray'">
            {{ hasFilters ? '已发布' : '未发布' }}
          </a-tag>
        </div>
        <a-space wrap>
          <a-button size="small" type="primary" @click="publishFilters">
            当前搜索存为模板
          </a-button>
          <a-button size="small" status="danger" :disabled="!hasFilters" @click="clearFilters">
            清除搜索模板
          </a-button>
        </a-space>
        <p v-if="hasFilters" class="domain-tip">
          搜索模板对所有用户生效；已保存个人搜索的用户不受影响。
        </p>
      </div>

      <a-divider />

      <a-space>
        <a-button status="danger" :disabled="!hasTemplate" @click="deleteTemplate">
          删除整个模板
        </a-button>
        <a-button @click="emit('update:visible', false)">关闭</a-button>
      </a-space>
    </a-spin>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { ViewProfileModel } from '@cube/api-core';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { serializeNamedView, serializeSavedFilters } from '@/core/utils/viewProfile';
import { useViewProfileStore } from '@/stores/viewProfile';

const props = defineProps<{
  visible: boolean;
  typePath: string;
}>();

const emit = defineEmits<{ 'update:visible': [v: boolean] }>();

const evpStore = useViewProfileStore();
const template = ref<ViewProfileModel | null>(null);
const loading = ref(false);

const hasViews = computed(() => !!template.value?.viewsJson);
const hasFilters = computed(() => !!template.value?.filtersJson);
const hasTemplate = computed(() => hasViews.value || hasFilters.value);

async function refreshTemplate() {
  if (!props.typePath) return;
  loading.value = true;
  try {
    const res = await cubeApi.profile.getViewProfileTemplate(props.typePath);
    template.value = res?.data ?? null;
  } catch (err) {
    Message.error(formatApiError(err, '读取模板失败'));
    template.value = null;
  } finally {
    loading.value = false;
  }
}

/** 模板变更后刷新当前页面：重新解析个人+模板，模板来源域即时更新（OSC-0014） */
function refreshPage() {
  const entry = evpStore.byType[props.typePath];
  if (!entry) return;
  void evpStore.load(props.typePath, entry.metaKeys);
}

watch(
  () => props.visible,
  (v) => {
    if (v) void refreshTemplate();
  },
  { immediate: true },
);

/** 把当前有效视图域发布为模板（OSC-0014） */
async function publishViews() {
  const st = evpStore.getState(props.typePath);
  if (!st?.views?.length) {
    Message.warning('当前无视图可发布');
    return;
  }
  try {
    await cubeApi.profile.putViewProfileTemplate({
      typePath: props.typePath,
      viewsJson: JSON.stringify(st.views.map(serializeNamedView)),
    });
    Message.success('视图模板已发布');
    await refreshTemplate();
    refreshPage();
  } catch (err) {
    Message.error(formatApiError(err, '发布视图模板失败'));
  }
}

/** 把当前有效筛选域发布为模板（OSC-0014） */
async function publishFilters() {
  try {
    await cubeApi.profile.putViewProfileTemplate({
      typePath: props.typePath,
      filtersJson: serializeSavedFilters(evpStore.getSavedFilters(props.typePath)),
    });
    Message.success('搜索模板已发布');
    await refreshTemplate();
    refreshPage();
  } catch (err) {
    Message.error(formatApiError(err, '发布搜索模板失败'));
  }
}

/** 清除视图模板（该域回落系统默认） */
async function clearViews() {
  try {
    await cubeApi.profile.putViewProfileTemplate({ typePath: props.typePath, viewsJson: '' });
    Message.success('视图模板已清除');
    await refreshTemplate();
    refreshPage();
  } catch (err) {
    Message.error(formatApiError(err, '清除视图模板失败'));
  }
}

/** 清除筛选模板（该域回落空） */
async function clearFilters() {
  try {
    await cubeApi.profile.putViewProfileTemplate({ typePath: props.typePath, filtersJson: '' });
    Message.success('搜索模板已清除');
    await refreshTemplate();
    refreshPage();
  } catch (err) {
    Message.error(formatApiError(err, '清除搜索模板失败'));
  }
}

/** 删除整个模板（视图/筛选域回落系统默认） */
async function deleteTemplate() {
  if (!window.confirm('删除该实体的全局模板？未个人化的用户将回落系统默认。')) return;
  try {
    await cubeApi.profile.deleteViewProfileTemplate(props.typePath);
    Message.success('模板已删除');
    template.value = null;
    refreshPage();
  } catch (err) {
    Message.error(formatApiError(err, '删除模板失败'));
  }
}
</script>

<style scoped>
.domain-section {
  margin-bottom: 4px;
}
.domain-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}
.domain-head h4 {
  margin: 0;
  font-weight: 500;
  font-size: 14px;
  color: var(--color-text-1);
}
.domain-tip {
  margin: 8px 0 0;
  font-size: 12px;
  color: var(--color-text-3);
}
</style>
