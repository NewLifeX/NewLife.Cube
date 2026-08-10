<!--
 * 魔方 AI 助手（Vue 版）悬浮窗
 * 右下角对话面板：SSE 流式对话 + 工具调用可视化 + 表单智能填充
 * 协议与 MVC 版 ai-assistant.js 一致：POST /Ai/AiChat（body 携带 area/controller 目标页面）→ SSE {type:text|tool|error|done}
 -->
<template>
  <div v-if="enabled" class="ai-assistant" :class="{ 'panel-open': visible }" :style="aiStyle">
    <!-- 悬浮球 -->
    <div class="ai-fab" :title="'AI 助手'" @click="visible = !visible">
      <svg viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
        <g fill="#2ecc71">
          <circle cx="12" cy="7" r="4.6" />
          <circle cx="17" cy="12" r="4.6" />
          <circle cx="12" cy="17" r="4.6" />
          <circle cx="7" cy="12" r="4.6" />
        </g>
        <circle cx="12" cy="12" r="2.6" fill="#ffffff" />
      </svg>
    </div>

    <!-- 对话面板 -->
    <Transition name="ai-fade">
      <div v-if="visible" class="ai-panel" :class="{ maximized }">
        <div class="ai-panel-header">
          <span class="ai-title">
            <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
              <g fill="#2ecc71">
                <circle cx="12" cy="7" r="4.6" />
                <circle cx="17" cy="12" r="4.6" />
                <circle cx="12" cy="17" r="4.6" />
                <circle cx="7" cy="12" r="4.6" />
              </g>
              <circle cx="12" cy="12" r="2.6" fill="#ffffff" />
            </svg>
            AI 助手
          </span>
          <div class="ai-actions">
            <el-button text size="small" :title="maximized ? '还原' : '最大化'" @click="toggleMaximize"><el-icon><FullScreen v-if="!maximized" /><CopyDocument v-else /></el-icon></el-button>
            <el-button text size="small" title="清空会话" @click="clear"><el-icon><Delete /></el-icon></el-button>
            <el-button text size="small" title="收起" @click="visible = false"><el-icon><Close /></el-icon></el-button>
          </div>
        </div>

        <div class="ai-messages" ref="msgBox">
          <!-- 欢迎语 + 快捷指令 -->
          <div v-if="messages.length === 0" class="ai-welcome">
            <p>你好，我是魔方 AI 助手 👋 我可以帮你分析当前数据、填写表单、检查系统状态。</p>
            <div class="ai-quick">
              <el-button v-if="page === 'list'" size="small" round @click="quick('分析当前列表数据')">📊 分析当前数据</el-button>
              <el-button v-if="page === 'form'" size="small" round @click="quick('帮我填写当前表单')">📝 帮我填表</el-button>
              <el-button size="small" round @click="quick('检查系统运行状态')">🩺 系统诊断</el-button>
            </div>
          </div>

          <!-- 消息 -->
          <template v-for="m in messages" :key="m.id">
            <div v-if="m.type === 'user'" class="ai-msg ai-user"><div class="ai-bubble">{{ m.text }}</div></div>
            <div v-else-if="m.type === 'assistant'" class="ai-msg ai-assistant"><div class="ai-bubble" v-html="m.html"></div></div>
          </template>

          <!-- 工具卡片 -->
          <div v-for="t in toolCards" :key="t.id" class="ai-tool" :class="{ 'ai-tool-done': t.status === 'done', 'ai-tool-error': t.status === 'error' }">
            <template v-if="t.status === 'start'"><span>🔧 正在调用 <b>{{ t.name }}</b>...</span></template>
            <template v-else-if="t.status === 'done'"><span>✅ <b>{{ t.name }}</b> 完成</span></template>
            <template v-else><span>❌ <b>{{ t.name }}</b> 失败</span></template>
          </div>
        </div>

        <div class="ai-panel-footer">
          <label class="ai-think" title="深度推理更准确，但耗时更长">
            <el-checkbox v-model="think">深度</el-checkbox>
          </label>
          <el-input
            v-model="input"
            type="textarea"
            :rows="1"
            resize="none"
            placeholder="输入问题，Enter 发送，Shift+Enter 换行"
            @keydown.enter.exact.prevent="send"
          />
          <el-button type="primary" circle :icon="Promotion" title="发送" :loading="streaming" @click="send" />
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { Delete, Close, Promotion, FullScreen, CopyDocument } from '@element-plus/icons-vue';
import { marked } from 'marked';
import { Session } from '/@/utils/storage';

