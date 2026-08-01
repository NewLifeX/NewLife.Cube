<template>
  <a-drawer
    :visible="visible"
    :width="380"
    unmount-on-close
    :footer="false"
    class="view-config-drawer"
    @update:visible="(v: boolean) => $emit('update:visible', v)"
  >
    <template #title>
      <span class="drawer-title">列表视图</span>
    </template>

    <a-tabs v-model:active-key="activeTab" type="line" size="medium">
      <a-tab-pane key="basic" title="基础配置">
        <section class="cfg-block">
          <div class="cfg-label">视图名称</div>
          <a-input v-model="localName" allow-clear placeholder="视图名称" @change="emitName" />
        </section>

        <section class="cfg-block">
          <div class="cfg-label">
            数据表
            <a-tooltip content="当前实体列表的数据来源（只读）">
              <icon-info-circle class="hint-ico" />
            </a-tooltip>
          </div>
          <a-input :model-value="typePath" disabled />
        </section>

        <section class="cfg-block">
          <div class="cfg-label">数据范围</div>
          <div class="range-row">
            <span>全部记录</span>
            <a-typography-text type="secondary" style="font-size: 12px">
              （筛选条件使用列表上方搜索区）
            </a-typography-text>
          </div>
        </section>

        <section class="cfg-block">
          <div class="cfg-label row-between">
            <span>字段配置</span>
            <a-typography-text type="secondary" style="font-size: 12px">
              {{ visibleCount }} / {{ localColumns.length }} 可见
            </a-typography-text>
          </div>
          <a-empty v-if="!localColumns.length" description="暂无字段可配置" />
          <ul v-else class="field-list">
            <li
              v-for="(col, idx) in localColumns"
              :key="col.key"
              class="field-item"
              @dragover.prevent
              @drop="onDrop(idx)"
            >
              <span
                class="drag-handle"
                draggable="true"
                title="拖动排序"
                @dragstart="onDragStart(idx, $event)"
              >
                <icon-drag-dot-vertical />
              </span>
              <a-input
                class="field-title-input"
                size="mini"
                :class="{ muted: !col.visible }"
                :model-value="displayTitle(col)"
                :placeholder="titles[col.key] || col.key"
                @update:model-value="(v: string) => onTitleEdit(col, v)"
                @press-enter="(e: KeyboardEvent) => (e.target as HTMLInputElement)?.blur()"
              />
              <a-button
                type="text"
                size="mini"
                disabled
                class="freeze-btn"
                title="左冻结暂不可用"
              >
                <icon-pushpin />
              </a-button>
              <a-button
                type="text"
                size="mini"
                :title="col.visible ? '隐藏' : '显示'"
                @click="toggleVisible(col)"
              >
                <icon-eye v-if="col.visible" />
                <icon-eye-invisible v-else />
              </a-button>
            </li>
          </ul>
        </section>

        <section class="cfg-block">
          <div class="cfg-label">默认排序</div>
          <a-space direction="vertical" fill style="width: 100%">
            <a-select
              :model-value="localSort?.field || ''"
              allow-clear
              placeholder="无"
              @change="onSortField"
            >
              <a-option value="">无</a-option>
              <a-option
                v-for="col in localColumns.filter((c) => c.visible)"
                :key="col.key"
                :value="col.key"
              >
                {{ displayTitle(col) }}
              </a-option>
            </a-select>
            <a-radio-group
              v-if="localSort?.field"
              :model-value="localSort.desc ? 'desc' : 'asc'"
              type="button"
              size="small"
              @change="onSortDir"
            >
              <a-radio value="asc">升序</a-radio>
              <a-radio value="desc">降序</a-radio>
            </a-radio-group>
          </a-space>
        </section>
      </a-tab-pane>

      <a-tab-pane key="custom" title="自定义配置">
        <!-- 背景色 -->
        <section class="cfg-block">
          <div class="cfg-label">背景色</div>
          <button
            type="button"
            class="color-trigger"
            :class="{ filled: bgFilled }"
            :style="bgTriggerStyle"
            @click="togglePanel('bg')"
          >
            <span class="color-chip" :style="chipStyle(chrome.bgPreset === 'custom' ? chrome.bgColor : null)" />
            <span class="color-trigger-text">{{ bgTriggerLabel }}</span>
            <icon-down :class="{ open: openPanel === 'bg' }" />
          </button>
          <div v-if="openPanel === 'bg'" class="color-panel">
            <a-button long class="restore-btn" @click="restoreBgDefault">恢复默认</a-button>
            <div class="color-section-title">推荐颜色</div>
            <div class="swatch-grid">
              <button
                v-for="c in recommendedColors"
                :key="c.key"
                type="button"
                class="swatch"
                :class="{ none: c.none, selected: isBgSwatchSelected(c) }"
                :style="c.none ? undefined : { background: c.color }"
                :title="c.label"
                @click="pickBgSwatch(c)"
              >
                <icon-check v-if="isBgSwatchSelected(c)" class="swatch-check" />
              </button>
            </div>
            <div class="color-section-title">更多颜色</div>
            <a-color-picker
              v-model="chrome.bgColor"
              hide-trigger
              disabled-alpha
              format="hex"
              style="width: 100%"
              @change="onBgColorPick"
            />
            <div class="slider-row">
              <span>不透明度</span>
              <span class="slider-val">{{ chrome.bgOpacity }}%</span>
            </div>
            <a-slider v-model="chrome.bgOpacity" :min="0" :max="100" @change="emitChrome" />
            <div class="slider-row">
              <span>背景模糊</span>
              <span class="slider-val">{{ chrome.bgBlur }}%</span>
            </div>
            <a-slider v-model="chrome.bgBlur" :min="0" :max="100" @change="emitChrome" />
          </div>
        </section>

        <!-- 宽度：图标在左 -->
        <section class="cfg-block">
          <div class="cfg-label">宽度</div>
          <div class="seg-group">
            <button
              type="button"
              class="seg-item"
              :class="{ active: chrome.widthMode === 'default' }"
              @click="setWidth('default')"
            >
              <span class="seg-ico width-default" />
              <span>默认宽度</span>
            </button>
            <button
              type="button"
              class="seg-item"
              :class="{ active: chrome.widthMode === 'fill' }"
              @click="setWidth('fill')"
            >
              <span class="seg-ico width-fill" />
              <span>填充容器</span>
            </button>
          </div>
        </section>

        <!-- 高度：图标在左 -->
        <section class="cfg-block">
          <div class="cfg-label">高度</div>
          <div class="seg-group seg-group-3">
            <button
              type="button"
              class="seg-item"
              :class="{ active: chrome.heightMode === 'default' }"
              @click="setHeight('default')"
            >
              <span class="seg-ico height-default" />
              <span>默认高度</span>
            </button>
            <button
              type="button"
              class="seg-item"
              :class="{ active: chrome.heightMode === 'fit' }"
              @click="setHeight('fit')"
            >
              <span class="seg-ico height-fit" />
              <span>适应内容</span>
            </button>
            <button
              type="button"
              class="seg-item"
              :class="{ active: chrome.heightMode === 'fill' }"
              @click="setHeight('fill')"
            >
              <span class="seg-ico height-fill" />
              <span>填充容器</span>
            </button>
          </div>
        </section>

        <!-- 顶部栏 -->
        <section class="cfg-block">
          <button type="button" class="collapse-head" @click="topBarOpen = !topBarOpen">
            <span>顶部栏</span>
            <icon-down :class="{ open: topBarOpen }" />
          </button>
          <div v-show="topBarOpen" class="collapse-body">
            <div class="switch-row">
              <span>筛选</span>
              <a-switch v-model="chrome.showFilter" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>分组</span>
              <a-switch v-model="chrome.showGroup" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>排序</span>
              <a-switch v-model="chrome.showSort" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>搜索</span>
              <a-switch v-model="chrome.showSearch" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>允许添加记录</span>
              <a-switch v-model="chrome.allowAdd" @change="emitChrome" />
            </div>
            <div v-if="chrome.allowAdd" class="nested-field">
              <div class="cfg-label">按钮文字</div>
              <a-input v-model="chrome.addButtonText" placeholder="添加记录" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>自定义按钮</span>
              <a-switch v-model="chrome.customButton" @change="emitChrome" />
            </div>
          </div>
        </section>

        <!-- 列表区 -->
        <section class="cfg-block">
          <button type="button" class="collapse-head" @click="listAreaOpen = !listAreaOpen">
            <span>列表区</span>
            <icon-down :class="{ open: listAreaOpen }" />
          </button>
          <div v-show="listAreaOpen" class="collapse-body">
            <div class="switch-row">
              <span>分页器</span>
              <a-switch v-model="chrome.showPager" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>
                允许查看记录详情
                <a-tooltip content="关闭后点击行与「详情」入口将不可用">
                  <icon-info-circle class="hint-ico" />
                </a-tooltip>
              </span>
              <a-switch v-model="chrome.allowViewDetail" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>
                允许删除记录
                <a-tooltip content="仍受菜单权限约束；关闭后隐藏删除入口">
                  <icon-info-circle class="hint-ico" />
                </a-tooltip>
              </span>
              <a-switch v-model="chrome.allowDelete" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>
                展开行记录
                <a-tooltip content="在表格左侧显示展开列，点击可查看详情">
                  <icon-info-circle class="hint-ico" />
                </a-tooltip>
              </span>
              <a-switch v-model="chrome.expandRow" @change="emitChrome" />
            </div>
          </div>
        </section>
      </a-tab-pane>
    </a-tabs>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import {
  IconCheck,
  IconDown,
  IconDragDotVertical,
  IconEye,
  IconEyeInvisible,
  IconInfoCircle,
  IconPushpin,
} from '@arco-design/web-vue/es/icon';
import {
  DEFAULT_CHROME,
  type ColumnPref,
  type HeightMode,
  type ViewChrome,
  type ViewSort,
  type WidthMode,
} from '@/core/utils/entityViewProfile';

