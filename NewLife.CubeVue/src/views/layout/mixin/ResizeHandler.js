const { body } = document
const WIDTH = 1024
const RATIO = 3

export default {
  data() {
    return {
      currentDevice: 'desktop',
    }
  },
  watch: {
    $route() {
      if (this.device === 'mobile' && this.sidebar.opened) {
        this.currentDevice = 'mobile'
        this.$store.dispatch('closeSideBar', { withoutAnimation: false })
      }
    },
  },
  beforeMount() {
    window.addEventListener('resize', this.resizeHandler)
  },
  mounted() {
    const isMobile = this.isMobile()
    if (isMobile) {
      this.currentDevice = 'mobile'
      this.$store.dispatch('toggleDevice', 'mobile')
      this.$store.dispatch('closeSideBar', { withoutAnimation: true })
    }
  },
  methods: {
    isMobile() {
      const rect = body.getBoundingClientRect()
      return rect.width - RATIO < WIDTH
    },
    resizeHandler() {
      if (!document.hidden) {
        const isMobile = this.isMobile()
        if (isMobile && this.currentDevice !== 'mobile') {
          this.currentDevice = 'mobile'
          this.$store.dispatch('toggleDevice', 'mobile')
          this.$store.dispatch('closeSideBar', { withoutAnimation: true })
        } else if (!isMobile && this.currentDevice !== 'desktop') {
          this.currentDevice = 'desktop'
          this.$store.dispatch('toggleDevice', 'desktop')
          this.$store.dispatch('toggleSideBar')
        }
      }
    },
  },
}
