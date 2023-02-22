<template>
  <div class="list-container">
    <!-- 搜索组件 -->
    <TableSearch
      v-model="queryParams"
      :columns="columns"
      @operator="operator"
    ></TableSearch>

    <!-- 操作栏 -->
    <TableOperator
      :columns="columns"
      :operatorList="operatorList"
      @operator="operator"
    ></TableOperator>

    <NormalTable
      :columns="columns"
      :permissionFlags="permissionFlags"
      :tableData="tableData"
      @operator="operator"
    ></NormalTable>

    <!-- 分页 -->
    <div>
      <el-pagination
        :current-page="page.pageIndex"
        :page-size="page.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="page.totalCount"
        @current-change="currentChange"
        @size-change="handleSizeChange"
        layout="total, sizes, prev, pager, next, jumper"
      ></el-pagination>
    </div>
  </div>
</template>
<script>
import { computed } from 'vue'
import NormalTable from '../components/NormalTable.vue'

export default {
  components: { NormalTable },
  name: 'List',
  data() {
    let permissionFlags = {
      none: 0,
      detail: 1,
      insert: 2,
      update: 4,
      delete: 8
    }

    return {
      tableData: [],
      tableHeight: '300px',
      queryParams: {
        Q: null,
        dateRange: null
      },
      page: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 0
      },
      headerData: [],
      listLoading: false,
      permissionFlags,
      operatorList: [
        {
          name: '新增',
          action: 'add',
          type: 'primary'
        },
        {
          name: '导出',
          action: 'export',
          type: 'primary'
        }
      ],
      actionList: [
        {
          name: 'actionList',
          displayName: '操作',
          width: '125px',
          showInList: true,
          actionList: [
            //             {
            //   action: 'detail',
            //   permission: permissionFlags.detail,
            //   text: '查看',
            //   type:'primary'
            // },
            {
              action: 'editData',
              permission: permissionFlags.update,
              text: '编辑',
              type: 'primary'
            },
            {
              action: 'deleteData',
              permission: permissionFlags.delete,
              text: '删除',
              type: 'danger'
            }
          ]
        }
      ]
    }
  },
  // provide() {
  //   let vm = this
  //   return {
  //     permissionFlags: vm.permissionFlags
  //   }
  // },
  computed: {
    columns() {
      let vm = this
      return vm.headerData.concat(vm.actionList)
    },
    currentPath() {
      return this.$route.path
    },
    queryData() {
      let vm = this
      let dateRange = vm.queryParams.dateRange
      if (dateRange) {
        vm.queryParams.dtStart = dateRange[0]
        vm.queryParams.dtEnd = dateRange[1]
      } else {
        vm.queryParams.dtStart = null
        vm.queryParams.dtEnd = null
      }

      let temp = {}
      // 查询参数也添加上
      Object.assign(temp, vm.page, vm.queryParams)
      temp.dateRange = undefined
      return temp
    }
  },
  // watch: {
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
  activated() {
    this.init()
  },
  methods: {
    init() {
      this.setQueryParams()
      this.getColumns()
      this.query()
    },
    setQueryParams() {
      // 设置查询参数
      let vm = this
      for (const key in vm.$route.query) {
        if (Object.hasOwnProperty.call(vm.$route.query, key)) {
          const element = vm.$route.query[key]
          vm.queryParams[key] = element
        }
      }
    },
    getUrl(column, entity) {
      // 针对指定实体对象计算url，替换其中变量
      const reg = /{(\w+)}/g
      return column.cellUrl.replace(reg, (a, b) => entity[b])
    },
    getColumns() {
      // TODO 可改造成vue的属性，自动根据路由获取对应的列信息
      let vm = this
      let path = vm.currentPath
      vm.$store.getters.apis.getColumns(path).then((res) => {
        vm.headerData = res.data.data
      })
    },
    getListFields() {
      let vm = this
      let path = vm.currentPath
      let key = path + '-list'
      let fields = vm.$store.state.entity.listFields[key]
      if (fields) {
        vm.headerData = fields
        return
      }

      // 没有获取过字信息，请求回来后保存一份
      vm.$store.getters.apis.getListFields(path).then((res) => {
        fields = res.data.data
        vm.headerData = fields

        vm.$store.dispatch('setListFields', { key, fields })
      })
    },
    add() {
      let vm = this
      vm.$router.push(vm.currentPath + '/Add')
    },
    detail(row) {
      let vm = this
      vm.$router.push(vm.currentPath + '/Detail/' + row.id)
    },
    editData(row) {
      let vm = this
      vm.$router.push(vm.currentPath + '/Edit/' + row.id)
    },
    deleteData(row) {
      let vm = this
      vm.$store.getters.apis.deleteById(vm.currentPath, row.id).then(() => {
        vm.getTableData()
      })
    },
    query() {
      this.page.pageIndex = 1
      this.getTableData()
    },
    getTableData() {
      let vm = this
      vm.listLoading = true

      vm.$store.getters.apis
        .getDataList(vm.currentPath, vm.queryData)
        .then((res) => {
          vm.listLoading = false
          vm.tableData = res.data.data
          vm.page = res.data.pager
          vm.page.Q = undefined
        })
    },
    currentChange(val) {
      this.page.pageIndex = val
      this.getTableData()
    },
    handleSizeChange(val) {
      this.page.pageSize = val
      this.getTableData()
    },
    rowDblclick(row) {
      this.editData(row)
    },
    // 判断操作id会否有权限
    hasPermission(actionId) {
      let vm = this
      let menuId = vm.$route.meta.menuId
      let permissions = vm.$route.meta.permissions
      let has = vm.$store.state.user.hasPermission(vm.$store, {
        menuId,
        actionId,
        permissions
      })
      return has
    },
    // 重置搜索条件
    resetSearch() {
      let vm = this
      vm.queryParams = {}
      vm.query()
    },
    // 子组件调用此方法，再通过参数action调用本组件方法
    operator(option, data, callback) {
      let vm = this
      let action = option.action
      let func = vm[action]
      if (!func || typeof func !== 'function') {
        let msg = `未实现的方法：${action}`
        console.error(msg)
        vm.$message.error(msg)
      } else {
        let returnData = func.call(vm, data)
        if (typeof callback === 'function') {
          callback(returnData)
        }
      }
    },
    sortChange({ col, prop, order }) {
      if (order === 'ascending') {
        this.page.desc = false
        this.page.sort = prop
      } else if (order === 'descending') {
        this.page.desc = true
        this.page.sort = prop
      } else {
        this.page.desc = undefined
        this.page.sort = undefined
      }
      this.getTableData()
    }
  }
}
</script>
<style lang="scss" scoped>
.list-container {
  height: -moz-calc(100vh - 51px);
  height: -webkit-calc(100vh - 51px);
  height: calc(100vh - 51px);
  overflow-x: hidden;
  overflow-y: auto;
}
</style>