type Swatch = { key: string; label: string; color?: string; none?: boolean };

const recommendedColors: Swatch[] = [
  // 第 1 行
  { key: 'blue', label: '蓝', color: '#3370FF' },
  { key: 'purple', label: '紫', color: '#7B67EE' },
  { key: 'teal', label: '青', color: '#14C0FF' },
  { key: 'cyan', label: '靛', color: '#00D6B9' },
  { key: 'sky', label: '天蓝', color: '#4DC3FF' },
  { key: 'yellow', label: '黄', color: '#FFC60A' },
  { key: 'orange', label: '橙', color: '#FF8800' },
  { key: 'red', label: '红', color: '#F54A45' },
  { key: 'magenta', label: '品红', color: '#F01D94' },
  // 第 2 行（明亮饱满）
  { key: 'vivid-blue', label: '亮蓝', color: '#2B6CFF' },
  { key: 'vivid-indigo', label: '靛蓝', color: '#5B5CFF' },
  { key: 'vivid-violet', label: '亮紫', color: '#A855F7' },
  { key: 'vivid-fuchsia', label: '玫红', color: '#E11D8F' },
  { key: 'vivid-rose', label: '玫紫', color: '#FF2D55' },
  { key: 'vivid-coral', label: '珊瑚', color: '#FF5A36' },
  { key: 'vivid-amber', label: '琥珀', color: '#FFB020' },
  { key: 'vivid-lime', label: '青绿', color: '#32D74B' },
  { key: 'vivid-emerald', label: '翠绿', color: '#00C2A8' },
  // 第 3 行（明亮饱满）
  { key: 'neon-cyan', label: '霓虹青', color: '#00E5FF' },
  { key: 'neon-azure', label: '宝蓝', color: '#1E90FF' },
  { key: 'neon-purple', label: '电紫', color: '#8B5CFF' },
  { key: 'neon-pink', label: '亮粉', color: '#FF4D9E' },
  { key: 'neon-orange', label: '亮橙', color: '#FF6B00' },
  { key: 'neon-gold', label: '金黄', color: '#FFD60A' },
  { key: 'neon-chartreuse', label: '草绿', color: '#A8E10C' },
  { key: 'neon-mint', label: '薄荷绿', color: '#00E39A' },
  { key: 'neon-turquoise', label: '松石', color: '#00CFC8' },
  // 无 / 白
  { key: 'none', label: '无', none: true },
  { key: 'white', label: '白', color: '#FFFFFF' },
];

