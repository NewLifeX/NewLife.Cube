<template>
  <div class="box">
    <el-scrollbar wrap-class="scrollbar-wrapper">
      <el-menu
        :default-active="$route.path"
        :collapse="isCollapse"
        background-color="#304156"
        text-color="#bfcbd9"
        :unique-opened="true"
        active-text-color="#409EFF"
        :collapse-transition="false"
        mode="vertical"
      >
        <sidebar-item
          v-for="route in menuRouters"
          :key="route.path"
          :item="route"
          :base-path="route.path"
        />
      </el-menu>
    </el-scrollbar>
  </div>
</template>

<script>
import SidebarItem from './SidebarItem'

export default {
  name: 'Sidebar',
  components: { SidebarItem },
  computed: {
    menuRouters() {
      return this.$store.getters.menuRouters
    },
    sidebar() {
      return this.$store.getters.sidebar
    },
    isCollapse() {
      return !this.sidebar.opened
    }
  },
  data() {
    return {
      active: '1-1-1',
      data: []
    }
  },
  created() {
    // window.menuRouters = this.menuRouters
    // console.log(this.menuRouters)
  }
}
</script>

<style scoped>
.box-wrap {
  display: -moz-flex;
  display: -webkit-flex;
  display: flex;
  height: auto;
}
.box {
  width: auto;
}
</style>
