/**
 * 魔方 AI 洞察 JavaScript
 * 处理 SSE 流式输出、Markdown 渲染和结果展示
 */
(function (window) {
    'use strict';

    var CubeAI = window.CubeAI || {};

    // 简单 Markdown 渲染器（不依赖外部库）
    function renderMarkdown(text) {
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
            // 标题
            .replace(/^### (.+)$/gm, '<h3>$1</h3>')
            .replace(/^## (.+)$/gm, '<h2>$1</h2>')
            // 粗体和斜体
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.+?)\*/g, '<em>$1</em>')
            // 行内代码
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            // 引用块
            .replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>')
            // 水平线
            .replace(/^---$/gm, '<hr>')
            // 列表项
            .replace(/^- (.+)$/gm, '<li>$1</li>')
            .replace(/^\d+\. (.+)$/gm, '<li>$1</li>')
            // 段落（连续非空行）
            .replace(/\n\n/g, '</p><p>')
            // 换行
            .replace(/\n/g, '<br>');

        // 包装段落
        html = '<p>' + html + '</p>';

        // 修复列表项（连续的 li 用 ul 包裹）
        html = html.replace(/((?:<li>.*?<\/li><br>)+)/g, function (match) {
            return '<ul>' + match.replace(/<br>/g, '') + '</ul>';
        });

        // 修复引用块（连续的 blockquote 合并）
        html = html.replace(/((?:<blockquote>.*?<\/blockquote><br>)+)/g, function (match) {
            return match.replace(/<\/blockquote><br><blockquote>/g, '<br>').replace(/<br>/g, '');
        });

        return html;
    }

    /**
     * 消费 SSE 流（fetch + ReadableStream），逐块解析 data: 事件并回调
     * @param {string} url - 请求地址
     * @param {Object} handlers - 事件回调 onMeta/onText/onDone/onEmpty/onError
     */
    function consumeSse(url, handlers) {
        fetch(url, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('HTTP ' + response.status);
                }

                // 非 SSE 响应（如 AISwitch 未开启时后端返回 JSON 错误），解析错误信息并回调
                var contentType = response.headers.get('Content-Type') || '';
                if (contentType.indexOf('application/json') >= 0) {
                    return response.json().then(function (data) {
                        var msg = (data && (data.data || data.message)) ? (data.data || data.message) : 'AI 服务返回异常';
                        if (handlers.onError) handlers.onError({ message: msg }, 'fetch');
                    }).catch(function () {
                        if (handlers.onError) handlers.onError({ message: 'AI 服务响应解析失败' }, 'fetch');
                    });
                }

                var reader = response.body.getReader();
                var decoder = new TextDecoder();
                var buffer = '';
                var resultText = '';

                function processChunk() {
                    reader.read().then(function (_a) {
                        var done = _a.done, value = _a.value;

                        if (done) {
                            // 流结束
                            if (!resultText && handlers.onEmpty) handlers.onEmpty();
                            if (handlers.onDone) handlers.onDone();
                            return;
                        }

                        buffer += decoder.decode(value, { stream: true });
                        var lines = buffer.split('\n');
                        // 最后一行可能不完整，保留
                        buffer = lines.pop() || '';

                        for (var i = 0; i < lines.length; i++) {
                            var line = lines[i].trim();
                            if (!line.startsWith('data: ')) continue;

                            var jsonStr = line.substring(6);
                            try {
                                var event_1 = JSON.parse(jsonStr);

                                if (event_1.type === 'meta') {
                                    if (handlers.onMeta) handlers.onMeta(event_1);
                                }
                                else if (event_1.type === 'text') {
                                    resultText += event_1.content;
                                    if (handlers.onText) handlers.onText(resultText, event_1.content);
                                }
                                else if (event_1.type === 'done') {
                                    if (handlers.onDone) handlers.onDone();
                                }
                            }
                            catch (e) {
                                // JSON 解析失败，跳过
                            }
                        }

                        processChunk();
                    }).catch(function (err) {
                        console.error('SSE stream error:', err);
                        if (handlers.onError) handlers.onError(err, 'stream');
                    });
                }

                processChunk();
            })
            .catch(function (err) {
                console.error('SSE fetch error:', err);
                if (handlers.onError) handlers.onError(err, 'fetch');
            });
    }

    /**
     * 执行 AI 系统诊断（流式输出，弹窗即时打开）
     */
    CubeAI.diagnose = function () {
        var modal = $('#aiDiagnoseModal');
        var content = $('#aiDiagnoseContent');
        var subtitle = $('#aiDiagnoseSubtitle');
        var statusEl = $('#aiDiagStatus');

        // 重置弹窗并立即显示
        content.html('<div class="text-center" style="padding: 40px 0;">'
            + '<i class="ace-icon fa fa-spinner fa-spin fa-3x" style="color: #667eea;"></i>'
            + '<p style="margin-top: 15px; color: #999;">AI 正在诊断系统，请稍候...</p>'
            + '</div>');
        subtitle.text('正在收集系统指标...');
        statusEl.html('<i class="ace-icon fa fa-spinner fa-spin"></i> 诊断中...');
        modal.modal('show');

        // 使用 fetch + ReadableStream 消费 SSE
        var isFirstText = true;
        consumeSse('/Admin/Index/AiDiagnose?stream=true', {
            onMeta: function (event_1) {
                subtitle.text('系统诊断 - ' + (event_1.model || ''));
            },
            onText: function (resultText) {
                // 文本事件，打字机效果
                if (isFirstText) {
                    content.empty();
                    isFirstText = false;
                }
                content.html(renderMarkdown(resultText));
                // 自动滚动到底部
                content.scrollTop(content[0].scrollHeight);
            },
            onDone: function () {
                statusEl.html('<i class="ace-icon fa fa-check-circle" style="color: #5cb85c;"></i> 诊断完成');
            },
            onEmpty: function () {
                content.html('<div class="text-center" style="padding: 40px 0; color: #999;">'
                    + '<i class="ace-icon fa fa-inbox fa-3x"></i>'
                    + '<p style="margin-top: 15px;">AI 未返回有效诊断结果</p>'
                    + '</div>');
            },
            onError: function (err) {
                console.error('AI Diagnose error:', err);
                content.html('<div class="alert alert-danger">'
                    + '<i class="ace-icon fa fa-exclamation-circle"></i> AI 诊断请求失败：' + err.message
                    + '<br><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>'
                    + '</div>');
                statusEl.text('');
            }
        });
    };

    window.CubeAI = CubeAI;
})(window);