type PanelKey = 'bg' | null;

const props = defineProps<{
  visible: boolean;
  typePath: string;
  viewName: string;
  columns: ColumnPref[];
  titles: Record<string, string>;
  sort: ViewSort | null;
  chrome?: ViewChrome | null;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  'update:columns': [cols: ColumnPref[]];
  'update:sort': [sort: ViewSort | null];
  'update:chrome': [chrome: ViewChrome];
  'update:name': [name: string];
}>();

const activeTab = ref('basic');
const openPanel = ref<PanelKey>(null);
const topBarOpen = ref(true);
const listAreaOpen = ref(true);
const localColumns = ref<ColumnPref[]>([]);
const localName = ref('');
const localSort = ref<ViewSort | null>(null);
const chrome = reactive<Required<ViewChrome>>({ ...DEFAULT_CHROME });
let dragFrom = -1;

const visibleCount = computed(() => localColumns.value.filter((c) => c.visible).length);

function isColorValue(v: string | null | undefined): boolean {
  return !!v && v !== 'default' && v !== 'transparent' && /^#([0-9a-fA-F]{6})$/.test(v);
}

function contrastText(hex: string): string {
  const h = hex.replace('#', '');
  if (h.length !== 6) return '#1f2329';
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  const yiq = (r * 299 + g * 587 + b * 114) / 1000;
  return yiq >= 160 ? '#1f2329' : '#ffffff';
}

