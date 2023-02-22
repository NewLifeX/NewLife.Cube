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
          v-if="column.name.toLowerCase() != 'id'"
          :key="k"
          :prop="column.name"
          :label="column.displayName || column.name"
        >
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

          <el-input v-else v-model="form[column.name]" type="text" />
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

      typeMap: { Add: '新增', Detail: '查看', Edit: '编辑' },
      fields: [
        {
          name: 'id',
          displayName: '编号',
          dataType: 'Int32',
          description: '编号'
        },
        {
          name: 'name',
          displayName: '名称',
          dataType: 'String',
          length: 50,
          description: '名称'
        },
        {
          name: 'displayName',
          displayName: '显示名',
          dataType: 'String',
          length: 50,
          description: '显示名'
        },
        {
          name: 'fullName',
          displayName: '全名',
          dataType: 'String',
          length: 200,
          description: '全名'
        },
        {
          name: 'parentID',
          displayName: '父编号',
          dataType: 'Int32',
          description: '父编号'
        },
        {
          name: 'url',
          displayName: '链接',
          dataType: 'String',
          length: 200,
          description: '链接'
        },
        {
          name: 'sort',
          displayName: '排序',
          dataType: 'Int32',
          description: '排序'
        },
        {
          name: 'icon',
          displayName: '图标',
          dataType: 'String',
          length: 50,
          description: '图标'
        },
        {
          name: 'visible',
          displayName: '可见',
          dataType: 'Boolean',
          description: '可见'
        },
        {
          name: 'necessary',
          displayName: '必要',
          dataType: 'Boolean',
          description:
            '必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色'
        },
        {
          name: 'permission',
          displayName: '权限子项',
          dataType: 'String',
          length: 200,
          description: '权限子项。逗号分隔，每个权限子项名值竖线分隔'
        },
        {
          name: 'ex1',
          displayName: '扩展1',
          dataType: 'Int32',
          description: '扩展1'
        },
        {
          name: 'ex2',
          displayName: '扩展2',
          dataType: 'Int32',
          description: '扩展2'
        },
        {
          name: 'ex3',
          displayName: '扩展3',
          dataType: 'Double',
          description: '扩展3'
        },
        {
          name: 'ex4',
          displayName: '扩展4',
          dataType: 'String',
          length: 50,
          description: '扩展4'
        },
        {
          name: 'ex5',
          displayName: '扩展5',
          dataType: 'String',
          length: 50,
          description: '扩展5'
        },
        {
          name: 'ex6',
          displayName: '扩展6',
          dataType: 'String',
          length: 50,
          description: '扩展6'
        },
        {
          name: 'remark',
          displayName: '备注',
          dataType: 'String',
          length: 500,
          description: '备注'
        }
      ]
    }
  },
  computed: {
    id() {
      return this.$route.params.id
    },
    currentPath() {
      let vm = this
      let rplStr = `/${vm.type}${vm.id === undefined ? '' : '/' + vm.id}`
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
    }
  },
  watch: {
    $route: {
      handler: function() {
        this.init()
      },
      immediate: true
    }
  },
  methods: {
    init() {
      // this.getFields()
      // this.getColumns()
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
    // getFields() {
    //   let vm = this
    //   let path = vm.currentPath
    //   let key = path + '-' + fieldType
    //   let fields = vm.$store.state.entity[vm.fieldType][key]
    //   if (fields) {
    //     vm.fields = fields
    //     return
    //   }

    //   // 没有获取过字信息，请求回来后保存一份
    //   vm.$store.getters.apis[vm.getFieldType](path).then((res) => {
    //     fields = res.data.data
    //     vm.fields = fields

    //     vm.$store.dispatch(vm.setFieldType, { key, fields })
    //   })
    // },
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
