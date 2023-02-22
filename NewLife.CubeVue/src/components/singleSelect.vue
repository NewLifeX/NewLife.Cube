<template>
  <el-select v-model="data" filterable clearable @focus="getData">
    <el-option
      v-for="item in options"
      :key="item.value"
      :label="item.key"
      :value="item.value"
    >
    </el-option>
  </el-select>
</template>

<script>
export default {
  name: 'singleSelect',
  props: {
    url: {
      type: String,
      required: true,
    },
    value: {
      required: true,
    },
  },
  computed: {
    kv() {
      var search = this.url.substring(this.url.lastIndexOf('?') + 1)
      var obj = {}
      var reg = /([^?&=]+)=([^?&=]*)/g
      search.replace(reg, function(rs, $1, $2) {
        var name = decodeURIComponent($1)
        var val = decodeURIComponent($2)
        val = String(val)
        obj[name] = val
        return rs
      })
      return obj
    },
  },
  data() {
    return {
      options: [],
      data: '',
    }
  },
  watch: {
    data(val, oldVal) {
      this.$emit('input', val)
    },
  },
  methods: {
    getData() {
      let vm = this

      if (!vm.url) {
        return
      }

      if (vm.options.length > 0) return
      // 如果是[开头，说明数据是数组
      if (vm.url.substring(0, 1) === '[') {
        vm.getLocalData()
      } else {
        vm.getRemoteData()
      }
    },
    getRemoteData() {
      let vm = this
      vm.$store.getters
        .request({
          url: vm.url,
          method: 'post',
        })
        .then((resp) => {
          let array = resp.data.data
          for (let i = 0; i < array.length; i++) {
            const e = array[i]
            vm.options[i] = { key: e[vm.kv.key], value: e[vm.kv.value] + '' }
          }
          vm.$forceUpdate()
        })
    },
    getLocalData() {
      let vm = this
      let data = JSON.parse(vm.url)
      vm.options = data
      vm.$forceUpdate()
    },
  },
}
</script>

<style></style>
