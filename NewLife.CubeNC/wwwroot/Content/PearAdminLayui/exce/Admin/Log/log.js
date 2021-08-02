layui.use(['table', 'form', "commonExtend"], function () {
    let $ = layui.jquery;
    let table = layui.table;
    let form = layui.form;
    let apiUtil = layui.commonExtend;
    let cols = [
        { field: 'category', title: '类别', align: 'center' },
        { field: 'action', title: '操作', align: 'center' },
        { field: 'success', title: '成功', align: 'center', templet: '#success' },
        { field: 'remark', title: '详细信息', align: 'center' },
        { field: 'linkID', title: '链接', align: 'center' },
        { field: 'userName', title: '用户名', align: 'center' },
        { field: 'createIP', title: '地址', align: 'center' },
        { field: 'createAddress', title: '物理地址', align: 'center' },
        { field: 'createTime', title: '创建时间', align: 'center' },
/*        { field: 'range', title: '附近', align: 'center', templet: '#range' },*/
    ];

    let comm_tableId = "log-table";
    let comm_url = "/Admin/Log";
    let form_filter = 'submit(comm-query)';// 监听搜索操作
    var opt =
    {
        id: comm_tableId,
        elem: '#' + comm_tableId,
        url: comm_url,
        page: true,
        cols: [cols],
        skin: 'line',
        request:
        {
            pageName: 'PageIndex',  //页码的参数名称，默认：page
            limitName: 'PageSize',  //每页数据量的参数名，默认：limit
        },
        toolbar: true,
        defaultToolbar: [{
            layEvent: 'refresh',
            icon: 'layui-icon-refresh',
        }, 'filter', 'print', 'exports']
        , parseData: function (res) { //res 即为原始返回的数据
            return {
                "code": res.code, //解析接口状态
                "msg": res.message, //解析提示文本
                "count": res.pager.totalCount, //解析数据长度
                "data": res.data //解析数据列表
            };
        }
    }
    table.render(opt);
    form.on(form_filter, function (data) {
        console.log(data);
        //执行搜索重载
        table.reload(comm_tableId,{
            page: {
                curr: 1
            }
            , where: data.field
        }, 'data');
        return false;
    });
});