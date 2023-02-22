<template>
  <el-row type="flex" justify="center" align="center" class="operator">
    <el-col :span="12" class="left-search">
      <el-button
        size="small"
        v-for="(btn, idx) in operatorList"
        :key="idx"
        @click="operator(btn)"
        :type="btn.type"
        :plain="btn.plain"
      >
        {{ btn.name }}
      </el-button>
    </el-col>
    <el-col
      :span="12"
      style="display: flex; justify-content: flex-end; align-items: center;"
    >
      <el-tooltip effect="dark" content="刷新" placement="top-end">
        <el-icon class="action" @click="operator({ action: 'getTableData' })">
          <refresh />
        </el-icon>
      </el-tooltip>

      <el-popover placement="bottom" :width="220" trigger="click">
        <div>设置列字段</div>
        <div class="setting-btn">
          <div style="padding-top: 5px">
            <el-checkbox
              @change="chooseAll"
              v-model="allChoose"
              :indeterminate="isIndeterminate"
            >
              全选
            </el-checkbox>
          </div>
        </div>
        <div style="height:68vh;overflow: auto;">
          <div v-for="(field, index) in columns" :key="index">
            <template v-if="field.showInList">
              <el-checkbox
                @change="chooseItem(field)"
                :model-value="!field.hidden"
              >
                {{ field.displayName }}
              </el-checkbox>
            </template>
          </div>
        </div>
        <template #reference>
          <el-icon class="action">
            <setting />
          </el-icon>
        </template>
      </el-popover>

      <!-- <el-tooltip effect="dark" content="全屏" placement="top-end">
        <el-icon class="action">
          <full-screen />
        </el-icon>
      </el-tooltip> -->
    </el-col>
  </el-row>
</template>

<script>
export default {
  props: {
    columns: {
      type: Array,
      default: () => []
    },
    operatorList: {
      type: Array,
      default: () => []
    },
    permissionFlags: {
      type: Object,
      default: () => {}
    }
  },
  emits: ['operator'],
  //   setup(props, context) {
  //     // console.log(arguments)

  //     const operator = (option) => {
  //       context.emit('operator', option)
  //     }

  //     return {
  //       operator
  //     }
  //   },
  data() {
    return {
      allChoose: true,
      isIndeterminate: false
    }
  },
  methods: {
    checkChoose() {
      let trueLength = this.columns.filter((item) => !item.hidden).length
      if (trueLength == this.columns.length) {
        this.allChoose = true
        return
      }
      this.allChoose = false
      this.isIndeterminate = trueLength > 0 && trueLength < this.columns.length
    },
    chooseAll(val) {
      this.columns.forEach((item) => {
        item.hidden = !val
      })
    },
    chooseItem(val) {
      val.hidden = !val.hidden
      this.checkChoose()
    },
    operator(option, data) {
      this.$emit('operator', option, data)
    }
  }
}
</script>

<style scoped>
.operator {
  margin: 2px 0 10px 0;
}

.action {
  margin: 0 8px;
  cursor: pointer;
  font-size: 17px;
}
</style>
