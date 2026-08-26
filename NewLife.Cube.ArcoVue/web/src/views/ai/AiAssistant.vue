<template>
  <Teleport to="body">
  <div v-if="enabled" class="ai-assistant" :class="{ 'panel-open': visible }" :style="aiStyle">
    <a-tooltip v-bind="aiTip" content="AI 助手" :disabled="fabDragging || visible">
      <button
        v-show="!visible"
        type="button"
        class="ai-fab"
        :class="{ dragging: fabDragging }"
        :style="fabStyle"
        @pointerdown="onFabPointerDown"
        @pointermove="onFabPointerMove"
        @pointerup="onFabPointerUp"
        @pointercancel="onFabPointerUp"
        @click="onFabClick"
      >
        <icon-park type="robot-one" />
      </button>
    </a-tooltip>

    <div
      v-if="visible"
      class="ai-panel"
      :class="{ maximized, dragging: panelDragging, resizing: panelResizing }"
      :style="panelStyle"
    >
      <header
        class="ai-panel-header"
        @pointerdown="onPanelPointerDown"
        @pointermove="onPanelPointerMove"
        @pointerup="onPanelPointerUp"
        @pointercancel="onPanelPointerUp"
      >
        <span class="ai-title">
          <icon-park type="robot-one" />
          AI 助手
          <a-tooltip v-bind="aiTip" content="对话会带上当前页面与筛选上下文，不会改权限。Enter 发送，Shift+Enter 换行。">
            <icon-park type="info" />
          </a-tooltip>
        </span>
        <div class="ai-actions">
          <a-tooltip v-bind="aiTip" :content="maximized ? '还原' : '最大化'">
            <a-button type="text" size="mini" @click="toggleMaximize">
              <icon-park :type="maximized ? 'off-screen' : 'full-screen'" />
            </a-button>
          </a-tooltip>
          <a-tooltip v-bind="aiTip" content="关闭">
            <a-button type="text" size="mini" @click="visible = false">
              <icon-park type="close" />
            </a-button>
          </a-tooltip>
        </div>
      </header>

      <div ref="msgBox" class="ai-messages">
        <div v-if="messages.length === 0" class="ai-welcome">
          <p class="ai-hello">{{ hello }}</p>
          <p class="ai-sub">{{ subtitle }}</p>
          <a-tabs v-model:active-key="welcomeTab" type="rounded" size="small">
            <a-tab-pane key="recommend" title="推荐" />
            <a-tab-pane key="ask" title="提问" />
            <a-tab-pane key="analyze" title="分析" />
          </a-tabs>
          <p v-if="welcomeTab === 'ask'" class="ai-ask-hint">{{ askHint }}</p>
          <button
            v-for="item in quickItems"
            :key="item.label + item.message"
            type="button"
            class="ai-suggest"
            @click="send(item.message)"
          >
            {{ item.label }}
          </button>
        </div>

        <template v-for="m in messages" :key="m.id">
          <div v-if="m.type === 'user'" class="ai-msg ai-user">
            <div class="ai-bubble">{{ m.text }}</div>
          </div>
          <div v-else class="ai-msg ai-bot">
            <div class="ai-bubble" v-html="m.html"></div>
          </div>
        </template>

        <div
          v-for="t in toolCards"
          :key="t.id"
          class="ai-tool"
          :class="{ 'ai-tool-done': t.status === 'done', 'ai-tool-error': t.status === 'error' }"
        >
          <template v-if="t.status === 'start'">🔧 正在调用 <b>{{ t.name }}</b>...</template>
          <template v-else-if="t.status === 'done'">✅ <b>{{ t.name }}</b> 完成</template>
          <template v-else>❌ <b>{{ t.name }}</b> 失败</template>
        </div>
      </div>

      <footer class="ai-panel-footer">
        <div v-if="messages.length" class="ai-footer-meta">
          <a-tooltip v-bind="aiTip" content="清空会话">
            <button type="button" class="ai-clear" :disabled="streaming" @click="clear">
              清空会话
            </button>
          </a-tooltip>
        </div>
        <div v-if="attachments.length" class="ai-chips">
          <span v-for="a in attachments" :key="a.id" class="ai-chip">
            <icon-park type="file-addition" />
            <span class="ai-chip-name" :title="a.name">{{ a.name }}</span>
            <button type="button" class="ai-chip-remove" :disabled="streaming" title="移除" @click="removeAttachment(a.id)">
              <icon-park type="close" />
            </button>
          </span>
        </div>
        <div class="ai-composer">
          <a-textarea
            v-model="input"
            :auto-size="{ minRows: 3, maxRows: 8 }"
            placeholder="输入问题…"
            :disabled="streaming"
            @keydown="onEditorKeydown"
          />
          <input
            ref="fileInput"
            type="file"
            multiple
            class="ai-file-input"
            :disabled="streaming"
            @change="onAttachFiles"
          />
          <div class="ai-bar">
            <a-tooltip v-bind="aiTip" content="添加附件">
              <button type="button" class="ai-icon-btn" :disabled="streaming" @click="openAttach">
                <icon-park type="plus" />
              </button>
            </a-tooltip>
            <span v-if="!input.trim()" class="ai-hint">Enter 发送 / Shift+Enter 换行</span>
            <span class="ai-bar-spacer" />
            <a-tooltip v-bind="aiTip" :content="think ? '关闭深度思考' : '深度思考'">
              <button
                type="button"
                class="ai-think-btn"
                :class="{ on: think }"
                :disabled="streaming"
                @click="toggleThink"
              >
                <icon-park type="lightning" />
                深度思考
              </button>
            </a-tooltip>
            <a-tooltip v-bind="aiTip" content="发送">
              <button
                type="button"
                class="ai-send-btn"
                :disabled="!canSend"
                @click="send()"
              >
                <icon-park type="send" />
              </button>
            </a-tooltip>
          </div>
        </div>
      </footer>
      <span
        v-for="dir in resizeDirs"
        v-show="!maximized"
        :key="dir"
        class="ai-resize"
        :class="'ai-resize-' + dir"
        :data-dir="dir"
        @pointerdown="onResizePointerDown"
        @pointermove="onResizePointerMove"
        @pointerup="onResizePointerUp"
        @pointercancel="onResizePointerUp"
      />
    </div>
  </div>
  </Teleport>
