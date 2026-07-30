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
     * 执行 AI 洞察
     * @param {string} queryData - Base64 编码的查询条件
     * @param {boolean} think - 是否启用深度推理
     */
    CubeAI.insight = function (queryData, think) {
        var modal = $('#aiInsightModal');
        var content = $('#aiInsightContent');
        var subtitle = $('#aiInsightSubtitle');
        var statusEl = $('#aiInsightStatus');

        // 构建当前路径
        var path = window.location.pathname;
        // 构建 API URL
        var url = path.replace(/\/Index$/i, '') + '/AiInsight?_query=' + encodeURIComponent(queryData)
            + '&think=' + think
            + '&stream=true'
            + '&maxRows=100';

        // 重置弹窗
        content.html('<div class="text-center" style="padding: 40px 0;">'
            + '<i class="ace-icon fa fa-spinner fa-spin fa-3x" style="color: #667eea;"></i>'
            + '<p style="margin-top: 15px; color: #999;">AI 正在分析数据，请稍候...</p>'
            + '</div>');
        subtitle.text(think ? '深度分析模式 - 推理过程可视化' : '快速洞察模式');
        statusEl.text('');

        // 显示弹窗
        modal.modal('show');

        // 使用 fetch + ReadableStream 消费 SSE
        fetch(url, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('HTTP ' + response.status);
                }

                var reader = response.body.getReader();
                var decoder = new TextDecoder();
                var buffer = '';
                var resultText = '';
                var isFirstText = true;

                function processChunk() {
                    reader.read().then(function (_a) {
                        var done = _a.done, value = _a.value;

                        if (done) {
                            // 流结束
                            if (!resultText) {
                                content.html('<div class="text-center" style="padding: 40px 0; color: #999;">'
                                    + '<i class="ace-icon fa fa-inbox fa-3x"></i>'
                                    + '<p style="margin-top: 15px;">AI 未返回有效分析结果</p>'
                                    + '</div>');
                            }
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
                                    // 元数据事件
                                    var thinkingLabel = event_1.thinking ? '（深度推理中...）' : '';
                                    subtitle.text((think ? '深度分析模式' : '快速洞察模式') + ' - ' + (event_1.model || '') + thinkingLabel);
                                }
                                else if (event_1.type === 'text') {
                                    // 文本事件
                                    if (isFirstText) {
                                        content.empty();
                                        isFirstText = false;
                                    }
                                    resultText += event_1.content;
                                    content.html(renderMarkdown(resultText));
                                    // 自动滚动到底部
                                    content.scrollTop(content[0].scrollHeight);
                                }
                                else if (event_1.type === 'done') {
                                    statusEl.html('<i class="ace-icon fa fa-check-circle" style="color: #5cb85c;"></i> 分析完成');
                                }
                            }
                            catch (e) {
                                // JSON 解析失败，跳过
                            }
                        }

                        processChunk();
                    }).catch(function (err) {
                        console.error('AI Insight stream error:', err);
                        content.html('<div class="alert alert-danger">'
                            + '<i class="ace-icon fa fa-exclamation-circle"></i> 流读取失败：' + err.message
                            + '</div>');
                    });
                }

                processChunk();
            })
            .catch(function (err) {
                console.error('AI Insight fetch error:', err);
                content.html('<div class="alert alert-danger">'
                    + '<i class="ace-icon fa fa-exclamation-circle"></i> AI 洞察请求失败：' + err.message
                    + '<br><small>请确认 AI 服务已启用（系统设置中开启 AISwitch）</small>'
                    + '</div>');
            });
    };

    window.CubeAI = CubeAI;
})(window);
