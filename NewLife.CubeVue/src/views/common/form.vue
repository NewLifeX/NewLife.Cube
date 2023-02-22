<template>
  <div>
    <div>{{ typeMap[type] }}</div>
    <el-form
      ref="form"
      v-model="form"
      label-position="right"
      label-width="120px"
      :inline="true"
      class="form-container"
    >
      <template v-for="(column, k) in fields">
        <el-form-item
          v-if="column.name.toLowerCase() != 'id' && showInForm(column)"
          :key="k"
          :prop="column.isDataObjectField ? column.name : column.columnName"
          :label="column.displayName || column.name"
        >
          <template
            v-if="
              column.description && column.displayName != column.description
            "
            #label
          >
            <div style="display:inline-flex">
              <span>{{ column.displayName || column.name }}</span>
              <el-tooltip :content="column.description">
                <i class="el-icon-warning-outline"></i>
              </el-tooltip>
            </div>
          </template>
          <el-switch
            v-if="column.dataType == 'Boolean'"
            v-model="form[column.name]"
            active-color="#13ce66"
            inactive-color="#ff4949"
          />

          <el-date-picker
            v-else-if="column.dataType == 'DateTime'"
            v-model="form[column.name]"
            type="datetime"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DD HH:mm:ss"
            placeholder="选择日期时间"
          />

          <el-input
            v-else-if="column.dataType == 'String' && column.length > 50"
            v-model="form[column.name]"
            autosize
            type="textarea"
          />

          <el-input
            v-else
            v-model="
              form[column.isDataObjectField ? column.name : column.columnName]
            "
            type="text"
          />
        </el-form-item>
      </template>

      <el-form-item v-if="!isDetail">
        <div
          style="position: fixed; margin:20px; float:right; bottom: 0px; right: 0px; z-index: 1;"
        >
          <el-button @click="returnIndex">取消</el-button>
          <el-button type="primary" @click="confirm">保存</el-button>
        </div>
      </el-form-item>
    </el-form>
  </div>
</template>

<script>
export default {
  data() {
    return {
      form: {},
      fields: [],
      typeMap: { Add: '新增', Detail: '查看', Edit: '编辑' }
    }
  },
  computed: {
    id() {
      return this.$route.params.id
    },
    currentPath() {
      let vm = this
      let rplStr = `/${vm.type}`
      if (!vm.isAdd) {
        rplStr += `/${vm.id}`
      }
      return this.$route.path.replace(rplStr, '')
    },
    type() {
      return this.$route.params.type
    },
    getFieldType() {
      let vm = this
      return vm.isAdd
        ? 'getAddFormFields'
        : vm.isDetail
        ? 'getDetailFields'
        : 'getEditFormFields'
    },
    setFieldType() {
      let vm = this
      return vm.isAdd
        ? 'setAddFormFields'
        : vm.isDetail
        ? 'setDetailFields'
        : 'setEditFormFields'
    },
    fieldType() {
      let vm = this
      return vm.isAdd
        ? 'addFormFields'
        : vm.isDetail
        ? 'detailFields'
        : 'editFormFields'
    },
    isAdd() {
      return this.type === 'Add'
    },
    isDetail() {
      return this.type === 'Detail'
    },
    isEdit() {
      return this.type === 'Edit'
    }
  },
  // watch: {
  // 原本是通过路由变化初始化数据，但是切换页面时仍会触发
  //   $route: {
  //     handler: function() {
  //       this.init()
  //     },
  //     immediate: true
  //   }
  // },
  created() {
    this.init()
  },
  methods: {
    init() {
      this.getColumns()
      if (!this.isAdd) {
        this.query()
      }
    },
    getColumns() {
      // TODO 可改造成vue的属性，自动根据路由获取对应的列信息
      let vm = this
      let path = vm.currentPath

      vm.$store.getters.apis.getColumns(path).then((res) => {
        vm.fields = res.data.data
      })
    },
    query() {
      let vm = this
      if (vm.isDetail) {
        vm.$store.getters.apis
          .getDetailData(vm.currentPath, vm.id)
          .then((res) => {
            vm.form = res.data.data
          })
      } else {
        vm.$store.getters.apis.getData(vm.currentPath, vm.id).then((res) => {
          vm.form = res.data.data
        })
      }
    },
    confirm() {
      let vm = this
      if (vm.isAdd) {
        vm.$store.getters.apis.add(vm.currentPath, vm.form).then(() => {
          vm.$message({
            message: '新增成功',
            type: 'success',
            duration: 5 * 1000
          })
        })
      } else {
        vm.$store.getters.apis.edit(vm.currentPath, vm.form).then(() => {
          vm.$message({
            message: '保存成功',
            type: 'success',
            duration: 5 * 1000
          })
        })
      }
    },
    returnIndex() {
      this.$router.push(this.currentPath)
    },
    showInForm(col) {
      let vm = this
      if (vm.isAdd) {
        return col.showInAddForm
      } else if (vm.isDetail) {
        return col.showInDetailForm
      } else {
        return col.showInEditForm
      }
    }
  }
}
</script>

<style scoped>
.form-container {
  margin-left: 50px;
  margin-bottom: 75px;
  max-height: -moz-calc(100vh - 160px);
  max-height: -webkit-calc(100vh - 160px);
  max-height: calc(100vh - 160px);
  overflow-y: auto;
  box-shadow: 1px 1px 4px rgb(0 21 41 / 8%);
}
.el-switch,
.el-input,
.el-textarea {
  width: 220px;
}
</style>