function chipStyle(color: string | null | undefined): Record<string, string> {
  if (!isColorValue(color)) {
    return {
      background:
        'linear-gradient(to top right, transparent calc(50% - 1px), #f54a45 calc(50% - 1px), #f54a45 calc(50% + 1px), transparent calc(50% + 1px)), #fff',
    };
  }
  return { background: color! };
}

const bgFilled = computed(
  () => chrome.bgPreset === 'custom' && isColorValue(chrome.bgColor),
);

const bgTriggerLabel = computed(() =>
  chrome.bgPreset === 'default' || !chrome.bgColor
    ? '默认'
    : chrome.bgColor === 'transparent'
      ? '无'
      : chrome.bgColor.toUpperCase(),
);

const bgTriggerStyle = computed(() => {
  if (!bgFilled.value) return {};
  return {
    backgroundColor: chrome.bgColor,
    color: contrastText(chrome.bgColor),
    borderColor: 'transparent',
  };
});

function togglePanel(key: Exclude<PanelKey, null>) {
  openPanel.value = openPanel.value === key ? null : key;
}

function syncFromProps() {
  localColumns.value = (props.columns || []).map((c) => ({ ...c }));
  localName.value = props.viewName || '';
  localSort.value = props.sort ? { ...props.sort } : null;
  Object.assign(chrome, DEFAULT_CHROME, props.chrome || {});
}

watch(
  () => props.visible,
  (vis) => {
    if (vis) {
      activeTab.value = 'basic';
      openPanel.value = null;
      syncFromProps();
    }
  },
);

watch(
  () => [props.columns, props.viewName, props.sort, props.chrome] as const,
  () => {
    if (props.visible) syncFromProps();
  },
  { deep: true },
);

function commitColumns() {
  emit(
    'update:columns',
    localColumns.value.map((c) => ({ ...c })),
  );
}

function displayTitle(col: ColumnPref): string {
  return (col.title?.trim() || props.titles[col.key] || col.key).toString();
}

function onTitleEdit(col: ColumnPref, raw: string) {
  const meta = props.titles[col.key] || col.key;
  const next = (raw ?? '').trim();
  if (!next || next === meta) {
    delete col.title;
  } else {
    col.title = next;
  }
  commitColumns();
}

