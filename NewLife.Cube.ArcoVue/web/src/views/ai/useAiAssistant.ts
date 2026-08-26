import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { resolveTenantHeader } from '@/stores/tenantHeader';
import { renderAiMarkdown } from '@/core/utils/aiMarkdown';
import { isAiSseDone, parseSseDataLine, takeSseEvents } from '@/core/utils/aiSse';
import { parseAreaController } from '@/core/utils/aiChatContext';
import { DEFAULT_AI_CONFIG } from '@/core/utils/aiConfig';
import {
  AI_FAB_DRAG_THRESHOLD,
  AI_FAB_SIZE,
  AI_PANEL_HEIGHT,
  AI_PANEL_WIDTH,
  AI_RESIZE_DIRS,
  applyAiResize,
  clampAiBoxPos,
  clampAiFabPos,
  clampAiPanelSize,
  defaultAiFabPos,
  defaultAiPanelPos,
  isAiResizeDir,
  resolveAiPanelSize,
  type AiPanelRect,
  type AiResizeDir,
} from '@/core/utils/aiFab';
import {
  aiQuickItemsForTab,
  aiWelcomeHello,
  aiWelcomeSubtitle,
  type AiWelcomeTab,
} from '@/core/utils/aiWelcome';
import { parseFillFormValue } from '@/core/utils/aiFill';
import {
  attachSkipMessage,
  buildAiChatMessage,
  displayAiUserText,
  isTextAttachment,
  pickAiAttachments,
  truncateAiAttachText,
  type AiAttachMeta,
} from '@/core/utils/aiAttach';

interface AiAttachItem {
  id: number;
  name: string;
  size: number;
  type: string;
  file: File;
}

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

function aiAuthHeaders(): Record<string, string> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  const token = cubeApi.tokenManager.getToken();
  if (token) headers.Authorization = token;
  try {
    Object.assign(headers, resolveTenantHeader(sessionStorage.getItem('cube.tenant.code')));
  } catch {
    /* ignore */
  }
  return headers;
}

