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
      <span class="drawer-title">{{ drawerTitle }}</span>
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
              <icon-park type="info" class="hint-ico" />
            </a-tooltip>
          </div>
          <a-input :model-value="typePath" disabled />
        </section>

        <section class="cfg-block">
          <div class="cfg-label">数据范围</div>
          <div class="range-row">
            <span>全部记录</span>
            <a-typography-text type="secondary" class="cfg-hint">
              （筛选条件使用列表上方搜索区）
            </a-typography-text>
          </div>
        </section>

        <section class="cfg-block">
          <div class="cfg-label row-between">
            <span>字段配置</span>
            <a-typography-text type="secondary" class="cfg-hint">
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
                <icon-park type="drag" />
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
                class="freeze-btn"
                :class="{ 'is-frozen': col.frozen === 'left' }"
                :title="col.frozen === 'left' ? '取消左冻结' : '左冻结至此列'"
                @click="toggleFreeze(col)"
              >
                <icon-park type="pin" />
              </a-button>
              <a-button
                type="text"
                size="mini"
                :title="col.visible ? '隐藏' : '显示'"
                @click="toggleVisible(col)"
              >
                <icon-park v-if="col.visible" type="preview-open" />
                <icon-park v-else type="preview-close" />
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

        <section class="cfg-block">
          <div class="cfg-label">查询洞察</div>
          <div class="switch-row">
            <span>统计标签</span>
            <a-switch v-model="localInsight.showStat" @change="emitInsight" />
          </div>
          <div class="switch-row">
            <span>
              固定图表
              <a-tooltip content="随列表当前搜索条件显示一张固定图表；无图表端点权限时仅图表区降级">
                <icon-park type="info" class="hint-ico" />
              </a-tooltip>
            </span>
            <a-switch v-model="localInsight.showChart" @change="emitInsight" />
          </div>
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
            <icon-park type="down" :class="{ open: openPanel === 'bg' }" />
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
                <icon-park v-if="isBgSwatchSelected(c)" type="check" class="swatch-check" />
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

        <!-- 工具栏 -->
        <section class="cfg-block">
          <button type="button" class="collapse-head" @click="topBarOpen = !topBarOpen">
            <span>工具栏</span>
            <icon-park type="down" :class="{ open: topBarOpen }" />
          </button>
          <div v-show="topBarOpen" class="collapse-body">
            <div class="switch-row">
              <span>筛选</span>
              <a-switch v-model="chrome.showFilter" @change="emitChrome" />
            </div>
            <div v-if="props.viewKind === 'table'" class="switch-row">
              <!-- 分组仅表格视图支持（OSC-0015：树状视图工具栏不提供分组） -->
              <span>分组</span>
              <a-switch v-model="chrome.showGroup" @change="emitChrome" />
            </div>
            <div v-if="isTableLikeViewKind(props.viewKind)" class="switch-row">
              <!-- 排序仅控制列表/树状视图标题栏（表头）排序图标，工具栏不显示排序按钮（OSC-0015） -->
              <span>排序</span>
              <a-switch v-model="chrome.showSort" @change="emitChrome" />
            </div>
            <div class="switch-row">
              <span>搜索</span>
              <a-switch v-model="chrome.showSearch" @change="emitChrome" />
            </div>
          </div>
        </section>

        <!-- 列表区 -->
        <section class="cfg-block">
          <button type="button" class="collapse-head" @click="listAreaOpen = !listAreaOpen">
            <span>{{ listAreaLabel }}</span>
            <icon-park type="down" :class="{ open: listAreaOpen }" />
          </button>
          <div v-show="listAreaOpen" class="collapse-body">
            <template v-if="viewKind === 'table' || viewKind === 'tree'">
              <div class="switch-row">
                <span>分页器</span>
                <a-switch v-model="chrome.showPager" @change="emitChrome" />
              </div>
              <div class="switch-row">
                <span>
                  允许查看记录详情
                  <a-tooltip content="关闭后点击行与「详情」入口将不可用">
                    <icon-park type="info" class="hint-ico" />
                  </a-tooltip>
                </span>
                <a-switch v-model="chrome.allowViewDetail" @change="emitChrome" />
              </div>
              <div class="switch-row">
                <span>
                  允许删除记录
                  <a-tooltip content="仍受菜单权限约束；关闭后隐藏删除入口">
                    <icon-park type="info" class="hint-ico" />
                  </a-tooltip>
                </span>
                <a-switch v-model="chrome.allowDelete" @change="emitChrome" />
              </div>
              <div class="switch-row">
                <span>
                  展开行记录
                  <a-tooltip content="在表格左侧显示展开列，点击可查看详情">
                    <icon-park type="info" class="hint-ico" />
                  </a-tooltip>
                </span>
                <a-switch v-model="chrome.expandRow" @change="emitChrome" />
              </div>
            </template>

            <template v-else-if="viewKind === 'card'">
              <div class="nested-field">
                <div class="cfg-label">卡片标题</div>
                <a-select
                  v-model="localMapping.titleField"
                  placeholder="选择标题字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in titleCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">卡片图片</div>
                <a-select
                  v-model="localMapping.imageField"
                  allow-clear
                  placeholder="无"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in imageCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">卡片布局</div>
                <a-radio-group
                  v-model="localMapping.layout"
                  type="button"
                  size="small"
                  @change="onCardLayoutChange"
                >
                  <a-radio v-for="l in cardLayouts" :key="l.value" :value="l.value">
                    {{ l.label }}
                  </a-radio>
                </a-radio-group>
              </div>
              <div class="nested-field">
                <div class="cfg-label">内容排版列数</div>
                <div class="seg-group seg-group-3">
                  <button
                    v-for="c in cardBodyColumnOptions"
                    :key="c.value"
                    type="button"
                    class="seg-item"
                    :class="{ active: localMapping.bodyColumns === c.value }"
                    :disabled="c.value === 3 && localMapping.layout !== 'row'"
                    @click="setBodyColumns(c.value)"
                  >
                    <span>{{ c.label }}</span>
                  </button>
                </div>
              </div>
              <div class="nested-field">
                <div class="cfg-label">内容排版</div>
                <div class="seg-group">
                  <button
                    type="button"
                    class="seg-item"
                    :class="{ active: localMapping.fieldOrientation === 'horizontal' }"
                    @click="setFieldOrientation('horizontal')"
                  >
                    <span>横向</span>
                  </button>
                  <button
                    type="button"
                    class="seg-item"
                    :class="{ active: localMapping.fieldOrientation === 'vertical' }"
                    @click="setFieldOrientation('vertical')"
                  >
                    <span>竖向</span>
                  </button>
                </div>
              </div>
            </template>

            <template v-else-if="viewKind === 'kanban'">
              <div class="nested-field">
                <div class="cfg-label">分组依据</div>
                <a-select
                  v-model="localMapping.groupField"
                  placeholder="选择分组字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in groupCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">卡片标题</div>
                <a-select
                  v-model="localMapping.titleField"
                  placeholder="选择标题字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in titleCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">卡片图片</div>
                <a-select
                  v-model="localMapping.imageField"
                  allow-clear
                  placeholder="无"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in imageCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
            </template>

            <template v-else-if="viewKind === 'calendar'">
              <div class="nested-field">
                <div class="cfg-label">开始日期 *</div>
                <a-select
                  v-model="localMapping.startField"
                  placeholder="选择开始日期字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in dateCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">结束日期</div>
                <a-select
                  v-model="localMapping.endField"
                  allow-clear
                  placeholder="无"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in dateCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">标题</div>
                <a-select
                  v-model="localMapping.titleField"
                  placeholder="选择标题字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in titleCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">颜色</div>
                <a-select
                  v-model="localMapping.colorField"
                  allow-clear
                  placeholder="无"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in colorCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
            </template>

            <template v-else-if="viewKind === 'gantt'">
              <div class="nested-field">
                <div class="cfg-label">开始 *</div>
                <a-select
                  v-model="localMapping.startField"
                  placeholder="选择开始日期字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in dateCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">结束 *</div>
                <a-select
                  v-model="localMapping.endField"
                  placeholder="选择结束日期字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in dateCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">标题</div>
                <a-select
                  v-model="localMapping.titleField"
                  placeholder="选择标题字段"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in titleCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div class="nested-field">
                <div class="cfg-label">颜色</div>
                <a-select
                  v-model="localMapping.colorField"
                  allow-clear
                  placeholder="无"
                  @change="emitMapping"
                >
                  <a-option
                    v-for="f in colorCandidates"
                    :key="f.name"
                    :value="f.name"
                  >
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
            </template>
          </div>
        </section>
      </a-tab-pane>
    </a-tabs>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import {
  DEFAULT_CHROME,
  normalizeInsight,
  type ColumnPref,
  type HeightMode,
  type ViewChrome,
  type ViewInsight,
  type ViewKind,
  type ViewMapping,
  type ViewSort,
  type WidthMode,
} from '@/core/utils/viewProfile';
import type { FieldMeta } from '@/core/types/field';
import {
  VIEW_KIND_LABEL,
  colorFieldCandidates,
  dateFieldCandidates,
  groupFieldCandidates,
  imageFieldCandidates,
  isTableLikeViewKind,
  normalizeCardBodyColumns,
  normalizeMapping,
  titleFieldCandidates,
  type CardBodyColumns,
  type CardFieldOrientation,
  type CardLayout,
} from '@/core/utils/viewMapping';

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