interface Props {
  /** 页面类型：list / form / detail */
  page: string;
  /** 表单模式：add / edit */
  mode?: string;
  /** 记录编号 */
  id?: number;
  /** 列表页查询条件（_query Base64），表单页可空 */
  query?: string;
  /** 目标页面控制器路径，如 'Admin/User' 或 'User'。请求统一走全局端点 /Ai/AiChat，经 area/controller 解析目标控制器能力 */
  url: string;
}
const props = withDefaults(defineProps<Props>(), {
  mode: 'add',
  id: 0,
  query: '',
});

const emit = defineEmits<{
  /** fill_form 工具完成，携带生成的字段值，由宿主合并到表单 */
  (e: 'fill-form', values: Record<string, any>): void;
}>();

interface Msg {
  id: number;
  type: 'user' | 'assistant';
  text?: string;
  html: string;
}
interface ToolCard {
  id: string;
  name: string;
  status: 'start' | 'done' | 'error';
}

const visible = ref(false);
const think = ref(false);
/** 全屏放大状态，localStorage 持久化，重开面板自动恢复 */
const maximized = ref(localStorage.getItem('cube-ai-maximized') === '1');

/** AI 助手开关与配色（来自 /Cube/GetAiConfig，CubeSetting 配置） */
const enabled = ref(false);
const primaryColor = ref('#2ecc71');
const secondaryColor = ref('#1e8e3e');

/** 根元素 CSS 变量，驱动全部浮窗配色 */
const aiStyle = computed(() => ({
  '--ai-primary': primaryColor.value,
  '--ai-secondary': secondaryColor.value,
}));
const input = ref('');
const streaming = ref(false);
const messages = ref<Msg[]>([]);
const toolCards = ref<ToolCard[]>([]);
const msgBox = ref<HTMLElement>();

let seq = 0;
let sessionId = localStorage.getItem('cube-ai-session') || '';
if (!sessionId) {
  sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
  localStorage.setItem('cube-ai-session', sessionId);
}

