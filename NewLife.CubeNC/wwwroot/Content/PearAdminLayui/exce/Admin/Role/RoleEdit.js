//layui.use(['jquery', 'treeTablelay', 'form','table'], function () {
//    let table = layui.table;
//    let $ = layui.jquery;
//    let treetable = layui.treeTablelay;
//    let form = layui.form;

//    let cols = [
//        { type: 'checkbox',},
//        { field: 'id', hide: true },
//        { field: 'displayName', title: '权限名称', width:300},
//        { field: 'permission', title: '权限子项', templet: "#permission"},
//    ];
//    let comm_title = "菜单";
//    let comm_url = "/Admin/Role/GetMenuTreeQX";
//    let comm_url2 = "/Admin/Menu";
//    let comm_tableId = "role-table";
//    let comm_form_filter = "comm-query";

//    //var opt = {
//    //    treeColIndex: 1,
//    //    treeSpid: 0,
//    //    treeIdName: 'id',
//    //    treePidName: 'parentID',
//    //    treeDefaultClose: false,
//    //    page: true,
//    //    elem: '#' + comm_tableId,
//    //    url: comm_url,
//    //    page: false,
//    //    cols: [cols],
//    //    skin: 'nob',
//    //    toolbar: '#common-toolbar',
//    //    defaultToolbar: [{
//    //        layEvent: 'refresh',
//    //        icon: 'layui-icon-refresh',
//    //    }, 'filter', 'print', 'exports']
//    //    , parseData: function (res) { //res 即为原始返回的数据
//    //        return {
//    //            "code": res.code, //解析接口状态
//    //            "data": res.data //解析数据列表
//    //        };
//    //    }
//    //};
//    //treetable.render(opt);

//    var opt = {
//        tree: {
//            iconIndex: 2,
//            isPidData: true,
//            idName: 'id',
//            pidName: 'parentID',
//            openName: 'id',
//        },
//        elem: '#' + comm_tableId,
//        url: comm_url,
//        page: false,
//        cols: [cols],
//        skin: "nob", 
//        parseData: function (res) { //res 即为原始返回的数据
//            return {
//                "code": res.code, //解析接口状态
//                "data": res.data //解析数据列表
//            };
//        }
//    }
//    treetable.render(opt);
//    form.on('checkbox(checkboxIsSelected)', function (data) {
//        var checked = data.elem.checked;
//        var $cb = $(data.elem);
//        var $layCb = $cb.next('.layui-form-checkbox');
//        console.log($layCb);
//    });   
//});


layui.use(['form', 'layer', 'xmSelect', 'commonExtend'], function () {
    var $ = layui.jquery,
        form = layui.form,
        apiUtil = layui.commonExtend,
        layer = layui.layer;

    $('input.authorize').on('change', function () {
        var $this = $(this);
        var status = $this.prop('checked');
        var childkey = $this.attr('child');
        // 规避change 需要在失去焦点时触发的问题，设置值完成后手工再次触发该事件
        $('input[parentkey="' + childkey + '"]').prop('checked', status).change();
    });
    // 只读/查看
    $('input.pro_detail').on('change', function () {
        $this = $(this);
        var status = $this.prop('checked');
        var key = $this.attr('prochildkey');
        $('input[proparentkey=' + key + ']').prop('checked', status);
    });
    // 全部权限
    $('input.pro_all').on('change', function () {
        $this = $(this);
        var status = $this.prop('checked');
        var key = $this.attr('prochildkey');
        $('input[prokey=' + key + ']').prop('checked', status);
    });
    var index = parent.layer.getFrameIndex(window.name); //先得到当前iframe层的索引
    form.render();
    form.on('submit(saveBtn)', function (data) {
        var obj = $(this);
        $(obj).attr("disabled", true).addClass("layui-btn-disabled_self");
        $(obj).text("正在保存中...");
        var data = apiUtil.serializeObject($('#formRole'));
        apiUtil.ajax('/Admin/Role/Edit', data, "POST", function (res) {
            if (res.code == 0) {
                apiUtil.swalSuccess({ title: "编辑角色成功" }, function () {
                    parent.refeshTable();
                    parent.layer.close(index);
                });

            } else {
                $(obj).attr("disabled", false).removeClass("layui-btn-disabled_self");
                $(obj).text("确定保存");
                apiUtil.swalError("编辑角色失败", res.msg);
            }
        });
        return false;
    });
});


//layui.use(['form', 'layer', 'xmSelect',  'commonExtend'], function () {
//    var $ = layui.jquery,
//        form = layui.form,
//        apiUtil = layui.commonExtend,
//        layer = layui.layer;
//    let xmSelect = layui.xmSelect;
//    apiUtil.ajax('/Admin/Role/GetMenuTreeQX', [], "get", function (res) {
//        var tree_Permission = xmSelect.render({
//            el: '#tree_Permission',
//            autoRow: true,
//            filterable: true,
//            name: "permission",
//            tree: {
//                show: true,
//                showFolderIcon: true,
//                showLine: true,
//                //非严格模式
//                strict: true,
//                indent: 20,
//                expandedKeys: true,
//            },
//            toolbar: {
//                show: true,
//                list: ['ALL', 'REVERSE', 'CLEAR']
//            },
//            filterable: true,
//            height: 'auto',
//            data: function () {
//                return res.data;
//            }
//        });
//        tree_Permission.changeExpandedKeys(false);
//        var arr = $("#tree_Permission_hidden").val().split(',');
//        console.log(arr);
//        tree_Permission.setValue(arr);
//    });

//    var index = parent.layer.getFrameIndex(window.name); //先得到当前iframe层的索引
//    form.render();
//    form.on('submit(saveBtn)', function (data) {
//        var obj = $(this);
//        $(obj).attr("disabled", true).addClass("layui-btn-disabled_self");
//        $(obj).text("正在保存中...");

//        apiUtil.ajax('/Sys/Role/RoleAdd', data.field, "POST", function (res) {
//            if (res.code == 0) {
//                apiUtil.swalSuccess({ title: "编辑角色成功" }, function () {
//                    parent.refeshTable();
//                    parent.layer.close(index);
//                });

//            } else {
//                $(obj).attr("disabled", false).removeClass("layui-btn-disabled_self");
//                $(obj).text("确定保存");
//                apiUtil.swalError("编辑角色失败", res.msg);
//            }
//        });
//        return false;
//    });
//});