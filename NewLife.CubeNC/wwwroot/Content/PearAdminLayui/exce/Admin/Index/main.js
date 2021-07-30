layui.use(['table', 'form',"commonExtend"], function () {
    let $ = layui.jquery;
    let table = layui.table;
    let reg = new RegExp("&quot;", "g");
    let data = JSON.parse($("#comm_dataArray").val().replace(reg, '"'));
    let form = layui.form;
    let apiUtil = layui.commonExtend;
    let cols = [
        { field: 'name', title: '名称', align: 'center'},
        { field: 'title', title: '标题', align: 'center' },
        { field: 'fileVersion', title: '文件版本', align: 'center' },
        { field: 'version', title: '内部版本', align: 'center' },
        { field: 'compile', title: '编译时间', align: 'center' },
        { field: 'description', title: '描述', align: 'center'},
    ];

    let comm_tableId = "ass-table";
    var opt =
    {
        id: comm_tableId,
        elem: '#' + comm_tableId,
        data: data,
        page: false,
        cols: [cols],
        skin: 'line',
        defaultToolbar: [{
            layEvent: 'refresh',
            icon: 'layui-icon-refresh',
        }, 'filter', 'print', 'exports']
    }
    table.render(opt);
    form.on('submit(restart)', function (data) {
        var obj = $(this);
        $(obj).attr("disabled", true).addClass("layui-btn-disabled_self");
        $(obj).text("正在重启中...");
        var url = $(obj).attr("data-href");
        console.log(url);
        apiUtil.swalConfirm({ title: "确定重启吗", html: "仅重启ASP.Net Core应用程序域，而不是操作系统！<br/>确认重启？" }, function () {
            window.location.href = url;
        });
        return false;
    });
}); 