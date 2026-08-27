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

    /* ================= Mermaid 图表渲染（CDN 懒加载，移植 StarChat mermaidHelper） ================= */
    /** 修复 LLM 生成的 `&amp;` HTML 实体，在 Mermaid 代码中还原为 `&` */
    function fixAmpersandEntities(code) {
        return code.replace(/&amp;/g, '&');
    }
    /** 移除 LLM 生成的 `&nbsp;` HTML 实体（Mermaid 语法位置的无效 token） */
    function fixNbspInCode(code) {
        return code.replace(/&nbsp;/g, ' ');
    }
    /** 修复 LLM 将 Markdown 表格管道 `|` 与 Mermaid 连线 `-->` 混淆（行首 |--> → -->） */
    function fixPipeArrowConfusion(code) {
        return code.replace(/^(\s*)\|-->/gm, '$1-->');
    }
    /** 删除 LLM 幻想的"布局控制"伪指令行，如 layoutTB[隐藏默认连线方向] */
    function removePseudoLayoutDirectives(code) {
        return code.replace(/^\s*layout(?:TB|LR|RL|BT)\s*\[[^\n]*\n?/gim, '');
    }
    /** 修复 classDef 中 stroke-dasharray 值含空格的问题（保留第一个数值） */
    function fixClassDefStrokeDasharray(code) {
        return code.replace(/(stroke-dasharray)\s*:\s*(\d+(?:\.\d+)?)(?:\s+[\d.]+)+/g, '$1:$2');
    }
    /** 修复 classDef 应用语法（:::）中多余冒号和前导空格，`A[Node] :::::className` → `A[Node]:::className` */
    function fixClassApplicationColons(code) {
        return code.replace(/\s*(:{3,})\s*(\w+)/g, ':::$2');
    }
    /** 修复节点标签 [...] 中含有 `|` 的情况（PIPE 解析错误），自动包裹为 ["..."] */
    function fixPipesInNodeLabels(code) {
        return code.replace(/\[([^\[\]"]*\|[^\[\]"]*)\]/g, function (_, content) {
            return '["' + content + '"]';
        });
    }
    /** 将节点标签和边标签内的字面量 \n（反斜杠+n）转换为 <br/>，配合 htmlLabels 产生换行 */
    function fixNewlinesInLabels(code) {
        var br = '<br/>';
        return code
            .replace(/\(\(([^()]*)\)\)/g, function (_, c) { return '((' + c.replace(/\\n/g, br) + '))'; })
            .replace(/\(([^()]*)\)/g, function (_, c) { return '(' + c.replace(/\\n/g, br) + ')'; })
            .replace(/\["([^"]*)"\]/g, function (_, c) { return '["' + c.replace(/\\n/g, br) + '"]'; })
            .replace(/\[([^\[\]"]*)\]/g, function (_, c) { return '[' + c.replace(/\\n/g, br) + ']'; })
            .replace(/\|([^|]*)\|/g, function (_, c) { return '|' + c.replace(/\\n/g, br) + '|'; });
    }
    /** 移除 classDef 中 Mermaid 不支持的 SVG 几何属性（rx、ry 等） */
    function fixClassDefInvalidProps(code) {
        return code.replace(/^(\s*classDef\b[^\n]*)/gm, function (line) {
            return line.replace(/,\s*r[xy]\s*:\s*[\d.]+/g, '');
        });
    }
    /** 修复 LLM 将 <br/> 及追加文本写在节点标签括号之外的问题，收纳回括号内 */
    function fixBrAfterNodeLabel(code) {
        return code.replace(/\[([^\[\]"]+)\](<br\/>)([^:\n\[\]{}|]*)(:::[\w]+)?/g, function (_m, label, br, extra, cls) {
            return '["' + label + br + String(extra).trimEnd() + '"]' + (cls || '');
        });
    }
    /** 修复 LLM 在方括号节点标签内嵌入双引号的问题，替换为单引号 */
    function fixQuotesInBracketLabels(code) {
        return code.replace(/\[([^"\[\]\n{}<>|][^\[\]\n{}<>]*)\]/g, function (m, label) {
            return label.indexOf('"') >= 0 ? '[' + label.replace(/"/g, "'") + ']' : m;
        });
    }
    /** 修复 gantt 图的纯时间 dateFormat（如 HH:mm），补虚拟日期 2000-01-01，配合 axisFormat %H:%M 只显示时间 */
    function fixGanttTimeFormat(code) {
        if (!/^\s*gantt\b/m.test(code)) return code;
        var dfMatch = code.match(/^(\s*dateFormat\s+)(.+)$/m);
        if (!dfMatch) return code;
        var fmt = dfMatch[2].trim();
        if (/[YMDd]/.test(fmt)) return code;
        var dummyDate = '2000-01-01';
        var nextDate = '2000-01-02';
        code = code.replace(/^(\s*dateFormat\s+).+$/m, '$1YYYY-MM-DD ' + fmt);
        var lines = code.split('\n');
        var result = [];
        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            // 用 lastIndexOf 精准定位任务名与属性的分隔符 " :"
            var sepIdx = line.lastIndexOf(' :');
            if (sepIdx < 0) { result.push(line); continue; }
            var desc = line.substring(0, sepIdx);
            var attrs = line.substring(sepIdx + 2);
            // 收集属性中所有裸 HH:MM 及其位置
            var timeRe = /\b(\d{1,2}:\d{2})\b/g;
            var matches = [];
            var m;
            while ((m = timeRe.exec(attrs)) !== null) {
                matches.push({ full: m[0], time: m[1], index: m.index });
            }
            if (matches.length === 0) { result.push(line); continue; }
            // 最后一个时间 < 倒数第二个时视为跨午夜，用翌日
            var lastDate = dummyDate;
            if (matches.length >= 2) {
                var prev = matches[matches.length - 2].time;
                var last = matches[matches.length - 1].time;
                if (last < prev) lastDate = nextDate;
            }
            // 从右向左替换，避免 index 偏移
            var fixedAttrs = attrs;
            for (var j = matches.length - 1; j >= 0; j--) {
                var t = matches[j];
                var date = (j === matches.length - 1 && matches.length >= 2) ? lastDate : dummyDate;
                var replacement = date + ' ' + t.time;
                fixedAttrs = fixedAttrs.substring(0, t.index) + replacement + fixedAttrs.substring(t.index + t.full.length);
            }
            result.push(desc + ' : ' + fixedAttrs);
        }
        return result.join('\n');
    }
    /** 字符级扫描：转义标签上下文（引号/边标签/方括号/圆括号）内的花括号 {}，防止被误判为菱形节点 */
    function escapeBracesInLabels(code) {
        var stack = [];
        var result = '';
        for (var i = 0; i < code.length; i++) {
            var ch = code[i];
            var top = stack[stack.length - 1];
            var inQuoted = top === '"';
            if (ch === '"') {
                if (top === '"') stack.pop(); else stack.push('"');
                result += ch;
                continue;
            }
            if (inQuoted) {
                result += ch === '{' ? '&#123;' : (ch === '}' ? '&#125;' : ch);
                continue;
            }
            if (ch === '|') {
                if (top === '|') stack.pop(); else stack.push('|');
                result += ch;
                continue;
            }
            if (ch === '[' || ch === '(') { stack.push(ch); result += ch; continue; }
            if (ch === ']' && top === '[') { stack.pop(); result += ch; continue; }
            if (ch === ')' && top === '(') { stack.pop(); result += ch; continue; }
            if ((ch === '{' || ch === '}') && stack.length > 0) {
                result += ch === '{' ? '&#123;' : '&#125;';
                continue;
            }
            result += ch;
        }
        return result;
    }
    /** 规范化 Mermaid 代码：修复 LLM 常见生成缺陷（与 StarChat mermaidHelper.normalizeMermaidCode 同套） */
    function normalizeMermaidCode(code) {
        code = fixGanttTimeFormat(code);
        code = fixAmpersandEntities(code);
        code = fixNbspInCode(code);
        code = fixPipeArrowConfusion(code);
        code = removePseudoLayoutDirectives(code);
        code = fixClassDefStrokeDasharray(code);
        code = fixClassApplicationColons(code);
        code = fixClassDefInvalidProps(code);
        code = fixPipesInNodeLabels(code);
        code = fixNewlinesInLabels(code);
        code = fixBrAfterNodeLabel(code);
        code = fixQuotesInBracketLabels(code);
        code = escapeBracesInLabels(code);
        return code;
    }
    /** XSS 防护：剥离 Mermaid 代码中的 script/iframe 等标签与 on* 事件属性（securityLevel 为 loose 时允许 HTML） */
    function sanitizeMermaidCode(code) {
        return String(code)
            .replace(/<\/?(?:script|iframe|object|embed|form|base|link|meta|applet)(\s[^>]*)?>/gi, '')
            .replace(/\son\w+\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, '');
    }
    /* Mermaid 库懒加载状态：只加载一次，多个图表块并发共享 */
    var _mermaidLoading = null;
    var _mermaidInit = false;
    /** 按需从 CDN 加载 mermaid 并执行一次性初始化（地址来自服务端注入的 CubeAI_MermaidUrl） */
    function getMermaid() {
        if (window.mermaid && window.mermaid.render) return Promise.resolve(window.mermaid);
        if (_mermaidLoading) return _mermaidLoading;
        var url = window.CubeAI_MermaidUrl || 'https://registry.npmmirror.com/mermaid/11.12.3/files/dist/mermaid.min.js';
        _mermaidLoading = new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = url;
            s.async = true;
            s.onload = function () {
                try {
                    if (!window.mermaid) throw new Error('mermaid 未暴露全局变量');
                    if (!_mermaidInit) {
                        window.mermaid.initialize({ startOnLoad: false, theme: 'default', securityLevel: 'loose' });
                        _mermaidInit = true;
                    }
                    resolve(window.mermaid);
                } catch (e) { _mermaidLoading = null; reject(e); }
            };
            s.onerror = function () { _mermaidLoading = null; reject(new Error('mermaid 加载失败: ' + url)); };
            document.head.appendChild(s);
        });
        return _mermaidLoading;
    }
    /** 解析可渲染的 Mermaid 代码：先原始、后规范化，用 mermaid.parse 校验，均失败返回 null */
    function resolveRenderableMermaidCode(mermaid, code) {
        var candidates = [code];
        var normalized = normalizeMermaidCode(code);
        if (normalized !== code) candidates.push(normalized);
        var chain = Promise.resolve(null);
        for (var i = 0; i < candidates.length; i++) {
            (function (candidate) {
                chain = chain.then(function (found) {
                    if (found) return found;
                    return mermaid.parse(candidate, { suppressErrors: true }).then(function (ok) {
                        // parse 对非法代码解析为 false（不抛异常），必须校验布尔结果
                        return ok ? candidate : null;
                    }).catch(function () { return null; });
                });
            })(candidates[i]);
        }
        return chain;
    }
    var _mermaidSeq = 0;
    /** 回退：将图表占位还原为源码代码块展示 */
    function restoreMermaidSource(holder, raw) {
        var pre = document.createElement('pre');
        var code = document.createElement('code');
        code.className = 'language-mermaid';
        code.textContent = raw;
        pre.appendChild(code);
        holder.parentNode.replaceChild(pre, holder);
    }
    /** 渲染单个 mermaid 代码块为 SVG，失败回退源码 */
    function renderMermaidBlock(codeEl, mermaid) {
        var pre = codeEl.parentNode;
        if (!pre) return;
        var raw = (codeEl.textContent || '').replace(/\n+$/, '');
        var holder = document.createElement('div');
        holder.className = 'ai-mermaid';
        holder.textContent = '⏳ 图表渲染中...';
        pre.parentNode.replaceChild(holder, pre);

        resolveRenderableMermaidCode(mermaid, sanitizeMermaidCode(raw)).then(function (renderable) {
            if (!renderable) { restoreMermaidSource(holder, raw); return; }
            var id = 'ai-mermaid-' + (++_mermaidSeq);
            mermaid.render(id, renderable).then(function (rs) {
                var svg = rs && rs.svg;
                if (!svg) { restoreMermaidSource(holder, raw); return; }
                holder.innerHTML = svg;
                var svgEl = holder.querySelector('svg');
                if (svgEl && svgEl.getAttribute) {
                    // 从 viewBox 推算自然宽度，避免 SVG 被强制拉伸到容器宽度
                    var vb = (svgEl.getAttribute('viewBox') || '').trim().split(/[\s,]+/);
                    if (vb.length >= 4) {
                        var w = parseFloat(vb[2]);
                        if (w > 0) svgEl.setAttribute('width', String(Math.ceil(w)));
                    }
                    svgEl.style.maxWidth = '100%';
                    svgEl.style.height = 'auto';
                    svgEl.style.display = 'block';
                }
                // 源码切换：<details> 展开查看原始 mermaid 源码
                var details = document.createElement('details');
                var summary = document.createElement('summary');
                summary.textContent = '查看源码';
                details.appendChild(summary);
                var src = document.createElement('pre');
                src.textContent = raw;
                details.appendChild(src);
                holder.appendChild(details);
            }).catch(function () { restoreMermaidSource(holder, raw); });
        });
    }
    /** 渲染容器内所有 mermaid 代码块为 SVG。CDN 加载失败时保持源码块展示 */
    function renderMermaidBlocks(container) {
        if (!container) return;
        var codes = container.querySelectorAll('pre code.language-mermaid');
        if (!codes.length) return;
        getMermaid().then(function (mermaid) {
            for (var i = 0; i < codes.length; i++) {
                renderMermaidBlock(codes[i], mermaid);
            }
        }).catch(function () {
            // CDN 加载失败：保持源码块展示，无需额外处理
        });
    }

    /* ================= 状态 ================= */
    var streaming = false;
    // 会话按页面隔离：sessionStorage（每标签页独立）+ 页面路径作用域。不同页面互不串话，同页多轮共享，刷新/返回恢复
    var sessionKey = 'cube-ai-session:' + (location.pathname || '/');
    var sessionId = sessionStorage.getItem(sessionKey);
    if (!sessionId) {
        sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
        sessionStorage.setItem(sessionKey, sessionId);
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
                url: location.pathname,
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
                        // AI 回复结束后渲染 Mermaid 图表（流式期间保持源码展示，避免逐字重渲染）
                        renderMermaidBlocks(bubble);
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
                        if (json.type === 'content_delta') {
                            if (isFirst) { bubble.innerHTML = ''; isFirst = false; }
                            full += json.content || '';
                            bubble.innerHTML = renderMarkdown(full);
                            scrollBottom();
                        } else if (json.type === 'tool_call_start') {
                            appendTool('start', json.toolCallId, json.name, json.arguments);
                        } else if (json.type === 'tool_call_done') {
                            appendTool('done', json.toolCallId, json.name, json.result);
                        } else if (json.type === 'tool_call_error') {
                            appendTool('error', json.toolCallId, json.name, json.error);
                        } else if (json.type === 'run_js') {
                            handleRunJs(json);
                        } else if (json.type === 'error') {
                            if (isFirst) { bubble.innerHTML = ''; isFirst = false; }
                            bubble.innerHTML = '<span style="color:#c62828">⚠️ ' + (json.message || 'AI 调用失败') + '</span>';
                        }
                        // message_start / message_done / thinking_delta / heartbeat 规范事件无需处理，忽略
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
            // 清空仅作用于当前页面会话，不影响其他页面
            sessionStorage.removeItem(sessionKey);
            sessionId = 's' + Date.now() + Math.random().toString(16).substring(2, 8);
            sessionStorage.setItem(sessionKey, sessionId);
            var box = getEl('aiMessages');
            if (box) {
                // 快捷指令容器（#aiQuickActions）是 #aiMessages 的子元素，直接替换 innerHTML 会把快捷指令一并销毁，
                // 清空会话后快捷指令永久丢失。只移除消息气泡并插入欢迎语，保留快捷指令容器
                var msgs = box.querySelectorAll('.ai-msg');
                for (var i = 0; i < msgs.length; i++) msgs[i].remove();
                box.insertAdjacentHTML('afterbegin', '<div class="ai-msg ai-msg-assistant"><div class="ai-bubble">会话已清空，有什么可以帮你？</div></div>');
                var quick = getEl('aiQuickActions');
                if (quick) quick.style.display = '';
                refreshQuickActions();
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

        // 按页面类型适配快捷指令可见性（实体列表/表单/详情 vs 非实体页面）
        refreshQuickActions();
    }

    /**
     * 按页面类型刷新快捷指令可见性，避免误导：
     * - 实体列表页（#aiPage=list）：分析当前数据 / 系统诊断
     * - 实体表单页（#aiPage=form）：帮我填表 /（编辑模式）分析当前记录 / 系统诊断
     * - 实体详情页（#aiPage=detail）：分析当前记录 / 系统诊断
     * - 非实体页面（无 #aiPage）：
     *   - 含表单控件的配置表单页（如魔方设置）：帮我填表 / 分析当前数据（页面分析）/ 系统诊断
     *   - 其他无表单页（如服务器信息/数据库信息）：分析当前数据（页面分析）/ 系统诊断
     * 系统诊断在任何页面均可用。
     */
    function refreshQuickActions() {
        var quick = getEl('aiQuickActions');
        if (!quick) return;
        var ctx = getPageContext();
        var hasForm = hasFormControls();
        var chips = quick.querySelectorAll('.ai-chip');
        for (var i = 0; i < chips.length; i++) {
            var c = chips[i];
            var prompt = c.getAttribute('data-prompt') || '';
            var show = true;
            if (prompt === '分析当前列表数据') {
                // 实体列表页或非实体页（无 #aiPage，按页面分析处理）；表单/详情页隐藏
                show = (ctx.page === 'list' || ctx.page === '');
                // 非实体页面：提示词改为"分析当前页面"，引导 LLM 调用 get_page_context 分析页面内容
                c.setAttribute('data-prompt', ctx.page === '' ? '分析当前页面内容' : '分析当前列表数据');
            } else if (prompt === '帮我填写当前表单') {
                // 实体表单页，或含表单控件的非实体配置页（如魔方设置）——均为可填表单
                show = (ctx.page === 'form' || (ctx.page === '' && hasForm));
            } else if (prompt === '分析当前记录') {
                // 详情页恒显示；编辑表单页显示（有当前记录）；新增表单与列表页隐藏
                show = (ctx.page === 'detail' || (ctx.page === 'form' && ctx.mode === 'edit'));
            }
            c.style.display = show ? '' : 'none';
        }
    }

    /**
     * 检测当前页面是否含可填表单控件（input/select/textarea 且带 name）。
     * 用于区分配置表单页（魔方设置等）与普通展示页（服务器信息/数据库信息）。
     * @returns {Boolean} 是否含表单控件
     */
    function hasFormControls() {
        return document.querySelectorAll('input[name], select[name], textarea[name]').length > 0;
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.CubeAI = CubeAI;
})(window);