function toggleVisible(col: ColumnPref) {
  col.visible = !col.visible;
  commitColumns();
}


function onDragStart(idx: number, e: DragEvent) {
  dragFrom = idx;
  e.dataTransfer?.setData('text/plain', String(idx));
}

function onDrop(toIdx: number) {
  if (dragFrom < 0 || dragFrom === toIdx) return;
  const arr = localColumns.value.slice();
  const [item] = arr.splice(dragFrom, 1);
  arr.splice(toIdx, 0, item);
  localColumns.value = arr;
  dragFrom = -1;
  commitColumns();
}

function onSortField(
  v: string | number | boolean | Record<string, unknown> | (string | number | boolean | Record<string, unknown>)[],
) {
  const field = v == null || v === '' ? '' : String(v);
  if (!field) {
    localSort.value = null;
    emit('update:sort', null);
    return;
  }
  localSort.value = { field, desc: !!localSort.value?.desc };
  emit('update:sort', { ...localSort.value });
}

function onSortDir(v: string | number | boolean) {
  if (!localSort.value?.field) return;
  localSort.value = { field: localSort.value.field, desc: String(v) === 'desc' };
  emit('update:sort', { ...localSort.value });
}

function emitChrome() {
  emit('update:chrome', { ...chrome });
}

function emitName() {
  const name = localName.value.trim();
  if (name && name !== props.viewName) emit('update:name', name);
}

function restoreBgDefault() {
  chrome.bgPreset = 'default';
  chrome.bgColor = DEFAULT_CHROME.bgColor;
  chrome.bgOpacity = DEFAULT_CHROME.bgOpacity;
  chrome.bgBlur = DEFAULT_CHROME.bgBlur;
  emitChrome();
}

function isBgSwatchSelected(c: Swatch) {
  if (c.none) return chrome.bgPreset === 'custom' && chrome.bgColor === 'transparent';
  return chrome.bgPreset === 'custom' && chrome.bgColor.toUpperCase() === (c.color || '').toUpperCase();
}

function pickBgSwatch(c: Swatch) {
  chrome.bgPreset = 'custom';
  chrome.bgColor = c.none ? 'transparent' : c.color || '#FFFFFF';
  emitChrome();
}

function onBgColorPick() {
  chrome.bgPreset = 'custom';
  emitChrome();
}

function setWidth(mode: WidthMode) {
  chrome.widthMode = mode;
  emitChrome();
}

function setHeight(mode: HeightMode) {
  chrome.heightMode = mode;
  emitChrome();
}

</script>

<style scoped>
.drawer-title {
  font-weight: 600;
}
.cfg-block {
  margin-bottom: 22px;
}
.cfg-label {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
  font-size: 13px;
  color: var(--color-text-1);
  font-weight: 500;
}
.row-between {
  justify-content: space-between;
}
.hint-ico {
  color: var(--color-text-3);
  cursor: help;
}
.range-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px 12px;
  background: var(--color-fill-2);
  border-radius: 4px;
}
.field-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 360px;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: 4px;
}
.field-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 8px;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-bg-2);
}
.field-item:last-child {
  border-bottom: none;
}
.drag-handle {
  display: inline-flex;
  align-items: center;
  color: var(--color-text-3);
  flex-shrink: 0;
  cursor: grab;
  padding: 2px;
}
.drag-handle:active {
  cursor: grabbing;
}
.field-title-input {
  flex: 1;
  min-width: 0;
}
.field-title-input.muted :deep(.arco-input) {
  color: var(--color-text-3);
}
.freeze-btn {
  opacity: 0.35;
  cursor: not-allowed;
}

.color-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
  background: var(--color-bg-2);
  color: var(--color-text-1);
  text-align: left;
}
.color-trigger:hover {
  border-color: rgb(var(--primary-6));
}
.color-trigger.filled .color-chip {
  border-color: rgba(255, 255, 255, 0.55);
}
.color-chip {
  width: 18px;
  height: 18px;
  border-radius: 3px;
  border: 1px solid var(--color-border-2);
  flex-shrink: 0;
}
.color-trigger-text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.color-trigger .arco-icon {
  color: inherit;
  opacity: 0.65;
  transition: transform 0.2s;
  flex-shrink: 0;
}
.color-trigger .arco-icon.open {
  transform: rotate(180deg);
}

