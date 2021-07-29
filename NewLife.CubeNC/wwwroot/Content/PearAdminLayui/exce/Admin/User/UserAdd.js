layui.use(['xmSelect', 'commonExtend', 'upload'], function () {
    let xmSelect = layui.xmSelect;
    let $ = layui.jquery;
    let form = layui.form,
        apiUtil = layui.commonExtend,
        layer = layui.layer,
        upload = layui.upload;

    xmSelect.render({
        el: '[xid=enable]',
        name: "Enable",
        radio: true,
        clickClose: true,
        theme: {
            color: '#0081ff',
        },
        toolbar: {
            show: true,
            list: ['ALL', 'CLEAR']
        },
        data: [
            { name: '启用', value: 1 },
            { name: '禁用', value: 0 }
        ]
    });

    apiUtil.ajax('/Admin/User/GetRole', [], "get", function (res) {
        var user_RoleID =xmSelect.render({
            el: '[xid=user_RoleID]',
            name: "RoleID",
            radio: true,
            clickClose: true,
            prop: {
                name: 'name',
                value: 'id',
            },
            toolbar: {
                show: true,
                list: ['ALL', 'CLEAR']
            },
            data: function () {
                return res.data;
            }
        });
        var user_RoleIDs = xmSelect.render({
            el: '[xid=user_RoleIDs]',
            name: "RoleIDs",
            prop: {
                name: 'name',
                value: 'id',
            },
            toolbar: {
                show: true,
                list: ['ALL', 'CLEAR']
            },
            data: function () {
                return res.data;
            }
        });
    });

    //上级部门
    apiUtil.ajax('/Admin/User/GetSelectDeparTree', {}, "get", function (res) {
        var tree_DepartmentID = xmSelect.render({
            el: '#tree_DepartmentID',
            autoRow: true,
            filterable: true,
            name: "DepartmentID",
            radio: true,
            clickClose: true,
            tree: {
                show: true,
                showFolderIcon: true,
                showLine: true,
                //非严格模式
                strict: false,
                indent: 20,
                expandedKeys: false,
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
    });


    //拖拽上传
    upload.render({
        elem: '#user_Attachment'
        , url: '/Sys/Home/MultiFileUpload' //改成您自己的上传接口
        , accept: 'file' //普通文件
        , done: function (res) {
            var filePath = res.filePathArray[0].FilePath;
            $(".form input[name='Avatar']").val(filePath);
            layui.$('#user_Attachment_Upload').removeClass('layui-hide').find('img').attr('src', filePath);
        }
        , before: function (obj) {
            element.progress('demo', '0%');//进度条复位
            layer.msg('上传中', { icon: 16, time: 0 });
        }
        //进度条
        , progress: function (n, index, e) {
            element.progress('demo', n + '%');
            if (n == 100) {
                layer.msg('上传完毕', { icon: 1 });
            }
        }
    });
    var index = parent.layer.getFrameIndex(window.name); //先得到当前iframe层的索引
    form.render();
    form.on('submit(saveBtn)', function (data) {
        var obj = $(this);
        $(obj).attr("disabled", true).addClass("layui-btn-disabled_self");
        $(obj).text("正在保存中...");
        data.field.Enable = data.field.Enable == 1 ? true : false;
        apiUtil.ajax('/Admin/User/Add', data.field, "POST", function (res) {
            if (res.code == 0) {
                apiUtil.swalSuccess({ title: "新增用户成功" }, function () {
                    parent.refeshTable();
                    parent.layer.close(index);
                });

            } else {
                $(obj).attr("disabled", false).removeClass("layui-btn-disabled_self");
                $(obj).text("确定保存");
                apiUtil.swalError("新增用户失败", res.msg);
            }
        });
        return false;
    });
});