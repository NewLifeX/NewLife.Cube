/**
 * AI 助手（右下角对话面板，对齐 Vue 皮肤 components/ai/AiAssistant.vue + MVC ai-assistant.js）
 *
 * 协议：POST /Ai/AiChat（body 携带 area/controller 目标页面 + sessionId 会话号）→ SSE
 *   {type: content_delta|tool_call_start|tool_call_done|tool_call_error|run_js|error}
 * 能力：
 * - AISwitch 开关门控：GetAiConfig 读取，关闭时不渲染（对齐 Vue v-if="enabled"）
 * - 会话管理：sessionStorage 按页面隔离，SPA 路由切换自动切换，多轮共享历史
 * - 快捷指令：按页面类型一键任务（分析当前数据 / 帮我填表 / 系统诊断）
 * - run_js 浏览器操作：执行后端下发脚本，POST /Ai/OperationResult 回传（AI-10）
 * - fill_form 表单填充：派发 cube:ai-fill-form 事件，表单页监听 setFieldsValue
 * - think 深度推理开关 + 最大化/清空会话
 * - 流式渲染 Markdown；结束后渲染 Mermaid 图表（CDN 懒加载）
 */
import { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Checkbox, Input, Space, Tooltip } from 'antd';
import {
  CloseOutlined,
  DeleteOutlined,
  FullscreenExitOutlined,
  FullscreenOutlined,
  RobotOutlined,
  SendOutlined,
} from '@ant-design/icons';
import { useLocation } from 'react-router-dom';
import { marked } from 'marked';
import DOMPurify from 'dompurify';
import { api } from '@/api';
import { renderMermaidBlocks } from './mermaidRenderer';

interface ChatMsg {
  id: number;
  type: 'user' | 'assistant';
  html: string;
}

interface ToolCard {
  id: string;
  name: string;
  status: 'start' | 'done' | 'error';
}

/** AI 助手配置（/Cube/GetAiConfig 返回，FastJson camelCase） */
interface AiConfigPayload {
  /** AI 总开关（兼容 PascalCase 旧键） */
  aISwitch?: boolean;
  AISwitch?: boolean;
  /** 主色 */
  aIPrimaryColor?: string;
  /** 辅色 */
  aISecondaryColor?: string;
  /** Mermaid 图表库 CDN 地址 */
  mermaidUrl?: string;
}

let seq = 0;

/** HTML 转义，防止用户输入被当作标记注入（用户消息回显时使用） */
function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
}