.color-panel {
  margin-top: 10px;
  padding: 12px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-bg-2);
}
.restore-btn {
  margin-top: 10px;
  margin-bottom: 4px;
}
.color-section-title {
  margin: 14px 0 8px;
  font-size: 12px;
  color: var(--color-text-3);
}
.swatch-grid {
  display: grid;
  grid-template-columns: repeat(9, 1fr);
  gap: 8px;
}
.swatch {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: 1px solid var(--color-border-2);
  padding: 0;
  cursor: pointer;
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
}
.swatch.none {
  border-radius: 4px;
  background:
    linear-gradient(to top right, transparent calc(50% - 1px), #f54a45 calc(50% - 1px), #f54a45 calc(50% + 1px), transparent calc(50% + 1px)),
    #fff;
}
.swatch.selected {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.swatch-check {
  color: #fff;
  font-size: 12px;
  filter: drop-shadow(0 0 1px rgba(0, 0, 0, 0.45));
}
.slider-row {
  display: flex;
  justify-content: space-between;
  margin-top: 14px;
  margin-bottom: 4px;
  font-size: 13px;
  color: var(--color-text-2);
}
.slider-val {
  color: var(--color-text-3);
}

.seg-group {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}
.seg-group-3 {
  grid-template-columns: 1fr 1fr 1fr;
}
.seg-item {
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 40px;
  padding: 8px 10px;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-bg-2);
  color: var(--color-text-2);
  font-size: 12px;
  cursor: pointer;
  line-height: 1.2;
}
.seg-item:hover {
  border-color: rgb(var(--primary-6));
}
.seg-item.active {
  border-color: rgb(var(--primary-6));
  background: var(--color-primary-light-1);
  color: rgb(var(--primary-6));
}
.seg-ico {
  width: 28px;
  height: 18px;
  position: relative;
  display: block;
}
.seg-ico::before,
.seg-ico::after {
  content: '';
  position: absolute;
  background: currentColor;
}
/* 默认宽度：中间短条 */
.width-default::before {
  left: 6px;
  right: 6px;
  top: 8px;
  height: 2px;
  border-radius: 1px;
}
/* 填充容器宽：双向箭头感 */
.width-fill::before {
  left: 2px;
  right: 2px;
  top: 8px;
  height: 2px;
}
.width-fill::after {
  inset: 5px 0;
  background: transparent;
  border-left: 2px solid currentColor;
  border-right: 2px solid currentColor;
}
/* 默认高度 */
.height-default::before {
  left: 13px;
  top: 2px;
  bottom: 2px;
  width: 2px;
}
/* 适应内容 */
.height-fit::before {
  left: 4px;
  right: 4px;
  top: 4px;
  bottom: 4px;
  background: transparent;
  border: 1px dashed currentColor;
  border-radius: 2px;
}
/* 填充高度 */
.height-fill::before {
  left: 13px;
  top: 1px;
  bottom: 1px;
  width: 2px;
}
.height-fill::after {
  left: 8px;
  right: 8px;
  top: 0;
  bottom: 0;
  background: transparent;
  border-top: 2px solid currentColor;
  border-bottom: 2px solid currentColor;
}

.collapse-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 0;
  margin-bottom: 4px;
  border: none;
  background: transparent;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: var(--color-text-1);
}
.collapse-head .arco-icon {
  color: var(--color-text-3);
  transition: transform 0.2s;
}
.collapse-head .arco-icon.open {
  transform: rotate(180deg);
}
.collapse-body {
  padding-top: 4px;
}
.switch-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 40px;
  padding: 6px 0;
  border-bottom: 1px solid var(--color-border);
  font-size: 13px;
  color: var(--color-text-2);
  gap: 8px;
}
.switch-row > span {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
.nested-field {
  padding: 8px 0 12px 12px;
  border-bottom: 1px solid var(--color-border);
}
</style>
