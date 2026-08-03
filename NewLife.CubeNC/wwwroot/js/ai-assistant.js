/**
 * 魔方 AI 助手 JavaScript
 * 右下角对话面板：SSE 流式对话 + 工具调用可视化 + 表单智能填充
 * 依赖 jQuery（魔方主题均内置）
 */
(function (window) {
    'use strict';

    var CubeAI = window.CubeAI || {};

    /* ================= 轻量 Markdown 渲染 ================= */
    function renderMarkdown(text) {
        if (!text) return '';
        var html = text
            .replace(/^### (.+)$/gm, '<h3>$1</h3>')
            .replace(/^## (.+)$/gm, '<h2>$1</h2>')
            .replace(/^# (.+)$/gm, '<h1>$1</h1>')
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.+?)\*/g, '<em>$1</em>')
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            .replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>')
            .replace(/^---$/gm, '<hr>')
            .replace(/^- (.+)$/gm, '<li>$1</li>')
            .replace(/^\d+\. (.+)$/gm, '<li>$1</li>')
            .replace(/\n\n/g, '</p><p>')
            .replace(/\n/g, '<br>');
        html = '<p>' + html + '</p>';
        html = html.replace(/((?:<li>.*?<\/li><br>)+)/g, function (m) {
            return '<ul>' + m.replace(/<br>/g, '') + '</ul>';
        });
        html = html.replace(/((?:<blockquote>.*?<\/blockquote><br>)+)/g, function (m) {
            return m.replace(/<\/blockquote><br><blockquote>/g, '<br>').replace(/<br>/g, '');
        });
        return html;
    }

    /* ================= 状态 ================= */
    var streaming = false;
    var sessionId = localStorage.getItem('cube-ai-session');
    if (!sessionId) {
        sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
        localStorage.setItem('cube-ai-session', sessionId);
    }

    /* ================= 页面上下文 ================= */
    function getPageContext() {
        var get = function (id) {
            var el = document.getElementById(id);
            return el ? el.value : '';
        };
        var page = get('aiPage');
        var mode = get('aiMode');
        var query = get('aiQuery');
        var id = parseInt(get('aiId'), 10) || 0;
        // 从 URL 兜底解析记录编号
        if (!id) {
            var m = location.pathname.match(/\/(?:Detail|Edit)\/(\d+)/i);
            if (m) id = parseInt(m[1], 10);
        }
        return { page: page, mode: mode, query: query, id: id };
    }

    /* ================= 会话 UI ================= */
    function getEl(id) { return document.getElementById(id); }

    function scrollBottom() {
        var box = getEl('aiMessages');
        if (box) box.scrollTop = box.scrollHeight;
    }

    function appendUser(text) {
        var box = getEl('aiMessages');
        var div = document.createElement('div');
        div.className = 'ai-msg ai-msg-user';
        div.innerHTML = '<div class="ai-bubble"></div>';
        div.firstChild.textContent = text;
        box.appendChild(div);
        scrollBottom();
    }

    function appendAssistant() {
        var box = getEl('aiMessages');
        var div = document.createElement('div');
        div.className = 'ai-msg ai-msg-assistant';
        div.innerHTML = '<div class="ai-bubble"></div>';
        box.appendChild(div);
        return div.firstChild;
    }

    function appendTool(type, id, name, value) {
        var box = getEl('aiMessages');
        var div = document.createElement('div');
        div.className = 'ai-tool';
        div.setAttribute('data-tool-id', id);
        if (type === 'start') {
            div.innerHTML = '<span>🔧 正在调用 <b>' + name + '</b>...</span>';
        } else if (type === 'done') {
            div.innerHTML = '<span>✅ <b>' + name + '</b> 完成</span>';
            div.classList.add('ai-tool-done');
        } else {
            div.innerHTML = '<span>❌ <b>' + name + '</b> 失败</span>';
            div.classList.add('ai-tool-error');
        }
        // 若已存在同名工具卡片则更新，否则追加
        var old = box.querySelector('.ai-tool[data-tool-id="' + id + '"]');
        if (old) old.outerHTML = div.outerHTML;
        else box.appendChild(div);
        scrollBottom();

        // fill_form 完成 → 应用表单值
        if (type === 'done' && name === 'fill_form') {
            try {
                var data = JSON.parse(value);
                if (data && data.kind === 'fill_form' && data.values) {
                    var rs = applyFormValues(data.values);
                    showFillNotice(rs.count, rs.names, data.skipped || []);
                }
            } catch (e) { }
        }
    }

    /* ================= 表单智能填充 ================= */
    /**
     * 将 AI 返回的 {字段名:值} 填入当前表单控件。
     * 魔方表单所有控件均按 name=字段名 渲染（TextBox/CheckBox/DropDownList/textarea），
     * 因此可按字段名定位控件并按其类型填值。
     */
    function applyFormValues(values) {
        var count = 0, names = [];
        for (var name in values) {
            var v = values[name];
            var el = document.querySelector(
                'input[name="' + name + '"]:not([type=hidden]),' +
                'select[name="' + name + '"],' +
                'textarea[name="' + name + '"]'
            );
            if (!el) continue;

            var $el = jQuery(el);
            if (el.type === 'checkbox') {
                el.checked = !!v;
                // bootstrap-switch 开关同步
                if (jQuery && jQuery.fn && jQuery.fn.bootstrapSwitch && $el.parent().hasClass('bootstrap-switch')) {
                    $el.bootstrapSwitch('state', !!v);
                }
            } else if (el.tagName === 'SELECT') {
                el.value = String(v);
                if (jQuery) $el.trigger('change'); // select2/multiselect 监听 change
            } else if (el.tagName === 'TEXTAREA') {
                el.value = v == null ? '' : String(v);
                // 富文本编辑器实例同步（summernote 等）
                if (jQuery && jQuery.fn && jQuery.fn.summernote && $el.is(':hidden') && $el.next('.note-editor').length) {
                    $el.summernote('code', el.value);
                } else if (jQuery) {
                    $el.trigger('change');
                }
            } else {
                el.value = v == null ? '' : String(v);
                if (jQuery) $el.trigger('change');
            }
            // 高亮被填充字段
            el.classList.add('ai-field-highlight');
            setTimeout(function () { el.classList.remove('ai-field-highlight'); }, 3000);
            count++;
            names.push(name);
        }
        return { count: count, names: names };
    }

    function showFillNotice(count, names, skipped) {
        if (count <= 0) {
            appendAssistantNotice('AI 未能生成可用的表单值' + (skipped.length ? '（跳过：' + skipped.join('、') + '）' : '') + '，请检查后重试。');
            return;
        }
        var hint = 'AI 已预填 <b>' + count + '</b> 个字段（' + names.join('、') + '），请检查后点击保存提交。';
        if (skipped.length) hint += '<br><small>跳过：' + skipped.join('、') + '</small>';
        appendAssistantNotice(hint);
    }

    function appendAssistantNotice(html) {
        var bubble = appendAssistant();
        bubble.innerHTML = html;
    }

    /* ================= 发送消息 ================= */
    function sendMessage(text) {
        if (streaming) return;
        text = (text || '').trim();
        if (!text) return;
        streaming = true;

        var ctx = getPageContext();
        var think = getEl('aiThink') ? getEl('aiThink').checked : false;
        var quick = getEl('aiQuickActions');
        if (quick) quick.style.display = 'none';

        appendUser(text);
        var bubble = appendAssistant();
        var full = '';

        // 构造 AiChat 端点：去掉当前页面动作段（/Index /Add /Edit /Detail/{id}），
        // 得到控制器路径 + /AiChat；否则在表单/详情页会拼出 /Admin/User/Add/AiChat 这类错误路由
        var path = location.pathname
            .replace(/\/Index$/i, '')
            .replace(/\/(?:Add|Edit|Detail)(?:\/\d+)?$/i, '');
        var url = path + '/AiChat';

        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({
                sessionId: sessionId,
                message: text,
                page: ctx.page,
                mode: ctx.mode,
                id: ctx.id,
                query: ctx.query,
                think: think
            })
        }).then(function (response) {
            if (!response.ok) {
                return response.json().then(function (data) {
                    throw new Error((data && data.data) || ('HTTP ' + response.status));
                }).catch(function (e) {
                    if (e && e.message) throw e;
                    throw new Error('HTTP ' + response.status);
                });
            }
            var reader = response.body.getReader();
            var decoder = new TextDecoder();
            var buffer = '';
            var isFirst = true;

            function processChunk() {
                return reader.read().then(function (r) {
                    if (r.done) {
                        streaming = false;
                        // 标记仍在“正在调用”的工具卡片为中断（服务商可能不支持函数调用导致回合中断）
                        var pending = document.querySelectorAll('.ai-tool:not(.ai-tool-done):not(.ai-tool-error)');
                        for (var i = 0; i < pending.length; i++) {
                            pending[i].classList.add('ai-tool-error');
                            pending[i].innerHTML = '<span>⚠️ 工具调用中断（当前 AI 服务商可能不支持函数调用）</span>';
                        }
                        return;
                    }
                    buffer += decoder.decode(r.value, { stream: true });
                    var lines = buffer.split('\n');
                    buffer = lines.pop() || '';
                    for (var i = 0; i < lines.length; i++) {
                        var line = lines[i].trim();
                        if (line.indexOf('data: ') !== 0) continue;
                        var json = null;
                        try { json = JSON.parse(line.substring(6)); } catch (e) { continue; }
                        if (!json) continue;
                        if (json.type === 'text') {
                            if (isFirst) { bubble.innerHTML = ''; isFirst = false; }
                            full += json.content || '';
                            bubble.innerHTML = renderMarkdown(full);
                            scrollBottom();
                        } else if (json.type === 'tool') {
                            appendTool(json.event, json.id, json.name, json.value);
                        } else if (json.type === 'error') {
                            if (isFirst) { bubble.innerHTML = ''; isFirst = false; }
                            bubble.innerHTML = '<span style="color:#c62828">⚠️ ' + (json.message || 'AI 调用失败') + '</span>';
                        }
                    }
                    return processChunk();
                });
            }
            return processChunk();
        }).catch(function (err) {
            streaming = false;
            bubble.innerHTML = '<span style="color:#c62828">⚠️ ' + (err.message || '请求失败') + '</span><br><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>';
        });
    }

    /* ================= 初始化 ================= */
    function init() {
        var fab = getEl('aiAssistantFab');
        var panel = getEl('aiAssistantPanel');
        if (!fab || !panel) return;

        fab.addEventListener('click', function () {
            panel.style.display = panel.style.display === 'none' ? 'flex' : 'none';
        });
        var close = getEl('aiClosePanel');
        if (close) close.addEventListener('click', function () { panel.style.display = 'none'; });

        var clear = getEl('aiClearChat');
        if (clear) clear.addEventListener('click', function () {
            localStorage.removeItem('cube-ai-session');
            sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
            localStorage.setItem('cube-ai-session', sessionId);
            var box = getEl('aiMessages');
            if (box) {
                box.innerHTML = '<div class="ai-msg ai-msg-assistant"><div class="ai-bubble">会话已清空，有什么可以帮你？</div></div>';
                var quick = getEl('aiQuickActions');
                if (quick) quick.style.display = '';
            }
        });

        var input = getEl('aiInput');
        var send = getEl('aiSend');
        var doSend = function () { sendMessage(input.value); input.value = ''; input.style.height = 'auto'; };
        if (send) send.addEventListener('click', doSend);
        if (input) {
            input.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    doSend();
                }
            });
            input.addEventListener('input', function () {
                input.style.height = 'auto';
                input.style.height = Math.min(input.scrollHeight, 90) + 'px';
            });
        }

        // 快捷指令
        var chips = document.querySelectorAll('.ai-chip');
        for (var i = 0; i < chips.length; i++) {
            chips[i].addEventListener('click', function () {
                sendMessage(this.getAttribute('data-prompt') || this.textContent);
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.CubeAI = CubeAI;
})(window);