/** 页面会话键：/Admin/User → 'Admin/User' */
function pageKeyOf(pathname: string): string {
  return pathname.replace(/^\//, '') || '/';
}

/** 读取或创建当前页面的会话号（sessionStorage 按标签页×页面隔离，多轮对话共享历史） */
function loadSessionId(key: string): string {
  let sid = sessionStorage.getItem('cube-ai-session:' + key);
  if (!sid) {
    sid = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
    sessionStorage.setItem('cube-ai-session:' + key, sid);
  }
  return sid;
}

/** 序列化 run_js 脚本执行结果，处理循环引用/函数等无法 JSON 化的值 */
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

/** 是否表单类路径（含 ?id= 或 /Edit /Add /New /Detail 后缀，与 DefaultEntityPage 一致） */
function isFormPath(pathname: string, search: string): boolean {
  if (search.includes('id=')) return true;
  return /\/+(edit|add|new|detail)$/i.test(pathname);
}

/** 页面是否含表单（antd Form 渲染 <form class="ant-form">，字段控件无 name 属性，按容器判定）。
 * 覆盖配置页（ConfigPage/魔方设置）、表单页（FormPage）与列表弹窗表单（FormDialog） */
function hasFormControls(): boolean {
  return !!document.querySelector('form.ant-form, .ant-form');
}

/** 从 /Cube/GetAiConfig 加载 AI 配置：开关门控 + 配色 + Mermaid CDN 地址 */
async function loadAiConfig(): Promise<AiConfigPayload> {
  try {
    const res = await api.client.get('/Cube/GetAiConfig');
    return ((res.data as { data?: AiConfigPayload })?.data ?? {}) as AiConfigPayload;
  } catch {
    return {};
  }
}

export default function AiAssistant() {
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [tools, setTools] = useState<ToolCard[]>([]);
  const [streaming, setStreaming] = useState(false);
  const [think, setThink] = useState(false);
  /** AI 总开关：AISwitch 关闭时不渲染悬浮球（对齐 Vue） */
  const [enabled, setEnabled] = useState(false);
  /** 最大化状态，localStorage 持久化，重开面板自动恢复 */
  const [maximized, setMaximized] = useState(() => localStorage.getItem('cube-ai-maximized') === '1');
  const bodyRef = useRef<HTMLDivElement>(null);
  const mermaidUrlRef = useRef('');
  const sessionIdRef = useRef('');

  /** 页面会话键（路由切换时更新，驱动会话切换） */
  const [pageKey, setPageKey] = useState(() => pageKeyOf(location.pathname));

  // 页面类型上下文：list / form，及编辑态与记录编号
  const [pageKind, setPageKind] = useState<'list' | 'form'>('list');
  const [isEdit, setIsEdit] = useState(false);
  const [recordId, setRecordId] = useState(0);

  // 路由切换 → 切换会话键（组件常驻不重挂载）
  useEffect(() => {
    setPageKey(pageKeyOf(location.pathname));
  }, [location.pathname]);

  // 会话号随页面键切换
  useEffect(() => {
    sessionIdRef.current = loadSessionId(pageKey);
  }, [pageKey]);

  // 加载 AI 配置（开关门控 + Mermaid CDN）
  useEffect(() => {
    let cancelled = false;
    void loadAiConfig().then((cfg) => {
      if (cancelled) return;
      setEnabled(!!(cfg.aISwitch ?? cfg.AISwitch));
      mermaidUrlRef.current = cfg.mermaidUrl ?? '';
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // 页面上下文：URL + DOM 表单控件探测 → page/mode/id
  useEffect(() => {
    const form = isFormPath(location.pathname, location.search) || hasFormControls();
    setPageKind(form ? 'form' : 'list');
    const id = Number(new URLSearchParams(location.search).get('id') ?? 0) || 0;
    // 兼容 /Detail/{id} 路径
    const m = location.pathname.match(/\/Detail\/(\d+)/i);
    const rid = id || (m ? Number(m[1]) : 0);
    setRecordId(rid);
    setIsEdit(rid > 0 || /\/+(edit|detail)$/i.test(location.pathname));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname, location.search, open]);

  // 目标页面标识：/Admin/User → area=Admin, controller=User
  const urlParts = location.pathname.replace(/^\//, '').split('/');
  const targetController = urlParts[urlParts.length - 1] || '';
  const targetArea = urlParts.length > 1 ? urlParts.slice(0, -1).join('/') : '';

  const scrollBottom = () => {
    requestAnimationFrame(() => {
      if (bodyRef.current) bodyRef.current.scrollTop = bodyRef.current.scrollHeight;
    });
  };

  const renderMarkdown = (text: string): string => {
    const html = marked.parse(text, { breaks: true, gfm: true }) as string;
    return DOMPurify.sanitize(html);
  };

  /** 切换最大化，状态持久化，重开面板自动恢复 */
  const toggleMaximize = () => {
    setMaximized((m) => {
      const next = !m;
      localStorage.setItem('cube-ai-maximized', next ? '1' : '0');
      return next;
    });
    scrollBottom();
  };

  /** 清空当前页面会话（不影响其他页面） */
  const handleClear = () => {
    setMessages([]);
    setTools([]);
    const key = pageKeyOf(location.pathname);
    sessionStorage.removeItem('cube-ai-session:' + key);
    sessionIdRef.current = loadSessionId(key);
  };

  /** 回传浏览器操作结果到全局 AI 控制器，完成等待中的工具调用 */
  const postOperationResult = async (checkpointId: string, result: string) => {
    try {
      const token = api.tokenManager.getToken();
      await fetch('/Ai/OperationResult', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ checkpointId, result }),
      });
    } catch {
      // 回传失败忽略，后端会超时自动失败
    }
  };

  /** 处理后端下发的 run_js 事件：执行脚本并回传结果 */
  const handleRunJs = (json: Record<string, unknown>) => {
    const checkpointId = String(json.checkpointId ?? '');
    const script = String(json.script ?? '');
    let result: string;
    try {
      const fn = new Function(script);
      const v = fn();
      result = JSON.stringify({ ok: true, value: serializeResult(v) });
    } catch (e) {
      result = JSON.stringify({ ok: false, error: (e as Error)?.message || String(e) });
    }
    // 结果过大时截断，避免请求体膨胀
    if (result.length > 8192) result = result.substring(0, 8192);
    void postOperationResult(checkpointId, result);
  };

  /** 处理工具事件（规范协议：tool_call_start / tool_call_done / tool_call_error） */
  const handleToolEvent = (status: 'start' | 'done' | 'error', json: Record<string, unknown>) => {
    const toolId = String(json.toolCallId ?? json.id ?? '');
    const name = String(json.name ?? json.tool ?? '工具');
    // 后端对同一工具调用会发两次 tool_call_start（一次无参数、一次带参数），按 toolId 去重避免重复卡片
    setTools((prev) => {
      const exist = prev.find((t) => t.id === toolId);
      if (status === 'start') {
        if (exist) return prev.map((t) => (t.id === toolId ? { ...t, name: name || t.name, status: 'start' } : t));
        return [...prev, { id: toolId, name, status }];
      }
      return prev.map((t) => (t.id === toolId ? { ...t, status } : t));
    });
    // fill_form 完成 → 派发事件，宿主表单页监听后填充
    if (status === 'done' && name === 'fill_form' && json.result) {
      try {
        const data = JSON.parse(String(json.result)) as {
          kind?: string;
          values?: Record<string, unknown>;
          skipped?: string[];
        };
        if (data?.kind === 'fill_form' && data.values) {
          window.dispatchEvent(
            new CustomEvent('cube:ai-fill-form', { detail: { values: data.values, skipped: data.skipped ?? [] } }),
          );
        }
      } catch {
        // 解析失败忽略
      }
    }
  };

  const handleSend = async (prefill?: string) => {
    const text = (prefill ?? input).trim();
    if (!text || streaming) return;
    setInput('');
    setStreaming(true);

    // 用户消息需回显文本（转义后渲染），否则气泡为空、发送无反馈
    const userMsg: ChatMsg = { id: ++seq, type: 'user', html: escapeHtml(text) };
    const assistMsg: ChatMsg = { id: ++seq, type: 'assistant', html: '' };
    setMessages((prev) => [...prev, userMsg, assistMsg]);
    let full = '';

    const updateAssist = (html: string) => {
      setMessages((prev) => prev.map((m) => (m.id === assistMsg.id ? { ...m, html } : m)));
    };

    try {
      const token = api.tokenManager.getToken();
      const resp = await fetch('/Ai/AiChat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          sessionId: sessionIdRef.current,
          message: text,
          page: pageKind,
          mode: isEdit ? 'edit' : 'add',
          id: recordId,
          query: '',
          area: targetArea,
          controller: targetController,
          url: location.pathname,
          think,
        }),
      });
      if (!resp.ok) {
        // 非 2xx：读 JSON 或纯文本错误
        const body = await resp.text();
        let msg = `HTTP ${resp.status}`;
        if (body) {
          try {
            const data = JSON.parse(body) as { message?: string };
            if (data?.message) msg = data.message;
          } catch {
            // 非 JSON 响应体，保留 HTTP 状态码
          }
        }
        throw new Error(msg);
      }

      // 区分 SSE 与 JSON 错误：后端失败时返回 application/json（如 AISwitch 关闭），SSE 为 text/event-stream。
      // 若 HTTP 200 但非 SSE，整读 JSON 解析错误消息，避免"发消息无响应"静默吞错
      const ct = resp.headers.get('content-type') || '';
      if (!ct.includes('text/event-stream')) {
        const body = await resp.text();
        let msg = 'AI 请求失败';
        if (body) {
          try {
            const data = JSON.parse(body) as { message?: string };
            if (data?.message) msg = data.message;
          } catch {
            // 非 JSON 响应体，保留通用错误
          }
        }
        throw new Error(msg);
      }

      const reader = resp.body!.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      for (;;) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';
        for (const line of lines) {
          const t = line.trim();
          if (!t.startsWith('data: ')) continue;
          let json: Record<string, unknown>;
          try {
            json = JSON.parse(t.substring(6)) as Record<string, unknown>;
          } catch {
            continue;
          }
          if (!json) continue;
          if (json.type === 'content_delta') {
            full += String(json.content ?? '');
            updateAssist(renderMarkdown(full));
            scrollBottom();
          } else if (json.type === 'tool_call_start') {
            handleToolEvent('start', json);
          } else if (json.type === 'tool_call_done') {
            handleToolEvent('done', json);
          } else if (json.type === 'tool_call_error') {
            handleToolEvent('error', json);
          } else if (json.type === 'run_js') {
            handleRunJs(json);
          } else if (json.type === 'error') {
            updateAssist(`<span style="color:var(--cube-danger)">⚠️ ${String(json.message ?? 'AI 调用失败')}</span>`);
            scrollBottom();
          }
          // message_start / message_done / thinking_delta / heartbeat 规范事件无需处理，忽略
        }
      }
    } catch (err) {
      updateAssist(
        `<span style="color:var(--cube-danger)">⚠️ ${(err as Error)?.message || '请求失败'}</span><br/><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>`,
      );
    } finally {
      setTools((prev) => prev.map((t) => (t.status === 'start' ? { ...t, status: 'error', name: t.name + '（中断）' } : t)));
      setStreaming(false);
      // 渲染 Mermaid 图表
      requestAnimationFrame(() => {
        if (bodyRef.current) renderMermaidBlocks(bodyRef.current, mermaidUrlRef.current);
      });
    }
  };

  /** 快捷指令：按页面类型一键任务 */
  const quickActions = useCallback((): { label: string; prompt: string }[] => {
    if (pageKind === 'form') {
      return [
        { label: '📝 帮我填表', prompt: '帮我填写当前表单' },
        ...(isEdit ? [{ label: '🔍 分析当前记录', prompt: '分析当前记录' }] : []),
        { label: '🩺 系统诊断', prompt: '检查系统运行状态' },
      ];
    }
    return [
      { label: '📊 分析当前数据', prompt: '分析当前列表数据' },
      { label: '🩺 系统诊断', prompt: '检查系统运行状态' },
    ];
  }, [pageKind, isEdit]);

  // AISwitch 关闭时不渲染（对齐 Vue v-if="enabled"）；hook 已全部在顶部，早退安全
  if (!enabled) return null;

  return (
    <>
      {/* 悬浮按钮 */}
      {!open && (
        <Tooltip title="AI 助手">
          <Button
            type="primary"
            className="cube-ai-fab"
            shape="circle"
            icon={<RobotOutlined />}
            onClick={() => setOpen(true)}
          />
        </Tooltip>
      )}

      {/* 对话面板 */}
      {open && (
        <div className={`cube-ai-panel${maximized ? ' maximized' : ''}`}>
          {/* 头部 */}
          <div className="cube-ai-header">
            <span className="cube-ai-header-title">
              <RobotOutlined /> AI 助手
              <span className="cube-ai-header-path">{location.pathname}</span>
            </span>
            <Space size={2}>
              <Button
                type="text"
                size="small"
                className="cube-ai-header-btn"
                title={maximized ? '还原' : '最大化'}
                icon={maximized ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
                onClick={toggleMaximize}
              />
              <Button
                type="text"
                size="small"
                className="cube-ai-header-btn"
                title="清空会话"
                icon={<DeleteOutlined />}
                onClick={handleClear}
              />
              <Button type="text" size="small" className="cube-ai-header-close" icon={<CloseOutlined />} onClick={() => setOpen(false)} />
            </Space>
          </div>

          {/* 消息区 */}
          <div ref={bodyRef} className="cube-ai-body">
            {messages.length === 0 && (
              <div className="cube-ai-empty">
                <div className="cube-ai-empty-icon">
                  <RobotOutlined />
                </div>
                <p>我是 AI 助手，可以分析数据、填写表单、检查系统状态</p>
                <div className="cube-ai-quick">
                  {quickActions().map((q) => (
                    <Button key={q.prompt} size="small" onClick={() => void handleSend(q.prompt)}>
                      {q.label}
                    </Button>
                  ))}
                </div>
              </div>
            )}
            {messages.map((m) => (
              <div key={m.id} className={`cube-ai-row ${m.type === 'user' ? 'user' : ''}`}>
                <div
                  className={`cube-ai-bubble ${m.type === 'user' ? 'user' : 'assistant'}`}
                  dangerouslySetInnerHTML={{ __html: m.html || (m.type === 'assistant' && streaming ? '...' : '') }}
                />
              </div>
            ))}
            {/* 工具卡片 */}
            {tools.map((t) => (
              <div key={t.id} className="cube-ai-tool">
                {t.status === 'start' ? '⏳' : t.status === 'done' ? '✅' : '❌'} <b>{t.name}</b>{' '}
                {t.status === 'start' ? '调用中...' : t.status === 'done' ? '完成' : '失败'}
              </div>
            ))}
          </div>

          {/* 输入区 */}
          <div className="cube-ai-input">
            <div className="cube-ai-input-row">
              <Checkbox
                className="cube-ai-think"
                checked={think}
                onChange={(e) => setThink(e.target.checked)}
                title="深度推理更准确，但耗时更长"
              >
                深度
              </Checkbox>
              <Space.Compact style={{ flex: 1 }}>
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onPressEnter={() => void handleSend()}
                  placeholder="输入问题，Enter 发送"
                  disabled={streaming}
                />
                <Button type="primary" icon={<SendOutlined />} loading={streaming} onClick={() => void handleSend()} />
              </Space.Compact>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