const props = withDefaults(
  defineProps<{
    visible: boolean;
    typePath: string;
    viewName: string;
    columns: ColumnPref[];
    titles: Record<string, string>;
    sort: ViewSort | null;
    chrome?: ViewChrome | null;
    viewKind?: ViewKind;
    fields?: FieldMeta[];
    mapping?: ViewMapping | null;
    insight?: ViewInsight | null;
  }>(),
  {
    viewKind: 'table',
    fields: () => [],
  },
);

const emit = defineEmits<{
  'update:visible': [boolean];
  'update:columns': [cols: ColumnPref[]];
  'update:sort': [sort: ViewSort | null];
  'update:chrome': [chrome: ViewChrome];
  'update:name': [name: string];
  'update:mapping': [mapping: ViewMapping | undefined];
  'update:insight': [insight: ViewInsight];
}>();

const activeTab = ref('basic');
const openPanel = ref<PanelKey>(null);
const topBarOpen = ref(true);
const listAreaOpen = ref(true);
const localColumns = ref<ColumnPref[]>([]);
const localName = ref('');
const localSort = ref<ViewSort | null>(null);
const chrome = reactive<Required<ViewChrome>>({ ...DEFAULT_CHROME });
const localInsight = ref<ViewInsight>({ showStat: false, showChart: false });
const localMapping = reactive({
  titleField: '',
  imageField: undefined as string | undefined,
  groupField: '',
  startField: '',
  endField: undefined as string | undefined,
  colorField: undefined as string | undefined,
  layout: 'standard' as CardLayout,
  bodyColumns: 2 as CardBodyColumns,
  fieldOrientation: 'vertical' as CardFieldOrientation,
});

