<template>
  <el-drawer
    v-model="visible"
    title="AI 洞察"
    direction="rtl"
    size="50%"
    :before-close="onClose"
    custom-class="ai-insight-drawer"
  >
    <template #header>
      <div class="ai-insight-header">
        <span class="ai-insight-title">
          <el-icon :size="20" color="#667eea"><MagicStick /></el-icon>
          AI 洞察
        </span>
        <el-tag v-if="thinking" type="warning" size="small" effect="dark">深度推理中...</el-tag>
        <el-tag v-else type="success" size="small" effect="dark">快速洞察</el-tag>
      </div>
    </template>

    <div class="ai-insight-body" ref="bodyRef">
      <!-- 加载状态 -->
      <div v-if="loading" class="ai-insight-loading">
        <el-icon class="is-loading" :size="48" color="#667eea"><Loading /></el-icon>
        <p>AI 正在分析数据，请稍候...</p>
      </div>

      <!-- 错误状态 -->
      <div v-else-if="error" class="ai-insight-error">
        <el-result icon="error" :title="error" sub-title="请确认 AI 服务已启用" />
      </div>

      <!-- 结果展示 -->
      <div v-else-if="result" class="ai-insight-result markdown-body" v-html="renderedMarkdown" />
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { MagicStick, Loading } from '@element-plus/icons-vue';

const props = defineProps<{
  modelValue: boolean;
  url: string;
  thinking: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
});

const loading = ref(false);
const error = ref('');
const result = ref('');
const bodyRef = ref<HTMLElement>();

// 简单 Markdown 渲染
function renderMarkdown(text: string): string {
  let html = text
    .replace(/^### (.+)$/gm, '<h3>$1</h3>')
    .replace(/^## (.+)$/gm, '<h2>$1</h2>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>')
    .replace(/^---$/gm, '<hr>')
    .replace(/^- (.+)$/gm, '<li>$1</li>')
    .replace(/\n\n/g, '</p><p>')
    .replace(/\n/g, '<br>');
  html = '<p>' + html + '</p>';
  html = html.replace(/((?:<li>.*?<\/li><br>)+)/g, '<ul>$1</ul>');
  return html;
}

const renderedMarkdown = computed(() => renderMarkdown(result.value));

// 执行 AI 洞察
async function runInsight() {
  loading.value = true;
  error.value = '';
  result.value = '';

  try {
    const response = await fetch(props.url, { credentials: 'same-origin' });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const reader = response.body?.getReader();
    if (!reader) throw new Error('No response body');

    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed.startsWith('data: ')) continue;

        try {
          const event = JSON.parse(trimmed.substring(6));
          if (event.type === 'text') {
            result.value += event.content;
            // 自动滚动到底部
            if (bodyRef.value) {
              bodyRef.value.scrollTop = bodyRef.value.scrollHeight;
            }
          }
        } catch {
          // JSON parse fail, skip
        }
      }
    }

    if (!result.value) {
      error.value = 'AI 未返回有效分析结果';
    }
  } catch (e: any) {
    error.value = `请求失败：${e.message}`;
  } finally {
    loading.value = false;
  }
}

function onClose() {
  visible.value = false;
}

watch(visible, (val) => {
  if (val) runInsight();
});
</script>

<style scoped>
.ai-insight-header {
  display: flex;
  align-items: center;
  gap: 10px;
}
.ai-insight-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}
.ai-insight-body {
  height: 100%;
  overflow-y: auto;
  padding: 0 8px;
}
.ai-insight-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 200px;
  color: #999;
  gap: 12px;
}
.ai-insight-error {
  padding: 40px 0;
}
/* Markdown 渲染样式 */
.markdown-body h2 {
  font-size: 18px;
  border-bottom: 2px solid #667eea;
  padding-bottom: 8px;
  margin: 20px 0 12px;
}
.markdown-body h3 {
  font-size: 15px;
  margin: 16px 0 8px;
}
.markdown-body table {
  width: 100%;
  border-collapse: collapse;
  margin: 10px 0;
}
.markdown-body th, .markdown-body td {
  border: 1px solid #ddd;
  padding: 6px 10px;
  font-size: 13px;
}
.markdown-body th {
  background: #f5f5f5;
}
.markdown-body blockquote {
  border-left: 4px solid #667eea;
  padding: 8px 16px;
  margin: 10px 0;
  background: #f9f9ff;
}
</style>