function serializeResult(v: unknown): string {
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

export function useAiAssistant() {
  const route = useRoute();
  const appStore = useAppStore();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();

  const enabled = computed(() => appStore.aiConfig.enabled);
  const primaryColor = computed(() => appStore.aiConfig.primary || DEFAULT_AI_CONFIG.primary);
  const secondaryColor = computed(() => appStore.aiConfig.secondary || DEFAULT_AI_CONFIG.secondary);
  const visible = ref(false);
  const think = ref(false);
  const maximized = ref(localStorage.getItem('cube-ai-maximized') === '1');
  const input = ref('');
  const streaming = ref(false);
  const attachments = ref<AiAttachItem[]>([]);
  const fileInput = ref<HTMLInputElement | null>(null);
  const messages = ref<Msg[]>([]);
  const toolCards = ref<ToolCard[]>([]);
  const msgBox = ref<HTMLElement | null>(null);
  let attachSeq = 0;
  const welcomeTab = ref<AiWelcomeTab>('recommend');
  const viewport = ref({ w: viewportWidth(), h: viewportHeight() });
  const fabPos = ref(defaultAiFabPos(viewport.value.w, viewport.value.h));
  const panelPos = ref(defaultAiPanelPos(viewport.value.w, viewport.value.h));
  const panelBox = ref({ w: AI_PANEL_WIDTH, h: AI_PANEL_HEIGHT });
  const fabDragging = ref(false);
  const panelDragging = ref(false);
  const panelResizing = ref(false);
  let fabMoved = false;
  let panelMoved = false;
  let fabDragStart = { x: 0, y: 0, left: 0, top: 0 };
  let panelDragStart = { x: 0, y: 0, left: 0, top: 0 };
  let resizeDir: AiResizeDir = 'se';
  let resizeStart: AiPanelRect = { x: 0, y: 0, w: AI_PANEL_WIDTH, h: AI_PANEL_HEIGHT };
  let resizeOrigin = { x: 0, y: 0 };

  let seq = 0;
  let sessionId = localStorage.getItem('cube-ai-session') || '';
  if (!sessionId) {
    sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
    localStorage.setItem('cube-ai-session', sessionId);
  }

  const aiStyle = computed(() => ({
    '--ai-primary': primaryColor.value,
    '--ai-secondary': secondaryColor.value,
  }));

  const fabStyle = computed(() => ({
    ...aiStyle.value,
    left: `${fabPos.value.x}px`,
    top: `${fabPos.value.y}px`,
    width: `${AI_FAB_SIZE}px`,
    height: `${AI_FAB_SIZE}px`,
  }));

  const panelSize = computed(() =>
    resolveAiPanelSize(viewport.value.w, viewport.value.h, panelBox.value),
  );

  const panelStyle = computed(() => {
    if (maximized.value) return { ...aiStyle.value };
    const { w, h } = panelSize.value;
    return {
      ...aiStyle.value,
      left: `${panelPos.value.x}px`,
      top: `${panelPos.value.y}px`,
      width: `${w}px`,
      height: `${h}px`,
    };
  });

  const ctx = computed(() => appStore.aiContext);
  const hello = computed(() => aiWelcomeHello(userStore.displayName));
  const subtitle = computed(() => aiWelcomeSubtitle(ctx.value.page));
  const quickItems = computed(() => aiQuickItemsForTab(ctx.value.page, welcomeTab.value));
  const askHint = '在下方输入问题，或切到推荐查看快捷指令。';

  const areaCtrl = computed(() => parseAreaController(ctx.value.typePath || route.path));

  function viewportWidth() {
    return typeof window === 'undefined' ? 1280 : window.innerWidth;
  }
  function viewportHeight() {
    return typeof window === 'undefined' ? 800 : window.innerHeight;
  }

  function applyFabFromProfile() {
    const saved = profileStore.workspace.aiFab;
    const { w, h } = viewport.value;
    fabPos.value = saved ? clampAiFabPos(saved.x, saved.y, w, h) : defaultAiFabPos(w, h);
  }

  function applyPanelFromProfile() {
    const saved = profileStore.workspace.aiPanel;
    const { w, h } = viewport.value;
    const size = resolveAiPanelSize(w, h, saved);
    panelBox.value = size;
    panelPos.value = saved
      ? clampAiBoxPos(saved.x, saved.y, size.w, size.h, w, h)
      : defaultAiPanelPos(w, h, fabPos.value);
  }

  function persistFabPos() {
    profileStore.patchWorkspace({ aiFab: { ...fabPos.value } });
  }

  function persistPanelPos() {
    profileStore.patchWorkspace({
      aiPanel: {
        ...panelPos.value,
        w: panelBox.value.w,
        h: panelBox.value.h,
      },
    });
  }

  function onFabPointerDown(e: PointerEvent) {
    if (e.button !== 0) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    fabDragging.value = true;
    fabMoved = false;
    fabDragStart = {
      x: e.clientX,
      y: e.clientY,
      left: fabPos.value.x,
      top: fabPos.value.y,
    };
  }

  function onFabPointerMove(e: PointerEvent) {
    if (!fabDragging.value) return;
    const dx = e.clientX - fabDragStart.x;
    const dy = e.clientY - fabDragStart.y;
    if (Math.abs(dx) + Math.abs(dy) >= AI_FAB_DRAG_THRESHOLD) fabMoved = true;
    fabPos.value = clampAiFabPos(
      fabDragStart.left + dx,
      fabDragStart.top + dy,
      viewport.value.w,
      viewport.value.h,
    );
  }

  function onFabPointerUp() {
    if (!fabDragging.value) return;
    fabDragging.value = false;
    if (fabMoved) persistFabPos();
  }

  function onFabClick() {
    if (fabMoved) {
      fabMoved = false;
      return;
    }
    applyPanelFromProfile();
    visible.value = true;
  }

  function onPanelPointerDown(e: PointerEvent) {
    if (e.button !== 0 || maximized.value || panelResizing.value) return;
    const t = e.target as HTMLElement | null;
    if (t?.closest('.ai-actions') || t?.closest('.ai-resize')) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    panelDragging.value = true;
    panelMoved = false;
    panelDragStart = {
      x: e.clientX,
      y: e.clientY,
      left: panelPos.value.x,
      top: panelPos.value.y,
    };
  }

  function onPanelPointerMove(e: PointerEvent) {
    if (!panelDragging.value) return;
    const dx = e.clientX - panelDragStart.x;
    const dy = e.clientY - panelDragStart.y;
    if (Math.abs(dx) + Math.abs(dy) >= AI_FAB_DRAG_THRESHOLD) panelMoved = true;
    const { w: bw, h: bh } = panelSize.value;
    panelPos.value = clampAiBoxPos(
      panelDragStart.left + dx,
      panelDragStart.top + dy,
      bw,
      bh,
      viewport.value.w,
      viewport.value.h,
    );
  }

  function onPanelPointerUp() {
    if (!panelDragging.value) return;
    panelDragging.value = false;
    if (panelMoved) persistPanelPos();
  }

  function onResizePointerDown(e: PointerEvent) {
    if (e.button !== 0 || maximized.value) return;
    const dir = (e.currentTarget as HTMLElement).dataset.dir;
    if (!isAiResizeDir(dir)) return;
    e.preventDefault();
    e.stopPropagation();
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    panelResizing.value = true;
    resizeDir = dir;
    resizeOrigin = { x: e.clientX, y: e.clientY };
    resizeStart = {
      x: panelPos.value.x,
      y: panelPos.value.y,
      w: panelBox.value.w,
      h: panelBox.value.h,
    };
  }

  function onResizePointerMove(e: PointerEvent) {
    if (!panelResizing.value) return;
    const next = applyAiResize(
      resizeStart,
      resizeDir,
      e.clientX - resizeOrigin.x,
      e.clientY - resizeOrigin.y,
      viewport.value.w,
      viewport.value.h,
    );
    panelPos.value = { x: next.x, y: next.y };
    panelBox.value = { w: next.w, h: next.h };
  }

  function onResizePointerUp() {
    if (!panelResizing.value) return;
    panelResizing.value = false;
    persistPanelPos();
  }

  function onViewportResize() {
    viewport.value = { w: viewportWidth(), h: viewportHeight() };
    if (!fabDragging.value) {
      fabPos.value = clampAiFabPos(fabPos.value.x, fabPos.value.y, viewport.value.w, viewport.value.h);
    }
    if (!panelDragging.value && !panelResizing.value) {
      const size = clampAiPanelSize(panelBox.value.w, panelBox.value.h, viewport.value.w, viewport.value.h);
      panelBox.value = size;
      panelPos.value = clampAiBoxPos(
        panelPos.value.x,
        panelPos.value.y,
        size.w,
        size.h,
        viewport.value.w,
        viewport.value.h,
      );
    }
  }

  function scrollBottom() {
    nextTick(() => {
      if (msgBox.value) msgBox.value.scrollTop = msgBox.value.scrollHeight;
    });
  }

  function toggleMaximize() {
    maximized.value = !maximized.value;
    localStorage.setItem('cube-ai-maximized', maximized.value ? '1' : '0');
    scrollBottom();
  }

  function onWindowKeydown(e: KeyboardEvent) {
    if (e.key !== 'Escape') return;
    if (visible.value && maximized.value) {
      e.preventDefault();
      toggleMaximize();
    }
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

  function handleTool(json: Record<string, unknown>) {
    const id = String(json.id || '');
    let card = toolCards.value.find((t) => t.id === id);
    if (!card) {
      card = { id, name: String(json.name || ''), status: 'start' };
      toolCards.value.push(card);
    }
    const event = json.event;
    if (event === 'start') {
      card.name = String(json.name || card.name);
      card.status = 'start';
    } else if (event === 'done') {
      card.status = 'done';
      if (json.name === 'fill_form' && json.value) {
        const values = parseFillFormValue(json.value);
        if (values) {
          const apply = ctx.value.applyFill;
          if (!apply) {
            Message.info('请先打开添加或编辑');
          } else {
            apply(values);
          }
        }
      }
    } else {
      card.status = 'error';
    }
    scrollBottom();
  }

  async function postOperationResult(checkpointId: string, result: string) {
    try {
      await fetch('/Ai/OperationResult', {
        method: 'POST',
        headers: aiAuthHeaders(),
        body: JSON.stringify({ checkpointId, result }),
      });
    } catch {
      /* 回传失败忽略 */
    }
  }

  function handleRunJs(json: Record<string, unknown>) {
    const checkpointId = String(json.checkpointId || '');
    const script = String(json.script || '');
    let result: string;
    try {
      const fn = new Function(script);
      const v = fn();
      result = JSON.stringify({ ok: true, value: serializeResult(v) });
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : String(e);
      result = JSON.stringify({ ok: false, error: msg });
    }
    if (result.length > 8192) result = result.substring(0, 8192);
    void postOperationResult(checkpointId, result);
  }

  function applySseJson(json: Record<string, unknown>, am: Msg, acc: { full: string }): boolean {
    const type = json.type;
    if (type === 'text') {
      acc.full += String(json.content || '');
      am.html = renderAiMarkdown(acc.full);
      scrollBottom();
    } else if (type === 'tool') {
      handleTool(json);
    } else if (type === 'run_js') {
      handleRunJs(json);
    } else if (type === 'error') {
      am.html = `<span style="color:#c62828">⚠️ ${json.message || 'AI 调用失败'}</span>`;
      scrollBottom();
    }
    return isAiSseDone(json);
  }

  function toggleThink() {
    think.value = !think.value;
  }

  function openAttach() {
    if (streaming.value) return;
    fileInput.value?.click();
  }

  function removeAttachment(id: number) {
    if (streaming.value) return;
    attachments.value = attachments.value.filter((a) => a.id !== id);
  }

  function onAttachFiles(e: Event) {
    const el = e.target as HTMLInputElement;
    const files = Array.from(el.files || []);
    el.value = '';
    if (!files.length || streaming.value) return;
    const { accepted, skipped } = pickAiAttachments(attachments.value.length, files);
    const warn = attachSkipMessage(skipped);
    if (warn) Message.warning(warn);
    for (const file of accepted) {
      attachSeq += 1;
      attachments.value = [
        ...attachments.value,
        {
          id: attachSeq,
          name: file.name,
          size: file.size,
          type: file.type,
          file,
        },
      ];
    }
  }

  async function readAttachMeta(item: AiAttachItem): Promise<AiAttachMeta> {
    const meta: AiAttachMeta = { name: item.name, size: item.size, type: item.type };
    if (!isTextAttachment(item.file)) return meta;
    try {
      meta.text = truncateAiAttachText(await item.file.text());
    } catch {
      /* ignore */
    }
    return meta;
  }

  async function send(preset?: string) {
    if (streaming.value) return;
    const pending = preset ? [] : attachments.value.slice();
    const text = (preset ?? input.value).trim();
    if (!text && !pending.length) return;
    if (!preset) {
      input.value = '';
      attachments.value = [];
    }
    streaming.value = true;
    welcomeTab.value = 'recommend';

    const metas = pending.length ? await Promise.all(pending.map(readAttachMeta)) : [];
    const display = displayAiUserText(text, pending);
    const message = buildAiChatMessage(text, metas) || display;
    appendUser(display);
    const am = appendAssistant();
    const { area, controller } = areaCtrl.value;

    try {
      const resp = await fetch('/Ai/AiChat', {
        method: 'POST',
        headers: aiAuthHeaders(),
        body: JSON.stringify({
          sessionId,
          message,
          page: ctx.value.page,
          mode: ctx.value.mode,
          id: ctx.value.id || 0,
          query: ctx.value.queryB64 || '',
          area,
          controller,
          think: think.value,
        }),
      });
      if (!resp.ok) {
        let msg = 'HTTP ' + resp.status;
        try {
          const data = (await resp.json()) as { message?: string };
          if (data?.message) msg = data.message;
        } catch {
          /* ignore */
        }
        throw new Error(msg);
      }

      const reader = resp.body!.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      const acc = { full: '' };
      let stop = false;
      while (!stop) {
        const { done, value } = await reader.read();
        buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
        if (done) buffer += decoder.decode();
        const taken = takeSseEvents(done ? buffer + '\n' : buffer);
        buffer = taken.rest;
        for (const json of taken.events) {
          if (applySseJson(json, am, acc)) {
            stop = true;
            break;
          }
        }
        if (done) break;
      }
      if (!stop && buffer.trim()) {
        const json = parseSseDataLine(buffer) as Record<string, unknown> | null;
        if (json) applySseJson(json, am, acc);
      }
      try {
        await reader.cancel();
      } catch {
        /* 流已结束 */
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : '请求失败';
      am.html = `<span style="color:#c62828">⚠️ ${msg}</span><br><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>`;
    } finally {
      for (const c of toolCards.value) {
        if (c.status === 'start') {
          c.status = 'error';
          c.name = c.name + '（中断）';
        }
      }
      streaming.value = false;
    }
  }

  function onEditorKeydown(e: KeyboardEvent) {
    if (e.key !== 'Enter' || e.shiftKey) return;
    e.preventDefault();
    void send();
  }

  function clear() {
    messages.value = [];
    toolCards.value = [];
    sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
    localStorage.setItem('cube-ai-session', sessionId);
    welcomeTab.value = 'recommend';
  }

  watch(visible, (v) => {
    if (v) scrollBottom();
  });

  watch(
    () => profileStore.workspace.aiFab,
    () => {
      if (!fabDragging.value) applyFabFromProfile();
    },
  );

  watch(
    () => profileStore.workspace.aiPanel,
    () => {
      if (!panelDragging.value && !panelResizing.value) applyPanelFromProfile();
    },
  );

  onMounted(() => {
    viewport.value = { w: viewportWidth(), h: viewportHeight() };
    applyFabFromProfile();
    applyPanelFromProfile();
    window.addEventListener('resize', onViewportResize);
    window.addEventListener('keydown', onWindowKeydown);
    void appStore.fetchAiConfig();
  });

  onUnmounted(() => {
    window.removeEventListener('resize', onViewportResize);
    window.removeEventListener('keydown', onWindowKeydown);
  });

  return {
    enabled,
    visible,
    maximized,
    think,
    input,
    streaming,
    attachments,
    fileInput,
    canSend: computed(() => !streaming.value && (!!input.value.trim() || attachments.value.length > 0)),
    messages,
    toolCards,
    msgBox,
    welcomeTab,
    aiStyle,
    fabStyle,
    panelStyle,
    fabDragging,
    panelDragging,
    panelResizing,
    resizeDirs: AI_RESIZE_DIRS,
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
  };
}
