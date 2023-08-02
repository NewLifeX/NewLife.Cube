layui.define(['table', 'jquery', 'form', 'dtree', 'laypage'], function (exports) {
    "use strict";

    var MOD_NAME = 'treeTableExtend',
        $ = layui.jquery,
        table = layui.table,
        dtree = layui.dtree,
        laypage = layui.laypage,
        form = layui.form;

    var treeTableExtend = function () {
        this.v = '1.1.0';
    };

    /**
    * 初始化表格选择器
    */
    treeTableExtend.prototype.render = function (opt) {
        var elem = $(opt.elem);
        var tableDone = opt.table.done || function () { };
        var treeDone = opt.done || function () { };
        var btnDone = opt.btnDone || function () { };
        var initDone = opt.initDone || function () { };

        //默认设置
        opt.tree = opt.tree || {};
        opt.init = opt.init || false;
        opt.searchPlaceholder = opt.searchPlaceholder || '关键词搜索';
        opt.height = opt.height || 315;
        opt.table.page = opt.table.page || true;
        opt.table.cols = opt.table.cols || [[]];
        opt.table.limit = opt.table.limit || 10;
        opt.table.limits = opt.table.limits || [10, 20, 30, 50, 100];
        opt.table.toolbar = opt.table.toolbar || '';
        opt.table.defaultToolbar = opt.table.defaultToolbar || false;
        opt.table.cellMinWidth = opt.table.cellMinWidth || 100;
        opt.table.url = opt.table.url || '';
        opt.checkbar = opt.checkbar || false;
        opt.skin = opt.skin || "laySimple";
        opt.showAll = opt.showAll || false;
        opt.showSearch = opt.showSearch || false;
        opt.showToolBar = opt.showToolBar || false;
        opt.isEdit = opt.isEdit || false;
        opt.checked = opt.checked || false;
        opt.query = opt.query || '';
        opt.code = opt.code || 0;
        opt.css = opt.css || "display: inline-block; background: #fff;width: 100%;";

        var data = new Array();
        var tableName = opt.table.id || "tableSelect_table_" + new Date().getTime();
        var treeName = "tableTree_" + new Date().getTime();
        var toolbarDivName = "toolbarDiv_" + new Date().getTime();
        var pageName = "laypage_" + new Date().getTime();
        var searchInputName = "searchInput_" + new Date().getTime();
        var searchBtn = "search_btn_" + new Date().getTime();
        var getDataBtn = "getdata_btn" + new Date().getTime();
        var removeDataBtn = "removedata_btn" + new Date().getTime();
        var itemDivider = "margin: 5px 0; padding: 0; height: 0;  line-height: 0; border-bottom: 1px solid #eee; overflow: hidden;";
        var layuiItem = "position: relative;margin: 1px 0;line-height: 26px;color: rgba(0,0,0,.8);font-size: 14px;white-space: nowrap;transition: all .3s;";

        var tableBox = '<style> tr.layui-table-click{background-color: #dbfbf0!important;}</style>';
        tableBox += '<div class="layui-row" style="' + opt.css + '">'
        tableBox += '<div class="layui-col-xs2">';
        tableBox += '<div class="layui-tree">';
        tableBox += '<div style="' + layuiItem + '">';

        if (opt.showSearch) { 
            tableBox += '<div class="layui-input-inline">';
            tableBox += '<input class="layui-input" id="' + searchInputName + '" value="" placeholder="' + opt.searchPlaceholder + '">';
            tableBox += '</div>';
            tableBox += '<div class="layui-input-inline">';
            tableBox += '<button class="layui-btn layui-btn-primary" data-type="reload" type="submit" id="' + searchBtn + '"><i class="layui-icon">&#xe615;</i></button>';
            tableBox += '</div>';
        }
        if (opt.showToolBar) {

            tableBox += '<a class="layui-btn" id="' + getDataBtn + '"><i class="layui-icon">&#xe654;</i>提取数据</a><a class="layui-btn" id="' + removeDataBtn +'"><i class="layui-icon">&#xe640;</i>移除数据</a>';
        }

       tableBox += '</div>';
       tableBox += '<div style="'+itemDivider+'"></div>';
        tableBox += '<div style="' + layuiItem +'">';
       tableBox += '<div id="' + toolbarDivName + '" style="overflow: auto;height:' + opt.height + 'px;">';
       tableBox += '<ul id="' + treeName + '" class="dtree" data-id="0"></ul>';
       tableBox += '</div>';
       tableBox += '</div>';
       tableBox += '<div id="' + pageName + '"></div>';
       tableBox += '</div>';
       tableBox += '</div>';
       tableBox += '<div class="layui-col-xs10">';
       tableBox += '<table lay-filter="' + tableName + '" id="' + tableName + '"></table>';
       tableBox += '</div>';
        tableBox += '</div>';

        tableBox = $(tableBox);
        $(elem).html(tableBox);

        //渲染TREE
        opt.tree.elem = "#" + treeName;
        opt.tree.btn = "#" + searchBtn;
        opt.tree.search = "#" + searchInputName;


        opt.tree.getDataBtn = "#" + getDataBtn;
        opt.tree.removeDataBtn = "#" + removeDataBtn;

        table.init(tableName, {
            page: opt.table.page,
            limit: opt.table.limit,
            limits: opt.table.limits,
            toolbar: opt.table.toolbar,
            defaultToolbar: opt.table.defaultToolbar,
            cellMinWidth: opt.table.cellMinWidth,
            height: opt.height + 94,
            filterMethod: "from",
            data: [],
            cols: opt.table.cols,
            drag: { toolbar: true },
            filter: {
                items: ['column', 'data', 'condition', 'clearCache'],
                cache: false,
                bottom: false
            },
            done: function (res, curr, count) {
                tableDone(res, curr, count, opt.code);
            }
        });

        table.on('row(' + tableName + ')', function (obj) {
            var _data = obj.data;
            var tc = obj.tr.siblings().filter("tr.layui-table-click");
            layui.each(tc, function (index, item) {
                $(item).removeClass('layui-table-click');
            })
            obj.tr.addClass('layui-table-click');
        });

        function renderTable(field) {
            table.reload(tableName, {
                page: {
                    limit: opt.table.limit,
                    curr: 1
                },
                url: opt.table.url,
                where: field
            });
        }
        function loadData(page, limit, key) {
            var url = opt.url.indexOf('?') > -1 ? opt.url + '&' : opt.url + '?';
            if (!opt.isEdit) {
                if (url.substr(url.length - 1, 1) !== '&') {
                    url += opt.query + "&";
                }
            }
            $.get(url + 'page=' + page + '&limit=' + limit + '&key=' + key, function (res) {
                if (res.code == 0) {
                    var children = new Array();
                    layui.each(res.data, function (index, item) {
                        children.push({ id: item.code, title: item.name, last: true, parentId: 0 })
                    }); 
                    if (opt.showAll) {
                        data = [{ "id": "0", "title": "所有", "last": false, "parentId": "0", "children": children }];
                    } else {
                        data = children;
                    }
                    if (page == 1) {
                        loadPage(res.count, "");
                    }
                    opt.tree.data = data;
                    loadTree(data);
                    initDone(data);
                }

            });
        }
        function loadPage(count, value) {
            laypage.render({
                elem: pageName,
                count: count,
                groups: 1,
                limit: opt.table.limit,
                first: false,
                last: false,
                layout: ['prev', 'page', 'next', 'count'],
                jump: function (obj, first) {
                    // 首次不执行
                    if (!first) {
                        loadData(obj.curr, opt.table.limit, value);
                    }
                }
            });
        }
        function loadTree(data) {
            var DTree = dtree.render({
                elem: opt.tree.elem,
                data: data,
                checkbar: opt.checkbar,
                skin: opt.skin,
                done: function (data, obj, first) {
                    if (first) {
                        $(opt.tree.btn).unbind("click");
                        $(opt.tree.btn).click(function () {
                            var value = $(opt.tree.search).val();
                            loadData(1, opt.table.limit, value);
                            return false;
                        });
                    }
                }
            });
        }
        dtree.on("node(" + treeName + ")", function (obj) {
            var code = obj.param.nodeId;
            opt.checked = true;
            if (opt.table.url != undefined && opt.table.url !== '') {
                renderTable({ code: code, key: '' });
            }
            opt.code = code;
            opt.node = obj;
            treeDone(code);
        })

        $(opt.tree.getDataBtn).on('click', function () {
            opt.isEdit = false;
            opt.checked = false;
            loadData(1, opt.table.limit, '');
            btnDone(opt.tree.data, 0);
        })

        $(opt.tree.removeDataBtn).on('click', function () {
            var newData = []; 
            if (!opt.checked || opt.code === undefined || opt.code === '') return;
            layui.each(opt.tree.data, function (index, item) {
                var childData = [];
                if (item.id !== opt.code) {
                    if (item.children) {
                        layui.each(item.children, function (cindex, citem) {
                            if (citem.id !== opt.code) {
                                childData.push(citem);
                            }
                        })
                        item.children = childData;
                        newData.push(item);
                    } else {
                        newData.push(item);
                    }
                }
            });
            loadTree(newData);
            opt.tree.data = newData;
            btnDone(newData, 1);
            if (newData.length > 0) { 
                treeDone(newData[0].code);
            }
        })

        //加载或初始化
        if (opt.init) {
            renderTable({ code: 0, key: '' })
        }
        loadData(1, opt.table.limit, '');
        
    }
    //自动完成渲染
    var treeTableExtend = new treeTableExtend();


    exports(MOD_NAME, treeTableExtend);
})