<template>
  <div class="get-page-config-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>获取页面配置</h3>
          <el-button type="primary" @click="handleGetConfig">获取配置</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="类型">
          <el-input v-model="searchForm.kind" placeholder="请输入配置类型" clearable />
        </el-form-item>
        <el-form-item label="页面">
          <el-input v-model="searchForm.page" placeholder="请输入页面名称" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleGetConfig">获取配置</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 配置结果展示 -->
      <el-card v-if="configData" class="config-card">
        <template #header>
          <h4>配置结果</h4>
        </template>
        <el-descriptions :column="1" border>
          <el-descriptions-item label="类型">
            {{ searchForm.kind }}
          </el-descriptions-item>
          <el-descriptions-item label="页面">
            {{ searchForm.page }}
          </el-descriptions-item>
          <el-descriptions-item label="配置数据">
            <el-input
              v-model="formattedConfig"
              type="textarea"
              :rows="15"
              readonly
              placeholder="配置数据将显示在这里"
            />
          </el-descriptions-item>
        </el-descriptions>
        <div class="config-actions">
          <el-button type="primary" @click="copyConfig">复制配置</el-button>
          <el-button type="success" @click="downloadConfig">下载配置</el-button>
        </div>
      </el-card>

      <!-- 历史查询记录 -->
      <el-card v-if="queryHistory.length > 0" class="history-card">
        <template #header>
          <h4>查询历史</h4>
        </template>
        <el-table :data="queryHistory" border style="width: 100%">
          <el-table-column prop="kind" label="类型" width="150" />
          <el-table-column prop="page" label="页面" width="150" />
          <el-table-column prop="queryTime" label="查询时间" width="160" />
          <el-table-column prop="hasData" label="是否有数据" width="100">
            <template #default="scope">
              <el-tag :type="scope.row.hasData ? 'success' : 'warning'">
                {{ scope.row.hasData ? '有数据' : '无数据' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120">
            <template #default="scope">
              <el-button type="primary" size="small" @click="loadHistory(scope.row)">重新查询</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { ElMessage } from 'element-plus';

// 定义接口类型
interface PageConfigParams {
  kind?: string;
  page?: string;
}

interface QueryHistoryItem extends PageConfigParams {
  queryTime: string;
  hasData: boolean;
}

// 查询表单
const searchForm = reactive<PageConfigParams>({
  kind: '',
  page: '',
});

// 配置数据
const configData = ref<unknown>(null);
const queryHistory = ref<QueryHistoryItem[]>([]);

// 格式化后的配置数据
const formattedConfig = computed(() => {
  if (!configData.value) return '';
  try {
    return JSON.stringify(configData.value, null, 2);
  } catch {
    return String(configData.value);
  }
});

// 获取配置
const handleGetConfig = async () => {
  if (!searchForm.kind || !searchForm.page) {
    ElMessage.warning('请输入类型和页面名称');
    return;
  }

  try {
    const response = await request.get('/Cube/GetPageConfig', {
      params: {
        kind: searchForm.kind,
        page: searchForm.page,
      },
    });

    configData.value = response;

    // 添加到查询历史
    queryHistory.value.unshift({
      kind: searchForm.kind,
      page: searchForm.page,
      queryTime: new Date().toLocaleString(),
      hasData: response !== null && response !== undefined,
    });

    // 保持历史记录在10条以内
    if (queryHistory.value.length > 10) {
      queryHistory.value = queryHistory.value.slice(0, 10);
    }

    ElMessage.success('配置获取成功');
  } catch {
    ElMessage.error('配置获取失败');
    configData.value = null;
  }
};

// 重置搜索
const resetSearch = () => {
  searchForm.kind = '';
  searchForm.page = '';
  configData.value = null;
};

// 复制配置
const copyConfig = async () => {
  if (!formattedConfig.value) {
    ElMessage.warning('没有配置数据可复制');
    return;
  }

  try {
    await navigator.clipboard.writeText(formattedConfig.value);
    ElMessage.success('配置已复制到剪贴板');
  } catch {
    ElMessage.error('复制失败，请手动复制');
  }
};

// 下载配置
const downloadConfig = () => {
  if (!formattedConfig.value) {
    ElMessage.warning('没有配置数据可下载');
    return;
  }

  const blob = new Blob([formattedConfig.value], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${searchForm.kind}_${searchForm.page}_config.json`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);

  ElMessage.success('配置文件下载完成');
};

// 载入历史记录
const loadHistory = (item: QueryHistoryItem) => {
  Object.assign(searchForm, {
    kind: item.kind,
    page: item.page,
  });
  handleGetConfig();
};
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}

.config-card {
  margin-top: 20px;
}

.config-actions {
  margin-top: 15px;
  text-align: right;
}

.history-card {
  margin-top: 20px;
}
</style>
