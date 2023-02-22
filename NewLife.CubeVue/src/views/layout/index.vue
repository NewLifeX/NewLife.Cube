<template>
  <div :class="classObj" class="app-wrapper">
    <div>
      <div
        v-show="!hiddenLayout && device === 'mobile' && sidebar.opened"
        class="drawer-bg"
        @click="handleClickOutside"
      />
      <navbar v-show="!hiddenLayout" />
      <sidebar v-show="!hiddenLayout" class="sidebar sidebar-container" />
      <div :class="classAppMain" class="main">
        <app-main></app-main>
      </div>
    </div>
  </div>
</template>

<script>
import { Navbar, Sidebar, AppMain } from '@/views/layout/components'
import ResizeMixin from '@/views/layout/mixin/ResizeHandler'

export default {
  components: {
    Navbar,
    Sidebar,
    AppMain,
  },
  mixins: [ResizeMixin],
  computed: {
    sidebar() {
      return this.$store.state.app.sidebar
    },
    device() {
      return this.$store.state.app.device
    },
    hiddenLayout() {
      let query = this.$route.query
      return query.hiddenLayout === 'true' || query.hl === 'true'
    },
    classObj() {
      return {
        hideSidebar: !this.sidebar.opened,
        openSidebar: this.sidebar.opened,
        withoutAnimation: this.sidebar.withoutAnimation,
        mobile: this.device === 'mobile',
      }
    },
    classAppMain() {
      return {
        hiddenLayout: this.hiddenLayout,
        hideSidebarMain: !this.sidebar.opened,
        openSidebarMain: this.sidebar.opened,
      }
    },
  },
  methods: {
    handleClickOutside() {
      this.$store.dispatch('closeSideBar', { withoutAnimation: false })
    },
  },
}
</script>

<style>
.app-wrapper {
  position: relative;
  height: 100%;
  width: 100%;
}

.sidebar {
  position: fixed;
  top: 50px;
  bottom: 0;
  height: 100%;
}

.main {
  position: fixed;
  top: 50px;
  bottom: 0;
  height: 100%;
}

.hideSidebarMain {
  left: 0px;
  width: -webkit-calc(100% - 40px);
  width: -moz-calc(100% - 40px);
  width: calc(100% - 40px);
}

.openSidebarMain {
  left: 210px;
  width: -webkit-calc(100% - 240px);
  width: -moz-calc(100% - 240px);
  width: calc(100% - 240px);
}

.hiddenLayout {
  left: 0px;
  width: -webkit-calc(100% - 40px);
  width: -moz-calc(100% - 40px);
  width: calc(100% - 40px);
  top: 0px;
}
</style>

<style scoped>
.drawer-bg {
  background: #000;
  opacity: 0.3;
  width: 100%;
  top: 0;
  height: 100%;
  position: fixed;
  z-index: 999;
}
</style>
