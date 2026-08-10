/**
 * 魔方 AI 助手 JavaScript
 * 右下角对话面板：SSE 流式对话 + 工具调用可视化 + 表单智能填充
 * 依赖 jQuery（魔方主题均内置）
 */
(function (window) {
    'use strict';

    var CubeAI = window.CubeAI || {};

    /* ================= Markdown 渲染（marked 库，未加载时回退轻量渲染） ================= */
    /** HTML 转义，防止 AI 输出的 raw HTML 注入（XSS） */
    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /** 初始化 marked：GFM + 单换行断行 + XSS 防护 */
    var _markedReady = false;
    (function initMarked() {
        if (typeof window.marked === 'undefined' || !window.marked.parse) return;
        _markedReady = true;
        window.marked.use({
            gfm: true,
            breaks: true, // 单换行 → <br>，AI 流式输出友好
            renderer: {
                // 安全：AI 输出中的 raw HTML 一律转义显示，防止 XSS
                html: function (token) { return escapeHtml(token.text); },
                // 代码块：语言徽标 + 内容转义 + 深色样式
                code: function (token) {
                    var lang = (token.lang || '').match(/^\S+/);
                    lang = lang ? lang[0] : '';
                    var label = lang ? '<span class="ai-code-lang">' + escapeHtml(lang) + '</span>' : '';
                    var text = String(token.text).replace(/\n+$/, '');
                    return '<pre>' + label + '<code class="language-' + escapeHtml(lang) + '">' + escapeHtml(text) + '</code></pre>';
                }
            }
        });
    })();

    /** Markdown 渲染主入口：优先 marked 库，未加载时回退轻量渲染 */
    function renderMarkdown(text) {
        if (!text) return '';
        if (_markedReady && window.marked && window.marked.parse) {
            try { return window.marked.parse(text); } catch (e) { /* 解析失败回退 */ }
        }
        return renderMarkdownLight(text);
    }

    /* ================= 轻量 Markdown 渲染（marked 未加载时的回退） ================= */
    function renderMarkdownLight(text) {
        if (!text) return '';

        // 表格：先于行级替换处理多行块（| 表头 | + | --- | 分隔行 + 数据行）
        text = text.replace(/((?:^\|.*\|[ \t]*\n?)+)/gm, function (block) {
            var lines = block.replace(/\n+$/, '').split('\n');
            if (lines.length < 2) return block;
            // 第二行必须是分隔行（| --- | --- |，可含 : 对齐），否则不当作表格
            if (!/^\|[\s:|-]+\|$/.test(lines[1].trim())) return block;
            var rows = [];
            for (var i = 0; i < lines.length; i++) {
                var ln = lines[i].trim();
                if (ln.length < 2 || ln.charAt(0) !== '|' || ln.charAt(ln.length - 1) !== '|') return block;
                rows.push(ln);
            }
            // 解析分隔行对齐：:--- 右对齐、:---: 居中、--- 默认左对齐
            var align = [];
            var sep = rows[1].slice(1, -1).split('|');
            for (var k = 0; k < sep.length; k++) {
                var s = sep[k].trim();
                if (s.charAt(0) === ':' && s.charAt(s.length - 1) === ':') align.push('center');
                else if (s.charAt(s.length - 1) === ':') align.push('right');
                else align.push('');
            }
            function cells(line) {
                return line.slice(1, -1).split('|').map(function (s) { return s.trim(); });
            }
            function cell(tag, content, idx) {
                var style = align[idx] ? ' style="text-align:' + align[idx] + '"' : '';
                return '<' + tag + style + '>' + content + '</' + tag + '>';
            }
            var html = '<table><thead><tr>';
            var header = cells(rows[0]);
            for (var j = 0; j < header.length; j++) html += cell('th', header[j], j);
            html += '</tr></thead><tbody>';
            for (var r = 2; r < rows.length; r++) {
                var tds = cells(rows[r]);
                html += '<tr>';
                for (var c = 0; c < tds.length; c++) html += cell('td', tds[c], c);
                html += '</tr>';
            }
            return html + '</tbody></table>';
        });

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
        // 目标页面标识：由服务端 data-ai-area / data-ai-controller 注入，全局 AiController 据此解析目标控制器能力
        var container = getEl('aiAssistant');
        var area = container ? (container.getAttribute('data-ai-area') || '') : '';
        var controller = container ? (container.getAttribute('data-ai-controller') || '') : '';
        return { page: page, mode: mode, query: query, id: id, area: area, controller: controller };
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
     * 同步下拉插件表面 UI。bootstrap-multiselect 初始化后隐藏原生 select，仅改 value 不会刷新
     * 按钮文字与勾选状态，需调用插件 refresh；chosen 需 trigger('chosen:updated')；其余监听 change。
     * @param {jQuery} $el 下拉元素
     */
    function syncSelectPlugin($el) {
        if (!jQuery) return;
        if (jQuery.fn.multiselect && $el.data('multiselect')) {
            // bootstrap-multiselect：值写入隐藏原生 select，调用插件刷新重建按钮文字与勾选
            $el.multiselect('refresh');
        } else if (jQuery.fn.chosen && $el.hasClass('chosen-select')) {
            $el.trigger('chosen:updated');
        } else {
            $el.trigger('change'); // select2 等监听 change
        }
    }

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
                'textarea[name="' + name + '"],' +
                'input[name="' + name + '"][type=hidden]'
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
                if (el.multiple) {
                    // 多选：支持数组或逗号串，逐个匹配并置选中
                    var vals = (v instanceof Array ? v : String(v).split(',')).map(function (s) { return String(s).trim(); });
                    for (var i = 0; i < el.options.length; i++) {
                        el.options[i].selected = vals.indexOf(el.options[i].value) >= 0;
                    }
                } else {
                    el.value = String(v);
                }
                // 同步下拉插件 UI（bootstrap-multiselect 隐藏原生 select，需刷新表面按钮/勾选）
                syncSelectPlugin($el);
            } else if (el.tagName === 'TEXTAREA') {
                el.value = v == null ? '' : String(v);
                // EasyMDE（Markdown 编辑器）实例同步：原 textarea 被隐藏后仅改 value 不刷新 CodeMirror，
                // 且提交时 EasyMDE 会用自身内容覆盖 textarea，必须调用实例方法
                if (el._easymde) {
                    el._easymde.value(el.value);
                } else if (jQuery && jQuery.fn && jQuery.fn.summernote && $el.is(':hidden') && $el.next('.note-editor').length) {
                    // 富文本编辑器实例同步（summernote 等）
                    $el.summernote('code', el.value);
                } else if (jQuery) {
                    $el.trigger('change');
                }
            } else if (el.type === 'hidden') {
                // Quill（HTML 编辑器）：表单字段是隐藏域，值为 HTML。提交时 _HtmlEditor 会用
                // q.root.innerHTML 覆盖隐藏域，因此必须同步 Quill 实例，否则 AI 填入的值会丢失
                el.value = v == null ? '' : String(v);
                var container = document.getElementById('html_' + name);
                if (container && container._quill) {
                    container._quill.clipboard.dangerouslyPasteHTML(el.value || '<p><br></p>');
                }
                if (jQuery) $el.trigger('change');
            } else {
                el.value = v == null ? '' : String(v);
                if (jQuery) $el.trigger('change');
            }
            // 高亮被填充字段。隐藏域（Quill）高亮其编辑器容器，更直观；其余高亮控件本身
            var hl = el.type === 'hidden' ? document.getElementById('html_' + name) : el;
            if (!hl) hl = el;
            hl.classList.add('ai-field-highlight');
            // 用 IIFE 捕获当前元素：var 循环变量被所有 setTimeout 共享，循环末尾若遇无控件字段 continue 后 el 变为 null，
            // 3 秒后全部回调读取 null 抛异常（Cannot read properties of null (reading 'classList')）
            (function (target) {
                setTimeout(function () {
                    if (target) target.classList.remove('ai-field-highlight');
                }, 3000);
            })(hl);
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

    /* ================= 浏览器操作（run_js） ================= */
    /**
     * 获取 AI 对话端点 URL：统一由服务端注入 data-ai-url（恒为全局端点 /Ai/AiChat），
     * 目标页面标识经 data-ai-area / data-ai-controller 注入，POST 时随请求体发送。
     */
    function getAiChatUrl() {
        var container = getEl('aiAssistant');
        return container ? (container.getAttribute('data-ai-url') || '') : '';
    }

    /**
     * 获取浏览器操作回传端点：全局 AI 控制器 OperationResult（统一无区域前缀），
     * 所有实体页面共用，不在各实体控制器上重复增加接口
     */
    function getAiOperationUrl() {
        return '/Ai/OperationResult';
    }

    /** 序列化脚本执行结果，处理循环引用/函数等无法 JSON 化的值 */
    function serializeResult(v) {
        if (v === undefined) return 'undefined';
        if (v === null) return 'null';
        if (typeof v === 'function') return '[Function]';
        if (typeof v === 'symbol' || typeof v === 'bigint') return String(v);
        if (typeof v === 'object') {
            try {
                var s = JSON.stringify(v);
                return s === undefined ? String(v) : s;
            } catch (e) {
                return String(v);
            }
        }
        return JSON.stringify(v);
    }

    /** 处理后端下发的 run_js 事件：执行脚本并回传结果 */
    function handleRunJs(json) {
        var checkpointId = json.checkpointId;
        var script = json.script || '';
        var result;
        try {
            var fn = new Function(script);
            var v = fn();
            result = JSON.stringify({ ok: true, value: serializeResult(v) });
        } catch (e) {
            result = JSON.stringify({ ok: false, error: (e && e.message) || String(e) });
        }
        // 结果过大时截断，避免请求体膨胀
        if (result.length > 8192) result = result.substring(0, 8192);
        postOperationResult(checkpointId, result);
    }

    /** 回传浏览器操作结果到全局 AI 控制器，完成等待中的工具调用 */
    function postOperationResult(checkpointId, result) {
        fetch(getAiOperationUrl(), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({ checkpointId: checkpointId, result: result })
        }).catch(function () {
            // 回传失败忽略，后端会超时自动失败
        });
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

        // 构造 AiChat 端点：实体页面走控制器路径，非实体页面走服务端注入的全局端点
        var url = getAiChatUrl();

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
                area: ctx.area,
                controller: ctx.controller,
                think: think
            })
        }).then(function (response) {
            if (!response.ok) {
                // 用 text() 读取并手动解析，避免非 JSON 响应体（如 404 的 HTML 错误页、空响应）
                // 触发 "Unexpected end of JSON input" 这类解析异常，只向用户展示可读错误
                return response.text().then(function (text) {
                    var msg = 'HTTP ' + response.status;
                    if (text) {
                        try {
                            var data = JSON.parse(text);
                            var d = data && (data.data || data.message);
                            if (typeof d === 'string' && d) msg = d;
                        } catch (e) { /* 非 JSON 响应体，保留 HTTP 状态码 */ }
                    }
                    throw new Error(msg);
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
                        } else if (json.type === 'run_js') {
                            handleRunJs(json);
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

    /* ================= 面板拖动 ================= */
    /**
     * 拖动面板标题栏移动对话窗口位置。拖起时从 right/bottom 定位切换为 left/top，
     * 按视口约束防止拖出屏幕；不做位置持久化，重开面板回到右下角默认位置。
     * @param {HTMLElement} panel 面板元素
     * @param {HTMLElement} header 标题栏拖动手柄
     */
    function initPanelDrag(panel, header) {
        var dragging = false;
        var startX = 0, startY = 0, startLeft = 0, startTop = 0;
        header.addEventListener('mousedown', function (e) {
            // 排除清空/关闭等按钮
            if (e.target.closest('button')) return;
            // 全屏放大态不参与拖动
            if (panel.classList.contains('maximized')) return;
            dragging = true;
            var rect = panel.getBoundingClientRect();
            startX = e.clientX;
            startY = e.clientY;
            startLeft = rect.left;
            startTop = rect.top;
            // 从 right/bottom 切换为 left/top 接管定位
            panel.style.right = 'auto';
            panel.style.bottom = 'auto';
            panel.style.left = startLeft + 'px';
            panel.style.top = startTop + 'px';
            e.preventDefault();
        });
        document.addEventListener('mousemove', function (e) {
            if (!dragging) return;
            var left = startLeft + (e.clientX - startX);
            var top = startTop + (e.clientY - startY);
            // 约束：至少保留 60px 在视口内
            left = Math.max(-(panel.offsetWidth - 60), Math.min(left, window.innerWidth - 60));
            top = Math.max(0, Math.min(top, window.innerHeight - 40));
            panel.style.left = left + 'px';
            panel.style.top = top + 'px';
        });
        document.addEventListener('mouseup', function () { dragging = false; });
    }

    /* ================= 面板开关与最大化 ================= */
    /**
     * 打开面板：显示面板、隐藏悬浮球并下沉占位（panel-open 类），恢复上次最大化状态
     * @param {HTMLElement} panel 面板元素
     * @param {HTMLElement} container 悬浮球容器
     */
    function openPanel(panel, container) {
        panel.style.display = 'flex';
        container.classList.add('panel-open');
        // 重开面板恢复上次最大化状态
        var maximized = localStorage.getItem('cube-ai-maximized') === '1';
        panel.classList.toggle('maximized', maximized);
        var max = getEl('aiMaximize');
        if (max) {
            var expand = max.querySelector('.fa-expand');
            var compress = max.querySelector('.fa-compress');
            if (expand) expand.style.display = maximized ? 'none' : '';
            if (compress) compress.style.display = maximized ? '' : 'none';
        }
        scrollBottom();
    }

    /**
     * 关闭面板：隐藏面板，恢复悬浮球显示
     * @param {HTMLElement} panel 面板元素
     * @param {HTMLElement} container 悬浮球容器
     */
    function closePanel(panel, container) {
        panel.style.display = 'none';
        container.classList.remove('panel-open');
    }

    /**
     * 切换面板最大化状态：全屏占满视口（inset 20px）↔ 还原到默认右下角定位。
     * 状态写入 localStorage，重开面板自动恢复。
     * @param {HTMLElement} panel 面板元素
     * @param {Boolean} maximized 是否最大化
     */
    function toggleMaximize(panel, maximized) {
        // 清理拖动遗留的 inline 定位，避免与 CSS 定位冲突
        panel.style.left = '';
        panel.style.top = '';
        panel.style.right = '';
        panel.style.bottom = '';
        panel.classList.toggle('maximized', maximized);
        // 切换放大/还原图标
        var max = getEl('aiMaximize');
        if (max) {
            var expand = max.querySelector('.fa-expand');
            var compress = max.querySelector('.fa-compress');
            if (expand) expand.style.display = maximized ? 'none' : '';
            if (compress) compress.style.display = maximized ? '' : 'none';
        }
        localStorage.setItem('cube-ai-maximized', maximized ? '1' : '0');
        scrollBottom();
    }

    /* ================= 初始化 ================= */
    function init() {
        var fab = getEl('aiAssistantFab');
        var panel = getEl('aiAssistantPanel');
        if (!fab || !panel) return;
        var container = getEl('aiAssistant');

        fab.addEventListener('click', function () {
            if (panel.style.display === 'none' || !panel.style.display) openPanel(panel, container);
            else closePanel(panel, container);
        });
        var close = getEl('aiClosePanel');
        if (close) close.addEventListener('click', function () { closePanel(panel, container); });

        // 最大化/还原
        var maximize = getEl('aiMaximize');
        if (maximize) maximize.addEventListener('click', function () {
            toggleMaximize(panel, !panel.classList.contains('maximized'));
        });

        // 面板拖动：拖标题栏移动对话窗口位置（全屏态不参与拖动）
        var header = panel.querySelector('.ai-panel-header');
        if (header) initPanelDrag(panel, header);

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