const cardLayouts: { value: CardLayout; label: string }[] = [
  { value: 'standard', label: '标准' },
  { value: 'large', label: '偏大' },
  { value: 'row', label: '整行' },
];
const cardBodyColumnOptions: { value: CardBodyColumns; label: string }[] = [
  { value: 1, label: '一列' },
  { value: 2, label: '两列' },
  { value: 3, label: '三列' },
];
let dragFrom = -1;

const drawerTitle = computed(() => `${VIEW_KIND_LABEL[props.viewKind]}视图`);
const listAreaLabel = computed(() => {
  switch (props.viewKind) {
    case 'table':
      return '表格区';
    case 'tree':
      return '树图区';
    case 'card':
      return '卡片区';
    case 'kanban':
      return '看板区';
    case 'calendar':
      return '日历区';
    case 'gantt':
      return '甘特图区';
    default:
      return '视图区';
  }
});
const titleCandidates = computed(() => titleFieldCandidates(props.fields));
const imageCandidates = computed(() => imageFieldCandidates(props.fields));
const groupCandidates = computed(() => groupFieldCandidates(props.fields));
const dateCandidates = computed(() => dateFieldCandidates(props.fields));
const colorCandidates = computed(() => colorFieldCandidates(props.fields));

function fieldLabel(f: FieldMeta): string {
  return (f.displayName?.trim() || f.name).toString();
}

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

function syncMappingFromProps() {
  localMapping.titleField = '';
  localMapping.imageField = undefined;
  localMapping.groupField = '';
  localMapping.startField = '';
  localMapping.endField = undefined;
  localMapping.colorField = undefined;
  localMapping.layout = 'standard';
  localMapping.bodyColumns = 2;
  localMapping.fieldOrientation = 'vertical';

  const kind = props.viewKind;
  if (kind === 'table' || kind === 'tree') return;

  const m = normalizeMapping(kind, props.mapping, props.fields);
  if (!m) return;

  if (m.kind === 'card') {
    localMapping.titleField = m.titleField;
    localMapping.imageField = m.imageField;
    localMapping.layout = m.layout;
    localMapping.bodyColumns = m.bodyColumns;
    localMapping.fieldOrientation = m.fieldOrientation;
  } else if (m.kind === 'kanban') {
    localMapping.groupField = m.groupField;
    localMapping.titleField = m.titleField;
    localMapping.imageField = m.imageField;
  } else if (m.kind === 'calendar') {
    localMapping.startField = m.startField;
    localMapping.endField = m.endField;
    localMapping.titleField = m.titleField;
    localMapping.colorField = m.colorField;
  } else if (m.kind === 'gantt') {
    localMapping.startField = m.startField;
    localMapping.endField = m.endField;
    localMapping.titleField = m.titleField;
    localMapping.colorField = m.colorField;
  }
}

