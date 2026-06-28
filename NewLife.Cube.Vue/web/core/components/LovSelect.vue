<template>
  <div class="lov-select">
    <!-- 元数据加载中 -->
    <el-select v-if="loading && !resolvedType" disabled loading placeholder="加载中..." style="width: 100%" />

    <!-- 枚举型 ↓ 普通下拉 -->
    <el-select
      v-else-if="resolvedType === 'ENUM'"
      v-model="selectedValue"
      :placeholder="placeholder"
      :clearable="clearable"
      :disabled="disabled"
      :loading="loading"
      style="width: 100%"
      @change="onChange"
    >
      <el-option
        v-for="opt in options"
        :key="opt.value"
        :label="opt.label"
        :value="opt.value"
      />
    </el-select>

    <!-- 列表型 ↓ 触发按钮，弹窗交给 LovSelectTable -->
    <template v-else-if="resolvedType === 'LIST'">
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
        :lov-meta="lovMeta"
        :inline-enums="metaInlineEnums"
        :translate-cache="translateCache"
        @select="onTableSelect"
      />
    </template>

    <!-- 未知类型，显示原始值 -->
    <el-input v-else :model-value="modelValue" disabled style="width: 100%" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { Search } from '@element-plus/icons-vue';
import { getConfig } from 'cube-front/core/configure';
import LovSelectTable from './LovSelectTable.vue';

const props = withDefaults(defineProps<{
  code: string;
  type?: string;
  modelValue?: string | number;
  placeholder?: string;
  clearable?: boolean;
  disabled?: boolean;
  size?: 'large' | 'default' | 'small';
}>(), {
  placeholder: '请选择',
  clearable: true,
  disabled: false,
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number | undefined): void;
  (e: 'change', value: string | number | undefined): void;
}>();

// ── 元数据请求（全局缓存） ──
const metaCache = new Map<string, any>();
const loading = ref(false);
const lovMeta = ref<any>(null);
const metaInlineEnums = ref<Record<string, any[]>>({});

// ── 类型（统一来自 API 响应） ──
const resolvedType = ref<string | null>(null);

// ── 枚举型 ──
const selectedValue = ref(props.modelValue);
const options = ref<any[]>([]);

// ── 列表型 ──
const dialogVisible = ref(false);
const displayText = ref('');

// ── 翻译缓存（LRU，与 LovSelectTable 共享引用） ──
const translateCache = new Map<string, string>();
const MAX_CACHE_SIZE = 100;

const lovCode = computed(() => props.code);

// ── 加载元数据 ──
async function loadMeta() {
  const code = lovCode.value;
  if (!code) return;

  if (metaCache.has(code)) {
    applyMeta(metaCache.get(code));
    return;
  }

  loading.value = true;
  try {
    const cfg = getConfig();
    const baseUrl = cfg.request?.baseUrl || '';
    const res = await fetch(`${baseUrl}/Admin/Lov/Meta?lovCode=${encodeURIComponent(code)}`);
    const json = await res.json();
    metaCache.set(code, json);
    applyMeta(json);
  } catch (err) {
    console.error('LovSelect: 加载元数据失败', err);
  } finally {
    loading.value = false;
  }
}

function applyMeta(metaData: any) {
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
        for (const item of items as any[]) {
          translateCache.set(`${enumLovCode}:${item.value}`, item.label);
        }
      }
    }
  }
}

function onTableSelect(row: any) {
  const valueField = lovMeta.value?.valueField || 'id';
  const labelField = lovMeta.value?.labelField || 'name';
  const val = row[valueField];
  selectedValue.value = val;
  displayText.value = row[labelField] ?? String(val);
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

watch(() => props.modelValue, () => updateDisplayText());
</script>

<style scoped>
.lov-select {
  width: 100%;
}
.lov-select :deep(.el-input-group__append) {
  cursor: pointer;
}
</style>