/** HTML 转义，防止 AI 输出的 raw HTML 注入（XSS） */
function escapeHtml(s: string): string {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/** Markdown 渲染：marked 库（GFM + 单换行断行 + XSS 防护） */
function renderMarkdown(text: string): string {
  if (!text) return '';
  return marked.parse(text, {
    gfm: true,
    breaks: true, // 单换行 → <br>，AI 流式输出友好
    renderer: {
      // 安全：AI 输出中的 raw HTML 一律转义显示，防止 XSS
      html: ({ text: t }) => escapeHtml(t),
      // 代码块：语言徽标 + 内容转义 + 深色样式
      code: ({ text: t, lang }) => {
        const l = (lang || '').match(/^\S+/)?.[0] || '';
        const label = l ? `<span class="ai-code-lang">${escapeHtml(l)}</span>` : '';
        const body = String(t).replace(/\n+$/, '');
        return `<pre>${label}<code class="language-${escapeHtml(l)}">${escapeHtml(body)}</code></pre>`;
      },
    } as any,
  });
}

function scrollBottom() {
  nextTick(() => {
    if (msgBox.value) msgBox.value.scrollTop = msgBox.value.scrollHeight;
  });
}

/** 切换全屏放大：占满视口留 20px 边距，状态持久化，重开面板自动恢复 */
function toggleMaximize() {
  maximized.value = !maximized.value;
  localStorage.setItem('cube-ai-maximized', maximized.value ? '1' : '0');
  scrollBottom();
}

function appendUser(text: string) {
  messages.value.push({ id: ++seq, type: 'user', text, html: '' });
  scrollBottom();
}
function appendAssistant(): Msg {
  const m: Msg = { id: ++seq, type: 'assistant', html: '' };
  messages.value.push(m);
  scrollBottom();
  return m;
}

/** 处理工具事件 */
function handleTool(json: any) {
  let card = toolCards.value.find((t) => t.id === json.id);
  if (!card) {
    card = { id: json.id || '', name: json.name || '', status: 'start' };
    toolCards.value.push(card);
  }
  if (json.event === 'start') {
    card.name = json.name || card.name;
    card.status = 'start';
  } else if (json.event === 'done') {
    card.status = 'done';
    // fill_form 完成 → 表单值交给宿主合并
    if (json.name === 'fill_form' && json.value) {
      try {
        const data = JSON.parse(json.value);
        if (data && data.kind === 'fill_form' && data.values) {
          emit('fill-form', data.values as Record<string, any>);
          const names = Object.keys(data.values).join('、');
          ElMessage.success(`AI 已预填 ${Object.keys(data.values).length} 个字段（${names}），请检查后保存`);
        }
      } catch {
        /* 解析失败忽略 */
      }
    }
  } else {
    card.status = 'error';
  }
  scrollBottom();
}

/** 序列化脚本执行结果，处理循环引用/函数等无法 JSON 化的值 */
function serializeResult(v: any): string {
  if (v === undefined) return 'undefined';
  if (v === null) return 'null';
  if (typeof v === 'function') return '[Function]';
  if (typeof v === 'symbol' || typeof v === 'bigint') return String(v);
  if (typeof v === 'object') {
    try {
      const s = JSON.stringify(v);
      return s === undefined ? String(v) : s;
    } catch {
      return String(v);
    }
  }
  return JSON.stringify(v);
}

/** 获取浏览器操作回传端点：全局 AI 控制器 OperationResult（统一无区域前缀），所有实体页面共用 */
function getAiOperationUrl(): string {
  return 'Ai/OperationResult';
}

/** 回传浏览器操作结果到全局 AI 控制器，完成等待中的工具调用 */
async function postOperationResult(checkpointId: string, result: string) {
  try {
    await fetch(getAiOperationUrl(), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(Session.get('token') ? { Authorization: `${Session.get('token')}` } : {}),
      },
      body: JSON.stringify({ checkpointId, result }),
    });
  } catch {
    // 回传失败忽略，后端会超时自动失败
  }
}

/** 处理后端下发的 run_js 事件：执行脚本并回传结果 */
function handleRunJs(json: any) {
  const checkpointId = json.checkpointId as string;
  const script = (json.script as string) || '';
  let result: string;
  try {
    const fn = new Function(script);
    const v = fn();
    result = JSON.stringify({ ok: true, value: serializeResult(v) });
  } catch (e: any) {
    result = JSON.stringify({ ok: false, error: e?.message || String(e) });
  }
  // 结果过大时截断，避免请求体膨胀
  if (result.length > 8192) result = result.substring(0, 8192);
  postOperationResult(checkpointId, result);
}

