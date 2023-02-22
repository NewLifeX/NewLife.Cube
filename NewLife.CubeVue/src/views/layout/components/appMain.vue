<template>
  <section class="app-main">
    <!-- <keep-alive :include="cachedViews"> -->
    <router-view :key="key" />
    <!-- </keep-alive> -->
  </section>
</template>

<script>
export default {
  name: 'AppMain',
  computed: {
    // cachedViews() {
    //   return this.$store.state.tagsView.cachedViews
    // },
    key() {
      return this.$route.fullPath
    },
  },
}
</script>

<style scoped>
.app-main {
  min-height: 100%;
  width: 100%;
  position: relative;
  overflow: hidden;
  padding: 0px 0px 0px 15px;
}
</style>
