<template>
  <el-tabs v-model="activeName" @tab-click="handleClick">
    <el-tab-pane label="魔方设置" name="Cube">
      <object-form path="/Admin/Cube"></object-form>
    </el-tab-pane>
    <el-tab-pane label="基础设置" name="Core">
      <object-form path="/Admin/Core"></object-form>
    </el-tab-pane>
    <el-tab-pane label="系统设置" name="Sys">
      <object-form path="/Admin/Sys"></object-form>
    </el-tab-pane>
    <el-tab-pane label="数据中间件" name="XCode">
      <object-form path="/Admin/XCode"></object-form>
    </el-tab-pane>
    <el-tab-pane label="OAuth设置" name="OAuthConfig">
      <router-link to="/Admin/OAuthConfig">跳转OAuth设置</router-link>
    </el-tab-pane>
  </el-tabs>
</template>
<script>
import ObjectForm from '@/views/common/objectForm'
import List from '@/views/common/list'
export default {
  components: {
    ObjectForm
  },
  data() {
    return {
      activeName: 'Cube'
    }
  },
  methods: {
    handleClick(tab, event) {
      let vm = this
      // console.log(tab, event)
      if (tab.name === 'OAuthConfig') {
        vm.$router.push({ path: '/Admin/OAuthConfig', component: List })
      }
    }
  }
}
</script>
