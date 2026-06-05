<template>
  <div>
    <div class="page-header">
      <h1>用户统计</h1>
      <p>查看用户访问统计数据</p>
    </div>

    <el-card>
      <template #header>
        <div class="card-header">
          <span>用户统计列表</span>
        </div>
      </template>

      <!-- 图表容器 -->
      <div class="chart-container">
        <VChart
          :option="chartOptions"
          :loading="loading"
          style="height: 400px; width: 100%"
        />
      </div>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <!-- 数据表格 -->
      <div class="table-container">      <el-table
        :data="tableData"
        v-loading="loading"
        style="width: 100%"
      >
          <el-table-column type="selection" width="55" />
          <el-table-column prop="id" label="编号" width="80" />
          <el-table-column prop="date" label="统计日期" width="120" />
          <el-table-column prop="total" label="总数" width="80" />
          <el-table-column prop="logins" label="登录数" width="80" />
          <el-table-column prop="oAuths" label="OAuth登录" width="100" />
          <el-table-column prop="maxOnline" label="最大在线" width="100" />
          <el-table-column prop="actives" label="活跃" width="80" />
          <el-table-column prop="activesT7" label="7天活跃" width="80" />
          <el-table-column prop="activesT30" label="30天活跃" width="80" />
          <el-table-column prop="news" label="新用户" width="80" />
          <el-table-column prop="newsT7" label="7天注册" width="80" />
          <el-table-column prop="newsT30" label="30天注册" width="80" />
          <el-table-column prop="onlineTime" label="在线时间" width="120">
            <template #default="{ row }">
              {{ formatOnlineTime(row.onlineTime) }}
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="120" show-overflow-tooltip>
            <template #default="scope">
              {{ scope.row.remark || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="handleDetail(row)">
                详情
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 分页 -->
      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 详情对话框 -->
    <el-dialog v-model="showDetailDialog" title="用户统计详情" width="600px">
      <div v-loading="loading">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="编号">{{ detailData.id }}</el-descriptions-item>
          <el-descriptions-item label="统计日期">{{ detailData.date }}</el-descriptions-item>
          <el-descriptions-item label="总数">{{ detailData.total }}</el-descriptions-item>
          <el-descriptions-item label="登录数">{{ detailData.logins }}</el-descriptions-item>
          <el-descriptions-item label="OAuth登录">{{ detailData.oAuths }}</el-descriptions-item>
          <el-descriptions-item label="最大在线">{{ detailData.maxOnline }}</el-descriptions-item>
          <el-descriptions-item label="活跃">{{ detailData.actives }}</el-descriptions-item>
          <el-descriptions-item label="7天活跃">{{ detailData.activesT7 }}</el-descriptions-item>
          <el-descriptions-item label="30天活跃">{{ detailData.activesT30 }}</el-descriptions-item>
          <el-descriptions-item label="新用户">{{ detailData.news }}</el-descriptions-item>
          <el-descriptions-item label="7天注册">{{ detailData.newsT7 }}</el-descriptions-item>
          <el-descriptions-item label="30天注册">{{ detailData.newsT30 }}</el-descriptions-item>
          <el-descriptions-item label="在线时间">{{ formatOnlineTime(detailData.onlineTime) }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ detailData.createTime ? formatDate(detailData.createTime) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="更新时间">{{ detailData.updateTime ? formatDate(detailData.updateTime) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注">{{ detailData.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <template #footer>
        <el-button @click="showDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted, computed } from 'vue'
import { request } from 'cube-front/core/utils/request'
import { apiDataToList } from 'cube-front/core/utils/api-helpers'
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue'
import CubeListPager from 'cube-front/core/components/CubeListPager.vue'
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  ToolboxComponent,
  DataZoomComponent
} from 'echarts/components'
import { LineChart } from 'echarts/charts'
import { UniversalTransition } from 'echarts/features'
import { CanvasRenderer } from 'echarts/renderers'

use([
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  ToolboxComponent,
  DataZoomComponent,
  LineChart,
  UniversalTransition,
  CanvasRenderer
])

// 用户统计接口，继承 BaseEntity
interface UserStat extends BaseEntity {
  date: string
  total: number
  logins: number
  oAuths: number
  maxOnline: number
  actives: number
  activesT7: number
  activesT30: number
  news: number
  newsT7: number
  newsT30: number
  onlineTime: number
  remark?: string
}

// 表格数据
const tableData = ref<UserStat[]>([])
const loading = ref(false)

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  ...pageInfoDefault
});

// 详情数据
const detailData = ref<Partial<UserStat>>({})
const showDetailDialog = ref(false)

// 图表数据转换函数
const transformDataForChart = (data: UserStat[]) => {
  if (!data || data.length === 0) return { dates: [], series: [] }

  // 按日期排序
  const sortedData = [...data].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())

  const dates = sortedData.map(item => item.date)

  const series = [
    {
      name: '总数',
      data: sortedData.map(item => item.total || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '登录数',
      data: sortedData.map(item => item.logins || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: 'OAuth登录',
      data: sortedData.map(item => item.oAuths || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '最大在线',
      data: sortedData.map(item => item.maxOnline || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '活跃',
      data: sortedData.map(item => item.actives || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '7天活跃',
      data: sortedData.map(item => item.activesT7 || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '30天活跃',
      data: sortedData.map(item => item.activesT30 || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '新用户',
      data: sortedData.map(item => item.news || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '7天注册',
      data: sortedData.map(item => item.newsT7 || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '30天注册',
      data: sortedData.map(item => item.newsT30 || 0),
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 }
    },
    {
      name: '在线时间',
      data: sortedData.map(item => item.onlineTime || 0), // 保持秒单位
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 4,
      lineStyle: { width: 2 },
      yAxisIndex: 1 // 使用右Y轴
    }
  ]

  return { dates, series }
}

// 图表配置
const chartOptions = computed(() => {
  const chartData = transformDataForChart(tableData.value)

  return {
    title: {
      text: '用户统计趋势图',
      left: 'center',
      textStyle: {
        fontSize: 16,
        fontWeight: 'normal'
      }
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross',
        label: {
          backgroundColor: '#6a7985'
        }
      },
      formatter: (params: unknown) => {
        if (!params || !Array.isArray(params) || params.length === 0) return ''

        const firstParam = params[0] as Record<string, unknown>
        let result = `<div style="font-weight: bold;">${firstParam.axisValue}</div>`
        params.forEach((item: Record<string, unknown>) => {
          if (item.seriesName === '在线时间') {
            result += `<div style="margin: 2px 0;">${item.marker}${item.seriesName}: ${item.value}秒</div>`
          } else {
            result += `<div style="margin: 2px 0;">${item.marker}${item.seriesName}: ${item.value}</div>`
          }
        })
        return result
      }
    },
    legend: {
      data: ['总数', '登录数', 'OAuth登录', '最大在线', '活跃', '7天活跃', '30天活跃', '新用户', '7天注册', '30天注册', '在线时间'],
      top: 35,
      type: 'scroll',
      orient: 'horizontal'
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: 80,
      containLabel: true
    },
    toolbox: {
      feature: {
        saveAsImage: {
          title: '保存为图片'
        },
        restore: {
          title: '重置'
        },
        dataZoom: {
          title: {
            zoom: '区域缩放',
            back: '区域缩放还原'
          }
        }
      }
    },
    xAxis: {
      type: 'category' as const,
      boundaryGap: false,
      data: chartData.dates,
      axisLabel: {
        rotate: 45
      }
    },
    yAxis: [
      {
        type: 'value' as const,
        name: '用户数',
        position: 'left',
        axisLabel: {
          formatter: '{value}'
        }
      },
      {
        type: 'value' as const,
        name: '在线时长(秒)',
        position: 'right',
        axisLabel: {
          formatter: '{value}'
        }
      }
    ],
    dataZoom: [
      {
        type: 'inside' as const,
        start: 0,
        end: 100
      },
      {
        start: 0,
        end: 100,
        handleIcon: 'M10.7,11.9v-1.3H9.3v1.3c-4.9,0.3-8.8,4.4-8.8,9.4c0,5,3.9,9.1,8.8,9.4v1.3h1.3v-1.3c4.9-0.3,8.8-4.4,8.8-9.4C19.5,16.3,15.6,12.2,10.7,11.9z M13.3,24.4H6.7V23.1h6.6V24.4z M13.3,19.6H6.7v-1.4h6.6V19.6z',
        handleSize: '80%',
        handleStyle: {
          color: '#fff',
          shadowBlur: 3,
          shadowColor: 'rgba(0, 0, 0, 0.6)',
          shadowOffsetX: 2,
          shadowOffsetY: 2
        }
      }
    ],
    series: chartData.series.map(item => ({
      ...item,
      type: 'line' as const,
      yAxisIndex: item.name === '在线时间' ? 1 : 0 // 在线时间使用右Y轴，其他使用左Y轴
    }))
  }
})

// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadUserStatData();
};

// 加载用户统计数据
const loadUserStatData = async () => {
  loading.value = true
  try {
    const data = await request.get('/Admin/UserStat', {
      params: queryParams
    })
    const { list, page } = apiDataToList<UserStat>(data)
    tableData.value = list
    queryParams.total = page?.totalCount || list.length
  } catch {
    tableData.value = []
    queryParams.total = 0
  } finally {
    loading.value = false
  }
}

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 获取用户统计详情
const getUserStatDetail = async (id: number) => {
  try {
    loading.value = true
    const response = await request.get(`/Admin/UserStat/Detail`, {
      params: { id }
    })
    if (response && typeof response === 'object') {
      if ('data' in response && response.data) {
        detailData.value = response.data as UserStat
      } else if ('id' in response) {
        detailData.value = response as unknown as UserStat
      }
    }
  } catch (error) {
    console.error('获取用户统计详情失败:', error)
    detailData.value = {}
  } finally {
    loading.value = false
  }
}

// 查看详情
const handleDetail = async (row: UserStat) => {
  await getUserStatDetail(row.id)
  showDetailDialog.value = true
}

// 格式化日期
const formatDate = (dateStr?: string) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

// 格式化在线时间（秒转换为时:分:秒）
const formatOnlineTime = (seconds?: number) => {
  if (!seconds) return '0:00:00'

  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
}

onMounted(() => {
  loadUserStatData();
});
</script>

<style scoped>
.page-header {
  margin-bottom: 20px;
}

.page-header h1 {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 500;
}

.page-header p {
  margin: 0;
  color: #666;
  font-size: 14px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.cube-search-form {
  margin-bottom: 20px;
  padding: 20px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.table-container {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
}

.chart-container {
  margin-bottom: 20px;
  padding: 20px;
  background-color: #fafafa;
  border-radius: 4px;
  border: 1px solid #e4e7ed;
}
</style>
