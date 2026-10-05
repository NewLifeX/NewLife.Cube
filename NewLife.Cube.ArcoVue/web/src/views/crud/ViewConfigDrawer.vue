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
          <div v-if="viewKind === 'map'" class="cfg-hint" style="margin-bottom: 6px">
            地图视图的可见字段决定悬停卡片内容（悬停散点显示）
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
              <a-tooltip content="拖动排序">
                <span
                  class="drag-handle"
                  draggable="true"
                  @dragstart="onDragStart(idx, $event)"
                >
                  <icon-park type="drag" />
                </span>
              </a-tooltip>
              <a-input
                class="field-title-input"
                size="mini"
                :class="{ muted: !col.visible }"
                :model-value="displayTitle(col)"
                :placeholder="titles[col.key] || col.key"
                @update:model-value="(v: string) => onTitleEdit(col, v)"
                @press-enter="(e: KeyboardEvent) => (e.target as HTMLInputElement)?.blur()"
              />
              <template v-if="isTableLikeViewKind(props.viewKind)">
                <a-tooltip :content="col.frozen === 'left' ? '取消左冻结' : '左冻结此列'">
                  <a-button
                    type="text"
                    size="mini"
                    class="freeze-btn"
                    :class="{ 'is-frozen': col.frozen === 'left' }"
                    @click="toggleFreeze(col)"
                  >
                    <icon-park type="left-bar" />
                  </a-button>
                </a-tooltip>
                <a-tooltip :content="col.frozen === 'right' ? '取消右冻结' : '右冻结此列'">
                  <a-button
                    type="text"
                    size="mini"
                    class="freeze-btn"
                    :class="{ 'is-frozen': col.frozen === 'right' }"
                    @click="toggleFreezeRight(col)"
                  >
                    <icon-park type="right-bar" />
                  </a-button>
                </a-tooltip>
              </template>
              <a-tooltip :content="col.visible ? '隐藏' : '显示'">
                <a-button
                  type="text"
                  size="mini"
                  @click="toggleVisible(col)"
                >
                  <icon-park v-if="col.visible" type="preview-open" />
                  <icon-park v-else type="preview-close" />
                </a-button>
              </a-tooltip>
            </li>
          </ul>
        </section>

        <!-- 地图（OSC-261004d7f4）：坐标字段 / 点聚合（暂不支持，验收降级）/ 拉取上限 / 地图中心+默认缩放 -->
        <section v-if="viewKind === 'map'" class="cfg-block">
          <div class="cfg-label">地图</div>
          <div class="nested-field">
            <div class="cfg-label">坐标模式</div>
            <div class="seg-group">
              <button
                type="button"
                class="seg-item"
                :class="{ active: localMapping.mapCoordMode === 'latlng' }"
                @click="setMapCoordMode('latlng')"
              >
                <span>经纬度分列</span>
              </button>
              <button
                type="button"
                class="seg-item"
                :class="{ active: localMapping.mapCoordMode === 'merged' }"
                @click="setMapCoordMode('merged')"
              >
                <span>合并坐标</span>
              </button>
            </div>
          </div>
          <template v-if="localMapping.mapCoordMode === 'latlng'">
            <div class="nested-field">
              <div class="cfg-label">经度字段 *</div>
              <a-select
                v-model="localMapping.mapLngField"
                placeholder="选择经度字段"
                @change="emitMapping"
              >
                <a-option v-for="f in mapNumericCandidates" :key="f.name" :value="f.name">
                  {{ fieldLabel(f) }}
                </a-option>
              </a-select>
            </div>
            <div class="nested-field">
              <div class="cfg-label">纬度字段 *</div>
              <a-select
                v-model="localMapping.mapLatField"
                placeholder="选择纬度字段"
                @change="emitMapping"
              >
                <a-option v-for="f in mapNumericCandidates" :key="f.name" :value="f.name">
                  {{ fieldLabel(f) }}
                </a-option>
              </a-select>
            </div>
          </template>
          <template v-else>
            <div class="nested-field">
              <div class="cfg-label">坐标字段 *</div>
              <a-select
                v-model="localMapping.mapCoordField"
                placeholder="选择存储“经度,纬度”的字段"
                @change="emitMapping"
              >
                <a-option v-for="f in mapMergedCandidates" :key="f.name" :value="f.name">
                  {{ fieldLabel(f) }}
                </a-option>
              </a-select>
            </div>
            <div class="nested-field">
              <div class="cfg-label">坐标顺序</div>
              <div class="seg-group">
                <button
                  type="button"
                  class="seg-item"
                  :class="{ active: localMapping.mapCoordOrder === 'lnglat' }"
                  @click="localMapping.mapCoordOrder = 'lnglat'; emitMapping()"
                >
                  <span>经度在前</span>
                </button>
                <button
                  type="button"
                  class="seg-item"
                  :class="{ active: localMapping.mapCoordOrder === 'latlng' }"
                  @click="localMapping.mapCoordOrder = 'latlng'; emitMapping()"
                >
                  <span>纬度在前</span>
                </button>
              </div>
            </div>
          </template>
          <div class="nested-field">
            <div class="cfg-label">数据坐标系</div>
            <a-select v-model="localMapping.mapCoordSystem" @change="emitMapping">
              <a-option v-for="cs in mapCoordSystemOptions" :key="cs" :value="cs">
                {{ cs === 'gcj02' ? 'GCJ-02（国内常见）' : cs === 'wgs84' ? 'WGS-84（GPS 原始）' : 'BD-09（百度）' }}
              </a-option>
            </a-select>
          </div>
          <div class="nested-field">
            <div class="cfg-label">悬停卡片标题</div>
            <a-select
              v-model="localMapping.titleField"
              placeholder="选择标题字段"
              @change="emitMapping"
            >
              <a-option v-for="f in titleCandidates" :key="f.name" :value="f.name">
                {{ fieldLabel(f) }}
              </a-option>
            </a-select>
          </div>
          <div class="switch-row">
            <span>点聚合（暂不支持，后续变更实现）</span>
            <a-switch v-model="localMapping.mapCluster" disabled @change="emitMapping" />
          </div>
          <div class="nested-field">
            <div class="cfg-label">最大拉取点位</div>
            <a-input-number
              v-model="localMapping.mapMaxPoints"
              :min="1000"
              :max="mapMaxPointsLimit"
              :step="1000"
              @change="emitMapping"
            />
          </div>
          <div class="nested-field">
            <div class="cfg-label">
              地图中心（按标题字段搜索）
              <a-tooltip content="填写标题字段的值（如：北京）。地图加载时自动定位；右下角「定位」按钮可随时回到该中心。">
                <icon-park type="info" class="hint-ico" />
              </a-tooltip>
            </div>
            <a-input
              v-model="localMapping.mapCenter"
              allow-clear
              placeholder="如：北京（按标题字段精确搜索）"
              @change="emitMapping"
            />
          </div>
          <div class="nested-field">
            <div class="cfg-label">默认缩放</div>
            <a-input-number v-model="localMapping.mapZoom" :min="3" :max="18" @change="emitMapping" />
          </div>
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
          <div class="cfg-label">页面仪表盘</div>
          <a-button size="small" @click="emit('openDashboard')">打开页面仪表盘</a-button>
        </section>
      </a-tab-pane>

      <a-tab-pane key="custom" title="自定义配置">
        <!-- 背景色（地图视图全出血，不提供背景/宽度/高度配置） -->
        <section v-if="viewKind !== 'map'" class="cfg-block">
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
        <section v-if="viewKind !== 'map'" class="cfg-block">
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
        <section v-if="viewKind !== 'map'" class="cfg-block">
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
            <div v-if="['table', 'tree', 'card', 'map'].includes(viewKind)" class="switch-row">
              <!-- 填色仅表格/树/卡片/地图视图有对应按钮（与 formatButtonVisible 同集合） -->
              <span>填色</span>
              <a-switch v-model="chrome.showColor" @change="emitChrome" />
            </div>
            <div v-if="viewKind !== 'map'" class="switch-row">
              <!-- 分享：地图工具栏无分享按钮，不展示该项 -->
              <span>分享</span>
              <a-switch v-model="chrome.showShare" @change="emitChrome" />
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
                    :disabled="bodyColumnDisabled(c.value)"
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
                <div class="cfg-label">计划开始 *</div>
                <a-select
                  v-model="localMapping.plannedStartField"
                  placeholder="选择计划开始日期字段"
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
                <div class="cfg-label">计划结束 *</div>
                <a-select
                  v-model="localMapping.plannedEndField"
                  placeholder="选择计划结束日期字段"
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
                <div class="cfg-label">分组字段</div>
                <a-select
                  v-model="localMapping.groupField"
                  placeholder="不分组"
                  @change="emitMapping"
                >
                  <a-option value="">不分组</a-option>
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
                <div class="cfg-label">实际开始</div>
                <a-select
                  v-model="localMapping.actualStartField"
                  allow-clear
                  placeholder="无（仅显示计划）"
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
                <div class="cfg-label">实际结束</div>
                <a-select
                  v-model="localMapping.actualEndField"
                  allow-clear
                  placeholder="无（仅显示计划）"
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
                <div class="cfg-label">任务条颜色</div>
                <div class="gantt-color-panel">
                  <!-- 预置色板 + 自定义色块（与外观设置「自定义主色」一致，OSC-0019 后续） -->
                  <div class="gantt-preset-swatches">
                    <button
                      v-for="c in PRESET_THEME_COLORS"
                      :key="c.key"
                      type="button"
                      class="gantt-preset-swatch"
                      :class="{ selected: barColorShown.toLowerCase() === c.color.toLowerCase() }"
                      :style="{ background: c.color }"
                      :title="c.name"
                      @click="pickBarPresetColor(c)"
                    >
                      <icon-park
                        v-if="barColorShown.toLowerCase() === c.color.toLowerCase()"
                        type="check"
                        class="gantt-swatch-check"
                      />
                    </button>
                    <!-- 自定义色块：当前值（含主题主色），非预置时显示选中态 -->
                    <button
                      type="button"
                      class="gantt-preset-swatch gantt-custom-swatch"
                      :class="{ selected: !isBarPresetActive() }"
                      :style="{ background: barColorShown }"
                      title="自定义任务条颜色"
                      @click="openBarColorPicker"
                    >
                      <icon-park v-if="!isBarPresetActive()" type="check" class="gantt-swatch-check" />
                    </button>
                    <input
                      ref="barColorInputRef"
                      type="color"
                      :value="barColorShown"
                      class="gantt-color-input-hidden"
                      @input="onBarColorInput"
                    />
                  </div>
                  <div class="gantt-color-actions">
                    <a-button size="mini" @click="clearBarColor">恢复默认</a-button>
                  </div>
                </div>
              </div>
            </template>

            <!-- 地图区（OSC-261004d7f4）：分类字段 + 值→图标/颜色 规则 + 默认样式 -->
            <template v-else-if="viewKind === 'map'">
              <div class="nested-field">
                <div class="cfg-label">分类字段（值 → 图标/颜色）</div>
                <a-select
                  v-model="localMapping.mapCategoryField"
                  allow-clear
                  placeholder="无（全部使用默认样式）"
                  @change="onMapCategoryChange"
                >
                  <a-option v-for="f in mapCategoryOptions" :key="f.name" :value="f.name">
                    {{ fieldLabel(f) }}
                  </a-option>
                </a-select>
              </div>
              <div v-if="localMapping.mapCategoryField" class="nested-field">
                <div v-for="(rule, idx) in localMapping.mapCategoryRules" :key="idx" class="map-rule-row">
                  <a-select
                    v-if="mapCategoryValueOptions.length"
                    v-model="rule.value"
                    size="mini"
                    class="map-rule-value"
                    @change="updateMapRule(idx, {})"
                  >
                    <a-option v-for="opt in mapCategoryValueOptions" :key="opt.value" :value="opt.value">
                      {{ opt.label }}
                    </a-option>
                  </a-select>
                  <a-input
                    v-else
                    v-model="rule.value"
                    size="mini"
                    class="map-rule-value"
                    placeholder="值"
                    @change="updateMapRule(idx, {})"
                  />
                  <a-popover trigger="click" position="bl">
                    <button
                      type="button"
                      class="map-rule-icon"
                      :style="{ color: rule.color || localMapping.mapDefaultColor }"
                    >
                      <icon-park :type="rule.icon || localMapping.mapDefaultIcon" />
                    </button>
                    <template #content>
                      <div class="map-icon-grid">
                        <button
                          v-for="ic in mapMarkerIcons"
                          :key="ic"
                          type="button"
                          class="map-icon-cell"
                          :class="{ selected: (rule.icon || localMapping.mapDefaultIcon) === ic }"
                          @click="updateMapRule(idx, { icon: ic })"
                        >
                          <icon-park :type="ic" />
                        </button>
                      </div>
                    </template>
                  </a-popover>
                  <input
                    type="color"
                    class="map-rule-color"
                    :value="rule.color || localMapping.mapDefaultColor"
                    @input="onMapRuleColorInput(idx, ($event.target as HTMLInputElement).value)"
                  />
                  <a-button type="text" size="mini" @click="removeMapRule(idx)">
                    <icon-park type="close" />
                  </a-button>
                </div>
                <a-button
                  size="mini"
                  :disabled="localMapping.mapCategoryRules.length >= mapMaxRules"
                  @click="addMapRule"
                >
                  + 添加分类样式
                </a-button>
              </div>
              <div class="nested-field">
                <div class="cfg-label">默认样式（未命中分类）</div>
                <div class="map-default-row">
                  <a-popover trigger="click" position="bl">
                    <button
                      type="button"
                      class="map-rule-icon"
                      :style="{ color: localMapping.mapDefaultColor }"
                    >
                      <icon-park :type="localMapping.mapDefaultIcon" />
                    </button>
                    <template #content>
                      <div class="map-icon-grid">
                        <button
                          v-for="ic in mapMarkerIcons"
                          :key="ic"
                          type="button"
                          class="map-icon-cell"
                          :class="{ selected: localMapping.mapDefaultIcon === ic }"
                          @click="setMapDefaultIcon(ic)"
                        >
                          <icon-park :type="ic" />
                        </button>
                      </div>
                    </template>
                  </a-popover>
                  <input
                    type="color"
                    class="map-rule-color"
                    :value="localMapping.mapDefaultColor"
                    @input="onMapDefaultColorInput(($event.target as HTMLInputElement).value)"
                  />
                  <a-button size="mini" @click="resetMapStyle">恢复默认</a-button>
                </div>
              </div>
            </template>
          </div>
        </section>
      </a-tab-pane>
    </a-tabs>
  </a-drawer>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import { isTableLikeViewKind } from '@/core/utils/viewMapping';
