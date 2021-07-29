layui.define(['jquery', 'commonExtend', 'treeTablelay', 'table'], function (exports) {
    "use strict";

    let table = layui.table;
    let $ = layui.jquery;
    let apiUtil = layui.commonExtend;
    let treetable = layui.treeTablelay;

    let viewShowTree =
    {
        table:
        {
            id: "", //表格ID
            form_filter: "",//表单过滤名称，如lay-filter="comm-query"
            getTable: "",//获取表格
            curr: "",//当前页码
        },
        inti: function (tableOption, opt, doneFunc) {
            let isTable = typeof (opt.isTable) == "undefined" ? false : true;
            let intiData = {
                tableId: opt.tableId,//表格ID
                form_filter: opt.form_filter,//表单过滤名称，如lay-filter="comm-query"
                tableToolBar:
                {
                    add:
                    {
                        title: opt.tableToolBar.add.title,
                        url: opt.tableToolBar.add.url,//新增
                        area: opt.tableToolBar.add.area,
                    }
                },
                tableTool:
                {
                    detail:
                    {
                        title: opt.tableTool.detail.title,
                        url: opt.tableTool.detail.url,//详情
                        area: opt.tableTool.detail.area,
                    },
                    edit:
                    {
                        title: opt.tableTool.edit.title,
                        url: opt.tableTool.edit.url,//编辑
                        area: opt.tableTool.edit.area,
                    },
                    delete:
                    {
                        url: opt.tableTool.delete.url,//删除
                        data: opt.tableTool.delete.data
                    }
                }
            }
            tableOption.done = function (res, curr, count) {
                viewShowTree.table.curr = curr;
                doneFunc(res, curr, count);
            }
            viewShowTree.table.id = intiData.tableId;
            treetable.render(tableOption);

            let tableTool = 'tool(' + intiData.tableId + ')';
            treetable.on(tableTool, function (obj) {
                if (obj.event === 'remove') {
                    apiUtil.swalConfirm({}, function () {
                        let content = intiData.tableTool.delete.url + obj.data.id;
                        apiUtil.ajax(content, intiData.tableTool.delete.data, "POST", function (res) {
                            if (res.code == 0) {
                                apiUtil.swalSuccess({ title: "删除成功" }, function () {
                                    viewShowTree.refeshTable();
                                });

                            } else {
                                apiUtil.swalError("删除失败", res.msg);
                            }
                        });
                    });
                } else if (obj.event === 'edit') {//编辑
                    $.extend(intiData.tableTool.edit, { content: intiData.tableTool.edit.url + obj.data.id });
                    viewShowTree.layerOpen(intiData.tableTool.edit);
                } else if (obj.event === 'detail') {//详情
                    $.extend(intiData.tableTool.detail, { content: intiData.tableTool.detail.url + obj.data.id });
                    viewShowTree.layerOpen(intiData.tableTool.detail);
                }
            });
            let tableToolBar = 'toolbar(' + intiData.tableId + ')';
            treetable.on(tableToolBar, function (obj) {
                if (obj.event === 'add') {//新增
                    $.extend(intiData.tableToolBar.add, { content: intiData.tableToolBar.add.url });
                    viewShowTree.layerOpen(intiData.tableToolBar.add);
                }
            });
        },
        refeshTable: function () {
            treetable.reload(viewShowTree.table.id);
        },
        layerOpen: function (open) {
            layer.open({
                title: open.title,
                type: 2,
                offset: "0px",
                shade: 0.2,
                maxmin: true,
                area: open.area,
                content: open.content,
            });
        }
    };
    exports('viewShowTree', viewShowTree);
});

