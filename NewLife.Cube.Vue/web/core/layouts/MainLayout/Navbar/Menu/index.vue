<script setup lang="tsx">
import { computed, ref, onMounted, nextTick, watch } from 'vue';
import { useRouter } from 'vue-router';
import { type TreeMenuItem, useMenuStore } from 'cube-front/core/stores/menu';
import CascaderMenu from '../CascaderMenu/index.vue';
import { ElScrollbar } from 'element-plus';
import { hasChildren } from 'cube-front/core/utils/menuHelpers';
import { openMenuTab } from 'cube-front/core/utils/menuTab';

interface MenuProps {
  tabPanes?: Array<TreeMenuItem>;
}

const props = defineProps<MenuProps>();
const cascaderMenuWidth = 724;
const clientX = ref<number>(0);
const menuCascaderHeight = ref<number>(500);
const router = useRouter();
const menuStore = useMenuStore();
const activeMenu = computed(() => menuStore.activeMenu);
const currentMenu = ref<TreeMenuItem | undefined>();

const scrollbarRef = ref<InstanceType<typeof ElScrollbar> | null>(null);
const menuRef = ref<HTMLElement | null>(null);
const showLeftArrow = ref(false);
const showRightArrow = ref(false);
const scrollPosition = ref(0);
const scrollWidth = ref(0);
const containerWidth = ref(0);

const updateArrowVisibility = () => {
  if (!menuRef.value || !scrollbarRef.value) return;
  const wrapperElement = scrollbarRef.value.$el.querySelector('.el-scrollbar__wrap');
  if (!wrapperElement) return;
  scrollPosition.value = wrapperElement.scrollLeft;
  scrollWidth.value = menuRef.value.scrollWidth;
  containerWidth.value = wrapperElement.clientWidth;
  showLeftArrow.value = scrollPosition.value > 0;
  const tolerance = 2;
  showRightArrow.value =
    scrollWidth.value > containerWidth.value &&
    scrollPosition.value < scrollWidth.value - containerWidth.value - tolerance;
};

const scrollLeft = () => {
  if (!scrollbarRef.value?.$el) return;
  const w = scrollbarRef.value.$el.querySelector('.el-scrollbar__wrap');
  if (!w) return;
  w.scrollLeft -= 200;
  updateArrowVisibility();
};

const scrollRight = () => {
  if (!scrollbarRef.value?.$el) return;
  const w = scrollbarRef.value.$el.querySelector('.el-scrollbar__wrap');
  if (!w) return;
  w.scrollLeft += 200;
  updateArrowVisibility();
};

const handleWheel = (e: WheelEvent) => {
  if (!scrollbarRef.value?.$el) return;
  const w = scrollbarRef.value.$el.querySelector('.el-scrollbar__wrap');
  if (!w) return;
  e.preventDefault();
  w.scrollLeft += e.deltaY || e.deltaX;
  updateArrowVisibility();
};

onMounted(async () => {
  await nextTick();
  updateArrowVisibility();
  window.addEventListener('resize', updateArrowVisibility);
  if (scrollbarRef.value?.$el) {
    const w = scrollbarRef.value.$el.querySelector('.el-scrollbar__wrap');
    if (w) {
      w.addEventListener('scroll', updateArrowVisibility);
      scrollbarRef.value.$el.addEventListener('wheel', handleWheel, { passive: false });
    }
  }
});

watch(
  () => props.tabPanes,
  async () => {
    await nextTick();
    updateArrowVisibility();
  },
  { deep: true },
);

const showCascaderMenu = computed(() => {
  if (currentMenu.value && !currentMenu.value.parentMenu) return hasChildren(currentMenu.value);
  return currentMenu.value !== undefined;
});

const isMenuActive = (menu: TreeMenuItem) => {
  if (menuStore.topLevelActiveMenu?.path === menu.path) return true;
  if (menuStore.topLevelActiveMenu?.id === menu.id) return true;
  if (activeMenu.value && isChildOf(activeMenu.value, menu)) return true;
  if (router.currentRoute.value.path === menu.path) return true;
  return false;
};

const isChildOf = (child: TreeMenuItem, parent: TreeMenuItem) => {
  if (!child || !parent) return false;
  if (child.id === parent.id) return true;
  let p = child.parentMenu;
  while (p) {
    if (p.id === parent.id) return true;
    p = p.parentMenu;
  }
  return false;
};

const handleMouseEnter = (event: MouseEvent, menuItem: TreeMenuItem) => {
  event.preventDefault();
  event.stopPropagation();
  const target = event.target as HTMLElement;
  let x = event.clientX - (target?.clientWidth || 0);
  if (x + cascaderMenuWidth > window.innerWidth) x = window.innerWidth - cascaderMenuWidth;
  clientX.value = x;
  currentMenu.value = menuItem;
};

const handleMouseLeave = (event: MouseEvent) => {
  event.preventDefault();
  event.stopPropagation();
  currentMenu.value = undefined;
};

