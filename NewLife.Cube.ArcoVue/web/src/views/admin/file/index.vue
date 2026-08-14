<template>
  <div class="file-page">
    <!-- 主题表面：与实体对象列表 list-panel / 对象页 obj-surface 同源主题外壳 -->
    <div class="file-surface">
      <!-- 顶部工具栏：文件夹位置与文件数量居左，刷新/上传居右 -->
      <div class="file-toolbar">
        <a-space wrap>
          <a-breadcrumb>
            <a-breadcrumb-item>根目录</a-breadcrumb-item>
            <a-breadcrumb-item v-if="current">{{ current }}</a-breadcrumb-item>
          </a-breadcrumb>
          <span class="file-stat">{{ rows.length }} 项</span>
          <a-select :model-value="sort" size="small" style="width: 140px" @change="onSortChange">
            <a-option value="name">名称（目录优先）</a-option>
            <a-option value="size">大小</a-option>
            <a-option value="lastwrite">修改时间</a-option>
          </a-select>
        </a-space>
        <a-space>
          <a-button :loading="loading" @click="load()">刷新</a-button>
          <a-button v-if="flags.canAdd" type="primary" @click="pickFile">上传文件</a-button>
        </a-space>
        <input ref="fileInput" type="file" style="display: none" @change="onPickFile" />
      </div>

      <a-alert v-if="clip.length" class="file-clip" type="info" show-icon>
        <template #icon><span>📋</span></template>
        剪切板：{{ clip.map((c) => c.name).join('、') || '（空）' }}
        <template #action>
          <a-space>
            <a-button size="mini" type="primary" :disabled="!flags.canAdd || busy" @click="paste">粘贴到当前目录</a-button>
            <a-button size="mini" :disabled="!flags.canAdd || busy" @click="move">移动到当前目录</a-button>
            <a-button size="mini" @click="clearClipboard">清空剪切板</a-button>
          </a-space>
        </template>
      </a-alert>

      <a-alert v-if="error" type="warning" show-icon class="file-alert">{{ error }}</a-alert>
      <a-alert v-if="message" type="success" show-icon class="file-alert">{{ message }}</a-alert>

      <a-spin :loading="loading" style="display: block">
        <a-empty v-if="!loading && !rows.length" description="目录为空" />
        <a-table v-else :data="rows" :pagination="false" row-key="fullName">
          <template #columns>
            <a-table-column title="文件名" data-index="name">
              <template #cell="{ record }">
                <a-link v-if="record.directory" @click="enter(record)">
                  {{ record.isParent ? '..' : '📁 ' + record.name }}
                </a-link>
                <span v-else>📄 {{ record.name }}</span>
              </template>
            </a-table-column>
            <a-table-column title="大小" data-index="size" :width="110" />
            <a-table-column title="修改时间" data-index="lastWrite" :width="180" />
            <a-table-column title="操作" :width="330">
              <template #cell="{ record }">
                <!-- 操作区：采用实体对象列表视图（RecordCard.record-card-ops）同款动作按钮样式 -->
                <div v-if="!record.isParent" class="file-ops">
                  <button v-if="record.directory" type="button" class="file-op-btn" @click="enter(record)">
                    进入
                  </button>
                  <button v-else type="button" class="file-op-btn" @click="download(record)">
                    下载
                  </button>
                  <button v-if="flags.canEdit" type="button" class="file-op-btn" @click="copy(record)">
                    复制
                  </button>
                  <button v-if="flags.canAdd" type="button" class="file-op-btn" @click="compress(record)">
                    压缩
                  </button>
                  <button
                    v-if="!record.directory && flags.canEdit"
                    type="button"
                    class="file-op-btn"
                    @click="decompress(record)"
                  >
                    解压
                  </button>
                  <button
                    v-if="flags.canDelete"
                    type="button"
                    class="file-op-btn file-op-btn--danger"
                    @click="confirmRemove(record)"
                  >
                    删除
                  </button>
                </div>
                <button v-else type="button" class="file-op-btn" @click="enter(record)">
                  返回上一级
                </button>
              </template>
            </a-table-column>
          </template>
        </a-table>
      </a-spin>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Admin/File 专用页（OSC-2608139feb）：目录导航 + 上传/下载/压缩/解压/复制粘贴/移动/删除。
 */
import { useFilePage } from './useFilePage';

const {
  current,
  sort,
  rows,
  clip,
  message,
  loading,
  error,
  busy,
  fileInput,
  flags,
  load,
  onSortChange,
  enter,
  pickFile,
  onPickFile,
  download,
  confirmRemove,
  compress,
  decompress,
  copy,
  paste,
  move,
  clearClipboard,
} = useFilePage();
</script>

<style scoped>
.file-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.file-surface {
  min-width: 0;
  padding: 16px 16px 8px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.file-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
}
.file-stat {
  font-size: 13px;
  color: var(--color-text-3);
}
.file-clip {
  margin-bottom: 12px;
}
.file-alert {
  margin-bottom: 12px;
}
/* 操作区：与实体对象列表视图 RecordCard.record-card-btn 同款（fill-2 底 + text-1 字 + hover fill-3） */
.file-ops {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
.file-op-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 24px;
  padding: 0 12px;
  border: none;
  border-radius: 4px;
  background: var(--color-fill-2);
  color: var(--color-text-1);
  font-size: 12px;
  line-height: 1;
  cursor: pointer;
  transition: color 0.2s, background-color 0.2s;
}
.file-op-btn:hover {
  background: var(--color-fill-3);
}
.file-op-btn--danger {
  color: rgb(var(--danger-6));
}
.file-op-btn--danger:hover {
  color: rgb(var(--danger-6));
  background: rgba(var(--danger-6), 0.08);
}
</style>
