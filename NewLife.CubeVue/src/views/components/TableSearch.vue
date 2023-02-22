<template>
  <el-row type="flex" justify="end" class="search">
    <el-col :span="24" class="right-search">
      <el-form
        ref="form"
        v-model="modelValue"
        label-position="right"
        :inline="true"
        class="search-form-container"
      >
        <template v-for="(col, k) in columns">
          <el-form-item
            style="height:36px;"
            v-if="col.showInSearch"
            :key="k"
            :prop="col.isDataObjectField ? col.name : col.columnName"
            :label="col.displayName || col.name"
          >
            <single-select
              v-if="col.itemType == 'singleSelect' && col.dataSource"
              v-model="
                modelValue[col.isDataObjectField ? col.name : col.columnName]
              "
              :url="col.dataSource"
            ></single-select>

            <multiple-select
              v-else-if="col.itemType == 'multipleSelect' && col.dataSource"
              v-model="
                modelValue[col.isDataObjectField ? col.name : col.columnName]
              "
              :url="col.dataSource"
            ></multiple-select>

            <el-switch
              v-else-if="col.dataType == 'Boolean'"
              v-model="
                modelValue[col.isDataObjectField ? col.name : col.columnName]
              "
              active-color="#13ce66"
              inactive-color="#ff4949"
            />

            <el-input
              v-else
              v-model="
                modelValue[col.isDataObjectField ? col.name : col.columnName]
              "
              type="text"
            />
          </el-form-item>
        </template>
        <el-date-picker
          v-model="modelValue.dateRange"
          type="daterange"
          unlink-panels
          value-format="YYYY-MM-DD"
          range-separator="~"
          start-placeholder="开始"
          end-placeholder="结束"
          :shortcuts="shortcuts"
        ></el-date-picker>
        <el-input
          style="width:auto"
          v-model="modelValue.Q"
          placeholder="关键字"
        ></el-input>
        <el-button type="primary" @click="operator({ action: 'getTableData' })">
          查询
        </el-button>
        <el-button type="default" @click="operator({ action: 'resetSearch' })">
          重置
        </el-button>
      </el-form>
    </el-col>
  </el-row>
</template>

<script>
import { ref } from 'vue'
import singleSelect from '../../components/singleSelect'
import multipleSelect from '../../components/multipleSelect'
export default {
  components: {
    singleSelect,
    multipleSelect
  },
  props: {
    columns: {
      type: Array,
      default: () => []
    },
    modelValue: {
      type: Object,
      default: () => {}
    }
  },
  emits: ['operator'],
  data() {
    return {
      shortcuts: [
        {
          text: '昨天',
          value() {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 1)
            end.setTime(end.getTime() - 3600 * 1000 * 24 * 1)
            return [start, end]
          }
        },
        {
          text: '今天',
          value() {
            const end = new Date()
            const start = new Date()
            return [start, end]
          }
        },
        {
          text: '最近一周',
          value() {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 7)
            return [start, end]
          }
        },
        {
          text: '最近一个月',
          value() {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 30)
            return [start, end]
          }
        }
      ]
    }
  },
  setup(props, context) {
    // console.log(arguments)

    const a = ref({
      Q: null,
      dateRange: null
    })

    return {
      a
    }
  },
  methods: {
    operator(option, data) {
      this.$emit('operator', option, data)
    }
  }
}
</script>

<style lang="scss" scoped>
.search {
  /* height: 60px; */
  /* overflow: hidden; */
  /* position: relative; */
  display: -moz-flex;
  display: -webkit-flex;
  display: flex;
  // border-bottom: 1px solid #eeeeee;
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
  display: flex;
  justify-content: flex-end;
}
.search .el-button + .el-button {
  margin-left: 0px;
}

/* 搜索框元素间距 */
::v-deep(.el-input) {
  margin-right: 10px;
}

::v-deep(.el-button) {
  margin-right: 10px;
}

::v-deep(.el-date-editor) {
  margin-right: 10px;
}

::v-deep(.el-date-editor) {
  width: 250px;
}

::v-deep(.el-form-item) {
  margin-bottom: 0;
}
</style>
