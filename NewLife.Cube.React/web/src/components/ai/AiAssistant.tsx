/**
 * AI 助手（右下角对话面板，对齐 Vue 皮肤 components/ai/AiAssistant.vue）
 *
 * 协议：POST /Ai/AiChat（body 携带 area/controller 目标页面）→ SSE
 *   {type: content_delta|tool_call_start|tool_call_done|tool_call_error|run_js|error}
 * 流式渲染 Markdown；结束后渲染 Mermaid 图表（CDN 懒加载）。
 */
import { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Input, Space, Tooltip, message } from 'antd';
import { CloseOutlined, RobotOutlined, SendOutlined } from '@ant-design/icons';
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

let seq = 0;

/** 从 /Cube/GetAiConfig 获取 Mermaid CDN 地址（失败回退公共 CDN） */
async function fetchMermaidUrl(): Promise<string> {
  try {
    const res = await api.client.get('/Cube/GetAiConfig');
    const data = res.data as { data?: { mermaidUrl?: string } };
    return data?.data?.mermaidUrl ?? '';
  } catch {
    return '';
  }
}

export default function AiAssistant() {
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [tools, setTools] = useState<ToolCard[]>([]);
  const [streaming, setStreaming] = useState(false);
  const bodyRef = useRef<HTMLDivElement>(null);
  const mermaidUrlRef = useRef('');

  useEffect(() => {
    void fetchMermaidUrl().then((u) => {
      mermaidUrlRef.current = u;
    });
  }, []);

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

  const handleToolEvent = (status: 'start' | 'done' | 'error', json: Record<string, unknown>) => {
    const toolId = String(json.toolCallId ?? json.id ?? '');
    const name = String(json.name ?? json.tool ?? '工具');
    if (status === 'start') {
      setTools((prev) => [...prev, { id: toolId, name, status }]);
    } else {
      setTools((prev) => prev.map((t) => (t.id === toolId ? { ...t, status } : t)));
    }
  };

  const handleSend = async () => {
    const text = input.trim();
    if (!text || streaming) return;
    setInput('');
    setStreaming(true);

    const userMsg: ChatMsg = { id: ++seq, type: 'user', html: '' };
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
          sessionId: '',
          message: text,
          page: 'list',
          mode: '',
          id: 0,
          query: '',
          area: targetArea,
          controller: targetController,
          url: location.pathname,
        }),
      });
      if (!resp.ok) {
        let msg = `HTTP ${resp.status}`;
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
          } else if (json.type === 'error') {
            updateAssist(`<span style="color:#c62828">⚠️ ${String(json.message ?? 'AI 调用失败')}</span>`);
            scrollBottom();
          }
        }
      }
    } catch (err) {
      updateAssist(
        `<span style="color:#c62828">⚠️ ${(err as Error)?.message || '请求失败'}</span><br/><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>`,
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
        <div className="cube-ai-panel">
          {/* 头部 */}
          <div className="cube-ai-header">
            <span className="cube-ai-header-title">
              <RobotOutlined /> AI 助手
              <span className="cube-ai-header-path">{location.pathname}</span>
            </span>
            <Button type="text" size="small" className="cube-ai-header-close" icon={<CloseOutlined />} onClick={() => setOpen(false)} />
          </div>

          {/* 消息区 */}
          <div ref={bodyRef} className="cube-ai-body">
            {messages.length === 0 && (
              <div className="cube-ai-empty">
                <div className="cube-ai-empty-icon">
                  <RobotOutlined />
                </div>
                <p>我是 AI 助手，可以回答关于当前页面的问题</p>
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
            <Space.Compact style={{ width: '100%' }}>
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
      )}
    </>
  );
}
