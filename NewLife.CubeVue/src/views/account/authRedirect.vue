<script>
export default {
  name: 'AuthRedirect',
  async created() {
    let vm = this
    const redirect = vm.$route.query.redirect || '/'
    let token = vm.$route.hash.replace('#token=', '')
    vm.$store.dispatch('setToken', token)
    // 报错解决：Redirected when going from "/auth-redirect" to "/Admin/User" via a navigation guard
    // https://blog.csdn.net/Tom__cy/article/details/112846816
    vm.$router.push(redirect, () => {})
  },
  render() {
    return ''
  },
}
</script>
