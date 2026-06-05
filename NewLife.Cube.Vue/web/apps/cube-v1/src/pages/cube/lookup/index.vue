<template>
  <div class="cube-lookup-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>Cube Lookup 查询</h3>
          <el-button type="primary" @click="handleLookup">查询</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="代码">
          <el-input v-model="searchForm.codes" placeholder="请输入代码，多个用逗号分隔" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleLookup">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="tableData" border style="width: 100%" v-loading="loading">
        <el-table-column prop="code" label="代码" width="120" />
        <el-table-column prop="name" label="名称" min-width="150" />
        <el-table-column prop="value" label="值" min-width="200" />
        <el-table-column prop="description" label="描述" min-width="200" />
        <el-table-column prop="category" label="分类" width="120" />
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.status ? 'success' : 'danger'">
              {{ scope.row.status ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { request } from 'cube-front/core/utils/request';

// 定义接口类型
interface LookupParams {
  codes: string;
}

interface LookupData {
  code: string;
  name: string;
  value: string;
  description: string;
  category: string;
  sort: number;
  status: boolean;
}

// 表格数据
const tableData = ref<LookupData[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);

// 查询表单
const searchForm = reactive<LookupParams>({
  codes: '',
});

// 加载数据
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/Lookup', {
      params: {
        codes: searchForm.codes || undefined,
      },
    });

    // 处理响应数据
    if (Array.isArray(response)) {
      tableData.value = response.map((item, index) => ({
        code: item.code || item.key || `CODE_${index + 1}`,
        name: item.name || item.displayName || '',
        value: item.value || item.val || '',
        description: item.description || item.desc || item.remark || '',
        category: item.category || item.type || '',
        sort: item.sort || item.order || 0,
        status: item.status !== false,
      }));
      total.value = response.length;
    } else if (response && typeof response === 'object') {
      // 如果返回的是对象，尝试转换为数组
      const lookupList: LookupData[] = [];
      Object.keys(response).forEach((key, index) => {
        const item = response[key as keyof typeof response];
        lookupList.push({
          code: key,
          name: typeof item === 'object' && item !== null ? (item as Record<string, unknown>).name as string || key : key,
          value: String(item),
          description: typeof item === 'object' && item !== null ? (item as Record<string, unknown>).description as string || '' : '',
          category: typeof item === 'object' && item !== null ? (item as Record<string, unknown>).category as string || '' : '',
          sort: index,
          status: true,
        });
      });
      tableData.value = lookupList;
      total.value = lookupList.length;
    } else {
      tableData.value = [];
      total.value = 0;
    }
  } catch {
    tableData.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
};

// 页码变更处理
const handleCurrentChange = (page: number) => {
  currentPage.value = page;
  loadData();
};

// 每页显示条数变更处理
const handleSizeChange = (size: number) => {
  pageSize.value = size;
  currentPage.value = 1;
  loadData();
};

// 查询
const handleLookup = () => {
  currentPage.value = 1;
  loadData();
};

// 重置搜索
const resetSearch = () => {
  searchForm.codes = '';
  currentPage.value = 1;
  loadData();
};

// 初始化加载数据
onMounted(() => {
  loadData();
});
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
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