const handleMaskTrigger = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  const relatedTarget = e.relatedTarget as HTMLElement;
  if (
    target?.classList.contains('side-mask') &&
    !relatedTarget?.closest('.menu-cascader-parent') &&
    !relatedTarget?.closest('.menu-container')
  ) {
    currentMenu.value = undefined;
  }
};

const onMenuClick = (menu: TreeMenuItem) => {
  openMenuTab({ url: menu.path, title: menu.name });
  menuStore.setActiveMenu(menu);
  currentMenu.value = undefined;
};
</script>

<template>
  <div class="menu-container" @mouseleave="handleMouseLeave">
    <!-- 左箭头 -->
    <div class="scroll-arrow scroll-arrow--left" @click="scrollLeft" v-show="showLeftArrow">
      <svg
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2.5"
        stroke-linecap="round"
        stroke-linejoin="round"
      >
        <polyline points="15 18 9 12 15 6" />
      </svg>
    </div>

    <ElScrollbar class="menu-scrollbar" ref="scrollbarRef">
      <div class="menu" ref="menuRef">
        <li v-for="(tabPane, index) in props.tabPanes" :key="index">
          <div
            class="menu-item"
            :class="{ 'menu-item--active': isMenuActive(tabPane) }"
            @mouseenter="(e) => handleMouseEnter(e, tabPane)"
          >
            {{ tabPane.title || tabPane.name }}
          </div>
        </li>
      </div>
    </ElScrollbar>

    <!-- 右箭头 -->
    <div class="scroll-arrow scroll-arrow--right" @click="scrollRight" v-show="showRightArrow">
      <svg
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2.5"
        stroke-linecap="round"
        stroke-linejoin="round"
      >
        <polyline points="9 18 15 12 9 6" />
      </svg>
    </div>

    <!-- 层叠菜单 -->
    <div
      v-if="currentMenu"
      :style="{ left: `${clientX}px`, height: `${menuCascaderHeight}px` }"
      class="menu-cascader-parent"
    >
      <CascaderMenu
        v-show="showCascaderMenu"
        :menu="currentMenu"
        :currentMenu="currentMenu"
        :activeMenu="activeMenu"
        :onMenuClick="onMenuClick"
      />
    </div>

    <!-- 遮罩 -->
    <div
      v-if="showCascaderMenu"
      class="side-mask"
      @mouseenter="(e) => handleMaskTrigger(e)"
      @mousemove="(e) => handleMaskTrigger(e)"
    ></div>
  </div>
</template>

<style lang="scss" scoped>
.menu-container {
  position: relative;
  width: 100%;
  height: 56px;
  display: flex;
  align-items: center;
  overflow: hidden;
}

.scroll-arrow {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 36px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.9);
  color: #64748b;
  cursor: pointer;
  z-index: 20;
  backdrop-filter: blur(6px);
  border: 1px solid #e2e0dc;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.18s ease;

  &:hover {
    background: #ffffff;
    color: #1e40af;
    border-color: #bfdbfe;
    box-shadow: 0 2px 8px rgba(30, 64, 175, 0.1);
  }

  &--left {
    left: 4px;
  }
  &--right {
    right: 4px;
  }
}

.menu-scrollbar {
  flex: 1;
  width: 100%;
  height: 63px;
  overflow: hidden;

  :deep(.el-scrollbar__wrap) {
    overflow-x: auto;
    overflow-y: hidden;
    scrollbar-width: none;
    -ms-overflow-style: none;
    &::-webkit-scrollbar {
      width: 0;
      height: 0;
      display: none;
    }
  }
  :deep(.el-scrollbar__bar) {
    display: none !important;
  }
}

.menu {
  display: flex;
  height: 56px;
  margin: 0;
  padding: 0 10px;
  list-style: none;
  white-space: nowrap;
  min-width: max-content;
  width: fit-content;

  .menu-item {
    display: flex;
    align-items: center;
    height: 100%;
    margin: auto 28px auto 4px;
    cursor: pointer;
    font-weight: 600;
    font-size: 14px;
    color: #64748b;
    padding: 0 6px;
    position: relative;
    transition: color 0.2s ease;

    &::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 50%;
      transform: translateX(-50%);
      width: 0;
      height: 2.5px;
      background: linear-gradient(90deg, #1e40af, #2563eb);
      border-radius: 2px 2px 0 0;
      transition: width 0.25s cubic-bezier(0.4, 0, 0.2, 1);
    }

    &--active {
      color: #1e40af;
      &::after {
        width: 100%;
      }
    }

    &:hover {
      color: #1e40af;
    }
  }
}

.menu-cascader-parent {
  position: fixed;
  min-height: 60vh;
  max-height: 80vh;
  overflow: auto;
  top: 56px;
  z-index: 1100;
}

.side-mask {
  position: fixed;
  inset: 56px 0 0;
  z-index: 1080;
  background: #0f172a;
  opacity: 0.12;
  backdrop-filter: blur(1px);
}
</style>
