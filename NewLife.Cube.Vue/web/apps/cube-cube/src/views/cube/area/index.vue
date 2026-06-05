<template>
  <div class="area-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>地区管理</h3>
          <el-button type="primary" @click="handleAdd">新增地区</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
        row-key="id"
        default-expand-all
        :tree-props="{ children: 'children' }"
      >
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="fullName" label="全称" />
        <el-table-column prop="code" label="代码" />
        <el-table-column prop="pinyin" label="拼音" />
        <el-table-column prop="jianpin" label="简拼" />
        <el-table-column prop="longitude" label="经度" />
        <el-table-column prop="latitude" label="纬度" />
        <el-table-column label="级别" width="100">
          <template #default="scope">
            <el-tag :type="getLevelType(scope.row.level)">
              {{ getLevelText(scope.row.level) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">编辑</el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 地区表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? '新增地区' : '编辑地区'"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item label="地区名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入地区名称" />
        </el-form-item>
        <el-form-item label="全称" prop="fullName">
          <el-input v-model="form.fullName" placeholder="请输入地区全称" />
        </el-form-item>
        <el-form-item label="地区代码" prop="code">
          <el-input v-model="form.code" placeholder="请输入地区代码" />
        </el-form-item>
        <el-form-item label="上级地区" prop="parentId">
          <el-select v-model="form.parentId" placeholder="请选择上级地区" clearable filterable>
            <el-option label="无上级" :value="0" />
            <el-option
              v-for="area in areaOptions"
              :key="area.id"
              :label="area.fullName || area.name"
              :value="area.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="级别" prop="level">
          <el-select v-model="form.level" placeholder="请选择级别">
            <el-option label="省份" :value="1" />
            <el-option label="城市" :value="2" />
            <el-option label="区县" :value="3" />
            <el-option label="乡镇" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item label="拼音" prop="pinyin">
          <el-input v-model="form.pinyin" placeholder="请输入拼音" />
        </el-form-item>
        <el-form-item label="简拼" prop="jianpin">
          <el-input v-model="form.jianpin" placeholder="请输入简拼" />
        </el-form-item>
        <el-form-item label="经度" prop="longitude">
          <el-input-number v-model="form.longitude" placeholder="请输入经度" :precision="6" />
        </el-form-item>
        <el-form-item label="纬度" prop="latitude">
          <el-input-number v-model="form.latitude" placeholder="请输入纬度" :precision="6" />
        </el-form-item>
        <el-form-item label="启用状态" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from 'cube-front/core/utils/request';
import { apiDataToList } from 'cube-front/core/utils/api-helpers';
import CubeListToolbarSearch from 'cube-front/core/components/CubeListToolbarSearch.vue';
import CubeListPager from 'cube-front/core/components/CubeListPager.vue';
import { pageInfoDefault } from 'cube-front/core/types/common';
import type { BaseEntity } from 'cube-front/core/types/common';

// 地区类型接口，继承 BaseEntity
interface Area extends BaseEntity {
  name: string;
  fullName: string;
  code: string;
  parentId: number;
  level: number;
  pinyin: string;
  jianpin: string;
  longitude: number;
  latitude: number;
  enable: boolean;
  children?: Area[];
}

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<Area[]>([]);
const areaOptions = ref<Area[]>([]);

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  level: undefined as number | undefined,
  ...pageInfoDefault
});

// 地区表单
const form = reactive<Partial<Area>>({
  name: '',
  fullName: '',
  code: '',
  parentId: 0,
  level: 1,
  pinyin: '',
  jianpin: '',
  longitude: 0,
  latitude: 0,
  enable: true,
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入地区名称', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '请输入地区代码', trigger: 'blur' }
  ],
  level: [
    { required: true, message: '请选择地区级别', trigger: 'change' }
  ]
});

// 获取级别类型
const getLevelType = (level: number): 'success' | 'primary' | 'warning' | 'danger' | 'info' => {
  const types: Record<number, 'success' | 'primary' | 'warning' | 'danger' | 'info'> = {
    1: 'success',
    2: 'primary',
    3: 'warning',
    4: 'danger'
  };
  return types[level] || 'info';
};

// 获取级别文本
const getLevelText = (level: number) => {
  const texts = ['', '省份', '城市', '区县', '乡镇'];
  return texts[level] || '未知';
};

// 加载地区选项
const loadAreaOptions = async () => {
  try {
    // 获取所有地区用于下拉选择（不分页）
    const response = await request.get('/Cube/Area', {
      params: { pageSize: 1000, pageIndex: 1 }
    });

    console.log('地区选项响应:', response);

    if (response && response.data) {
      const { list } = apiDataToList<Area>(response);
      areaOptions.value = list;
      console.log('地区选项数据:', areaOptions.value);
    }
  } catch (error) {
    console.error('加载地区选项失败:', error);
  }
};


// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载表格数据（用于搜索、分页等操作）
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/Area', { params: queryParams });
    const { list, page } = apiDataToList<Area>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error('加载数据失败');
    console.error('加载数据失败:', error);
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { q: '', level: undefined, pageIndex: 1 }, e || {});
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  // 确保地区选项是最新的
  if (areaOptions.value.length === 0) {
    loadAreaOptions();
  }
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Area) => {
  formType.value = 'edit';
  Object.assign(form, row);
  // 确保地区选项是最新的
  if (areaOptions.value.length === 0) {
    loadAreaOptions();
  }
  dialogVisible.value = true;
};

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 删除
const handleDelete = (row: Area) => {
  ElMessageBox.confirm(
    `确定要删除地区"${row.fullName || row.name}"吗？`,
    '确认删除',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/Area?id=${row.id}`);
      ElMessage.success('删除成功');
      loadData();
    } catch (error) {
      ElMessage.error('删除失败');
      console.error('删除失败:', error);
    }
  });
};

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    name: '',
    fullName: '',
    code: '',
    parentId: 0,
    level: 1,
    pinyin: '',
    jianpin: '',
    longitude: 0,
    latitude: 0,
    enable: true,
  });
  formRef.value?.clearValidate();
};

// 提交表单
const submitForm = () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (formType.value === 'add') {
          await request.post('/Cube/Area', form);
          ElMessage.success('新增成功');
        } else {
          await request.put('/Cube/Area', form);
          ElMessage.success('更新成功');
        }
        dialogVisible.value = false;
        loadData();
      } catch (error) {
        ElMessage.error(`${formType.value === 'add' ? '新增' : '更新'}失败`);
        console.error('操作失败:', error);
      }
    }
  });
};

// 初始化
onMounted(() => {
  loadAreaOptions();
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
