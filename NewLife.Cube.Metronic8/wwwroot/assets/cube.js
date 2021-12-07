$(function () {
    //时间
    $(".form_datetime").daterangepicker({
        singleDatePicker: true,
        showDropdowns: true,
        autoUpdateInput: false,
        locale: {
            format: "YYYY-MM-DD",
            applyLabel: '确定',
            cancelLabel: '取消',
            daysOfWeek: ['日', '一', '二', '三', '四', '五', '六'],
            monthNames: ['一月', '二月', '三月', '四月', '五月', '六月',
                '七月', '八月', '九月', '十月', '十一月', '十二月'
            ],
        }
    });
    $(".form_datetime_hm").daterangepicker({
        singleDatePicker: true,
        showDropdowns: true,
        autoUpdateInput: false,
        timePicker: true,
        timePicker24Hour: true,
        locale: {
            format: "YYYY-MM-DD hh:mm",
            applyLabel: '确定',
            cancelLabel: '取消',
            daysOfWeek: ['日', '一', '二', '三', '四', '五', '六'],
            monthNames: ['一月', '二月', '三月', '四月', '五月', '六月',
                '七月', '八月', '九月', '十月', '十一月', '十二月'
            ],
        }
    });
    $(".form_datetime_hms").daterangepicker({
        singleDatePicker: true,
        showDropdowns: true,
        //autoUpdateInput: false,
        timePicker: true,
        timePicker24Hour: true,
        timePickerSeconds: true,
        locale: {
            format: "YYYY-MM-DD hh:mm:ss",
            applyLabel: '确定',
            cancelLabel: '取消',
            daysOfWeek: ['日', '一', '二', '三', '四', '五', '六'],
            monthNames: ['一月', '二月', '三月', '四月', '五月', '六月',
                '七月', '八月', '九月', '十月', '十一月', '十二月'
            ],
        }
    });
    //时间事件
    $(".form_datetime").on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format('YYYY-MM-DD'));
    });
    $(".form_datetime_hm").on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format('YYYY-MM-DD hh:mm'));
    });
    $(".form_datetime_hms").on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format('YYYY-MM-DD hh:mm:ss'));
    });
    $(".form_datetime,.form_datetime_hm,.form_datetime_hms").on('cancel.daterangepicker', function (ev, picker) {
        $(this).val('');
    });


    //双击单元格跳转到form
    $('tr').dblclick(function () {
        var $this = $(this);
        var row = $this.closest("tr");
        //优先加载侧边栏
        var dblp = row.find('.dblclickpanel');
        if (dblp.length > 0) {
            var id = $(dblp).data("id");
            var url = $(dblp).data("url");
            $.ajax({
                url: url,
                data: { id: id },
                success: function (data) {
                    $("#kt_quick_panel_toggler_btn").trigger("click");
                    $("#kt_quick_panel_content").html(data);
                    //$("#kt_quick_panel").css("opacity",1);
                    console.log(Date.now());
                }
            });
            return;
        }
        //编辑页面
        var findcell = row.find('.editcell');
        if (findcell.length > 0) {
            window.location.href = findcell.attr("href");
        }
    });

    //弹出提示对话框
    $('.swal_tip').click(function (e) {
        var title = $(this).data("title");
        var url = $(this).data("url");
        var type = $(this).data("type");

        swal.fire({
            title: title,
            icon: type,
            showCancelButton: true,
            buttonsStyling: false,
            confirmButtonClass: "btn btn-outline btn-outline-dashed btn-outline-info btn-active-light-info me-2",
            cancelButtonClass: "btn btn-outline btn-outline-dashed btn-outline-dark btn-active-light-dark ",
            confirmButtonText: "<i class='bi bi-check-lg'></i>确 认  ",
            cancelButtonText: "<i class='bi bi-x-lg'></i>取 消  ",
        }).then(function (result) {
            if (result.value) {
                //确认
                window.location.href = url;
            } else if (result.dismiss === 'cancel') {
                //取消
            }
        });
    });

    //
});
