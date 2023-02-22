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
      <el-form-item label="名称" prop="name">
        <el-input v-model="form.name" />
      </el-form-item>
      <el-form-item label="启用" prop="enable">
        <el-switch
          v-model="form.enable"
          active-color="#13ce66"
          inactive-color="#ff4949"
        />
      </el-form-item>
      <el-form-item label="系统" prop="isSystem">
        <el-switch
          v-model="form.isSystem"
          active-color="#13ce66"
          inactive-color="#ff4949"
        />
      </el-form-item>
      <el-form-item label="备注" prop="remark">
        <el-input :rows="4" type="textarea" v-model="form.remark" />
      </el-form-item>

      <el-form-item>
        <div
          style="position: fixed; margin:20px; float:right; bottom: 0px; right: 0px; z-index: 1;"
        >
          <el-button @click="returnIndex">取消</el-button>
          <el-button type="primary" @click="confirm">保存</el-button>
        </div>
      </el-form-item>
      <el-table
        v-if="!isAdd"
        :data="tableData"
        :tree-props="{
          children: 'children',
          hasChildren: 'hasChildren'
        }"
        row-key="id"
        border
        default-expand-all
      >
        >
        <el-table-column prop="name" label="名称" width="180" />
        <el-table-column prop="displayName" label="显示名" width="100" />
        <!-- <el-table-column label="授权" width="60">
          <template v-slot="scope">
            <el-checkbox
              v-model="form['p' + scope.row.id]"
              @change="handleCheckAllChange"
            />
          </template>
        </el-table-column> -->
        <el-table-column label="操作">
          <template v-slot="scope">
            <template v-if="scope.row.permissions.length > 0">
              <el-checkbox
                :indeterminate="imObj[scope.row.id]"
                v-model="form['p' + scope.row.id]"
                @change="checkAllChange(scope.row)"
              >
                全选
              </el-checkbox>
              <el-checkbox
                v-for="item in scope.row.permissions"
                :key="scope.row.id + '' + item.k"
                :label="item.v"
                v-model="form['pf' + scope.row.id + '_' + item.k]"
                @change="checkChange(scope.row)"
              ></el-checkbox>
            </template>
            <template v-else>
              <el-checkbox
                :indeterminate="imObj[scope.row.id]"
                v-model="form['p' + scope.row.id]"
                @change="parentCheckAllChange(scope.row)"
              >
                全选
              </el-checkbox>
              <!-- <el-checkbox>读写</el-checkbox> -->
              <el-checkbox
                v-model="form['pc_readonly_' + scope.row.id]"
                @change="roCheck(scope.row)"
              >
                只读
              </el-checkbox>
            </template>
          </template>
        </el-table-column>
      </el-table>
    </el-form>
  </div>
</template>

