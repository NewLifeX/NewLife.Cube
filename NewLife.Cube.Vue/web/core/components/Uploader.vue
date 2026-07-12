<script setup lang="ts">
/**
 * 文件 / 图片上传组件（真实上传链路）
 *
 * v-model 绑定「上传后返回的 URL 字符串」（resolveControl 映射的 control = 'upload' / 'image'）。
 *
 * 流程：el-upload 选择文件 → 走 request（带鉴权 / baseUrl）POST 到后端上传接口
 *        → 后端返回 { code:0, message:null, data:{ attId, filePath, contentType } }
 *        → 将 data.filePath 回写 modelValue。
 *
 * 上传地址解析（与 GetPage 一致，跟随路由动态推导）：
 *   - 传入 apiPrefix（如 /Device/DeviceProfile）时，使用 `${apiPrefix}/UploadFile`（默认接口）
 *   - 未传入 apiPrefix 时，回退到显式 action（默认 /Test/TestUpload/Upload）以保证旧用法兼容
 */
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import type { UploadRequestOptions, UploadProps } from 'element-plus';
import request from '../utils/request';
import { resolveAssetUrl } from '../utils/url';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    /** file = 普通文件；image = 图片（带预览与类型校验） */
    kind?: 'file' | 'image';
    /** 上传接口地址（相对路径，走 request 的 baseUrl）。仅在未传入 apiPrefix 时生效 */
    action?: string;
    /** 接口前缀（动态类型，由路由计算得，如 /Device/DeviceProfile）。优先级高于 action 默认值 */
    apiPrefix?: string;
    disabled?: boolean;
  }>(),
  {
    modelValue: '',
    kind: 'file',
    action: '/Test/TestUpload/Upload',
    apiPrefix: '',
    disabled: false,
  },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

const uploading = ref(false);

/**
 * 实际上传地址：
 *   - 传入 apiPrefix 时动态拼接默认上传接口 `${apiPrefix}/UploadFile`；
 *   - 否则回退到显式 action（默认 /Test/TestUpload/Upload）以兼容旧用法。
 */
const uploadUrl = computed(() => {
  if (props.apiPrefix) return `${props.apiPrefix}/UploadFile`;
  return props.action;
});

/** 回显文件名（从 URL 末段提取） */
const fileName = computed(() => {
  if (!props.modelValue) return '';
  const idx = props.modelValue.lastIndexOf('/');
  return props.modelValue.substring(idx + 1) || props.modelValue;
});

const isImage = computed(() => props.kind === 'image');

/** 展示用地址：后端可能返回以「/」开头的相对路径，需拼接 baseUrl */
const displayUrl = computed(() => resolveAssetUrl(props.modelValue));

/** 上传前校验（图片类型） */
const beforeUpload: UploadProps['beforeUpload'] = (rawFile) => {
  if (isImage.value && !(rawFile as File).type.startsWith('image/')) {
    ElMessage.error('只能上传图片文件');
    return false;
  }
  return true;
};

/** 自定义上传：走 cubeAxios 以携带鉴权与 baseUrl */
async function customUpload(options: UploadRequestOptions) {
  const formData = new FormData();
  formData.append('file', options.file);
  try {
    uploading.value = true;
    // cubeAxios 响应拦截后成功时返回完整 ApiResponse：{ code, message, data }
    const res: any = await request.post(uploadUrl.value, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    // 兼容两种形态：拦截器返回完整 ApiResponse（res.data 为内层）或直接返回内层对象
    const apiRes = res?.data !== undefined ? res : { data: res };
    if (apiRes.code !== undefined && apiRes.code !== 0 && apiRes.code !== 200) {
      throw new Error(apiRes.message || '上传失败');
    }
    const data = apiRes.data ?? {};
    const filePath = data?.filePath ?? '';
    if (!filePath) throw new Error('上传成功但未返回文件地址');
    emit('update:modelValue', filePath);
    options.onSuccess?.(data);
  } catch (err: any) {
    const msg = err?.message || '上传失败';
    ElMessage.error(msg);
    options.onError?.(err);
  } finally {
    uploading.value = false;
  }
}

function clearFile() {
  emit('update:modelValue', '');
}
</script>

<template>
  <div class="uploader">
    <!-- 图片预览 -->
    <div v-if="isImage && modelValue" class="uploader__preview">
      <img :src="displayUrl" :alt="fileName" class="uploader__img" />
      <el-button size="small" text type="danger" :disabled="disabled" @click="clearFile">
        移除
      </el-button>
    </div>

    <!-- 文件链接 -->
    <div v-else-if="!isImage && modelValue" class="uploader__file">
      <el-link :href="displayUrl" target="_blank" type="primary" :underline="true">
        {{ fileName }}
      </el-link>
      <el-button size="small" text type="danger" :disabled="disabled" @click="clearFile">
        移除
      </el-button>
    </div>

    <!-- 上传按钮 -->
    <el-upload
      :http-request="customUpload"
      :before-upload="beforeUpload"
      :show-file-list="false"
      :disabled="disabled || uploading"
      class="uploader__btn"
    >
      <el-button :loading="uploading" :disabled="disabled">
        {{ uploading ? '上传中...' : isImage ? '上传图片' : '上传文件' }}
      </el-button>
    </el-upload>
  </div>
</template>

<style scoped>
.uploader {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.uploader__preview {
  display: flex;
  align-items: center;
  gap: 8px;
}

.uploader__img {
  width: 64px;
  height: 64px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--el-border-color-light);
}

.uploader__file {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
