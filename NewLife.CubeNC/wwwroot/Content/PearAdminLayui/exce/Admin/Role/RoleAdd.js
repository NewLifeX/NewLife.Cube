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
        apiUtil.ajax('/Admin/Role/Add', data, "POST", function (res) {
            if (res.code == 0) {
                apiUtil.swalSuccess({ title: "新增角色成功" }, function () {
                    parent.refeshTable();
                    parent.layer.close(index);
                });

            } else {
                $(obj).attr("disabled", false).removeClass("layui-btn-disabled_self");
                $(obj).text("确定保存");
                apiUtil.swalError("新增角色失败", res.msg);
            }
        });
        return false;
    });
});