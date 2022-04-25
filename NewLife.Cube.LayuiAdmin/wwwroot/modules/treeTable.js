/** 树形表格3.x Created by wangfan on 2020-05-12 https://gitee.com/whvse/treetable-lay */

layui.define(['laytpl', 'form', 'util'], function (exports) {
    var $ = layui.jquery;
    var laytpl = layui.laytpl;
    var form = layui.form;
    var util = layui.util;
    var device = layui.device();
    var MOD_NAME = 'treeTable';  // 模块名
    var _instances = {};  // 记录所有实例

    /* 表格默认参数 */
    var defaultOption = {
        elem: undefined,                                 // 容器
        cols: undefined,                                 // 列参数
        url: undefined,                                  // url模式请求
        method: undefined,                               // url模式请求方式
        where: undefined,                                // url模式请求条件
        contentType: undefined,                          // url模式请求类型
        headers: undefined,                              // url模式请求headers
        parseData: undefined,                            // url模式处理请求数据
        request: {pidName: 'pid'},                       // url模式请求字段自定义
        toolbar: undefined,                              // 表头工具栏
        defaultToolbar: undefined,                       // 表头工具栏右侧按钮
        width: undefined,                                // 容器宽度
        height: undefined,                               // 容器高度
        cellMinWidth: 90,                                // 单元格最小宽度
        done: undefined,                                 // 数据处理完回调
        data: undefined,                                 // 直接赋值数据
        title: undefined,                                // 定义table大标题，文件导出会用到
        skin: undefined,                                 // 表格风格
        even: undefined,                                 // 是否开启隔行变色
        size: undefined,                                 // 表格尺寸
        statusCode: 0,                                   // 数据返回值代码
        text: {
            none: '无数据'                               // 空数据提示
        },
        reqData: undefined,                              // 自定义加载数据方法
        useAdmin: false,                                  // 是否使用admin.ajax
        tree: {
            idName: 'id',                                // id的字段名
            pidName: 'pid',                              // pid的字段名
            childName: 'children',                       // children的字段名
            haveChildName: 'haveChild',                  // 是否有children标识的字段名
            haveChildReverse: false,                     // 是否将children标识的字段取反
            openName: 'open',                            // 是否默认展开的字段名
            iconIndex: 0,                                // 图标列的索引
            arrowType: undefined,                        // 折叠箭头类型
            onlyIconControl: undefined,                  // 仅点击图标控制展开折叠
            getIcon: function (d) {                      // 自定义图标
                var haveChild = d[this.haveChildName];
                if (haveChild !== undefined) {
                    haveChild = haveChild === true || haveChild === 'true';
                    if (this.haveChildReverse) haveChild = !haveChild;
                }
                else if (d[this.childName]) haveChild = d[this.childName].length > 0;
                if (haveChild) return '<i class="ew-tree-icon layui-icon layui-icon-layer"></i>';
                else return '<i class="ew-tree-icon layui-icon layui-icon-file"></i>';
            }
        }
    };
    /* 列默认参数 */
    var colDefaultOption = {
        field: undefined,     // 字段名
        title: undefined,     // 标题
        width: undefined,     // 宽度
        minWidth: undefined,  // 最小宽度
        type: 'normal',       // 列类型
        fixed: undefined,     // 固定列
        hide: undefined,      // 是否初始隐藏列
        unresize: undefined,  // 禁用拖拽列宽
        style: undefined,     // 单元格样式
        align: undefined,     // 对齐方式
        colspan: undefined,   // 单元格所占的列数
        rowspan: undefined,   // 单元格所占的行数
        templet: undefined,   // 自定义模板
        toolbar: undefined,   // 工具列
        'class': undefined,   // 单元格class
        singleLine: undefined // 是否一行显示
    };

    /** TreeTable类构造方法 */
    var TreeTable = function (options) {
        _instances[options.elem.substring(1)] = this;
        this.reload(options);
    };

    /**
     * 根据ID查找数据
     * @param id 数据条目的ID
     */
    TreeTable.prototype.findDataById = function (id) {
        var options = this.options;
        function each(data) {
            for (var i = 0; i < data.length; i++) {
                if (data[i][options.tree.idName] === id) return data[i];
                if (data[i][options.tree.childName]) {
                    var res = each(data[i][options.tree.childName]);
                    if (res) return res;
                }
            }
        }
        return each(options.data);
    }

    /** 参数设置 */
    TreeTable.prototype.initOptions = function (opt) {
        var that = this;

        // 处理特殊列
        function initCol(item) {
            if (!item.INIT_OK) item = $.extend({INIT_OK: true}, colDefaultOption, item);
            // 特殊列处理
            if (item.type === 'space') {  // 空列
                if (!item.width) item.width = 15;
                item.minWidth = item.width;
            } else if (item.type === 'numbers') {  // 序号列
                if (!item.width) item.width = 40;
                item.minWidth = item.width;
                if (!item.singleLine) item.singleLine = false;
                if (!item.unresize) item.unresize = true;
                if (!item.align) item.align = 'center';
            } else if (item.type === 'checkbox' || item.type === 'radio') {  // 复/单选框列
                if (!item.width) item.width = 48;
                item.minWidth = item.width;
                if (!item.singleLine) item.singleLine = false;
                if (!item.unresize) item.unresize = true;
                if (!item.align) item.align = 'center';
            }
            if (item.toolbar) item.type = 'tool';
            return item;
        }

        // 初始化列参数
        if ('Array' !== isClass(opt.cols[0])) opt.cols = [opt.cols];

        // 恢复cols参数初始状态
        for (var m = 0; m < opt.cols.length; m++) {
            for (var n = 0; n < opt.cols[m].length; n++) {
                opt.cols[m][n].INIT_OK = undefined;
                opt.cols[m][n].key = undefined;
                opt.cols[m][n].colGroup = undefined;
                opt.cols[m][n].HAS_PARENT = undefined;
                opt.cols[m][n].parentKey = undefined;
                opt.cols[m][n].PARENT_COL_INDEX = undefined;
            }
        }

        // cols参数处理
        var colArrays = [], colIndex = 0;
        for (var i1 = 0; i1 < opt.cols.length; i1++) {
            var item1 = opt.cols[i1];
            for (var i2 = 0; i2 < item1.length; i2++) {
                var item2 = item1[i2];
                if (!item2) {
                    item1.splice(i2, 1);
                    continue;
                }
                item2 = initCol(item2);
                // 合并单元格处理
                item2.key = i1 + '-' + i2;
                var CHILD_COLS = undefined;
                if (item2.colGroup || item2.colspan > 1) {
                    item2.colGroup = true;
                    item2.type = 'group';
                    CHILD_COLS = [];
                    colIndex++;
                    var childIndex = 0;
                    for (var i22 = 0; i22 < opt.cols[i1 + 1].length; i22++) {
                        var item22 = $.extend({INIT_OK: true}, colDefaultOption, opt.cols[i1 + 1][i22]);
                        if (item22.HAS_PARENT || (childIndex > 1 && childIndex == item2.colspan)) {
                            opt.cols[i1 + 1][i22] = item22;
                            continue;
                        }
                        item22.HAS_PARENT = true;
                        item22.parentKey = i1 + '-' + i2;
                        item22.key = (i1 + 1) + '-' + i22;
                        item22.PARENT_COL_INDEX = colIndex;
                        item22 = initCol(item22);
                        CHILD_COLS.push(item22);
                        childIndex = childIndex + parseInt(item22.colspan > 1 ? item22.colspan : 1);
                        opt.cols[i1 + 1][i22] = item22;
                    }
                }
                item2.CHILD_COLS = CHILD_COLS;
                if (!item2.PARENT_COL_INDEX) colArrays.push(item2);
                opt.cols[i1][i2] = item2;
            }
        }
        this.options = $.extend(true, {}, defaultOption, opt);
        this.options.colArrays = colArrays;

        // url加载模式转为reqData模式
        if (this.options.url) {
            this.options.reqData = function (data, callback) {
                if (!that.options.where) that.options.where = {};
                if (data) that.options.where[that.options.request.pidName] = data[that.options.tree.idName];
                (that.options.useAdmin ? layui.admin : $).ajax({
                    url: that.options.url,
                    data: that.options.contentType && that.options.contentType.indexOf('application/json') === 0 ? JSON.stringify(that.options.where) : that.options.where,
                    headers: that.options.headers,
                    type: that.options.method,
                    dataType: 'json',
                    contentType: that.options.contentType,
                    success: function (res) {
                        if (that.options.parseData) res = that.options.parseData(res);
                        if (res.code == that.options.statusCode) callback(res.data);
                        else callback(res.msg || '加载失败');
                    },
                    error: function (xhr) {
                        callback(xhr.status + ' - ' + xhr.statusText);
                    }
                });
            };
        } else if (this.options.data && this.options.data.length > 0 && this.options.tree.isPidData) {  // pid形式数据转children形式
            this.options.data = tt.pidToChildren(this.options.data, this.options.tree.idName, this.options.tree.pidName, this.options.tree.childName);
        }

        // toolbar参数处理
        if ('default' === this.options.toolbar) {
            this.options.toolbar = [
                '<div>',
                '   <div class="ew-tree-table-tool-item" title="添加" lay-event="add">',
                '      <i class="layui-icon layui-icon-add-1"></i>',
                '   </div>',
                '   <div class="ew-tree-table-tool-item" title="修改" lay-event="update">',
                '      <i class="layui-icon layui-icon-edit"></i>',
                '   </div>',
                '   <div class="ew-tree-table-tool-item" title="删除" lay-event="delete">',
                '      <i class="layui-icon layui-icon-delete"></i>',
                '   </div>',
                '</div>'
            ].join('');
        }
        if (this.options.defaultToolbar === undefined) this.options.defaultToolbar = ['filter', 'exports', 'print'];

        // 自定义图标参数处理
        if (typeof this.options.tree.getIcon === 'string') {
            var icon = this.options.tree.getIcon;
            this.options.tree.getIcon = function (d) {
                if (icon !== 'ew-tree-icon-style2') return icon;
                var haveChild = d[this.haveChildName];
                if (haveChild !== undefined) {
                    haveChild = haveChild === true || haveChild === 'true';
                    if (this.haveChildReverse) haveChild = !haveChild;
                }
                else if (d[this.childName]) haveChild = d[this.childName].length > 0;
                if (haveChild) return '<i class="ew-tree-icon ew-tree-icon-folder"></i>';
                else return '<i class="ew-tree-icon ew-tree-icon-file"></i>';
            }
        }
    };
    /** 初始化表格 */
    TreeTable.prototype.init = function () {
        var options = this.options;
        var $elem = $(options.elem);  // 原始表格
        var tbFilter = options.elem.substring(1);  // 表格的filter
        // 第一次生成树表格dom
        $elem.removeAttr('lay-filter');
        if ($elem.next('.ew-tree-table').length === 0) {
            $elem.css('display', 'none');
            $elem.after([
                '<div class="layui-form ew-tree-table" lay-filter="', tbFilter, '" style="', options.style || '', '">',
                '   <div class="ew-tree-table-tool" style="display: none;"></div>',
                '   <div class="ew-tree-table-head">',
                '      <table class="layui-table"></table>',
                '   </div>',
                '   <div class="ew-tree-table-box">',
                '      <table class="layui-table"></table>',
                '      <div class="ew-tree-table-loading">',
                '         <i class="layui-icon layui-icon-loading layui-anim layui-anim-rotate layui-anim-loop"></i>',
                '      </div>',
                '      <div class="ew-tree-table-empty">', options.text.none || '', '</div>',
                '   </div>',
                '</div>'
            ].join(''));
        }
        // 获取各个组件
        var components = this.getComponents();

        // 基础参数设置
        if (options.skin) components.$table.attr('lay-skin', options.skin);
        if (options.size) components.$table.attr('lay-size', options.size);
        if (options.even) components.$table.attr('lay-even', options.even);

        // 头部工具栏
        components.$toolbar.empty();
        if (options.toolbar === false || options.toolbar === undefined) {
            components.$toolbar.hide();
        } else {
            components.$toolbar.show();
            if (typeof options.toolbar === 'string') {
                laytpl($(options.toolbar).html()).render({}, function (html) {
                    components.$toolbar.html('<div style="display: inline-block;">' + html + '</div>');
                });
            }
            var tbRights = ['<div class="ew-tree-table-tool-right">'];
            for (var i = 0; i < options.defaultToolbar.length; i++) {
                var tbItem;
                if ('filter' === options.defaultToolbar[i]) {
                    tbItem = {title: '筛选', layEvent: 'LAYTABLE_COLS', icon: 'layui-icon-cols'};
                } else if ('exports' === options.defaultToolbar[i]) {
                    tbItem = {title: '导出', layEvent: 'LAYTABLE_EXPORT', icon: 'layui-icon-export'};
                } else if ('print' === options.defaultToolbar[i]) {
                    tbItem = {title: '打印', layEvent: 'LAYTABLE_PRINT', icon: 'layui-icon-print'};
                } else {
                    tbItem = options.defaultToolbar[i];
                }
                if (tbItem) {
                    tbRights.push('<div class="ew-tree-table-tool-item"');
                    tbRights.push(' title="' + tbItem.title + '"');
                    tbRights.push(' lay-event="' + tbItem.layEvent + '">');
                    tbRights.push('<i class="layui-icon ' + tbItem.icon + '"></i></div>');
                }
            }
            components.$toolbar.append(tbRights.join('') + '</div>');
        }

        // 固定宽度
        if (options.width) {
            components.$view.css('width', options.width);
            components.$tHeadGroup.css('width', options.width);
            components.$tBodyGroup.css('width', options.width);
        }
        // 表格尺寸设置
        var colgroupHtml = this.resize(true);
        // 生成thead
        var headHtml = '<thead>' + this.renderBodyTh() + '</thead>';

        // 渲染表头及空的表主体的结构
        components.$tBodyGroup.children('style').remove();
        if (options.height) {  // 固定表头
            components.$tHead.html(colgroupHtml + headHtml);
            components.$tBody.html(colgroupHtml + '<tbody></tbody>');
            if (options.height.indexOf('full-') === 0) {  // 差值高度
                var h = parseFloat(options.height.substring(5)) + components.$toolbar.outerHeight()
                    + components.$tHeadGroup.outerHeight() + 1;
                components.$tBodyGroup.append([
                    '<style>[lay-filter="', tbFilter, '"] .ew-tree-table-box {',
                    '   height: ', getPageHeight() - h, 'px;',
                    '   height: -moz-calc(100vh - ', h, 'px);',
                    '   height: -webkit-calc(100vh - ', h, 'px);',
                    '   height: calc(100vh - ', h, 'px);',
                    '}</style>'
                ].join(''));
                components.$tBodyGroup.data('full', h);
                components.$tBodyGroup.css('height', '');
            } else {  // 固定高度
                components.$tBodyGroup.css('height', options.height);
                components.$tBodyGroup.data('full', '');
            }
            components.$tHeadGroup.show();
        } else {
            components.$tHeadGroup.hide();
            var trH = {lg: 50, sm: 30, md: 38};
            components.$tBodyGroup.append([
                '<style>[lay-filter="', tbFilter, '"] .ew-tree-table-box:before {',
                '   content: "";',
                '   position: absolute;',
                '   top: 0; left: 0; right: 0;',
                '   height: ' + (trH[options.size || 'md'] * options.cols.length) + 'px;',
                '   background-color: #f2f2f2;',
                '   border-bottom: 1px solid #e6e6e6;',
                '}</style>'
            ].join(''));
            components.$tBody.html(colgroupHtml + headHtml + '<tbody></tbody>');
        }
        form.render('checkbox', tbFilter);  // 渲染表头的表单元素

        // 默认隐藏列修正colspan
        function patchHide($tr) {
            var parentKey = $tr.data('parent'), pCol;
            if (!parentKey) return;
            var $parent = components.$table.children('thead').children('tr').children('[data-key="' + parentKey + '"]');
            var colspan = $parent.attr('colspan') - 1;
            $parent.attr('colspan', colspan);
            if (colspan === 0) $parent.addClass('layui-hide');
            patchHide($parent);
        }

        components.$table.children('thead').children('tr').children('th.layui-hide').each(function () {
            patchHide($(this));
        });

        // 渲染数据
        if (options.reqData) {  // 异步加载
            this.options.data = undefined;
            this.renderBodyAsync();
        } else if (options.data && options.data.length > 0) {
            this.renderBodyData(options.data);
        } else {
            components.$loading.hide();
            components.$empty.show();
        }
    };

    /** 绑定各项事件 */
    TreeTable.prototype.bindEvents = function () {
        var that = this;
        var options = this.options;
        var components = this.getComponents();
        var $allBody = components.$table.children('tbody');

        /* 行事件公共返回对象 */
        var member = function (ext) {
            // 获取行dom
            var $tr = $(this);
            if (!$tr.is('tr')) {
                var $temp = $tr.parent('tr');
                if ($temp.length > 0) $tr = $temp;
                else $tr = $tr.parentsUntil('tr').last().parent();
            }
            var data = that.getDataByTr($tr);  // 行对应数据
            var obj = {
                tr: $tr,
                data: data,
                del: function () { // 删除行
                    var index = $tr.data('index');
                    var indent = parseInt($tr.data('indent'));
                    // 删除子级
                    $tr.nextAll('tr').each(function () {
                        if (parseInt($(this).data('indent')) <= indent) return false;
                        $(this).remove();
                    });
                    // 更新后面同辈的index
                    var indexLength = (typeof index === 'number' ? 1 : index.split('-').length);
                    $tr.nextAll('tr').each(function () {
                        var $this = $(this);
                        if (parseInt($this.data('indent')) < indent) return false;
                        var _index = $this.data('index').toString().split('-');
                        _index[indexLength - 1] = parseInt(_index[indexLength - 1]) - 1;
                        $this.data('index', _index.join('-'));
                    });
                    // 删除当前行
                    var $pTr = $tr.prevAll('tr');
                    that.del(undefined, index);
                    $tr.remove();
                    that.renderNumberCol();  // 渲染序号列
                    // 联动父级多选框
                    $pTr.each(function () {
                        var tInd = parseInt($(this).data('indent'));
                        if (tInd >= indent) return true;
                        that.checkParentCB($(this));
                        indent = tInd;
                    });
                    that.checkChooseAllCB();  // 联动全选框
                    if (options.data.length === 0) components.$empty.show();
                    updateFixedTbHead(components.$view);  // 更新滚动条补丁
                },
                update: function (fields) {  // 修改行
                    data = $.extend(true, data, fields);
                    var indent = parseInt($tr.data('indent'));
                    that.renderBodyTr(data, indent, undefined, $tr);  // 更新界面
                    form.render(null, components.filter);  // 渲染表单元素
                    that.renderNumberCol();  // 渲染序号列
                    // 联动父级多选框
                    $tr.prevAll('tr').each(function () {
                        var tInd = parseInt($(this).data('indent'));
                        if (tInd >= indent) return true;
                        that.checkParentCB($(this));
                        indent = tInd;
                    });
                    that.checkChooseAllCB();  // 联动全选框
                }
            };
            return $.extend(obj, ext);
        };

        // 绑定折叠展开事件
        $allBody.off('click.fold').on('click.fold', '.ew-tree-pack', function (e) {
            layui.stope(e);
            var $tr = $(this).parentsUntil('tr').last().parent();
            if ($tr.hasClass('ew-tree-table-loading')) return; // 已是加载中
            var haveChild = $tr.data('have-child');
            if (haveChild !== true && haveChild !== 'true') return; // 子节点
            var open = $tr.hasClass('ew-tree-table-open');
            var data = that.getDataByTr($tr);
            if (!open && !data[options.tree.childName]) {
                that.renderBodyAsync(data, $tr);
            } else {
                data[options.tree.openName] = toggleRow($tr);
            }
        });

        // 绑定lay-event事件
        $allBody.off('click.tool').on('click.tool', '*[lay-event]', function (e) {
            layui.stope(e);
            var $this = $(this);
            layui.event.call(this, MOD_NAME, 'tool(' + components.filter + ')', member.call(this, {
                event: $this.attr('lay-event')
            }));
        });

        // 绑定单选框事件
        form.on('radio(' + components.radioFilter + ')', function (data) {
            var d = that.getDataByTr($(data.elem).parentsUntil('tr').last().parent());
            that.removeAllChecked();
            d.LAY_CHECKED = true;  // 同时更新数据
            d.LAY_INDETERMINATE = false;
            layui.event.call(this, MOD_NAME, 'checkbox(' + components.filter + ')',
                {checked: true, data: d, type: 'one'});
        });

        // 绑定复选框事件
        form.on('checkbox(' + components.checkboxFilter + ')', function (data) {
            var checked = data.elem.checked;
            var $cb = $(data.elem);
            var $layCb = $cb.next('.layui-form-checkbox');
            // 如果是半选状态，点击全选
            if (!checked && $cb.hasClass('ew-form-indeterminate')) {
                checked = true;
                $cb.prop('checked', checked);
                $layCb.addClass('layui-form-checked');
                $cb.removeClass('ew-form-indeterminate');
            }
            var $tr = $cb.parentsUntil('tr').last().parent();
            var d = that.getDataByTr($tr);
            d.LAY_CHECKED = checked;  // 同时更新数据
            d.LAY_INDETERMINATE = false;
            // 联动操作
            if (d[options.tree.childName] && d[options.tree.childName].length > 0) {
                that.checkSubCB($tr, checked);  // 联动子级
            }
            var indent = parseInt($tr.data('indent'));
            $tr.prevAll('tr').each(function () {
                var tInd = parseInt($(this).data('indent'));
                if (tInd < indent) {
                    that.checkParentCB($(this));  // 联动父级
                    indent = tInd;
                }
            });
            that.checkChooseAllCB();  // 联动全选框
            // 回调事件
            layui.event.call(this, MOD_NAME, 'checkbox(' + components.filter + ')',
                {checked: checked, data: d, type: 'more'});
        });

        // 绑定全选复选框事件
        form.on('checkbox(' + components.chooseAllFilter + ')', function (data) {
            var checked = data.elem.checked;
            var $cb = $(data.elem);
            var $layCb = $cb.next('.layui-form-checkbox');
            if (!options.data || options.data.length === 0) {  // 如果数据为空
                $cb.prop('checked', false);
                $layCb.removeClass('layui-form-checked');
                $cb.removeClass('ew-form-indeterminate');
                return;
            }
            // 如果是半选状态，点击全选
            if (!checked && $cb.hasClass('ew-form-indeterminate')) {
                checked = true;
                $cb.prop('checked', checked);
                $layCb.addClass('layui-form-checked');
                $cb.removeClass('ew-form-indeterminate');
            }
            layui.event.call(this, MOD_NAME, 'checkbox(' + components.filter + ')', {checked: checked, type: 'all'});
            that.checkSubCB(components.$tBody.children('tbody'), checked);  // 联动操作
        });

        // 绑定行单击事件
        $allBody.off('click.row').on('click.row', 'tr', function () {
            layui.event.call(this, MOD_NAME, 'row(' + components.filter + ')', member.call(this, {}));
        });

        // 绑定行双击事件
        $allBody.off('dblclick.rowDouble').on('dblclick.rowDouble', 'tr', function () {
            layui.event.call(this, MOD_NAME, 'rowDouble(' + components.filter + ')', member.call(this, {}));
        });

        // 绑定单元格点击事件
        $allBody.off('click.cell').on('click.cell', 'td', function (e) {
            var $td = $(this);
            var type = $td.data('type');
            // 判断是否是复选框、单选框列
            if (type === 'checkbox' || type === 'radio') return layui.stope(e);
            var edit = $td.data('edit');
            var field = $td.data('field');
            if (edit) {  // 开启了单元格编辑
                layui.stope(e);
                if ($allBody.find('.ew-tree-table-edit').length > 0) return;
                var index = $td.data('index');
                var indent = $td.find('.ew-tree-table-indent').length;
                var d = that.getDataByTr($td.parent());
                if ('text' === edit || 'number' === edit) {  // 文本框
                    var $input = $('<input type="' + edit + '" class="layui-input ew-tree-table-edit"/>');
                    $input[0].value = d[field];
                    $td.append($input);
                    $input.focus();
                    $input.blur(function () {
                        var value = $(this).val();
                        if (value == d[field]) return $(this).remove();
                        var rs = layui.event.call(this, MOD_NAME, 'edit(' + components.filter + ')', member.call(this,
                            {value: value, field: field}));
                        if (rs === false) {
                            $(this).addClass('layui-form-danger');
                            $(this).focus();
                        } else {
                            d[field] = value;  // 同步更新数据
                            var keys = $td.data('key').split('-');
                            that.renderBodyTd(d, indent, index, $td, options.cols[keys[0]][keys[1]]);  // 更新单元格
                        }
                    });
                } else {
                    console.error('不支持的单元格编辑类型:' + edit);
                }
            } else {  // 回调单元格点击事件
                var rs = layui.event.call(this, MOD_NAME, 'cell(' + components.filter + ')', member.call(this,
                    {td: $td, field: field}));
                if (rs === false) layui.stope(e);
            }
        });

        // 绑定单元格双击事件
        $allBody.off('dblclick.cellDouble').on('dblclick.cellDouble', 'td', function (e) {
            var $td = $(this);
            var type = $td.data('type');
            // 判断是否是复选框、单选框列
            if (type === 'checkbox' || type === 'radio') return layui.stope(e);
            var edit = $td.data('edit');
            var field = $td.data('field');
            if (edit) return layui.stope(e);  // 开启了单元格编辑
            // 回调单元格双击事件
            var rs = layui.event.call(this, MOD_NAME, 'cellDouble(' + components.filter + ')', member.call(this,
                {td: $td, field: field}));
            if (rs === false) layui.stope(e);
        });

        // 绑定头部工具栏事件
        components.$toolbar.off('click.toolbar').on('click.toolbar', '*[lay-event]', function (e) {
            layui.stope(e);
            var $this = $(this);
            var event = $this.attr('lay-event');
            if ('LAYTABLE_COLS' === event) that.toggleCol();
            else if ('LAYTABLE_EXPORT' === event) that.exportData('show');
            else if ('LAYTABLE_PRINT' === event) that.printTable();
            else layui.event.call(this, MOD_NAME, 'toolbar(' + components.filter + ')', {event: event, elem: $this});
        });

        // 同步滚动条
        components.$tBodyGroup.on('scroll', function () {
            var $this = $(this);
            components.$tHeadGroup.scrollLeft($this.scrollLeft());
        });

        // 导出数据
        components.$toolbar.off('click.export').on('click.export', '.layui-table-tool-panel>[data-type]', function () {
            var type = $(this).data('type');
            if ('csv' === type || 'xls' === type) that.exportData(type);
        });
        components.$toolbar.off('click.panel').on('click.panel', '.layui-table-tool-panel', function (e) {
            layui.stope(e);
        });

        // 筛选列
        form.on('checkbox(' + components.colsToggleFilter + ')', function (data) {
            that.toggleCol(data.elem.checked, undefined, data.value);
        });

    };

    /** 获取各个组件 */
    TreeTable.prototype.getComponents = function () {
        var $view = $(this.options.elem).next('.ew-tree-table');  // 容器
        var filter = $view.attr('lay-filter');                    // 容器filter
        var $tHeadGroup = $view.children('.ew-tree-table-head');  // 表头部分容器
        var $tBodyGroup = $view.children('.ew-tree-table-box');   // 主体部分容器
        return {
            $view: $view,
            filter: filter,
            $tHeadGroup: $tHeadGroup,
            $tBodyGroup: $tBodyGroup,
            $tHead: $tHeadGroup.children('.layui-table'),                // 表头表格
            $tBody: $tBodyGroup.children('.layui-table'),                // 主体表格
            $table: $view.find('.layui-table'),                          // 所有表格
            $toolbar: $view.children('.ew-tree-table-tool'),             // 头部工具栏
            $empty: $tBodyGroup.children('.ew-tree-table-empty'),        // 空视图
            $loading: $tBodyGroup.children('.ew-tree-table-loading'),    // 加载视图
            checkboxFilter: 'ew_tb_checkbox_' + filter,                  // 复选框filter
            radioFilter: 'ew_tb_radio_' + filter,                        // 单选框filter
            chooseAllFilter: 'ew_tb_choose_all_' + filter,               // 全选按钮filter
            colsToggleFilter: 'ew_tb_toggle_cols' + filter               // 筛选列的filter
        };
    };

    /**
     * 遍历表头
     * @param callback
     * @param obj
     */
    TreeTable.prototype.eachCols = function (callback, obj) {
        if (!obj) obj = this.options.colArrays;
        for (var i = 0; i < obj.length; i++) {
            var item = obj[i];
            callback && callback(i, item);
            if (item.CHILD_COLS) this.eachCols(callback, item.CHILD_COLS);
        }
    };

    /**
     * 遍历数据
     * @param callback
     * @param data
     */
    TreeTable.prototype.eachData = function (callback, data) {
        if (!data) data = this.options.data;
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            callback && callback(i, item);
            if (item[this.options.tree.childName]) this.eachData(callback, item[this.options.tree.childName]);
        }
    };

    /**
     * 异步加载渲染
     * @param d 父级数据
     * @param $tr 父级dom
     */
    TreeTable.prototype.renderBodyAsync = function (d, $tr) {
        var that = this;
        var options = this.options;
        var components = this.getComponents();
        // 显示loading
        if ($tr) {
            $tr.addClass('ew-tree-table-loading');
            $tr.find('.ew-tree-pack').children('.ew-tree-table-arrow').addClass('layui-anim layui-anim-rotate layui-anim-loop');
        } else {
            components.$empty.hide();
            if (options.data && options.data.length > 0) components.$loading.addClass('ew-loading-float');
            components.$loading.show();
        }
        // 请求数据
        options.reqData(d, function (data) {
            if (typeof data !== 'string' && data && data.length > 0 && options.tree.isPidData) {
                data = tt.pidToChildren(data, options.tree.idName, options.tree.pidName, options.tree.childName);
            }
            that.renderBodyData(data, d, $tr);  // 渲染内容
        });
    };

    /**
     * 根据数据渲染body
     * @param data  数据集合
     * @param d 父级数据
     * @param $tr 父级dom
     */
    TreeTable.prototype.renderBodyData = function (data, d, $tr) {
        var msg;
        if (typeof data === 'string') {
            msg = data;
            data = [];
        }
        var that = this;
        var options = this.options;
        var components = this.getComponents();
        // 更新到数据
        if (d === undefined) options.data = data;
        else d[options.tree.childName] = data;
        var indent;
        if (d && $tr) {
            indent = parseInt($tr.data('indent')) + 1;
            d[options.tree.openName] = true;
        }
        var htmlStr = this.renderBody(data, indent, d);
        if (d && $tr) {
            // 移除旧dom
            $tr.nextAll('tr').each(function () {
                if (parseInt($(this).data('indent')) <= (indent - 1)) return false;
                $(this).remove();
            });
            // 渲染新dom
            $tr.after(htmlStr).addClass('ew-tree-table-open');
        } else {
            components.$tBody.children('tbody').html(htmlStr);
        }
        form.render(null, components.filter);  // 渲染表单元素
        this.renderNumberCol();  // 渲染序号列
        if (d && $tr) {
            // 更新父级复选框状态
            this.checkParentCB($tr);
            $tr.prevAll('tr').each(function () {
                var tInd = parseInt($(this).data('indent'));
                if (tInd < (indent - 1)) {
                    that.checkParentCB($(this));
                    indent = tInd + 1;
                }
            });
            // 移除loading
            $tr.removeClass('ew-tree-table-loading');
            var $arrow = $tr.find('.ew-tree-pack').children('.ew-tree-table-arrow');
            $arrow.removeClass('layui-anim layui-anim-rotate layui-anim-loop');
            if (msg) {  // 加载失败
                $tr.removeClass('ew-tree-table-open');
            } else if (data && data.length === 0) {  // 无子集
                d[options.tree.haveChildName] = !!options.tree.haveChildReverse;
                $tr.data('have-child', false);
                $arrow.addClass('ew-tree-table-arrow-hide');
                $arrow.next('.ew-tree-icon').after(options.tree.getIcon(d)).remove();
            }
        } else {
            // 移除loading
            components.$loading.hide();
            components.$loading.removeClass('ew-loading-float');
            // 显示空视图
            if (data && data.length > 0) {
                components.$empty.hide();
            } else {
                components.$empty.show();
                if (msg) components.$empty.text(msg);
                else components.$empty.html(options.text.none);
            }
        }
        this.checkChooseAllCB();  // 联动全选框
        updateFixedTbHead(components.$view);  // 滚动条补丁
        options.done && options.done(data);
    };

    /**
     * 递归渲染表格主体部分
     * @param data 数据列表
     * @param indent 缩进大小
     * @param parent 父级
     * @param h 父级是否隐藏
     * @returns {string}
     */
    TreeTable.prototype.renderBody = function (data, indent, parent, h) {
        var options = this.options;
        if (!indent) indent = 0;
        var html = '';
        if (!data || data.length === 0) return html;
        var hide = parent ? !parent[options.tree.openName] : undefined;
        if (h) hide = h;//当所有父级存在隐藏时，隐藏所有子集
        for (var i = 0; i < data.length; i++) {
            var d = data[i];
            d.LAY_INDEX = (parent ? parent.LAY_INDEX + '-' : '') + i;
            html += this.renderBodyTr(d, indent, hide);
            // 递归渲染子集
            html += this.renderBody(d[options.tree.childName], indent + 1, d, h);
        }
        return html;
    };

    /**
     * 渲染每一行数据
     * @param d 行数据
     * @param indent 缩进大小
     * @param hide 是否隐藏
     * @param $tr
     * @returns {string}
     */
    TreeTable.prototype.renderBodyTr = function (d, indent, hide, $tr) {
        var that = this;
        var options = this.options;
        if (!indent) indent = 0;
        var haveChild = d[options.tree.haveChildName];
        if (options.tree.haveChildReverse) haveChild = !haveChild;
        if (haveChild === undefined) haveChild = d[options.tree.childName] && d[options.tree.childName].length > 0;
        if ($tr) {
            $tr.data('have-child', haveChild ? 'true' : 'false');
            $tr.data('indent', indent);
            $tr.removeClass('ew-tree-table-loading');
        }
        var html = '<tr';
        var classNames = '';
        if (haveChild && d[options.tree.openName]) classNames += 'ew-tree-table-open';
        if (hide) classNames += 'ew-tree-tb-hide';
        html += (' class="' + classNames + '"');
        if (haveChild) html += (' data-have-child="' + haveChild + '"');
        html += (' data-index="' + d.LAY_INDEX + '"');
        html += (' data-indent="' + indent + '">');
        var index = 0;
        this.eachCols(function (i, col) {
            if (col.colGroup) return;
            html += that.renderBodyTd(d, indent, index, $tr ? $tr.children('td').eq(index) : undefined, col);
            index++;
        });
        html += '</tr>';
        return html;
    };

    /**
     * 渲染每一个单元格数据
     * @param d 行数据
     * @param indent 缩进大小
     * @param index 第几列
     * @param $td
     * @param col
     * @returns {string}
     */
    TreeTable.prototype.renderBodyTd = function (d, indent, index, $td, col) {
        if (!col||col.colGroup) return '';
        var options = this.options;
        var components = this.getComponents();
        if (!indent) indent = 0;
        // 内容填充
        var content = '', cell = '', icon = '';
        if (col.type === 'numbers') {  // 序号列
            content = '<span class="ew-tree-table-numbers"></span>';
        } else if (col.type === 'checkbox') {  // 复选框列
            content = [
                '<input type="checkbox"', d.LAY_CHECKED ? ' checked="checked"' : '',
                ' lay-filter="', components.checkboxFilter, '"',
                ' lay-skin="primary" class="ew-tree-table-checkbox',
                d.LAY_INDETERMINATE ? ' ew-form-indeterminate' : '', '" />'
            ].join('');
        } else if (col.type === 'radio') {  // 单选框列
            content = [
                '<input type="radio"', d.LAY_CHECKED ? ' checked="checked"' : '',
                ' lay-filter="', components.radioFilter, '"',
                ' name="', components.radioFilter, '"',
                ' class="ew-tree-table-radio" />'
            ].join('');
        } else if (col.templet) {  // 自定义模板
            if (typeof col.templet === 'function') {
                content = col.templet(d);
            } else if (typeof col.templet === 'string') {
                laytpl($(col.templet).html()).render(d, function (html) {
                    content = html;
                });
            }
        } else if (col.toolbar) {  // 操作列
            if (typeof col.toolbar === 'function') {
                content = col.toolbar(d);
            } else if (typeof col.toolbar === 'string') {
                laytpl($(col.toolbar).html()).render(d, function (html) {
                    content = html;
                });
            }
        } else if (col.field && d[col.field] !== undefined && d[col.field] !== null) {  // 普通字段
            content = util.escape(d[col.field] === 0 ? '0' : d[col.field]);
        }
        // 图标列处理
        if (index === options.tree.iconIndex) {
            // 缩进
            for (var i = 0; i < indent; i++) icon += '<span class="ew-tree-table-indent"></span>';
            icon += '<span class="ew-tree-pack">';
            // 加箭头
            var haveChild = d[options.tree.haveChildName];
            if (options.tree.haveChildReverse) haveChild = !haveChild;
            if (haveChild === undefined) haveChild = d[options.tree.childName] && d[options.tree.childName].length > 0;
            icon += ('<i class="ew-tree-table-arrow layui-icon' + (haveChild === true || haveChild === 'true' ? '' : ' ew-tree-table-arrow-hide'));
            icon += (' ' + (options.tree.arrowType || '') + '"></i>');
            // 加图标
            icon += options.tree.getIcon(d);
            content = '<span>' + content + '</span>';
            if (options.tree.onlyIconControl) content = icon + '</span>' + content;
            else content = icon + content + '</span>';
        }
        cell = [
            '<div class="ew-tree-table-cell', col.singleLine === undefined || col.singleLine ? ' single-line' : '', '"',
            col.align ? ' align="' + col.align + '"' : '',
            '>',
            '   <div class="ew-tree-table-cell-content">', content, '</div>',
            '   <i class="layui-icon layui-icon-close ew-tree-tips-c"></i>',
            '   <div class="layui-table-grid-down" style="display: none;"><i class="layui-icon layui-icon-down"></i></div>',
            '</div>'
        ].join('');

        if ($td) $td.html(cell);

        var html = '<td';
        if (col.field) html += (' data-field="' + col.field + '"');
        if (col.edit) html += (' data-edit="' + col.edit + '"');
        if (col.type) html += (' data-type="' + col.type + '"');
        if (col.key) html += (' data-key="' + col.key + '"');
        if (col.style) html += (' style="' + col.style + '"');
        if (col['class']) html += (' class="' + col['class'] + (col.hide ? ' layui-hide' : '') + '"');
        else if (col.hide) html += (' class="layui-hide"');
        html += ('>' + cell + '</td>');
        return html;
    };

    /**
     * 渲染表头
     * @returns {string}
     */
    TreeTable.prototype.renderBodyTh = function () {
        var options = this.options;
        var components = this.getComponents();
        var html = [];
        $.each(options.cols, function (i1, item1) {
            html.push('<tr>');
            $.each(item1, function (i2, item2) {
                html.push('<th');
                if (item2.colspan) html.push(' colspan="' + item2.colspan + '"');
                if (item2.rowspan) html.push(' rowspan="' + item2.rowspan + '"');
                if (item2.type) html.push(' data-type="' + item2.type + '"');
                if (item2.key) html.push(' data-key="' + item2.key + '"');
                if (item2.parentKey) html.push(' data-parent="' + item2.parentKey + '"');
                if (item2.hide) html.push(' class="layui-hide"');
                html.push('>');
                html.push('<div class="ew-tree-table-cell' + (item2.singleLine === undefined || item2.singleLine ? ' single-line' : '') + '"');
                if (item2.thAlign || item2.align) html.push(' align="' + (item2.thAlign || item2.align) + '"');
                html.push('>');
                html.push('<div class="ew-tree-table-cell-content">');
                // 标题
                var ca = '<input type="checkbox" lay-filter="' + components.chooseAllFilter + '" lay-skin="primary" class="ew-tree-table-checkbox"/>';
                if (item2.type === 'checkbox') html.push(ca);
                else html.push(item2.title || '');
                html.push('</div><i class="layui-icon layui-icon-close ew-tree-tips-c"></i>');
                html.push('<div class="layui-table-grid-down" style="display: none;"><i class="layui-icon layui-icon-down"></i></div></div>');
                // 列宽拖拽
                if (!item2.colGroup && !item2.unresize) html.push('<span class="ew-tb-resize"></span>');
                html.push('</th>');
            });
            html.push('</tr>');
        });
        return html.join('');
    };

    /** 重置表格尺寸 */
    TreeTable.prototype.resize = function (returnColgroup) {
        // 计算表格宽度、最小宽度、百分比宽度
        var options = this.options;
        var components = this.getComponents();
        var minWidth = 1, width = 1, needSetWidth = true, mwPercent = 0;
        this.eachCols(function (i, item) {
            if (item.colGroup || item.hide) return;
            if (item.width) {
                width += (item.width + 1);
                if (item.minWidth) {
                    if (item.width < item.minWidth) item.width = item.minWidth;
                } else if (item.width < options.cellMinWidth) item.width = options.cellMinWidth;
            } else needSetWidth = false;
            if (item.width) minWidth += (item.width + 1);
            else if (item.minWidth) {
                minWidth += (item.minWidth + 1);
                mwPercent += item.minWidth;
            } else {
                minWidth += (options.cellMinWidth + 1);
                mwPercent += options.cellMinWidth;
            }
        });
        if (minWidth) {
            components.$tHead.css('min-width', minWidth);
            components.$tBody.css('min-width', minWidth);
        } else {
            components.$tHead.css('min-width', 'auto');
            components.$tBody.css('min-width', 'auto');
        }
        if (needSetWidth) {
            components.$tHead.css('width', width);
            components.$tBody.css('width', width);
        } else {
            components.$tHead.css('width', '100%');
            components.$tBody.css('width', '100%');
        }

        // 生成colgroup
        var colgroupHtml = [];
        this.eachCols(function (i, item) {
            if (item.colGroup || item.hide) return;
            colgroupHtml.push('<col');
            if (item.width) colgroupHtml.push(' width="' + item.width + '"');
            else if (item.minWidth) colgroupHtml.push(' width="' + (item.minWidth / mwPercent * 100).toFixed(2) + '%"');
            else colgroupHtml.push(' width="' + (options.cellMinWidth / mwPercent * 100).toFixed(2) + '%"');
            if (item.type) colgroupHtml.push(' data-type="' + item.type + '"');
            if (item.key) colgroupHtml.push(' data-key="' + item.key + '"');
            colgroupHtml.push('/>');
        });
        colgroupHtml = colgroupHtml.join('');
        if (returnColgroup) return '<colgroup>' + colgroupHtml + '</colgroup>';
        components.$table.children('colgroup').html(colgroupHtml);
    };

    /** 获取行对应数据 */
    TreeTable.prototype.getDataByTr = function ($tr) {
        var data, index;
        if (typeof $tr !== 'string' && typeof $tr !== 'number') {
            if ($tr) index = $tr.data('index');
        } else index = $tr;
        if (index === undefined) return;
        if (typeof index === 'number') index = [index];
        else index = index.split('-');
        for (var i = 0; i < index.length; i++) {
            if (data) data = data[this.options.tree.childName][index[i]];
            else data = this.options.data[index[i]];
        }
        return data;
    };

    /**
     * 联动子级复选框状态
     * @param $tr 当前tr的dom
     * @param checked
     */
    TreeTable.prototype.checkSubCB = function ($tr, checked) {
        var that = this;
        var components = this.getComponents();
        var indent = -1, $trList;
        if ($tr.is('tbody')) {
            $trList = $tr.children('tr');
        } else {
            indent = parseInt($tr.data('indent'));
            $trList = $tr.nextAll('tr');
        }
        $trList.each(function () {
            if (parseInt($(this).data('indent')) <= indent) return false;
            var $cb = $(this).children('td').find('input[lay-filter="' + components.checkboxFilter + '"]');
            $cb.prop('checked', checked);
            $cb.removeClass('ew-form-indeterminate');
            if (checked) $cb.next('.layui-form-checkbox').addClass('layui-form-checked');
            else $cb.next('.layui-form-checkbox').removeClass('layui-form-checked');
            var d = that.getDataByTr($(this));
            d.LAY_CHECKED = checked;  // 同步更新数据
            d.LAY_INDETERMINATE = false;
        });
    };

    /**
     * 联动父级复选框状态
     * @param $tr 父级的dom
     */
    TreeTable.prototype.checkParentCB = function ($tr) {
        var options = this.options;
        var components = this.getComponents();
        var d = this.getDataByTr($tr);
        var ckNum = 0, unCkNum = 0;
        if (d[options.tree.childName]) {
            function checkNum(data) {
                for (var i = 0; i < data.length; i++) {
                    if (data[i].LAY_CHECKED) ckNum++;
                    else unCkNum++;
                    if (data[i][options.tree.childName]) checkNum(data[i][options.tree.childName]);
                }
            }

            checkNum(d[options.tree.childName]);
        }
        var $cb = $tr.children('td').find('input[lay-filter="' + components.checkboxFilter + '"]');
        if (ckNum > 0 && unCkNum === 0) {  // 全选
            $cb.prop('checked', true);
            $cb.removeClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').addClass('layui-form-checked');
            d.LAY_CHECKED = true;  // 同步更新数据
            d.LAY_INDETERMINATE = false;
        } else if (ckNum === 0 && unCkNum > 0) {  // 全不选
            $cb.prop('checked', false);
            $cb.removeClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').removeClass('layui-form-checked');
            d.LAY_CHECKED = false;  // 同步更新数据
            d.LAY_INDETERMINATE = false;
        } else if (ckNum > 0 && unCkNum > 0) {  // 半选
            $cb.prop('checked', true);
            $cb.data('indeterminate', 'true');
            $cb.addClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').addClass('layui-form-checked');
            d.LAY_CHECKED = true;  // 同步更新数据
            d.LAY_INDETERMINATE = true;
        }
    };

    /** 联动全选复选框 */
    TreeTable.prototype.checkChooseAllCB = function () {
        var options = this.options;
        var components = this.getComponents();
        var ckNum = 0, unCkNum = 0;

        function checkNum(data) {
            for (var i = 0; i < data.length; i++) {
                if (data[i].LAY_CHECKED) ckNum++;
                else unCkNum++;
                if (data[i][options.tree.childName]) checkNum(data[i][options.tree.childName]);
            }
        }

        checkNum(options.data);

        var $cb = components.$view.find('input[lay-filter="' + components.chooseAllFilter + '"]');
        if (ckNum > 0 && unCkNum === 0) {  // 全选
            $cb.prop('checked', true);
            $cb.removeClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').addClass('layui-form-checked');
        } else if ((ckNum === 0 && unCkNum > 0) || (ckNum === 0 && unCkNum === 0)) {  // 全不选
            $cb.prop('checked', false);
            $cb.removeClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').removeClass('layui-form-checked');
        } else if (ckNum > 0 && unCkNum > 0) {  // 半选
            $cb.prop('checked', true);
            $cb.addClass('ew-form-indeterminate');
            $cb.next('.layui-form-checkbox').addClass('layui-form-checked');
        }
    };

    /** 填充序号列 */
    TreeTable.prototype.renderNumberCol = function () {
        this.getComponents().$tBody.children('tbody').children('tr').each(function (i) {
            $(this).children('td').find('.ew-tree-table-numbers').text(i + 1);
        });
    };

    /** 根据id获取tr的index */
    TreeTable.prototype.getIndexById = function (id) {
        var options = this.options;

        function each(data, pi) {
            for (var i = 0; i < data.length; i++) {
                if (data[i][options.tree.idName] === id) return pi !== undefined ? pi + '-' + i : i;
                if (data[i][options.tree.childName]) {
                    var res = each(data[i][options.tree.childName], pi !== undefined ? pi + '-' + i : i);
                    //值不为undefined才return
                    if (res) return res;
                }
            }
        }

        return each(options.data);
    };

    /** 展开指定行 */
    TreeTable.prototype.expand = function (id, cascade) {
        var components = this.getComponents();
        var $tr = components.$table.children('tbody').children('tr[data-index="' + this.getIndexById(id) + '"]');
        if (!$tr.hasClass('ew-tree-table-open')) $tr.children('td').find('.ew-tree-pack').trigger('click');
        if (cascade === false) return;
        // 联动父级
        var indent = parseInt($tr.data('indent'));
        $tr.prevAll('tr').each(function () {
            var tInd = parseInt($(this).data('indent'));
            if (tInd < indent) {
                if (!$(this).hasClass('ew-tree-table-open')) {
                    $(this).children('td').find('.ew-tree-pack').trigger('click');
                }
                indent = tInd;
            }
        });
    };

    /** 折叠指定行 */
    TreeTable.prototype.fold = function (id) {
        var components = this.getComponents();
        var $tr = components.$table.children('tbody').children('tr[data-index="' + this.getIndexById(id) + '"]');
        if ($tr.hasClass('ew-tree-table-open')) $tr.children('td').find('.ew-tree-pack').trigger('click');
    };

    /** 全部展开 */
    TreeTable.prototype.expandAll = function () {
        this.getComponents().$table.children('tbody').children('tr').each(function () {
            if (!$(this).hasClass('ew-tree-table-open')) $(this).children('td').find('.ew-tree-pack').trigger('click');
        });
    };

    /** 全部折叠 */
    TreeTable.prototype.foldAll = function () {
        this.getComponents().$table.children('tbody').children('tr').each(function () {
            if ($(this).hasClass('ew-tree-table-open')) $(this).children('td').find('.ew-tree-pack').trigger('click');
        });
    };

    /** 获取当前数据 */
    TreeTable.prototype.getData = function () {
        return this.options.data;
    };

    /** 重载表格 */
    TreeTable.prototype.reload = function (opt) {
        this.initOptions(this.options ? $.extend(true, this.options, opt) : opt);
        this.init();  // 初始化表格
        this.bindEvents();  // 绑定事件
    };

    /** 获取当前选中行 */
    TreeTable.prototype.checkStatus = function (needIndeterminate) {
        if (needIndeterminate === undefined) needIndeterminate = true;
        var list = [];
        this.eachData(function (i, item) {
            if ((needIndeterminate || !item.LAY_INDETERMINATE) && item.LAY_CHECKED)
                list.push($.extend({isIndeterminate: item.LAY_INDETERMINATE}, item));
        });
        return list;
    };

    /** 设置复/单选框选中 */
    TreeTable.prototype.setChecked = function (ids) {
        var that = this;
        var components = this.getComponents();
        var $radio = components.$table.find('input[lay-filter="' + components.radioFilter + '"]');
        if ($radio.length > 0) {  // 开启了单选框
            $radio.each(function () {
                var d = that.getDataByTr($(this).parentsUntil('tr').parent());
                if (d && ids[ids.length - 1] == d[that.options.tree.idName]) {
                    $(this).next('.layui-form-radio').trigger('click');
                    return false;
                }
            });
        } else {  // 开启了复选框
            components.$table.find('input[lay-filter="' + components.checkboxFilter + '"]').each(function () {
                var $cb = $(this);
                var $layCb = $cb.next('.layui-form-checkbox');
                var checked = $cb.prop('checked');
                var indeterminate = $cb.hasClass('ew-form-indeterminate');
                var d = that.getDataByTr($cb.parentsUntil('tr').parent());
                for (var i = 0; i < ids.length; i++) {
                    if (d && ids[i] == d[that.options.tree.idName]) {
                        if (d[that.options.tree.childName] && d[that.options.tree.childName].length > 0) continue;
                        if (!checked || indeterminate) $layCb.trigger('click');
                    }
                }
            });
        }
    };

    /** 移除全部选中 */
    TreeTable.prototype.removeAllChecked = function () {
        this.checkSubCB(this.getComponents().$table.children('tbody'), false);
    };

    /** 导出 */
    TreeTable.prototype.exportData = function (type) {
        var components = this.getComponents();
        if ('show' === type) {
            components.$toolbar.find('.layui-table-tool-panel').remove();
            components.$toolbar.find('[lay-event="LAYTABLE_EXPORT"]').append([
                '<ul class="layui-table-tool-panel">',
                '   <li data-type="csv">导出到 Csv 文件</li>',
                '   <li data-type="xls">导出到 Excel 文件</li>',
                '</ul>'
            ].join(''));
        } else {
            if (device.ie) return layer.msg('不支持ie导出');
            if (!type) type = 'xls';
            var head = [], body = [];
            this.eachCols(function (i, item) {
                if (item.type !== 'normal' || item.hide) return;
                head.push(item.title || '');
            });
            components.$tBody.children('tbody').children('tr').each(function () {
                var items = [];
                $(this).children('td').each(function () {
                    var $this = $(this);
                    if ($this.data('type') !== 'normal' || $this.hasClass('layui-hide')) return true;
                    items.push($this.text().trim().replace(/,/g, '，'));
                });
                body.push(items.join(','));
            });
            // 创建下载文件的a标签
            var alink = document.createElement('a');
            var content = encodeURIComponent(head.join(',') + '\r\n' + body.join('\r\n'));
            var contentType = ({csv: 'text/csv', xls: 'application/vnd.ms-excel'})[type];
            alink.href = 'data:' + contentType + ';charset=utf-8,\ufeff' + content;
            alink.download = (this.options.title || 'table') + '.' + type;
            document.body.appendChild(alink);
            alink.click();
            document.body.removeChild(alink);
        }
    };

    /** 打印 */
    TreeTable.prototype.printTable = function () {
        var components = this.getComponents();
        var head = components.$tHead.children('thead').html();
        if (!head) head = components.$tBody.children('thead').html();
        var body = components.$tBody.children('tbody').html();
        var colgroup = components.$tBody.children('colgroup').html();
        var $html = $([
            '<table class="ew-tree-table-print">',
            '   <colgroup>', colgroup, '</colgroup>',
            '   <thead>', head, '</thead>',
            '   <tbody>', body, '</tbody>',
            '</table>'
        ].join(''));

        // 隐藏特殊列
        $html.find('col[data-type="checkbox"],col[data-type="radio"],col[data-type="tool"]').remove();
        $html.find('td[data-type="checkbox"],td[data-type="radio"],td[data-type="tool"],.layui-hide').remove();

        function hideCol($temp) {
            var parentKey = $temp.data('parent'), pCol;
            if (!parentKey) return;
            var $parent = $html.children('thead').children('tr').children('[data-key="' + parentKey + '"]');
            var colspan = parseInt($parent.attr('colspan')) - 1;
            $parent.attr('colspan', colspan);
            if (colspan === 0) $parent.remove();
            hideCol($parent);
        }

        $html.find('th[data-type="checkbox"],th[data-type="radio"],th[data-type="tool"]').each(function () {
            hideCol($(this));
        }).remove();

        // 打印内容样式
        var style = [
            '<style>',
            '   /* 打印表格样式 */',
            '   .ew-tree-table-print {',
            '      border: none;',
            '      border-collapse: collapse;',
            '      width: 100%;',
            '      table-layout: fixed;',
            '   }',
            '   .ew-tree-table-print td, .ew-tree-table-print th {',
            '      color: #555;',
            '      font-size: 14px;',
            '      padding: 9px 15px;',
            '      word-break: break-all;',
            '      border: 1px solid #888;',
            '      text-align: left;',
            '   }',
            '   .ew-tree-table-print .ew-tree-table-cell {',
            '      min-height: 20px;',
            '   }',
            '   /* 序号列样式 */',
            '   .ew-tree-table-print td[data-type="numbers"], .ew-tree-table-print th[data-type="numbers"] {',
            '      padding-left: 0;',
            '      padding-right: 0;',
            '   }',
            '   /* 单/复选框列样式 */',
            '   .ew-tree-table-print td[data-type="tool"], .ew-tree-table-print th[data-type="tool"], ',
            '   .ew-tree-table-print td[data-type="checkbox"], .ew-tree-table-print th[data-type="checkbox"], ',
            '   .ew-tree-table-print td[data-type="radio"], .ew-tree-table-print th[data-type="radio"] {',
            '      border: none;',
            '   }',
            '   .ew-tree-table-print td.layui-hide + td, .ew-tree-table-print th.layui-hide + th, ',
            '   .ew-tree-table-print td[data-type="tool"] + td, .ew-tree-table-print th[data-type="tool"] + th, ',
            '   .ew-tree-table-print td[data-type="checkbox"] + td, .ew-tree-table-print th[data-type="checkbox"] + th, ',
            '   .ew-tree-table-print td[data-type="radio"] + td, .ew-tree-table-print th[data-type="radio"] + th {',
            '      border-left: none;',
            '   }',
            '  /* 不显示的元素 */',
            '   .layui-hide, ',
            '   .ew-tree-table-print td[data-type="tool"] *, .ew-tree-table-print th[data-type="tool"] *, ',
            '   .ew-tree-table-print td[data-type="checkbox"] *, .ew-tree-table-print th[data-type="checkbox"] *, ',
            '   .ew-tree-table-print td[data-type="radio"] *, .ew-tree-table-print th[data-type="radio"] *, ',
            '   .layui-table-grid-down, .ew-tree-tips-c, .ew-tree-icon, .ew-tree-table-arrow.ew-tree-table-arrow-hide {',
            '      display: none;',
            '   }',
            '   /* tree缩进 */',
            '   .ew-tree-table-indent {',
            '      padding-left: 13px;',
            '   }',
            '   /* 箭头 */',
            '   .ew-tree-table-arrow {',
            '      position: relative;',
            '      padding-left: 13px;',
            '   }',
            '   .ew-tree-table-arrow:before {',
            '      content: "";',
            '      border: 5px solid transparent;',
            '      border-top-color: #666;',
            '      position: absolute;',
            '      left: 0;',
            '      top: 6px;',
            '   }',
            '</style>'
        ].join('');
        var pWindow = window.open('', '_blank');
        pWindow.focus();
        var pDocument = pWindow.document;
        pDocument.open();
        pDocument.write($html[0].outerHTML + style);
        pDocument.close();
        pWindow.print();
        pWindow.close();
    };

    /** 筛选列 */
    TreeTable.prototype.toggleCol = function (show, field, key) {
        var components = this.getComponents();
        if (show === undefined) {
            components.$toolbar.find('.layui-table-tool-panel').remove();
            var cols = ['<ul class="layui-table-tool-panel">'];
            this.eachCols(function (i, item) {
                if (item.type !== 'normal') return;
                cols.push('<li><input type="checkbox" lay-skin="primary"');
                cols.push(' lay-filter="' + components.colsToggleFilter + '"');
                cols.push(' value="' + item.key + '" title="' + util.escape(item.title || '') + '"');
                cols.push((item.hide ? '' : ' checked') + '></li>');
            });
            components.$toolbar.find('[lay-event="LAYTABLE_COLS"]').append(cols.join('') + '</ul>');
            form.render('checkbox', components.filter);
        } else {
            if (key) {
                var $td = components.$table.children('tbody').children('tr').children('[data-key="' + key + '"]');
                var $th = components.$table.children('thead').children('tr').children('[data-key="' + key + '"]');
                if (show) {
                    $td.removeClass('layui-hide');
                    $th.removeClass('layui-hide');
                } else {
                    $td.addClass('layui-hide');
                    $th.addClass('layui-hide');
                }
                // 同步更新数据
                var ks = key.split('-');
                var col = this.options.cols[ks[0]][ks[1]];
                col.hide = !show;

                // 更新colspan数据
                function changeParent($temp) {
                    var parentKey = $temp.data('parent'), pCol;
                    if (!parentKey) return;
                    var $parent = components.$table.children('thead').children('tr').children('[data-key="' + parentKey + '"]');
                    var colspan = $parent.attr('colspan');
                    show ? colspan++ : colspan--;
                    $parent.attr('colspan', colspan);
                    if (colspan === 0) $parent.addClass('layui-hide');
                    else $parent.removeClass('layui-hide');
                    changeParent($parent);
                }

                changeParent($th);

                // 同步eachCols数据
                this.eachCols(function (i, item) {
                    if (item.key === key) item.hide = col.hide;
                });
                this.resize();  // 更新表格尺寸
            }
        }
    };

    /**
     * 搜索数据
     * @param ids 关键字或数据id集合
     */
    TreeTable.prototype.filterData = function (ids) {
        var components = this.getComponents();
        components.$loading.show();
        if (this.options.data.length > 0) components.$loading.addClass('ew-loading-float');
        var $trList = components.$table.children('tbody').children('tr');
        var indexList = [];
        if (typeof ids === 'string') {  // 关键字
            $trList.each(function () {
                var index = $(this).data('index');
                $(this).children('td').each(function () {
                    if ($(this).text().indexOf(ids) !== -1) {
                        indexList.push(index);
                        return false;
                    }
                });
            });
        } else {
            for (var i = 0; i < ids.length; i++) {
                indexList.push(this.getIndexById(ids[i]));
            }
        }
        $trList.addClass('ew-tree-table-filter-hide');
        for (var j = 0; j < indexList.length; j++) {
            var $tr = $trList.filter('[data-index="' + indexList[j] + '"]');
            $tr.removeClass('ew-tree-table-filter-hide');
            var indent = parseInt($tr.data('indent'));
            // 联动子级
            $tr.nextAll('tr').each(function () {
                if (parseInt($(this).data('indent')) <= indent) return false;
                $(this).removeClass('ew-tree-table-filter-hide');
            });
            if ($tr.hasClass('ew-tree-table-open')) toggleRow($tr);
            // 联动父级
            $tr.prevAll('tr').each(function () {
                var tInd = parseInt($(this).data('indent'));
                if (tInd < indent) {
                    $(this).removeClass('ew-tree-table-filter-hide');
                    if (!$(this).hasClass('ew-tree-table-open')) toggleRow($(this));
                    indent = tInd;
                }
            });
        }
        // 最后再检查一遍
        /*$trList.not('.ew-tree-table-filter-hide').not('.ew-tree-tb-hide').each(function () {
            var index = $(this).data('index'), hide = true;
            for (var k = 0; k < indexList.length; k++) {
                if (indexList[k] === index) hide = false;
            }
            if (hide) $(this).addClass('ew-tree-table-filter-hide');
        });*/
        components.$loading.hide();
        components.$loading.removeClass('ew-loading-float');
        if (indexList.length === 0) components.$empty.show();
        updateFixedTbHead(components.$view);  // 更新滚动条补丁
    };

    /** 重置搜索 */
    TreeTable.prototype.clearFilter = function () {
        var components = this.getComponents();
        components.$table.children('tbody').children('tr').removeClass('ew-tree-table-filter-hide');
        if (this.options.data.length > 0) components.$empty.hide();
        updateFixedTbHead(components.$view);  // 更新滚动条补丁
    };

    /**
     * 刷新指定父级下的节点
     * @param id 父级id,空则全部刷新
     * @param data 非异步模式替换的数据
     */
    TreeTable.prototype.refresh = function (id, data) {
        if (isClass(id) === 'Array') {
            data = id;
            id = undefined;
        }
        var components = this.getComponents();
        var d, $tr;
        if (id !== undefined) {
            $tr = components.$table.children('tbody').children('tr[data-index="' + this.getIndexById(id) + '"]');
            d = this.getDataByTr($tr);
        }
        if (data) {  // 数据模式
            if (data.length > 0) components.$loading.addClass('ew-loading-float');
            components.$loading.show();
            if (data.length > 0 && this.options.tree.isPidData) {  // pid形式数据
                this.renderBodyData(tt.pidToChildren(data, this.options.tree.idName, this.options.tree.pidName, this.options.tree.childName), d, $tr);
            } else {
                this.renderBodyData(data, d, $tr);
            }
        } else {  // 异步模式
            this.renderBodyAsync(d, $tr);
        }
    };

    /** 删除数据 */
    TreeTable.prototype.del = function (id, index) {
        if (index === undefined) index = this.getIndexById(id);
        var indexList = (typeof index === 'number' ? [index] : index.split('-'));
        var d = this.options.data;
        if (indexList.length > 1) {
            for (var i = 0; i < indexList.length - 1; i++) {
                d = d[parseInt(indexList[i])][this.options.tree.childName];
            }
        }
        d.splice(indexList[indexList.length - 1], 1);
    };

    /** 更新数据 */
    TreeTable.prototype.update = function (id, fields) {
        $.extend(true, this.getDataByTr(this.getIndexById(id)), fields);
    };

    /** 折叠/展开行 */
    function toggleRow($tr) {
        var indent = parseInt($tr.data('indent'));
        var open = $tr.hasClass('ew-tree-table-open');
        if (open) {  // 折叠
            $tr.removeClass('ew-tree-table-open');
            $tr.nextAll('tr').each(function () {
                if (parseInt($(this).data('indent')) <= indent) return false;
                $(this).addClass('ew-tree-tb-hide');
            });
        } else {  // 展开
            $tr.addClass('ew-tree-table-open');
            var hideInd;
            $tr.nextAll('tr').each(function () {
                var ind = parseInt($(this).data('indent'));
                if (ind <= indent) return false;
                if (hideInd !== undefined && ind > hideInd) return true;
                $(this).removeClass('ew-tree-tb-hide');
                if (!$(this).hasClass('ew-tree-table-open')) hideInd = parseInt($(this).data('indent'));
                else hideInd = undefined;
            });
        }
        updateFixedTbHead($tr.parentsUntil('.ew-tree-table').last().parent());
        return open;
    }

    /** 固定表头滚动条补丁 */
    function updateFixedTbHead($view) {
        var $headBox = $view.children('.ew-tree-table-head');
        var $tbBox = $view.children('.ew-tree-table-box');
        //修复滚动条出现时，表头宽度不对的问题
        var sWidth = $headBox.width() - $tbBox.prop('clientWidth');
        $headBox.css('border-right', (sWidth > 0 ? sWidth : 0) + 'px solid #f2f2f2');
    }

    // 监听窗口大小改变
    $(window).resize(function () {
        $('.ew-tree-table').each(function () {
            updateFixedTbHead($(this));
            var $tbBox = $(this).children('.ew-tree-table-box');
            var full = $tbBox.data('full');
            if (full && device.ie && device.ie < 10) {
                $tbBox.css('height', getPageHeight() - full);
            }
        });
    });

    /** 表格溢出点击展开功能 */
    $(document).on('mouseenter', '.ew-tree-table-cell.single-line', function () {
        var $content = $(this).children('.ew-tree-table-cell-content');
        if ($content.prop('scrollWidth') > $content.outerWidth()) $(this).children('.layui-table-grid-down').show();
    }).on('mouseleave', '.ew-tree-table-cell.single-line', function () {
        $(this).children('.layui-table-grid-down').hide();
    });
    // 点击箭头展开
    $(document).on('click', '.ew-tree-table-cell>.layui-table-grid-down', function (e) {
        e.stopPropagation();
        hideAllTdTips();
        var $cell = $(this).parent();
        $cell.addClass('ew-tree-tips-open');
        $cell.children('.layui-table-grid-down').hide();
        var tw = $cell.parent().outerWidth() + 4;
        if ($cell.outerWidth() < tw) $cell.children('.ew-tree-table-cell-content').css({'width': tw, 'max-width': tw});
        var $box = $cell.parents().filter('.ew-tree-table-box');
        if ($box.length === 0) $box = $cell.parents().filter('.ew-tree-table-head');
        if ($box.length === 0) return;
        if (($cell.outerWidth() + $cell.offset().left) + 20 > $box.offset().left + $box.outerWidth()) {
            $cell.addClass('ew-show-left');
        }
        if (($cell.outerHeight() + $cell.offset().top + 10) > $box.offset().top + $box.outerHeight()) {
            $cell.addClass('ew-show-bottom');
        }
    });
    // 点击关闭按钮关闭
    $(document).on('click', '.ew-tree-table-cell>.ew-tree-tips-c', function () {
        hideAllTdTips();
    });
    // 点击空白部分关闭
    $(document).on('click', function () {
        hideAllTdTips();
        $('.ew-tree-table .layui-table-tool-panel').remove();
    });
    $(document).on('click', '.ew-tree-table-cell.ew-tree-tips-open', function (e) {
        e.stopPropagation();
    });

    /* 关闭所有单元格溢出提示框 */
    function hideAllTdTips() {
        $('.ew-tree-table-cell').removeClass('ew-tree-tips-open ew-show-left ew-show-bottom');
        $('.ew-tree-table-cell>.ew-tree-table-cell-content').css({'width': '', 'max-width': ''});
    }

    /** 拖拽调整列宽 */
    $(document).on('mousedown', '.ew-tb-resize', function (e) {
        layui.stope(e);
        var $this = $(this);
        $this.attr('move', 'true');
        var key = $this.parent().data('key');
        $this.data('x', e.clientX);
        var w = $this.parent().parent().parent().parent().children('colgroup').children('col[data-key="' + key + '"]').attr('width');
        if (!w || w.toString().indexOf('%') !== -1) w = $this.parent().outerWidth();
        $this.data('width', w);
        $('body').addClass('ew-tree-table-resizing');
    }).on('mousemove', function (e) {
        var $rs = $('.ew-tree-table .ew-tb-resize[move="true"]');
        if ($rs.length === 0) return;
        layui.stope(e);
        var x = $rs.data('x');
        var w = $rs.data('width');
        var nw = parseFloat(w) + e.clientX - parseFloat(x);
        if (nw <= 0) nw = 1;
        // 更新实例options中的宽度
        var ins = _instances[$rs.parentsUntil('.ew-tree-table').last().parent().attr('lay-filter')];
        var key = $rs.parent().data('key');
        var ks = key.split('-');
        ins.options.cols[ks[0]][ks[1]].width = nw;
        ins.eachCols(function (i, item) {
            if (item.key === key) item.width = nw;
        });
        ins.resize();
    }).on('mouseup', function (e) {
        $('.ew-tree-table .ew-tb-resize[move="true"]').attr('move', 'false');
        $('body').removeClass('ew-tree-table-resizing');
    }).on('mouseleave', function (e) {
        $('.ew-tree-table .ew-tb-resize[move="true"]').attr('move', 'false');
        $('body').removeClass('ew-tree-table-resizing');
    });

    /** 获取顶级的pId */
    function getPids(data, idName, pidName) {
        var pids = [];
        for (var i = 0; i < data.length; i++) {
            var hasPid = false;
            for (var j = 0; j < data.length; j++) {
                if (data[i][pidName] == data[j][idName]) {
                    hasPid = true;
                    break;
                }
            }
            if (!hasPid) pids.push(data[i][pidName]);
        }
        return pids;
    }

    /** 判断pId是否相等 */
    function pidEquals(pId, pIds) {
        if (isClass(pIds) === 'Array') {
            for (var i = 0; i < pIds.length; i++)
                if (pId == pIds[i]) return true;
        }
        return pId == pIds;
    }

    /** 获取变量类型 */
    function isClass(o) {
        if (o === null) return 'Null';
        if (o === undefined) return 'Undefined';
        return Object.prototype.toString.call(o).slice(8, -1);
    }

    /** 获取浏览器高度 */
    function getPageHeight() {
        return document.documentElement.clientHeight || document.body.clientHeight;
    }

    /** 对外提供的方法 */
    var tt = {
        /* 渲染 */
        render: function (options) {
            return new TreeTable(options);
        },
        /* 重载 */
        reload: function (id, opt) {
            _instances[id].reload(opt);
        },
        /* 事件监听 */
        on: function (events, callback) {
            return layui.onevent.call(this, MOD_NAME, events, callback);
        },
        /* pid转children形式 */
        pidToChildren: function (data, idName, pidName, childName, pId) {
            if (!childName) childName = 'children';
            var newList = [];
            for (var i = 0; i < data.length; i++) {
                if (data[i][idName] == data[i][pidName])
                    return console.error('第' + i + '条数据的' + idName + '与' + pidName + '相同', data[i]);
                if (pId === undefined) pId = getPids(data, idName, pidName);
                if (pidEquals(data[i][pidName], pId)) {
                    var children = this.pidToChildren(data, idName, pidName, childName, data[i][idName]);
                    if (children.length > 0) data[i][childName] = children;
                    newList.push(data[i]);
                }
            }
            return newList;
        }
    };

    /** 添加样式 */
    $('head').append([
        '<style id="ew-tree-table-css">',
        '/** 最外层容器 */',
        '.ew-tree-table {',
        '    margin: 10px 0;',
        '    position: relative;',
        '    border: 1px solid #e6e6e6;',
        '    border-bottom: none;',
        '    border-right: none;',
        '}',

        '.ew-tree-table:before, .ew-tree-table:after, .ew-tree-table .ew-tree-table-head:after {',
        '    content: "";',
        '    background-color: #e6e6e6;',
        '    position: absolute;',
        '    right: 0;',
        '    bottom: 0;',
        '}',

        '.ew-tree-table:before {',
        '    width: 1px;',
        '    top: 0;',
        '    z-index: 1;',
        '}',

        '.ew-tree-table:after, .ew-tree-table .ew-tree-table-head:after {',
        '    height: 1px;',
        '    left: 0;',
        '}',

        '.ew-tree-table .layui-table {',
        '    margin: 0;',
        '    position: relative;',
        '    table-layout: fixed;',
        '}',

        '/** 表格 */',
        '.ew-tree-table .layui-table td, .ew-tree-table .layui-table th {',
        '    border-top: none;',
        '    border-left: none;',
        '    padding: 0 !important;',
        '}',

        '.ew-tree-table .ew-tree-table-box {',
        '    overflow: auto;',
        '    position: relative;',
        '}',

        '.ew-tree-table .ew-tree-table-head {',
        '    overflow: hidden;',
        '    box-sizing: border-box;',
        '    background-color: #f2f2f2;',
        '    position: relative;',
        '}',

        '/** loading */',
        '.ew-tree-table div.ew-tree-table-loading {',
        '    padding: 10px 0;',
        '    text-align: center;',
        '}',

        '.ew-tree-table div.ew-tree-table-loading > i {',
        '    color: #999;',
        '    font-size: 30px;',
        '}',

        '.ew-tree-table div.ew-tree-table-loading.ew-loading-float {',
        '    position: absolute;',
        '    top: 0;',
        '    left: 0;',
        '    right: 0;',
        '}',

        '/** 空数据 */',
        '.ew-tree-table .ew-tree-table-empty {',
        '    color: #666;',
        '    font-size: 14px;',
        '    padding: 18px 0;',
        '    text-align: center;',
        '    display: none;',
        '}',

        '/** 单元格 */',
        '.ew-tree-table-cell.ew-tree-tips-open {',
        '    position: absolute;',
        '    top: 0;',
        '    left: 0;',
        '    padding: 0;',
        '    z-index: 9999;',
        '    background-color: #fff;',
        '    box-shadow: 3px 3px 8px rgba(0, 0, 0, .15);',
        '}',

        'thead .ew-tree-table-cell.ew-tree-tips-open {',
        '    background-color: #f2f2f2;',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open.ew-show-left {',
        '    right: 0;',
        '    left: auto;',
        '    box-shadow: -3px 3px 8px rgba(0, 0, 0, .15);',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open.ew-show-bottom {',
        '    bottom: 0;',
        '    top: auto;',
        '    box-shadow: 3px -3px 8px rgba(0, 0, 0, .15);',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open.ew-show-left.ew-show-bottom {',
        '    box-shadow: -3px -3px 8px rgba(0, 0, 0, .15);',
        '}',

        '.ew-tree-table-cell > .ew-tree-tips-c {',
        '    position: absolute;',
        '    right: -6px;',
        '    top: -3px;',
        '    width: 22px;',
        '    height: 22px;',
        '    line-height: 22px;',
        '    font-size: 16px;',
        '    color: #fff;',
        '    background-color: #666;',
        '    border-radius: 50%;',
        '    text-align: center;',
        '    cursor: pointer;',
        '    display: none;',
        '}',

        'table tr:first-child .ew-tree-table-cell > .ew-tree-tips-c {',
        '    top: 0;',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open > .ew-tree-tips-c {',
        '    display: block;',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open.ew-show-left > .ew-tree-tips-c {',
        '    left: -6px;',
        '    right: auto;',
        '}',

        '.ew-tree-table-cell > .ew-tree-table-cell-content {',
        '    padding: 5px 15px;',
        '    line-height: 28px;',
        '}',

        '[lay-size="lg"] .ew-tree-table-cell > .ew-tree-table-cell-content {',
        '    line-height: 40px;',
        '}',

        '[lay-size="sm"] .ew-tree-table-cell > .ew-tree-table-cell-content {',
        '    padding: 1px 15px;',
        '}',

        '.ew-tree-table-cell.single-line > .ew-tree-table-cell-content {',
        '    overflow: hidden;',
        '    white-space: nowrap;',
        '    text-overflow: ellipsis;',
        '}',

        '.ew-tree-table-cell.ew-tree-tips-open > .ew-tree-table-cell-content {',
        '    overflow: auto;',
        '    padding: 9px 15px;',
        '    height: auto;',
        '    min-height: 100%;',
        '    max-height: 110px;',
        '    line-height: inherit;',
        '    max-width: 260px;',
        '    width: 200px;',
        '    width: max-content;',
        '    width: -moz-max-content;',
        '    box-sizing: border-box;',
        '    white-space: normal;',
        '}',

        '.ew-tree-table-cell > .layui-table-grid-down {',
        '    box-sizing: border-box;',
        '}',

        '/** 图标列 */',
        '.ew-tree-table .ew-tree-pack {',
        '    cursor: pointer;',
        '    line-height: 16px;',
        '}',

        '.ew-tree-table .ew-tree-pack > .layui-icon, .ew-tree-table .ew-tree-pack > .ew-tree-icon {',
        '    margin-right: 5px;',
        '}',

        '.ew-tree-table .ew-tree-pack > * {',
        '    vertical-align: middle;',
        '}',

        '/* 缩进 */',
        '.ew-tree-table .ew-tree-table-indent {',
        '    margin-right: 5px;',
        '    padding-left: 16px;',
        '}',

        '/* 箭头 */',
        '.ew-tree-table .ew-tree-table-arrow:before {',
        '    content: "\\e623";',
        '}',

        '.ew-tree-table .ew-tree-table-open .ew-tree-table-arrow:before {',
        '    content: "\\e625";',
        '}',

        '.ew-tree-table .ew-tree-table-arrow.arrow2 {',
        '    font-size: 12px;',
        '    font-weight: 600;',
        '    line-height: 16px;',
        '    height: 16px;',
        '    width: 16px;',
        '    display: inline-block;',
        '    text-align: center;',
        '    color: #888;',
        '}',

        '.ew-tree-table .ew-tree-table-arrow.arrow2:before {',
        '    content: "\\e602";',
        '}',

        '.ew-tree-table .ew-tree-table-open .ew-tree-table-arrow.arrow2:before {',
        '    content: "\\e61a";',
        '}',

        '.ew-tree-table-arrow.ew-tree-table-arrow-hide {',
        '    visibility: hidden;',
        '}',

        '/* 箭头变加载中状态 */',
        '.ew-tree-table tr.ew-tree-table-loading > td .ew-tree-table-arrow:before {',
        '    content: "\\e63d" !important;',
        '}',

        '.ew-tree-table tr.ew-tree-table-loading > td .ew-tree-table-arrow {',
        '    margin-right: 0;',
        '}',

        '.ew-tree-table tr.ew-tree-table-loading > td .ew-tree-table-arrow + * {',
        '    margin-left: 5px;',
        '}',

        '.ew-tree-table tr.ew-tree-table-loading * {',
        '    pointer-events: none !important;',
        '}',

        '/** 折叠行 */',
        '.ew-tree-table .ew-tree-tb-hide {',
        '    display: none;',
        '}',

        '/** 特殊列调整 */',
        '.ew-tree-table td[data-type="numbers"] > .ew-tree-table-cell,',
        '.ew-tree-table th[data-type="numbers"] > .ew-tree-table-cell,',
        '.ew-tree-table td[data-type="checkbox"] > .ew-tree-table-cell,',
        '.ew-tree-table th[data-type="checkbox"] > .ew-tree-table-cell,',
        '.ew-tree-table td[data-type="radio"] > .ew-tree-table-cell,',
        '.ew-tree-table th[data-type="radio"] > .ew-tree-table-cell,',
        '.ew-tree-table td[data-type="space"] > .ew-tree-table-cell,',
        '.ew-tree-table th[data-type="space"] > .ew-tree-table-cell {',
        '    padding-left: 0;',
        '    padding-right: 0;',
        '}',

        '/* 单元格内表单元素样式调整 */',
        '.ew-tree-table .layui-form-switch',
        '.ew-tree-table .layui-form-radio {',
        '    margin: 0;',
        '}',

        '/* checkbox列调整 */',
        '.ew-tree-table-checkbox + .layui-form-checkbox {',
        '    padding: 0;',
        '}',

        '.ew-tree-table-checkbox + .layui-form-checkbox > .layui-icon {',
        '    font-weight: 600;',
        '    color: transparent;',
        '    transition: background-color .1s linear;',
        '    -webkit-transition: background-color .1s linear;',
        '}',

        '.ew-tree-table-checkbox + .layui-form-checkbox.layui-form-checked > .layui-icon {',
        '    color: #fff;',
        '}',

        '/* checkbox半选状态 */',
        '.ew-form-indeterminate + .layui-form-checkbox .layui-icon:before {',
        '    content: "";',
        '    width: 10px;',
        '    height: 2px;',
        '    background-color: #f1f1f1;',
        '    position: absolute;',
        '    top: 50%;',
        '    left: 50%;',
        '    margin: -1px 0 0 -5px;',
        '}',

        '/* radio列调整 */',
        '.ew-tree-table-radio + .layui-form-radio {',
        '    margin: 0;',
        '    padding: 0;',
        '    height: 20px;',
        '    line-height: 20px;',
        '}',

        '.ew-tree-table-radio + .layui-form-radio > i {',
        '    margin: 0;',
        '    height: 20px;',
        '    font-size: 20px;',
        '    line-height: 20px;',
        '}',

        '/** 单元格编辑 */',
        '.ew-tree-table .layui-table td[data-edit] {',
        '    cursor: text;',
        '}',

        '.ew-tree-table .ew-tree-table-edit {',
        '    position: absolute;',
        '    left: 0;',
        '    top: 0;',
        '    width: 100%;',
        '    height: 100%;',
        '    border-radius: 0;',
        '    box-shadow: 1px 1px 20px rgba(0, 0, 0, .15);',
        '}',

        '.ew-tree-table .ew-tree-table-edit:focus {',
        '    border-color: #5FB878 !important;',
        '}',

        '.ew-tree-table .ew-tree-table-edit.layui-form-danger {',
        '    border-color: #FF5722 !important;',
        '}',

        '/** 搜索数据隐藏行 */',
        '.ew-tree-table tr.ew-tree-table-filter-hide {',
        '    display: none !important;',
        '}',

        '/** 头部工具栏 */',
        '.ew-tree-table .ew-tree-table-tool {',
        '    min-height: 50px;',
        '    line-height: 30px;',
        '    padding: 10px 15px;',
        '    box-sizing: border-box;',
        '    background-color: #f2f2f2;',
        '    border-bottom: 1px solid #e6e6e6;',
        '}',

        '.ew-tree-table .ew-tree-table-tool .ew-tree-table-tool-right {',
        '    float: right;',
        '}',

        '.ew-tree-table .ew-tree-table-tool .ew-tree-table-tool-item {',
        '    position: relative;',
        '    color: #333;',
        '    width: 26px;',
        '    height: 26px;',
        '    line-height: 26px;',
        '    text-align: center;',
        '    margin-left: 10px;',
        '    display: inline-block;',
        '    border: 1px solid #ccc;',
        '    box-sizing: border-box;',
        '    vertical-align: middle;',
        '    -webkit-transition: .3s all;',
        '    transition: .3s all;',
        '    cursor: pointer;',
        '}',

        '.ew-tree-table .ew-tree-table-tool .ew-tree-table-tool-item:first-child {',
        '    margin-left: 0;',
        '}',

        '.ew-tree-table .ew-tree-table-tool .ew-tree-table-tool-item:hover {',
        '    border-color: #999;',
        '}',

        '.ew-tree-table .ew-tree-table-tool-right .layui-table-tool-panel {',
        '    left: auto;',
        '    right: -1px;',
        '    z-index: 9999;',
        '}',

        '/* 列宽拖拽调整 */',
        '.ew-tree-table .ew-tb-resize {',
        '    position: absolute;',
        '    right: 0;',
        '    top: 0;',
        '    bottom: 0;',
        '    width: 10px;',
        '    cursor: col-resize;',
        '}',

        '.ew-tree-table-resizing {',
        '    cursor: col-resize;',
        '    -ms-user-select: none;',
        '    -moz-user-select: none;',
        '    -webkit-user-select: none;',
        '    user-select: none;',
        '}',

        '/* 辅助样式 */',
        '.ew-tree-table .layui-form-switch {',
        '    margin: 0;',
        '}',

        '.ew-tree-table .pd-tb-0 {',
        '    padding-top: 0 !important;',
        '    padding-bottom: 0 !important;',
        '}',

        '.ew-tree-table .break-all {',
        '    word-break: break-all !important;',
        '}',

        '/** 扩展图标 */',
        '.ew-tree-table .ew-tree-icon-folder:after, .ew-tree-table .ew-tree-icon-file:after {',
        '    content: "";',
        '    padding: 2px 10px;',
        '    -webkit-background-size: cover;',
        '    -moz-background-size: cover;',
        '    -o-background-size: cover;',
        '    background-size: cover;',
        '    background-repeat: no-repeat;',
        '    background-image: url("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAE6UlEQVR4Xu2ZPYhcVRiGny9hC0FsTEBCGkFTWIQE/IlpgmKpWAiLyR0XLbYTxEKxkCAEhRCxEOwsjJnJioKIYClWKgqiskIIaCoLYyASVJT87JGspN37LrOXvec777Tzzrnvz7Nn2dnAr6YbiKbTOzwGoHEIDIABaLyBxuP7BjAAjTfQeHzfAAag8QYaj+8bwAA03kDj8X0DGIDGG2g8/lw3QFlhD9fZN7oOF7gYT3NudL5GaGjTAJQP2c1VXgIOExyGkf4/oXCe4I3oeH+EvY/G0qYAKFOWCV4Hdo8mQb+RL7jBE7HE3/3S9hQyAGXGCeDVSiv6lqscief4t1L/g9mWAChTHiD4ZrTXvVbPu9GxrEnbUWkAzPgeOJCglsXo+ChBji2L0AtAmfEgrP/0Z3hdZoF7Y5HLGcJsRQYFgLeAF7fiYSM540x0LI3Ey7bb6AdgytcEh7bd6VYa2MGhOJrmVpurmX4AZlwCds31lPF9+AI32O8/DYUvccqUKwR3jG/DuR29Gd36F1pNv/pvgKwAFK5RuC+e4eeWCWgXgJurFz6LCY8bgA0aSPwr4P/UhVNc43ir3xK2fQPcAr/wO/AewU/Ar6xRqroVdvAnC6zGIlc369sAbLax8er/ofAJOzkVR9e/uZVeBkCqqTrRyeh4RXFtAJSWatQUno0Jp/usG4C+hup9/yJ72BuPcH2jCAag3oEV50vRccYAKFXl1LwdHS8YgJzj9qcqfBoTnjQA/VVlVXwXHfcbgKzz9uf6IToOGoD+orIqDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzGUAxKKyygxA1mXFXAZALCqrzABkXVbMZQDEorLKDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzGUAxKKyygxA1mXFXAZALCqrzABkXVbMZQDEorLKDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzGUAxKKyygxA1mXFXAZALCqrzABkXVbMZQDEorLKDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzGUAxKKyygxA1mXFXAZALCqrzABkXVbMZQDEorLKDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzGUAxKKyygxA1mXFXAZALCqrzABkXVbMZQDEorLKDEDWZcVcBkAsKqvMAGRdVsxlAMSissoMQNZlxVwGQCwqq8wAZF1WzLUFAMy4BOwSH2jZmBoorMaE/RtZij6/ZcoFgrv7dH5/lA38Eh33zAfAjM+BR0cZz6Y2bqDwY0w4MB8AU04SvOyuq2zg4+h4aj4AzvIwha+qjN+66cLzMeGduQC4+eEy4zywr/U+K8t/hQX2xiJ/zQ/ACgdZ40vgtspKaNducCyOsdJXQO9fAbcOKGc5whofENzVd6jf3/YGTkTHccWFDMD6r4LT3MlOloHHCB4CblceYs3gDfxB4TeCcxReiwmr6hM3BYB6qHX1NGAA6tlqEKcGYJBa6znUANSz1SBODcAgtdZzqAGoZ6tBnBqAQWqt51ADUM9Wgzg1AIPUWs+hBqCerQZxagAGqbWeQw1APVsN4tQADFJrPYcagHq2GsTpf+KxwJB5Cd5mAAAAAElFTkSuQmCC");',
        '}',

        '.ew-tree-table tr.ew-tree-table-open > td .ew-tree-icon-folder:after {',
        '    background-image: url("data:image/png;base64,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");',
        '}',

        '.ew-tree-table .ew-tree-icon-file:after {',
        '    background-image: url("data:image/png;base64,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");',
        '}',
        '</style>'
    ].join(''));

    exports('treeTable', tt);
});
