layui.use(['xmSelect', 'form', 'layer', 'commonExtend'], function () {
    var $ = layui.jquery,
        form = layui.form,
        apiUtil = layui.commonExtend,
        layer = layui.layer;

    //上级菜单
    apiUtil.ajax('/Admin/Menu/GetSelectTree', [], "get", function (res) {
        var treeParentID = xmSelect.render({
            el: '[xid=parentID]',
            autoRow: true,
            filterable: true,
            name: "ParentID",
            radio: true,
            clickClose: true,
            tree: {
                show: true,
                //非严格模式
                strict: false,
                //默认展开节点
                expandedKeys: true,
            },
            //toolbar: {
            //    show: true,
            //    list: ['ALL', 'REVERSE', 'CLEAR']
            //},
            filterable: true,
            height: 'auto',
            data: function () {
                return res.data;
            }
        });
        let parentID = $("#tree_ParentID_hidden").val();
        console.log(parentID);
        treeParentID.setValue([parentID]);
    });
    var index = parent.layer.getFrameIndex(window.name); //先得到当前iframe层的索引
    form.render();
    //图标
    $("#menu_add_iconxz").on("click", function () {
        layer.open({
            title: "图标展示",
            type: 2,
            offset: "0px",
            shade: 0.2,
            maxmin: true,
            area: ["100%", "100%"],
            content: "/Sys/Menu/MenuIcon"
        });
    });
    form.on('submit(saveBtn)', function (data) {
        var obj = $(this);
        $(obj).attr("disabled", true).addClass("layui-btn-disabled_self");
        $(obj).text("正在保存中...");

        apiUtil.ajax('/Admin/Menu/Edit', data.field, "POST", function (res) {
            if (res.code == 0) {
                apiUtil.swalSuccess({ title: "编辑菜单成功" }, function () {
                    parent.refeshTable();
                    parent.layer.close(index);
                });

            } else {
                $(obj).attr("disabled", false).removeClass("layui-btn-disabled_self");
                $(obj).text("确定保存");
                apiUtil.swalError("编辑菜单失败", res.msg);
            }
        });
        return false;
    });
});