import type {
  ColumnPref,
  ViewChrome,
  ViewInsight,
  ViewKind,
  ViewMapping,
  ViewSort,
} from '@/core/utils/viewProfile';
import { PRESET_THEME_COLORS } from '@/core/utils/presetColors';
import { useViewConfigDrawer } from './useViewConfigDrawer';

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
    chartRows?: Record<string, unknown>[];
    /** 分类字段值获取（拉数据抽样提取去重值；无 dataSource 字段用，OSC-261004d7f4 增强） */
    loadCategoryValues?: (field: string) => Promise<string[]>;
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
  openDashboard: [];
}>();

const {
  activeTab,
  openPanel,
  topBarOpen,
  listAreaOpen,
  localColumns,
  localName,
  localSort,
  chrome,
  localMapping,
  barColorShown,
  barColorInputRef,
  cardLayouts,
  cardBodyColumnOptions,
  bodyColumnDisabled,
  recommendedColors,
  drawerTitle,
  listAreaLabel,
  titleCandidates,
  imageCandidates,
  groupCandidates,
  dateCandidates,
  colorCandidates,
  fieldLabel,
  visibleCount,
  chipStyle,
  bgFilled,
  bgTriggerLabel,
  bgTriggerStyle,
  togglePanel,
  displayTitle,
  onTitleEdit,
  toggleVisible,
  toggleFreeze,
  toggleFreezeRight,
  onDragStart,
  onDrop,
  onSortField,
  onSortDir,
  emitChrome,
  emitMapping,
  isBarPresetActive,
  pickBarPresetColor,
  openBarColorPicker,
  onBarColorInput,
  clearBarColor,
  onCardLayoutChange,
  setBodyColumns,
  setFieldOrientation,
  emitName,
  restoreBgDefault,
  isBgSwatchSelected,
  pickBgSwatch,
  onBgColorPick,
  setWidth,
  setHeight,
  mapCoordSystemOptions,
  mapMarkerIcons,
  mapMaxPointsLimit,
  mapMaxRules,
  mapNumericCandidates,
  mapMergedCandidates,
  mapCategoryOptions,
  mapCategoryValueOptions,
  setMapCoordMode,
  onMapCategoryChange,
  addMapRule,
  removeMapRule,
  updateMapRule,
  onMapRuleColorInput,
  setMapDefaultIcon,
  onMapDefaultColorInput,
  resetMapStyle,
} = useViewConfigDrawer(props, emit);
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
  /* 固定约 7 行可视高度，多余字段滚动查看（含已有 ViewProfile 长列表） */
  max-height: calc(7 * (24px + 12px + 1px));
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