<script>
export default {
  data() {
    return {
      form: {},
      fields: [],
      imObj: {},
      typeMap: { Add: '新增', Detail: '查看', Edit: '编辑' }
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
    },
    rolePermissions() {
      // 角色菜单权限
      let vm = this
      // permission格式: 1#255,2#255。#前为菜单id，#后为权限值
      let permission = vm.form.permission
      let pObj = {}
      if (!permission) return pObj
      let mlist = permission.split(',')
      for (const key in mlist) {
        const m = mlist[key]
        const p = m.split('#')
        pObj[p[0]] = p[1]
      }
      return pObj
    },
    tableData() {
      // console.log('init')
      // 角色菜单数据
      let vm = this
      let menuRouters = vm.$store.getters.menuRouters
      let menus = []
      menuRouters.map((i) => {
        let menu = {
          id: i.id,
          name: i.name,
          displayName: i.displayName,
          permissions: i.permissions,
          parentID: i.parentID
        }

        // // 父级全选勾选框是否勾选
        // let pCheck = false

        if (i.hasChildren) {
          menu.children = []
          i.children.map((j) => {
            // 表单路由不处理
            if (j.isFormRoute) return
            let menuChild = {
              id: j.id,
              name: j.name,
              displayName: j.displayName,
              permissions: j.permissions,
              parentID: j.parentID
            }

            // // 全选勾选框是否勾选，取决于子项是否有勾选
            // let pc = false
            // // 子项勾选个数
            // let checkChildCount = 0

            // // 设置操作勾选框
            // menuChild.permissions.map((p) => {
            //   let c = (p.k & vm.rolePermissions[j.id]) !== 0
            //   if (c) checkChildCount = checkChildCount + 1
            //   vm.form['pf' + j.id + '_' + p.k] = c
            //   pc = pc || c
            // })

            // // 设置全选勾选框
            // vm.form['p' + j.id] = pc
            // pCheck = pCheck || pc

            menu.children.push(menuChild)

            // 更新勾选框状态
            // vm.checkChange(menuChild)
          })
        } else {
          // pCheck = true
        }

        // 设置授权勾选框
        // vm.form['p' + i.id] = pCheck
        menus.push(menu)
      })

      return menus
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
      this.getFields()
      if (!this.isAdd) {
        this.query()
      }
    },
    getFields() {
      let vm = this
      let path = vm.currentPath
      let key = path + '-' + vm.fieldType
      let fields = vm.$store.state.entity[vm.fieldType][key]
      if (fields) {
        vm.fields = fields
        return
      }

      // 没有获取过字信息，请求回来后保存一份
      vm.$store.getters.apis[vm.getFieldType](path).then((res) => {
        fields = res.data.data
        vm.fields = fields

        vm.$store.dispatch(vm.setFieldType, { key, fields })
      })
    },
    query() {
      let vm = this
      vm.$store.getters.apis.getData(vm.currentPath, vm.id).then((res) => {
        vm.form = res.data.data
        vm.allCheckUpdate()
      })
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
    checkChange({ id, permissions, parentID }) {
      // console.log('checkChange', id)
      // 子权限项勾选，
      let vm = this
      let pCheck = false
      let checkCount = 0
      permissions.forEach((e) => {
        let c = vm.form['pf' + id + '_' + e.k]
        if (c) checkCount = checkCount + 1
        pCheck = pCheck || c
      })

      vm.form['p' + id] = pCheck
      vm.imObj[id] = checkCount > 0 && checkCount < permissions.length

      // 更新父级全选
      vm.parentCheckUpdate(parentID)
    },
    checkAllChange({ id, permissions, parentID }) {
      // console.log('checkAllChange', id, permissions)
      // 子权限项全勾选，更新子级级勾选
      let vm = this
      let pCheck = vm.form['p' + id]
      permissions.forEach((e) => {
        vm.form['pf' + id + '_' + e.k] = pCheck
      })

      vm.imObj[id] = false

      // 更新父级全选
      vm.parentCheckUpdate(parentID)
    },
    parentCheckAllChange({ id, children }) {
      // console.log('parentCheckAllChange', id, children)

      // 父级全选，勾选所有子权限项
      let vm = this
      let pCheck = vm.form['p' + id]
      children.forEach((e) => {
        vm.form['p' + e.id] = pCheck
        vm.checkAllChange({
          id: e.id,
          permissions: e.permissions,
          parentID: e.parentID
        })
      })

      vm.imObj[id] = false
    },
    parentCheckUpdate(parentID) {
      // console.log('parentCheckUpdate', parentID)

      // 父级勾选状态更新
      let vm = this
      let parentCheck = false
      let parentIm = false
      let parent = vm.tableData.find((f) => f.id == parentID)
      parent.children.forEach((e) => {
        let c = vm.form['p' + e.id]
        let im = vm.imObj[e.id]
        parentCheck = parentCheck || c || false
        parentIm = parentIm || !c || im || false
        // console.log(parentCheck, !parentIm, !c)
      })

      if (!parentCheck) {
        parentIm = false
      }

      vm.form['p' + parentID] = parentCheck
      vm.imObj[parentID] = parentIm
    },
    roCheck({ id, children }) {
      // 只读勾选，勾选所有子权限只读项
      let vm = this
      let pCheck = vm.form['pc_readonly_' + id]
      children.forEach((e) => {
        vm.form['pf' + e.id + '_' + 1] = pCheck
        vm.checkChange(e)
      })
    },
    allCheckUpdate() {
      // console.log('allCheckUpdate', this.tableData)
      // 更新所有勾选框状态
      let vm = this
      vm.tableData.map((menu) => {
        // console.log('0', menu)

        // 处理当前菜单权限勾选
        if (menu.permissions) {
          menu.permissions.map((p) => {
            // console.log('1', p)

            let c = (p.k & vm.rolePermissions[menu.id]) !== 0
            // console.log('allCheckUpdate1', menu.id, p.k, c)
            vm.form['pf' + menu.id + '_' + p.k] = c
          })
        }

        // 子菜单
        if (menu.children) {
          menu.children.map((j) => {
            // console.log('2', j)
            // 设置操作勾选框
            j.permissions.map((p) => {
              // console.log('3', p)

              let c = (p.k & vm.rolePermissions[j.id]) !== 0
              // console.log('allCheckUpdate2', j.id, p.k, c)

              vm.form['pf' + j.id + '_' + p.k] = c
            })
            vm.checkChange(j)
          })
        }
      })
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
