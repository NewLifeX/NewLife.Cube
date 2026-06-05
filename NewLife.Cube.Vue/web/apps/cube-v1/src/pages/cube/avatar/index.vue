<template>
  <div class="cube-avatar-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>头像管理</h3>
          <el-button type="primary" @click="handleGetAvatar">获取头像</el-button>
        </div>
      </template>

      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="用户ID">
          <el-input-number v-model="searchForm.id" placeholder="请输入用户ID" :min="0" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleGetAvatar">获取头像</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 头像展示区域 -->
      <el-row :gutter="20" v-if="avatarList.length > 0">
        <el-col :span="6" v-for="avatar in avatarList" :key="avatar.id">
          <el-card class="avatar-card">
            <div class="avatar-container">
              <img
                :src="avatar.url"
                :alt="avatar.name"
                class="avatar-image"
                @error="handleImageError"
              />
            </div>
            <div class="avatar-info">
              <h4>{{ avatar.name }}</h4>
              <p>ID: {{ avatar.id }}</p>
              <p>更新时间: {{ avatar.updateTime }}</p>
            </div>
            <div class="avatar-actions">
              <el-button type="primary" size="small" @click="viewAvatar(avatar)">查看</el-button>
              <el-button type="success" size="small" @click="downloadAvatar(avatar)">下载</el-button>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 如果没有头像 -->
      <el-empty v-else-if="searchForm.id && !loading" description="未找到头像" />

      <!-- 加载状态 -->
      <div v-loading="loading" class="loading-container" />
    </el-card>

    <!-- 头像查看对话框 -->
    <el-dialog v-model="dialogVisible" title="头像查看" width="500px">
      <div class="dialog-avatar-container" v-if="selectedAvatar">
        <img
          :src="selectedAvatar.url"
          :alt="selectedAvatar.name"
          class="dialog-avatar-image"
        />
        <el-descriptions :column="1" class="avatar-details">
          <el-descriptions-item label="用户ID">{{ selectedAvatar.id }}</el-descriptions-item>
          <el-descriptions-item label="用户名">{{ selectedAvatar.name }}</el-descriptions-item>
          <el-descriptions-item label="头像URL">{{ selectedAvatar.url }}</el-descriptions-item>
          <el-descriptions-item label="更新时间">{{ selectedAvatar.updateTime }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <template #footer>
        <el-button @click="dialogVisible = false">关闭</el-button>
        <el-button type="primary" @click="downloadAvatar(selectedAvatar)">下载头像</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { request } from 'cube-front/core/utils/request';
import { ElMessage } from 'element-plus';

// 定义接口类型
interface AvatarParams {
  id?: number;
}

interface AvatarData {
  id: number;
  name: string;
  url: string;
  updateTime: string;
}

// 表单数据
const searchForm = reactive<AvatarParams>({
  id: undefined,
});

// 头像列表
const avatarList = ref<AvatarData[]>([]);
const loading = ref(false);
const dialogVisible = ref(false);
const selectedAvatar = ref<AvatarData | null>(null);

// 获取头像
const handleGetAvatar = async () => {
  if (!searchForm.id) {
    ElMessage.warning('请输入用户ID');
    return;
  }

  loading.value = true;
  try {
    const response = await request.get('/Cube/Avatar', {
      params: {
        id: searchForm.id,
      },
      responseType: 'blob', // 头像通常是二进制数据
    });

    // 如果响应是blob，创建URL
    if (response instanceof Blob) {
      const url = URL.createObjectURL(response);
      const avatarData: AvatarData = {
        id: searchForm.id,
        name: `用户${searchForm.id}`,
        url,
        updateTime: new Date().toLocaleString(),
      };

      avatarList.value = [avatarData];
      ElMessage.success('头像获取成功');
    } else {
      // 如果返回的是URL字符串或其他格式
      const avatarData: AvatarData = {
        id: searchForm.id,
        name: `用户${searchForm.id}`,
        url: typeof response === 'string' ? response : `/Cube/Avatar?id=${searchForm.id}`,
        updateTime: new Date().toLocaleString(),
      };

      avatarList.value = [avatarData];
      ElMessage.success('头像获取成功');
    }
  } catch {
    ElMessage.error('头像获取失败');
    avatarList.value = [];
  } finally {
    loading.value = false;
  }
};

// 重置搜索
const resetSearch = () => {
  searchForm.id = undefined;
  avatarList.value = [];
};

// 查看头像
const viewAvatar = (avatar: AvatarData) => {
  selectedAvatar.value = avatar;
  dialogVisible.value = true;
};

// 下载头像
const downloadAvatar = (avatar: AvatarData | null) => {
  if (!avatar) return;

  const link = document.createElement('a');
  link.href = avatar.url;
  link.download = `avatar_${avatar.id}.png`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  ElMessage.success('头像下载完成');
};

// 图片加载错误处理
const handleImageError = (event: Event) => {
  const target = event.target as HTMLImageElement;
  target.src = '/path/to/default-avatar.png'; // 设置默认头像
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

.avatar-card {
  margin-bottom: 20px;
}

.avatar-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 150px;
  background-color: #f5f5f5;
  border-radius: 8px;
  margin-bottom: 15px;
}

.avatar-image {
  max-width: 120px;
  max-height: 120px;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-info {
  text-align: center;
  margin-bottom: 15px;
}

.avatar-info h4 {
  margin: 0 0 8px 0;
  color: #333;
}

.avatar-info p {
  margin: 4px 0;
  color: #666;
  font-size: 12px;
}

.avatar-actions {
  display: flex;
  justify-content: center;
  gap: 10px;
}

.loading-container {
  min-height: 200px;
}

.dialog-avatar-container {
  text-align: center;
}

.dialog-avatar-image {
  max-width: 200px;
  max-height: 200px;
  border-radius: 8px;
  margin-bottom: 20px;
}

.avatar-details {
  margin-top: 20px;
}
</style>