/* 甘特任务条颜色：预置色板 + 自定义色块（与外观设置「自定义主色」一致，OSC-0019 后续） */
.gantt-color-panel {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}
.gantt-preset-swatches {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
}
.gantt-preset-swatch {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid var(--color-border-2);
  padding: 0;
  cursor: pointer;
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
}
.gantt-preset-swatch:hover {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.gantt-preset-swatch.selected {
  box-shadow: 0 0 0 2px rgb(var(--primary-6));
}
.gantt-swatch-check {
  color: #fff;
  font-size: 14px;
  filter: drop-shadow(0 0 1px rgba(0, 0, 0, 0.45));
}
/* 自定义色块：与预置色同尺寸，覆盖在色板网格最后一个格子 */
.gantt-custom-swatch {
  grid-column: span 1;
}
.gantt-color-input-hidden {
  position: absolute;
  opacity: 0;
  width: 1px;
  height: 1px;
  pointer-events: none;
}
.gantt-color-actions {
  display: flex;
  justify-content: flex-end;
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
/* 地图分类样式编辑（OSC-261004d7f4） */
.map-rule-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}
.map-rule-value {
  flex: 1 1 auto;
  min-width: 0;
}
.map-rule-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: 1px solid var(--color-border-2);
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  font-size: 16px;
}
.map-rule-icon:hover {
  border-color: rgb(var(--primary-6));
}
.map-rule-color {
  width: 28px;
  height: 28px;
  padding: 0;
  border: 1px solid var(--color-border-2);
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
}
.map-icon-grid {
  display: grid;
  grid-template-columns: repeat(8, 28px);
  gap: 4px;
}
.map-icon-cell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: 1px solid transparent;
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  color: var(--color-text-2);
  font-size: 16px;
}
.map-icon-cell:hover {
  background: var(--color-fill-2);
}
.map-icon-cell.selected {
  border-color: rgb(var(--primary-6));
  color: rgb(var(--primary-6));
}
.map-default-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