/** 发送消息 */
async function send() {
  if (streaming.value) return;
  const text = input.value.trim();
  if (!text) return;
  input.value = '';
  streaming.value = true;

  appendUser(text);
  const am = appendAssistant();
  let full = '';

  // 解析目标页面标识：url 如 'Admin/User' → area='Admin', controller='User'；'User' → area=''
  const urlParts = (props.url || '').split('/');
  const targetController = urlParts[urlParts.length - 1] || '';
  const targetArea = urlParts.length > 1 ? urlParts.slice(0, -1).join('/') : '';

  try {
    // 统一走全局 AI 端点，服务端按 area/controller 解析目标控制器能力（实体数据工具/页面上下文/通用工具）
    const resp = await fetch('/Ai/AiChat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(Session.get('token') ? { Authorization: `${Session.get('token')}` } : {}),
      },
      body: JSON.stringify({
        sessionId,
        message: text,
        page: props.page,
        mode: props.mode,
        id: props.id || 0,
        query: props.query || '',
        area: targetArea,
        controller: targetController,
        think: think.value,
      }),
    });
    if (!resp.ok) {
      let msg = 'HTTP ' + resp.status;
      try {
        const data = await resp.json();
        if (data?.message) msg = data.message;
      } catch {
        /* ignore */
      }
      throw new Error(msg);
    }

    const reader = resp.body!.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';
      for (const line of lines) {
        const t = line.trim();
        if (!t.startsWith('data: ')) continue;
        let json: any;
        try {
          json = JSON.parse(t.substring(6));
        } catch {
          continue;
        }
        if (!json) continue;
        if (json.type === 'text') {
          full += json.content || '';
          am.html = renderMarkdown(full);
          scrollBottom();
        } else if (json.type === 'tool') {
          handleTool(json);
        } else if (json.type === 'run_js') {
          handleRunJs(json);
        } else if (json.type === 'error') {
          am.html = `<span style="color:#c62828">⚠️ ${json.message || 'AI 调用失败'}</span>`;
          scrollBottom();
        }
      }
    }
  } catch (err: any) {
    am.html = `<span style="color:#c62828">⚠️ ${err?.message || '请求失败'}</span><br><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>`;
  } finally {
    // 标记遗留"正在调用"的工具卡片为中断
    for (const c of toolCards.value) {
      if (c.status === 'start') {
        c.status = 'error';
        c.name = c.name + '（中断）';
      }
    }
    streaming.value = false;
  }
}

/** 快捷指令 */
function quick(prompt: string) {
  input.value = prompt;
  send();
}

/** 清空会话 */
function clear() {
  messages.value = [];
  toolCards.value = [];
  sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
  localStorage.setItem('cube-ai-session', sessionId);
}

/** 加载 AI 助手配置：开关控制渲染，配色注入 CSS 变量 */
async function loadConfig() {
  try {
    const resp = await fetch('/Cube/GetAiConfig', {
      headers: Session.get('token') ? { Authorization: `${Session.get('token')}` } : {},
    });
    if (!resp.ok) return;
    const json = await resp.json();
    const data = json?.data;
    if (data) {
      enabled.value = !!data.AISwitch;
      if (data.AIPrimaryColor) primaryColor.value = data.AIPrimaryColor;
      if (data.AISecondaryColor) secondaryColor.value = data.AISecondaryColor;
    }
  } catch {
    // 配置获取失败时保持默认配色，且不显示浮窗（AISwitch 默认关闭）
  }
}
onMounted(loadConfig);
</script>

