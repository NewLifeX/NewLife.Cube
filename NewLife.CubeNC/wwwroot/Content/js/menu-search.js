// 菜单搜索。数据随外壳页面下发，纯前端内存过滤，选择后定位模块与左侧菜单
(function () {
    'use strict';

    var MAX_RESULTS = 20;
    var MAX_RECENT = 12;
    var RECENT_KEY = 'cube-menu-search-recent';

    function normalize(text) {
        return (text || '').toString().toLowerCase();
    }

    function getTokens(keyword) {
        return normalize(keyword).split(/\s+/).filter(function (e) { return e.length > 0; });
    }

    function readRecent() {
        try {
            var list = JSON.parse(localStorage.getItem(RECENT_KEY) || '[]');
            return Array.isArray(list)
                ? list.filter(function (e) { return typeof e === 'string' && e; })
                : [];
        } catch (e) {
            return [];
        }
    }

    function writeRecent(url) {
        try {
            var list = readRecent().filter(function (e) { return e !== url; });
            list.unshift(url);
            if (list.length > MAX_RECENT) list.length = MAX_RECENT;
            localStorage.setItem(RECENT_KEY, JSON.stringify(list));
            return list;
        } catch (e) {
            return readRecent();
        }
    }

    function getHaystack(item) {
        return normalize([
            item.displayName,
            item.name,
            item.url,
            item.pinYin,
            item.initials,
            item.pathPinYin,
            item.pathInitials,
        ].join(' '));
    }

    function matchItem(item, tokens) {
        if (!tokens.length) return false;
        var haystack = getHaystack(item);
        for (var i = 0; i < tokens.length; i++) {
            if (haystack.indexOf(tokens[i]) < 0) return false;
        }
        return true;
    }

    function scoreItem(item, keyword) {
        var title = normalize(item.displayName);
        var name = normalize(item.name);
        var url = normalize(item.url);
        var initials = normalize(item.initials);
        var pinyin = normalize(item.pinYin);
        var pathInitials = normalize(item.pathInitials);
        var pathPinyin = normalize(item.pathPinYin);

        if (title === keyword) return 0;
        if (title.indexOf(keyword) === 0) return 1;
        if (initials.indexOf(keyword) === 0) return 2;
        if (pinyin.indexOf(keyword) === 0) return 3;
        if (name.indexOf(keyword) === 0) return 4;
        if (title.indexOf(keyword) >= 0) return 5;
        if (name.indexOf(keyword) >= 0) return 6;
        if (pathInitials.indexOf(keyword) === 0 || pathPinyin.indexOf(keyword) === 0) return 7;
        if (url.indexOf(keyword) >= 0) return 8;
        return 9;
    }

    function appendHighlighted(parent, text, keyword) {
        text = text || '';
        var index = keyword ? normalize(text).indexOf(keyword) : -1;
        if (index < 0) {
            parent.textContent = text;
            return;
        }

        parent.appendChild(document.createTextNode(text.substring(0, index)));
        var mark = document.createElement('mark');
        mark.textContent = text.substring(index, index + keyword.length);
        parent.appendChild(mark);
        parent.appendChild(document.createTextNode(text.substring(index + keyword.length)));
    }

    function init(root) {
        var dataEl = root.querySelector('.cube-menu-search-data');
        var input = root.querySelector('.cube-menu-search-input');
        var clear = root.querySelector('.cube-menu-search-clear');
        var panel = root.querySelector('.cube-menu-search-panel');
        var list = root.querySelector('.cube-menu-search-list');
        var empty = root.querySelector('.cube-menu-search-empty');
        if (!dataEl || !input || !panel || !list) return;

        var items = [];
        try {
            items = JSON.parse(dataEl.textContent || '[]') || [];
        } catch (e) {
            items = [];
        }

        var results = [];
        var activeIndex = -1;
        var recent = readRecent();

        function close() {
            panel.hidden = true;
            input.setAttribute('aria-expanded', 'false');
            activeIndex = -1;
            if (clear) clear.hidden = !input.value;
        }

        function open() {
            panel.hidden = false;
            input.setAttribute('aria-expanded', 'true');
            if (clear) clear.hidden = !input.value;
        }

        function select(item) {
            close();
            input.blur();
            recent = writeRecent(item.url);

            if (item.newWindow) {
                window.open(item.url, '_blank');
                return;
            }

            var mode = root.getAttribute('data-mode') || 'tab';
            var current = root.getAttribute('data-module') || 'base';
            var target = item.module || 'base';
            var shellUrl = root.getAttribute('data-shell') || window.location.pathname;

            if (mode === 'frame') {
                if (target === current) {
                    var frame = document.getElementById('main');
                    if (frame) frame.src = item.url;
                    if (typeof window.cubeLocateMenu === 'function') window.cubeLocateMenu(item.url);
                } else {
                    window.location.href = shellUrl + '?module=' + encodeURIComponent(target) + '&page=' + encodeURIComponent(item.url);
                }
                return;
            }

            if (target === current && typeof window.cubeAddTab === 'function') {
                window.cubeAddTab(item.url, item.displayName);
                if (typeof window.cubeLocateMenu === 'function') window.cubeLocateMenu(item.url);
                return;
            }

            // 跨模块先切换到目标模块，再由外壳的 hash 逻辑展开左侧菜单并打开标签。
            // 菜单地址本身以 / 开头，hash 只补 #，补成 #/ 会得到 #//Admin
            window.location.href = shellUrl + '?module=' + encodeURIComponent(target) + '#' + encodeURI(item.url);
        }

        function renderResults(keyword) {
            list.innerHTML = '';
            activeIndex = results.length ? 0 : -1;
            empty.hidden = results.length > 0 || !keyword;

            for (var j = 0; j < results.length; j++) {
                var item = results[j];
                var li = document.createElement('li');
                li.className = 'cube-menu-search-result' + (j === activeIndex ? ' is-active' : '');
                li.setAttribute('role', 'option');
                li.setAttribute('aria-selected', j === activeIndex ? 'true' : 'false');

                var name = document.createElement('span');
                name.className = 'cube-menu-search-name';
                appendHighlighted(name, item.displayName || item.name, keyword || '');

                var path = document.createElement('span');
                path.className = 'cube-menu-search-path';
                path.textContent = item.path || '';

                li.appendChild(name);
                li.appendChild(path);
                li.addEventListener('mousedown', function (e) {
                    e.preventDefault();
                    select(item);
                });
                li.addEventListener('mousemove', function () {
                    setActive(Array.prototype.indexOf.call(list.children, this));
                });

                list.appendChild(li);
            }

            // 有结果或已输入关键字时保持面板打开；无关键字且无最近使用时隐藏
            if (results.length || keyword) open(); else close();
        }

        function render() {
            var keyword = normalize(input.value).trim();
            var tokens = getTokens(keyword);

            results = [];
            if (tokens.length) {
                for (var i = 0; i < items.length; i++) {
                    if (matchItem(items[i], tokens)) results.push(items[i]);
                }

                var first = tokens[0];
                results.sort(function (a, b) {
                    // 最近使用优先，其次按匹配质量
                    var ra = recent.indexOf(a.url);
                    var rb = recent.indexOf(b.url);
                    if (ra >= 0 || rb >= 0) {
                        if (ra < 0) return 1;
                        if (rb < 0) return -1;
                        if (ra !== rb) return ra - rb;
                    }

                    var sa = scoreItem(a, first);
                    var sb = scoreItem(b, first);
                    if (sa !== sb) return sa - sb;
                    return (a.path || '').length - (b.path || '').length;
                });
                if (results.length > MAX_RESULTS) results.length = MAX_RESULTS;
            }

            renderResults(tokens[0] || '');
        }

        function renderRecent() {
            results = [];
            if (recent.length) {
                var byUrl = {};
                for (var i = 0; i < items.length; i++) {
                    if (!byUrl[items[i].url]) byUrl[items[i].url] = items[i];
                }

                for (var j = 0; j < recent.length && results.length < 8; j++) {
                    var item = byUrl[recent[j]];
                    if (item) results.push(item);
                }
            }

            renderResults('');
        }

        function setActive(index) {
            if (index < 0 || index >= results.length) return;
            activeIndex = index;
            for (var i = 0; i < list.children.length; i++) {
                var li = list.children[i];
                var active = i === index;
                li.classList.toggle('is-active', active);
                li.setAttribute('aria-selected', active ? 'true' : 'false');
                if (active) li.scrollIntoView({ block: 'nearest' });
            }
        }

        input.addEventListener('input', function () {
            if (input.value.trim()) render(); else renderRecent();
        });
        input.addEventListener('focus', function () {
            if (input.value.trim()) render(); else renderRecent();
        });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (panel.hidden) {
                    if (input.value.trim()) render(); else renderRecent();
                }
                setActive(Math.min(activeIndex + 1, results.length - 1));
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                setActive(Math.max(activeIndex - 1, 0));
            } else if (e.key === 'Enter') {
                if (!panel.hidden && results.length) {
                    e.preventDefault();
                    select(results[activeIndex >= 0 ? activeIndex : 0]);
                }
            } else if (e.key === 'Escape') {
                close();
                input.blur();
            }
        });

        if (clear) {
            clear.addEventListener('mousedown', function (e) {
                e.preventDefault();
                input.value = '';
                renderRecent();
                input.focus();
            });
        }

        document.addEventListener('mousedown', function (e) {
            if (!root.contains(e.target)) close();
        });

        close();
    }

    function initAll() {
        var nodes = document.querySelectorAll('.cube-menu-search');
        for (var i = 0; i < nodes.length; i++) init(nodes[i]);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
})();
