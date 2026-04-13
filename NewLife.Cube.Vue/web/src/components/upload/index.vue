<template>
  <el-upload
    class="upload-component"
    v-model:file-list="fileList"
    :action="action"
    :show-file-list="false"
    :disabled="limit !==1 && fileList.length >= limit"
    :accept="accept.toString()"
    :on-success="handleSuccess"
    :on-exceed="onExceed"
  >
    <draggable v-model="fileList" :item-key="(val: UploadUserFile) => getItemSrc(val)" class="flex flex-wrap" :animation="200">
      <template #item="{element, index}">
        <div class="relative mr-2 flex group" @click.stop :key="getItemSrc(element)" v-if="fileList.length <= limit || index >= limit">
          <el-image
            fit="cover"
            class="w-28 h-28 rounded mb-2"
            :preview-src-list="fileList.map(v => getItemSrc(v))"
            :initial-index="index"
            :src="getItemSrc(element)">
            <template #error>
              <div class="bg-gray-200 w-full h-full flex justify-center items-center font-bold text-green-500 text-xl flex-col border border-green-500" v-if="element.status === 'success'">
                <el-icon class="!text-4xl !text-green-500"><CircleCheck /></el-icon>
                {{ getItemSrc(element).split('.').reverse()[0] }}
              </div>
            </template>
          </el-image>
          <div class="absolute inset-0 flex justify-center items-center p-4" v-if="element.status === 'ready' || element.status === 'uploading'">
            <el-progress :text-inside="true" :stroke-width="20" :percentage="element.percentage" class="w-full" status="success"/>
          </div>
          <div class="!absolute right-0 top-0 bg-black/80 w-6 h-6 flex justify-center items-center rounded-tr opacity-0 group-hover:opacity-100 duration-75" @click="fileList.splice(index, 1)">
            <el-icon class="!text-white !text-xl"><Close /></el-icon>
          </div>
        </div>
      </template>
      <template #footer>
        <div class="avatar-uploader w-28 h-28 flex justify-center items-center flex-col" key="btn" :class="{'opacity-50': limit !== 1 && fileList.length >= limit}">
          <template v-if="limit === 1 && fileList.length === 1">
            <el-icon class="!text-3xl !text-gray-500"><Switch /></el-icon>
            <div class="text-gray-500 text-sm mt-2">更换</div>
          </template>
          <template v-else-if="limit !== 1 && fileList.length >= limit">
            <Icon class="text-gray-500 text-3xl" icon="system-uicons:no-sign"></Icon>
            <div class="text-gray-500 text-sm mt-2">(上传数量已满)</div>
          </template>
          <template v-else>
            <el-icon class="!text-3xl !text-gray-500"><Plus /></el-icon>
            <div class="text-gray-500 text-sm mt-2" v-if="limit > 1">({{fileList.length + ' / ' + limit }})</div>
          </template>
        </div>
      </template>
    </draggable>
  </el-upload>
</template>

<script lang="ts" setup>

import { ref } from 'vue'
import { Plus, Close, CircleCheck, Switch } from '@element-plus/icons-vue'
import type { UploadFile, UploadProps, UploadUserFile } from 'element-plus'
import { AcceptEnum } from './enum';
import { watch } from 'vue';
import draggable from 'vuedraggable'

interface Props {
  modelValue?: string | Array<string>;
  maxSize?: number;
  limit?: number;
  url?: string;
  resultKey?: string;
  accept?: Array<AcceptEnum | string>;
  modelType?: 'string' | 'array';
}
interface Emits {
  (e: 'update:modelValue', val: string | Array<string>): void
}

const props = withDefaults(defineProps<Props>(), {
  limit: 1,
  resultKey: 'filePath',
  modelType: 'string',
  accept: () => [],
})
const emits = defineEmits<Emits>()

const fileList = ref<UploadUserFile[]>([]);
const action = (import.meta.env.DEV ? '/base-api' : import.meta.env.VITE_API_URL) + props.url
const baseUrl = import.meta.env.VITE_IMG_BASE_URL
const onExceed = (_: any, uploadFiles: UploadUserFile[]) => {
  uploadFiles.splice(0, 1)
}

watch(fileList, () => {
  let srcList = getSrcList();
  if (props.modelValue?.toString() !== srcList.toString()) {
    emits('update:modelValue', props.modelType === 'string' ? srcList.toString() : srcList)
  }
}, {
  deep: true,
})

watch(() => props.modelValue, (val) => {
  if (val?.toString() !== getSrcList().toString()) {
    if (!val || !val.length) {
      fileList.value = []
    } else {
      fileList.value = (typeof val === 'string' ? val.split(',') : val).map(url => ({
        name: url,
        url,
        status: "success",
      }))
    }
  }
})

const getSrcList = () => {
  return fileList.value.filter(item => item.status == "success").map(item => item.url!)
}

const getItemSrc = (item: UploadUserFile & { rawUrl?: string }) => {
  let url = ""
  if (item.rawUrl)
    url = item.rawUrl;
  else if (item.raw && item.raw.type.indexOf('image') === 0) {
    item.rawUrl = URL.createObjectURL(item.raw)
    url = item.rawUrl
  } else if (item?.url)
    url = baseUrl + item.url
  else if (item?.response)
    url = baseUrl + (item.response as any).data[props.resultKey]
  return url
}

const handleSuccess: UploadProps['onSuccess'] = (_, uploadFile: UploadFile) => {
  uploadFile.url = (uploadFile.response as any).data[props.resultKey]
  if (props.limit === 1 && fileList.value.length > 1) {
    fileList.value.splice(0, fileList.value.length - 1)
  }
}
</script>

<style>
.upload-component .el-upload {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: flex-start;
}
.avatar-uploader {
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  transition: var(--el-transition-duration-fast);
}

.avatar-uploader:hover {
  border-color: var(--el-color-primary);
}
</style>