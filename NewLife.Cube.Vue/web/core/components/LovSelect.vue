<template>
  <div class="lov-select">
    <!-- 元数据加载中 -->
    <el-select
      v-if="loading && !resolvedType"
      disabled
      loading
      placeholder="加载中..."
      style="width: 100%"
    />

    <!-- 列表型 ↓ 触发按钮，弹窗交给 LovSelectTable -->
    <template v-if="resolvedType === 'LIST'">
      <el-input
        :model-value="displayText"
        :placeholder="placeholder"
        :disabled="disabled"
        readonly
        style="width: 100%"
        @click="dialogVisible = true"
      >
        <template #append>
          <el-button :disabled="disabled" @click="dialogVisible = true">
            <el-icon><Search /></el-icon>
          </el-button>
        </template>
      </el-input>

      <LovSelectTable
        v-model:dialog-visible="dialogVisible"
        :lov-code="lovCode"
        :lov-meta="listMeta"
        :inline-enums="metaInlineEnums"
        :translate-cache="translateCache"
        @select="onTableSelect"
      />
    </template>

    <!-- 枚举型 ↓ 普通下拉 -->
    <el-select
      v-else
      v-model="selectedValue"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :loading="loading"
      style="width: 100%"
      @change="onChange"
    >
      <el-option v-for="opt in options" :key="opt.value" :label="opt.label" :value="opt.value" />
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { Search } from '@element-plus/icons-vue';
import request from 'cube-front/core/utils/request';
import { fetchLovMeta, resolveLovType } from 'cube-front/core/utils/lov-api';
import type {
  LovMetaItem,
  LovEnumOption,
  LovListMeta,
  LovMetaResponse,
} from 'cube-front/core/types/lov';
import LovSelectTable from './LovSelectTable.vue';

const props = withDefaults(
  defineProps<{
    code: string;
    type?: string;
    modelValue?: string | number;
    placeholder?: string;
    clearable?: boolean;
    disabled?: boolean;
    size?: 'large' | 'default' | 'small';
  }>(),
  {
    placeholder: '请选择',
    clearable: true,
    disabled: false,
  },
);

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number | undefined): void;
  (e: 'change', value: string | number | undefined): void;
}>();

// ── 元数据请求（全局缓存） ──
const metaCache = new Map<string, LovMetaResponse>();
const loading = ref(false);
const lovMeta = ref<LovMetaItem | null>(null);
const metaInlineEnums = ref<Record<string, LovEnumOption[]>>({});

// ── 类型（统一来自 API 响应） ──
const resolvedType = ref<'ENUM' | 'LIST' | null>(null);

// ── 枚举型 ──
const selectedValue = ref(props.modelValue);
const options = ref<LovEnumOption[]>([]);

// ── 列表型 ──
const dialogVisible = ref(false);
const displayText = ref('');
/** 将 LovMetaItem 缩小为 LovListMeta（仅在 LIST 分支使用） */
const listMeta = computed<LovListMeta | null>(() =>
  lovMeta.value?.type === 'LIST' ? (lovMeta.value as LovListMeta) : null,
);

// ── 翻译缓存（LRU，与 LovSelectTable 共享引用） ──
const translateCache = new Map<string, string>();
const MAX_CACHE_SIZE = 100;

const lovCode = computed(() => props.code);

// ── 加载元数据 ──
async function loadMeta() {
  const code = lovCode.value;
  if (!code) return;

  if (metaCache.has(code)) {
    applyMeta(metaCache.get(code)!);
    return;
  }

  loading.value = true;
  try {
    const json = await fetchLovMeta(code);
    metaCache.set(code, json);
    applyMeta(json);
  } catch (err) {
    console.error('LovSelect: 加载元数据失败', err);
  } finally {
    loading.value = false;
  }
}

function applyMeta(metaData: LovMetaResponse) {
  const theMeta = metaData.meta?.[0];
  if (!theMeta) return;

  lovMeta.value = theMeta;
  metaInlineEnums.value = metaData.inlineEnums || {};
  resolvedType.value = theMeta.type;

  if (theMeta.type === 'ENUM') {
    options.value = theMeta.options || [];
  } else if (theMeta.type === 'LIST') {
    // 预填内联枚举到翻译缓存
    if (metaData.inlineEnums) {
      for (const [enumLovCode, items] of Object.entries(metaData.inlineEnums)) {
        for (const item of items) {
          translateCache.set(`${enumLovCode}:${item.value}`, item.label);
        }
      }
    }
  }
}

function onTableSelect(row: Record<string, unknown>) {
  const lov = lovMeta.value as LovListMeta | null;
  const valueField = lov?.valueField || 'id';
  const labelField = lov?.labelField || 'name';
  const val = row[valueField] as string | number | undefined;
  selectedValue.value = val;
  displayText.value = row[labelField] != null ? String(row[labelField]) : String(val ?? '');
  emit('update:modelValue', val);
  emit('change', val);
  dialogVisible.value = false;
}

function onChange(val: string | number | undefined) {
  emit('update:modelValue', val);
  emit('change', val);
}

function updateDisplayText() {
  if (resolvedType.value !== 'LIST' || props.modelValue == null) return;
  const cacheKey = `${lovCode.value}:${props.modelValue}`;
  displayText.value = translateCache.get(cacheKey) || String(props.modelValue);
}

onMounted(() => loadMeta());

watch(
  () => props.code,
  (newCode, oldCode) => {
    if (newCode && newCode !== oldCode) {
      // code 变化时重新加载元数据（支持异步配置场景：GetPage 返回后更新 code）
      loading.value = false;
      lovMeta.value = null;
      resolvedType.value = null;
      options.value = [];
      displayText.value = '';
      loadMeta();
    }
  },
);

watch(
  () => props.modelValue,
  () => updateDisplayText(),
);
</script>

<style scoped>
.lov-select {
  width: 100%;
}
.lov-select :deep(.el-input-group__append) {
  cursor: pointer;
}
</style>