</template>

<script setup lang="ts">
import { useAiAssistant } from './useAiAssistant';

/** 提示挂到 body，避免被浮窗 overflow / 更高 z-index 挡住（浮窗 3000，Arco 弹层默认 ~1000） */
const aiTip = { popupContainer: 'body' as const, contentClass: 'ai-tooltip' };

const {
  enabled,
  visible,
  maximized,
  think,
  input,
  streaming,
  attachments,
  fileInput,
  canSend,
  messages,
  toolCards,
  msgBox,
  welcomeTab,
  aiStyle,
  hello,
  subtitle,
  quickItems,
  askHint,
  toggleMaximize,
  toggleThink,
  openAttach,
  onAttachFiles,
  removeAttachment,
  send,
  onEditorKeydown,
  clear,
  fabStyle,
  panelStyle,
  fabDragging,
  panelDragging,
  panelResizing,
  resizeDirs,
  onFabPointerDown,
  onFabPointerMove,
  onFabPointerUp,
  onFabClick,
  onPanelPointerDown,
  onPanelPointerMove,
  onPanelPointerUp,
  onResizePointerDown,
  onResizePointerMove,
  onResizePointerUp,
} = useAiAssistant();
</script>

<style scoped>
.ai-assistant {
  position: fixed;
  z-index: 3000;
  pointer-events: none;
}
.ai-fab,
.ai-panel {
  pointer-events: auto;
}
.ai-assistant :deep(.arco-trigger) {
  pointer-events: auto;
}
.ai-fab {
  position: fixed;
  right: auto;
  bottom: auto;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 50%;
  background: var(--ai-primary);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: grab;
  touch-action: none;
  user-select: none;
  font-size: 16px;
  box-shadow: 0 4px 14px color-mix(in srgb, var(--ai-primary) 50%, transparent);
}
.ai-fab.dragging {
  cursor: grabbing;
  transform: scale(1.06);
}
.ai-fab:hover:not(.dragging) {
  transform: scale(1.08);
}
.ai-panel {
  position: fixed;
  width: 360px;
  height: 480px;
  max-width: calc(100vw - 32px);
  max-height: calc(100vh - 32px);
  background: var(--color-bg-1);
  border: 1px solid var(--color-border-2);
  border-radius: 12px;
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.14);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.ai-panel.dragging,
.ai-panel.resizing {
  box-shadow: 0 12px 36px rgba(0, 0, 0, 0.18);
}
.ai-panel.maximized {
  inset: 0;
  left: 0 !important;
  top: 0 !important;
  width: auto !important;
  height: auto !important;
  max-width: none;
  max-height: none;
  border-radius: 0;
  border: none;
}
.ai-panel-header {
  height: 48px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 12px;
  border-bottom: 1px solid var(--color-border-2);
  cursor: grab;
  touch-action: none;
  user-select: none;
}
.ai-panel.dragging .ai-panel-header,
.ai-panel-header:active {
  cursor: grabbing;
}
.ai-title {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: var(--cube-font-size-body);
  font-weight: 500;
  color: var(--color-text-1);
}
.ai-actions {
  display: flex;
  align-items: center;
  cursor: default;
}
.ai-actions :deep(.arco-trigger),
.ai-bar :deep(.arco-trigger) {
  display: inline-flex;
  align-items: center;
  line-height: 0;
}
.ai-messages {
  flex: 1;
  overflow-y: auto;
  padding: 14px;
}
.ai-hello {
  margin: 0 0 4px;
  font-size: 16px;
  font-weight: 500;
}
.ai-sub,
.ai-ask-hint {
  margin: 0 0 12px;
  color: var(--color-text-3);
  font-size: 13px;
}
.ai-suggest {
  display: block;
  width: 100%;
  text-align: left;
  background: transparent;
  border: none;
  border-bottom: 1px solid var(--color-border-2);
  padding: 10px 4px;
  cursor: pointer;
  color: var(--color-text-1);
  font-size: 13px;
}
.ai-suggest:hover {
  background: var(--color-fill-2);
}
.ai-msg {
  margin-bottom: 10px;
}
.ai-bubble {
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
.ai-bot .ai-bubble {
  background: var(--color-fill-2);
  color: var(--color-text-1);
  border-top-left-radius: 2px;
}
.ai-bot .ai-bubble :deep(pre) {
  background: color-mix(in srgb, var(--color-text-1) 88%, transparent);
  color: var(--color-bg-1);
  padding: 8px;
  border-radius: 6px;
  overflow-x: auto;
}
.ai-tool {
  font-size: 12px;
  color: var(--color-text-2);
  margin-bottom: 6px;
}
.ai-panel-footer {
  flex-shrink: 0;
  position: relative;
  z-index: 2;
  padding: 10px 12px 12px;
  background: var(--color-bg-1);
  pointer-events: auto;
}
.ai-footer-meta {
  display: flex;
  justify-content: flex-end;
  min-height: 0;
}
.ai-clear {
  border: none;
  background: transparent;
  padding: 0 0 6px;
  font-size: 12px;
  line-height: 22px;
  color: var(--color-text-3);
  cursor: pointer;
}
.ai-clear:hover:not(:disabled) {
  color: var(--color-text-1);
}
.ai-clear:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}
.ai-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 8px;
}
.ai-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  max-width: 100%;
  padding: 2px 6px 2px 8px;
  border-radius: 12px;
  background: var(--color-fill-2);
  color: var(--color-text-2);
  font-size: 12px;
}
.ai-chip-name {
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ai-chip-remove {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: transparent;
  color: inherit;
  padding: 0;
  cursor: pointer;
  font-size: 10px;
}
.ai-chip-remove:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}
.ai-composer {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--color-border-2);
  border-radius: 12px;
  background: var(--color-bg-2);
  overflow: hidden;
}
.ai-composer:focus-within {
  border-color: var(--ai-primary);
}
.ai-composer :deep(.arco-textarea-wrapper),
.ai-composer :deep(.arco-textarea-scroll) {
  width: 100%;
  border: none !important;
  box-shadow: none !important;
  background: transparent !important;
}
.ai-composer :deep(textarea.arco-textarea) {
  padding: 8px 10px 2px;
  min-height: calc(3 * 1.6em);
  line-height: 1.6;
  background: transparent;
}
.ai-file-input {
  display: none;
}
.ai-bar {
  display: flex;
  align-items: center;
  gap: 4px;
  height: 28px;
  padding: 0 6px 6px;
  flex-shrink: 0;
}
.ai-bar-spacer {
  flex: 1;
  min-width: 4px;
}
.ai-icon-btn,
.ai-send-btn,
.ai-think-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 22px;
  border: none;
  background: transparent;
  color: var(--color-text-2);
  cursor: pointer;
  padding: 0;
  line-height: 1;
}
.ai-icon-btn,
.ai-send-btn {
  width: 22px;
  flex-shrink: 0;
  font-size: 14px;
  border-radius: 50%;
}
.ai-think-btn {
  gap: 2px;
  padding: 0 8px;
  border-radius: 11px;
  font-size: 12px;
  white-space: nowrap;
}
.ai-think-btn.on {
  color: var(--ai-primary);
  background: color-mix(in srgb, var(--ai-primary) 12%, transparent);
}
.ai-send-btn {
  background: var(--ai-primary);
  color: #fff;
  font-size: 12px;
}
.ai-icon-btn:hover:not(:disabled),
.ai-think-btn:hover:not(:disabled) {
  background: var(--color-fill-2);
  color: var(--color-text-1);
}
.ai-send-btn:hover:not(:disabled) {
  filter: brightness(1.06);
}
.ai-icon-btn:disabled,
.ai-think-btn:disabled,
.ai-send-btn:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}
.ai-hint {
  margin: 0;
  min-width: 0;
  font-size: 12px;
  line-height: 22px;
  color: var(--color-text-3);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  pointer-events: none;
}
.ai-resize {
  position: absolute;
  z-index: 4;
}
.ai-resize-n,
.ai-resize-s {
  left: 10px;
  right: 10px;
  height: 6px;
  cursor: ns-resize;
}
.ai-resize-e,
.ai-resize-w {
  top: 10px;
  bottom: 10px;
  width: 6px;
  cursor: ew-resize;
}
.ai-resize-n { top: 0; }
.ai-resize-s { bottom: 0; }
.ai-resize-e { right: 0; }
.ai-resize-w { left: 0; }
.ai-resize-ne,
.ai-resize-nw,
.ai-resize-se,
.ai-resize-sw {
  width: 12px;
  height: 12px;
}
.ai-resize-ne { top: 0; right: 0; cursor: nesw-resize; }
.ai-resize-nw { top: 0; left: 0; cursor: nwse-resize; }
.ai-resize-se { bottom: 0; right: 0; cursor: nwse-resize; }
.ai-resize-sw { bottom: 0; left: 0; cursor: nesw-resize; }
</style>

<style>
.arco-trigger-popup:has(.ai-tooltip) {
  z-index: 3100 !important;
}
</style>