function syncFromProps() {
  localColumns.value = (props.columns || []).map((c) => ({ ...c }));
  localName.value = props.viewName || '';
  localSort.value = props.sort ? { ...props.sort } : null;
  Object.assign(chrome, DEFAULT_CHROME, props.chrome || {});
  localInsight.value = normalizeInsight(props.insight);
  syncMappingFromProps();
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
  () =>
    [
      props.columns,
      props.viewName,
      props.sort,
      props.chrome,
      props.mapping,
      props.viewKind,
      props.fields,
      props.insight,
    ] as const,
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

/** 左冻结：前缀连续冻结。钉住=冻结到该列（含之前可见列）；再点=自该列起解冻 */
function toggleFreeze(col: ColumnPref) {
  const cols = localColumns.value;
  const idx = cols.findIndex((c) => c.key === col.key);
  if (idx < 0) return;
  if (col.frozen === 'left') {
    for (let i = idx; i < cols.length; i++) cols[i].frozen = false;
  } else {
    for (let i = 0; i <= idx; i++) {
      if (cols[i].visible) cols[i].frozen = 'left';
    }
  }
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

/** 洞察开关变更：双开关独立或同时启用，无任意 option/数据源/多图表（OSC-0012） */
function emitInsight() {
  emit('update:insight', { ...localInsight.value });
}

function emitMapping() {
  const kind = props.viewKind;
  if (kind === 'table' || kind === 'tree') {
    emit('update:mapping', undefined);
    return;
  }
  if (kind === 'card') {
    if (!localMapping.titleField) return;
    const layout = localMapping.layout;
    const bodyColumns = normalizeCardBodyColumns(localMapping.bodyColumns, layout);
    localMapping.bodyColumns = bodyColumns;
    emit('update:mapping', {
      kind: 'card',
      titleField: localMapping.titleField,
      imageField: localMapping.imageField || undefined,
      layout,
      bodyColumns,
      fieldOrientation: localMapping.fieldOrientation === 'horizontal' ? 'horizontal' : 'vertical',
    });
    return;
  }
  if (kind === 'kanban') {
    if (!localMapping.groupField || !localMapping.titleField) return;
    emit('update:mapping', {
      kind: 'kanban',
      groupField: localMapping.groupField,
      titleField: localMapping.titleField,
      imageField: localMapping.imageField || undefined,
    });
    return;
  }
  if (kind === 'calendar') {
    if (!localMapping.startField || !localMapping.titleField) return;
    emit('update:mapping', {
      kind: 'calendar',
      startField: localMapping.startField,
      endField: localMapping.endField || undefined,
      titleField: localMapping.titleField,
      colorField: localMapping.colorField || undefined,
    });
    return;
  }
  if (kind === 'gantt') {
    if (!localMapping.startField || !localMapping.endField || !localMapping.titleField) return;
    emit('update:mapping', {
      kind: 'gantt',
      startField: localMapping.startField,
      endField: localMapping.endField,
      titleField: localMapping.titleField,
      colorField: localMapping.colorField || undefined,
    });
  }
}

function onCardLayoutChange() {
  localMapping.bodyColumns = normalizeCardBodyColumns(localMapping.bodyColumns, localMapping.layout);
  emitMapping();
}

function setBodyColumns(cols: CardBodyColumns) {
  if (cols === 3 && localMapping.layout !== 'row') return;
  localMapping.bodyColumns = cols;
  emitMapping();
}

function setFieldOrientation(orient: CardFieldOrientation) {
  localMapping.fieldOrientation = orient;
  emitMapping();
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
  font-size: var(--cube-font-size-title);
  font-weight: var(--cube-font-weight-medium);
}
.cfg-block {
  margin-bottom: 22px;
}
.cfg-label {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
  font-size: var(--cube-font-size-body);
  color: var(--color-text-1);
  font-weight: var(--cube-font-weight-medium);
}
.cfg-hint {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
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
  color: var(--color-text-3);
}
.freeze-btn.is-frozen {
  color: rgb(var(--primary-6));
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
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
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
.color-trigger svg {
  color: inherit;
  opacity: 0.65;
  transition: transform 0.2s;
  flex-shrink: 0;
}
.color-trigger svg.open {
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
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
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
  font-size: var(--cube-font-size-meta);
  filter: drop-shadow(0 0 1px rgba(0, 0, 0, 0.45));
}
.slider-row {
  display: flex;
  justify-content: space-between;
  margin-top: 14px;
  margin-bottom: 4px;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
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
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  cursor: pointer;
  line-height: 1.2;
}
.seg-item:hover:not(:disabled) {
  border-color: rgb(var(--primary-6));
}
.seg-item.active {
  border-color: rgb(var(--primary-6));
  background: var(--color-primary-light-1);
  color: rgb(var(--primary-6));
}
.seg-item:disabled {
  opacity: 0.45;
  cursor: not-allowed;
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
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.collapse-head svg {
  color: var(--color-text-3);
  transition: transform 0.2s;
}
.collapse-head svg.open {
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
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
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
}
</style>
