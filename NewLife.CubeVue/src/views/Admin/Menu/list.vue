<template>
  <div class="list-container">
    <el-row type="flex" class="search" justify="end">
      <el-col
        :span="4"
        class="left-search"
        v-if="hasPermission(permissionFlags.insert)"
      >
        <el-button type="primary" @click="add">
          新增
        </el-button>
      </el-col>
      <el-col :span="20" class="right-search"></el-col>
    </el-row>
    <div class="table-container">
      <el-table
        v-loading="listLoading"
        :data="tableData"
        stripe
        border
        @sort-change="sortChange"
        @row-dblclick="rowDblclick"
        :tree-props="{
          children: 'children',
          hasChildren: 'hasChildren'
        }"
        row-key="id"
        default-expand-all
      >
        <el-table-column prop="name" label="节点名" width="180" />
        <el-table-column prop="displayName" label="显示名" width="180" />
        <el-table-column prop="url" label="链接" width="250" />
        <el-table-column prop="sort" label="排序" width="50" />
        <el-table-column align="center" prop="visible" label="可见" width="80">
          <template v-slot="scope">
            <el-switch
              :value="scope.row.visible"
              active-color="#13ce66"
              inactive-color="#ff4949"
            />
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          prop="necessary"
          label="必要"
          width="80"
        >
          <template v-slot="scope">
            <el-switch
              :value="scope.row.necessary"
              active-color="#13ce66"
              inactive-color="#ff4949"
            />
          </template>
        </el-table-column>
        <el-table-column prop="permission" label="权限子项" width="250" />

        <el-table-column
          label="操作"
          align="center"
          width="140"
          class-name="small-padding fixed-width"
        >
          <template v-slot="scope">
            <el-button
              v-if="
                !hasPermission(permissionFlags.update) &&
                  hasPermission(permissionFlags.detail)
              "
              type="primary"
              size="mini"
              @click="detail(scope.row)"
            >
              查看
            </el-button>
            <el-button
              v-if="hasPermission(permissionFlags.update)"
              type="primary"
              size="mini"
              @click="editData(scope.row)"
            >
              编辑
            </el-button>
            <el-button
              v-if="hasPermission(permissionFlags.delete)"
              size="mini"
              type="danger"
              @click="deleteData(scope.row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>
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
export default {
  name: 'MenuList',
  data() {
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
      listLoading: false,
      permissionFlags: {
        none: 0,
        detail: 1,
        insert: 2,
        update: 4,
        delete: 8
      }
    }
  },
  computed: {
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
      this.setQueryParams()
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
          let tableData = vm.getTreeData(res.data.data)
          // console.log(tableData)
          vm.tableData = tableData
          vm.page = res.data.pager
          vm.setTableHeight(vm.tableData.length)
        })
    },
    getTreeData(dataList, pId = 0) {
      // 将列表数据构造成树状结构数据

      let vm = this

      if (!dataList || dataList.length < 1) {
        return []
      }

      let len = dataList.length
      // 父级列表
      let pList = []
      for (let idx = 0; idx < len; idx++) {
        const e = dataList[idx]

        if (e.parentID == pId) {
          let m = {
            id: e.id,
            name: e.name,
            displayName: e.displayName,
            parentID: e.parentID
          }
          pList.push(e)
          let children = vm.getTreeData(dataList, e.id)
          if (children.length > 0) {
            e.children = children
          }
        }
      }

      return pList
    },
    currentChange(val) {
      this.page.pageIndex = val
      this.getTableData()
    },
    handleSizeChange(val) {
      this.page.pageSize = val
      this.getTableData()
    },
    sortChange({ column, prop, order }) {
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
    },
    rowDblclick(row) {
      this.editData(row)
    },
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
    setTableHeight(count) {
      // 根据数据条数设置表格高度，最高设置708px，一页最多显示20条
      let vm = this
      // console.log(count)
      if (count && count > 0) {
        if (count > 20) count = 20
        else if (count < 8) count = 9
        setTimeout(() => {
          vm.tableHeight = count * 35.9 + 'px'
        }, 500)
      }
    }
  }
}
</script>
<style scoped>
.list-container {
  height: -moz-calc(100vh - 51px);
  height: -webkit-calc(100vh - 51px);
  height: calc(100vh - 51px);
  overflow-x: hidden;
  overflow-y: auto;
}

.search {
  /* height: 60px; */
  /* overflow: hidden; */
  /* position: relative; */
  display: -moz-flex;
  display: -webkit-flex;
  display: flex;
}

.search .left-search {
  line-height: 58px;
  /* height: 60px; */
  /* float: left; */
  padding: 0 10px;
}
.search .right-search {
  line-height: 58px;
  /* height: 65px; */
  /* float: right; */
  /* max-height: 110px; */
  padding: 0 10px;
  /* overflow-y: auto; */
}
.table-container {
  /* max-height: calc(100vh - 177px); */
  /* overflow-y: auto; */
  height: auto;
  margin-bottom: 2px;
  box-shadow: 1px 1px 4px rgb(0 21 41 / 8%);
}

.search .el-input,
.el-button,
.el-date-editor {
  margin-right: 2px;
}

.search .el-date-editor {
  width: 250px;
}

/** 操作按钮 */
.el-table .el-button + .el-button {
  margin-left: 3px;
}
</style>