<style scoped>
.ai-assistant {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 3000;
}
.ai-assistant.panel-open .ai-fab {
  /* 面板打开时隐藏悬浮球，关闭后恢复显示 */
  display: none;
}
.ai-fab {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--ai-primary) 0%, var(--ai-secondary) 100%);
  box-shadow: 0 4px 14px color-mix(in srgb, var(--ai-primary) 50%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: transform 0.2s;
}
.ai-fab:hover {
  transform: scale(1.08);
}
.ai-panel {
  position: absolute;
  right: 0;
  bottom: 64px;
  width: 460px;
  height: 70vh;
  min-height: 400px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.18);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.ai-assistant.panel-open .ai-panel {
  /* 面板打开时下沉到悬浮球位置，多占约 64px 高度 */
  bottom: 0;
}
.ai-panel.maximized {
  /* 全屏放大态：占满视口留 20px 边距 */
  position: fixed;
  left: 20px;
  top: 20px;
  right: 20px;
  bottom: 20px;
  width: auto;
  height: auto;
  border-radius: 12px;
}
.ai-panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  background: linear-gradient(135deg, var(--ai-primary) 0%, var(--ai-secondary) 100%);
  color: #fff;
  font-weight: 600;
}
.ai-panel-header .el-button {
  color: #fff;
}
.ai-messages {
  flex: 1;
  overflow-y: auto;
  padding: 14px;
}
.ai-welcome {
  color: #666;
  font-size: 13px;
}
.ai-welcome p {
  margin: 0 0 10px;
}
.ai-quick {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.ai-msg {
  margin-bottom: 10px;
  display: flex;
}
/* 用户/AI 消息气泡满宽，仅靠颜色区分，避免左右留白浪费 */
.ai-msg .ai-bubble {
  width: 100%;
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 13px;
  line-height: 1.6;
  word-break: break-word;
}
.ai-user .ai-bubble {
  background: var(--ai-primary);
  color: #fff;
  border-top-right-radius: 2px;
}
.ai-assistant .ai-bubble {
  background: #f2f3f5;
  color: #333;
  border-top-left-radius: 2px;
}
.ai-assistant .ai-bubble :deep(h1), .ai-assistant .ai-bubble :deep(h2), .ai-assistant .ai-bubble :deep(h3), .ai-assistant .ai-bubble :deep(h4) {
  margin: 8px 0 4px;
}
.ai-assistant .ai-bubble :deep(p) {
  margin: 4px 0;
}
.ai-assistant .ai-bubble :deep(code) {
  background: #e9e9e9;
  padding: 1px 4px;
  border-radius: 3px;
  font-family: Consolas, Monaco, 'Courier New', monospace;
  font-size: 90%;
}
.ai-assistant .ai-bubble :deep(pre) {
  background: #1e1e1e;
  color: #d4d4d4;
  padding: 10px 12px;
  border-radius: 6px;
  overflow-x: auto;
  margin: 6px 0;
  position: relative;
}
.ai-assistant .ai-bubble :deep(pre code) {
  background: none;
  padding: 0;
  color: inherit;
  font-size: 90%;
}
.ai-assistant .ai-bubble :deep(.ai-code-lang) {
  position: absolute;
  top: 4px;
  right: 8px;
  font-size: 10px;
  color: #888;
  text-transform: uppercase;
  user-select: none;
}
.ai-assistant .ai-bubble :deep(table) {
  border-collapse: collapse;
  width: 100%;
  margin: 6px 0;
}
.ai-assistant .ai-bubble :deep(th),
.ai-assistant .ai-bubble :deep(td) {
  border: 1px solid #ddd;
  padding: 4px 6px;
  font-size: 12px;
}
.ai-assistant .ai-bubble :deep(th) {
  background: #f5f5f5;
}
.ai-assistant .ai-bubble :deep(a) {
  color: var(--ai-primary);
  text-decoration: underline;
}
.ai-assistant .ai-bubble :deep(hr) {
  border: none;
  border-top: 1px solid #eee;
  margin: 8px 0;
}
.ai-assistant .ai-bubble :deep(ul),
.ai-assistant .ai-bubble :deep(ol) {
  padding-left: 18px;
  margin: 4px 0;
}
.ai-assistant .ai-bubble :deep(li) {
  margin: 2px 0;
}
.ai-assistant .ai-bubble :deep(input[type='checkbox']) {
  margin-right: 4px;
}
.ai-assistant .ai-bubble :deep(blockquote) {
  border-left: 3px solid var(--ai-primary);
  margin: 6px 0;
  padding: 4px 10px;
  background: color-mix(in srgb, var(--ai-primary) 6%, #fff);
  color: #555;
}
.ai-tool {
  font-size: 12px;
  color: #666;
  background: #fafafa;
  border: 1px solid #eee;
  border-radius: 6px;
  padding: 6px 10px;
  margin-bottom: 6px;
}
.ai-tool-done {
  color: #2e7d32;
  border-color: #c8e6c9;
  background: #f1f8f1;
}
.ai-tool-error {
  color: #c62828;
  border-color: #ffcdd2;
  background: #fff5f5;
}
.ai-panel-footer {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  padding: 10px 12px;
  border-top: 1px solid #eee;
}
.ai-think {
  flex-shrink: 0;
  padding-bottom: 6px;
  font-size: 12px;
  color: #666;
}
.ai-panel-footer .el-textarea {
  flex: 1;
}
.ai-fade-enter-active,
.ai-fade-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}
.ai-fade-enter-from,
.ai-fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
