layui.use(['viewShowTree'], function () {
    let $ = layui.jquery;
    let viewShowTree = layui.viewShowTree;

    let cols = [
        { type: 'numbers' },
        { type: 'checkbox' },
        { field: 'displayName', title: '菜单名称' },
        { field: 'permission', title: '权限子项'},
        {
            title: '菜单图标', align: 'center',
            templet: '<p><i class="{{d.icon}}"></i></p>'
        },
        { field: 'url', title: '路径'},
        { field: 'sort', title: '排序'},
    ];
    let cl =
    {
        title: "操作",
        toolbar: '#common-bar',
        align: 'left',
        fixed: 'right'
    }
    cols.push(cl);
    let comm_title = "菜单";
    let comm_url = "/Admin/Menu/GetMenuAll";
    let comm_url2 = "/Admin/Menu";
    let comm_tableId = "menu-table";
    let comm_form_filter = "comm-query";

    viewShowTree.inti(
        {
            tree: {
                iconIndex: 2,
                isPidData: true,
                idName: 'id',
                pidName: 'parentID',
                openName: 'id',
            },
            elem: '#' + comm_tableId,
            url: comm_url,
            page: false,
            cols: [cols],
            toolbar: '#common-toolbar',
            defaultToolbar: [{
                layEvent: 'refresh',
                icon: 'layui-icon-refresh',
            }, 'filter', 'print', 'exports']
            , parseData: function (res) { //res 即为原始返回的数据
                return {
                    "code": res.code, //解析接口状态
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
                    url: comm_url2 + "/Add/",
                    area: ["660px", "780px"],
                }
            },
            tableTool:
            {
                detail:
                {
                    title: comm_title + "详情",
                    url: comm_url2 + "/Detail/",
                    area: ["660px", "780px"],
                },
                edit:
                {
                    title: comm_title + "编辑",
                    url: comm_url2 + "/Edit/",//编辑
                    area: ["660px", "780px"],
                },
                delete:
                {
                    title: comm_title + "删除",
                    url: comm_url2 + "/Delete/",//编辑
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
        viewShowTree.refeshTable();
    };
});