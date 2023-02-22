<template>
  <div>
    <template v-if="icon">
      <i v-if="icon.includes('el-icon')" :class="[icon, 'sub-el-icon']" />
      <!-- <svg-icon v-else :icon-class="icon" /> -->
    </template>
    <template v-if="title">
      <span>{{ title }}</span>
    </template>
  </div>
</template>
<script>
export default {
  name: 'MenuItem',
  props: {
    icon: {
      type: String,
      default: ''
    },
    title: {
      type: String,
      default: ''
    }
  }
  // render(h, context) {
  //   console.log(context)
  //   const { icon, title } = context.props
  //   const vnodes = []

  //   if (icon) {
  //     if (icon.includes('el-icon')) {
  //       vnodes.push(<i class={[icon, 'sub-el-icon']} />)
  //     } else {
  //       vnodes.push(<svg-icon icon-class={icon} />)
  //     }
  //   }

  //   if (title) {
  //     vnodes.push(<span>{title}</span>)
  //   }
  //   return vnodes
  // }
}
</script>

<style scoped>
.sub-el-icon {
  color: currentColor;
  width: 1em;
  height: 1em;
}
</style>
