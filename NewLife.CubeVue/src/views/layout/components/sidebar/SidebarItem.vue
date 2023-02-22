<template>
  <div v-if="item.visible">
    <template
      v-if="
        hasOneShowingChild(item.children, item) &&
          (!onlyOneChild.children || onlyOneChild.noShowingChildren) &&
          !item.alwaysShow
      "
    >
      <router-link
        v-if="onlyOneChild"
        :to="onlyOneChild.path || onlyOneChild.url"
      >
        <el-menu-item
          :index="onlyOneChild.path || onlyOneChild.url"
          :class="{ 'submenu-title-noDropdown': !isNest }"
        >
          <item :title="onlyOneChild.displayName" />
        </el-menu-item>
      </router-link>
    </template>

    <el-sub-menu
      v-else
      ref="subMenu"
      :index="item.path || item.url"
      popper-append-to-body
    >
      <template v-slot:title>
        <item
          v-if="item"
          :icon="item.meta && item.meta.icon"
          :title="item.displayName"
        />
      </template>
      <sidebar-item
        v-for="child in item.children"
        :key="child.path"
        :is-nest="true"
        :item="child"
        :base-path="child.path"
        class="nest-menu"
      />
    </el-sub-menu>
  </div>
</template>

<script>
import Item from './Item'

export default {
  name: 'SidebarItem',
  components: { Item },
  props: {
    // route object
    item: {
      type: Object,
      required: true
    },
    isNest: {
      type: Boolean,
      default: false
    },
    basePath: {
      type: String,
      default: ''
    }
  },
  data() {
    // To fix https://github.com/PanJiaChen/vue-admin-template/issues/237
    // TODO: refactor with render function
    this.onlyOneChild = null
    return {}
  },
  created() {
    // console.log(this.item)
  },
  methods: {
    hasOneShowingChild(children = [], parent) {
      let showingChildren = []
      if (children) {
        showingChildren = children.filter((item) => {
          if (item.hidden) {
            return false
          } else {
            // Temp set(will be used if only has one showing child)
            this.onlyOneChild = item

            return true
          }
        })
      }

      // When there is only one child router, the child router is displayed by default
      if (showingChildren.length === 1) {
        return true
      }

      // Show parent if there are no child router to display
      if (showingChildren.length === 0) {
        this.onlyOneChild = { ...parent, /*: '',*/ noShowingChildren: true }
        return true
      }

      return false
    }
  }
}
</script>
