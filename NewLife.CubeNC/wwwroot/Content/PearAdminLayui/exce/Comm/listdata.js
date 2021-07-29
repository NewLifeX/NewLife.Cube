layui.use(['viewShow'], function () {
    let $ = layui.jquery;
    let viewShow = layui.viewShow;
    let reg = new RegExp("&quot;", "g");
    let cols = JSON.parse($("#comm_colsArray").val().replace(reg, '"'));
    let cl =
    {
        title: "操作",
        toolbar: '#common-bar',
        align: 'left',
        width: 200,
        fixed: 'right'
    }
    cols.push(cl);
    let comm_title = $("#comm_title").val();
    let comm_url = $("#comm_url").val();
    let comm_tableId = "comm-table";
    let comm_form_filter = "comm-query";
    viewShow.inti(
        {
            elem: '#' + comm_tableId,
            url: comm_url,
            page: true,
            cols: [cols],
            skin: 'line',
            toolbar: '#common-toolbar',
            request:
            {
                pageName: 'PageIndex',  //页码的参数名称，默认：page
                limitName: 'PageSize',  //每页数据量的参数名，默认：limit
            },
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
        },
        {
            tableId: comm_tableId,//表格ID
            form_filter: comm_form_filter,//表单过滤名称，如lay-filter="comm-query"
            tableToolBar:
            {
                add:
                {
                    title: comm_title + "新增",
                    url: comm_url + "/Add/",
                    area: ["660px", "780px"],
                }
            },
            tableTool:
            {
                detail:
                {
                    title: comm_title + "详情",
                    url: comm_url + "/Detail/",
                    area: ["660px", "780px"],
                },
                edit:
                {
                    title: comm_title + "编辑",
                    url: comm_url + "/Edit/",//编辑
                    area: ["660px", "780px"],
                },
                delete:
                {
                    title: comm_title + "删除",
                    url: comm_url + "/Delete/",//编辑
                    data: {}
                }
            }
        },
        function (res, curr, count) {
            //$.ajax({
            //    type: "GET",
            //    url: "/Cube/GetInfo",
            //    async: false,
            //    //data: { username: $("#username").val(), content: $("#content").val() },
            //    dataType: "json",
            //    success: function (data) {
            //        console.log(JSON.stringify(data));
            //    }
            //});
        });
    refeshTable = function () {
        viewShow.refeshTable();
    };
});