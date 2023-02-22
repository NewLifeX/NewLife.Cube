<template>
  <el-config-provider :locale="locale">
    <router-view></router-view>
  </el-config-provider>
</template>

<script>
// 设置语言为中文
// import { ElConfigProvider } from 'element-plus'
import zhCn from 'element-plus/lib/locale/lang/zh-cn'

export default {
  name: 'App',
  components: {
    // [ElConfigProvider.name]: ElConfigProvider
  },
  setup() {
    return {
      locale: zhCn
    }
  }
}
</script>
