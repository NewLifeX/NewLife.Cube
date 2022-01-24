$(function () {
    window.infoDialog = parent['infoDialog'] || function (title, msg) { alert(msg); };
    window.confirmDialog = parent['confirmDialog'] || function (msg, func) { if (confirm(msg)) func(); };
    window.tips = parent['tips'] || function (msg, modal, time, jumpUrl) { alert(msg); location.reload(); };

    //根据data-action的值确定操作类型 action为请求后端执行业务操作，url为直接跳转指定url地址
    //按钮请求action
    $(document).on('click',
        'button[data-action="action"], input[data-action="action"], a[data-action="action"]',
        function (e) {
            $this = $(this);
            //动态设置标签参数
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
                $this.attr('href', 'javascript:void(0);');
            }

            var cf = $this.data('confirm');

            if (cf && cf.length > 0) {
                confirmDialog(cf, function () {
                    doClickAction($this)
                });
                return false;
            }

            doClickAction($this);
            //阻止按钮本身的事件冒泡
            return false;
        });

    //直接执行Url地址
    $(document).on('click'
        , 'button[data-action="url"],input[data-action="url"],a[data-action="url"]'
        , function (data) {
            $this = $(this);
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
            }
            location = url;
        });

    //时间
    $(".form_datetime_hms").flatpickr({
        dateFormat: "Y-m-d H:i:S",
    });
    $(".form_datetime").flatpickr({
        dateFormat : "Y-m-d",
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

    //图片跟随鼠标预览功能
    $(".div_image").hover(function () {
        var $imgsrc = $(this).attr("src");
        var $div = "<div class='div_image_append'><img style='width: 500px; height: 400px;' src='" + $imgsrc + "'/></div>";
        $("#div_footer").append($div);
        //$("div img").attr("src", $imgsrc);
    }, function () {
        // out
        //鼠标移出的时候把其他列透明度去掉
        //$(this).siblings().removeClass("opacity_li");
        $(".div_image_append").remove();
    }).mousemove(function (e) {
        var wh = $(window).height();
        var ph = e.pageY;
        var diff = wh - ph;
        var offset = 10;
        if (diff < 420) offset = diff - 450;
        //console.log("wh:" + wh + " ph:" + ph + " diff:" + diff + " offset:" + offset);
        $(".div_image_append").css({
            position: "absolute",
            left: e.pageX + 10,
            top: e.pageY + offset
        }); //设置div绝对定位 坐标就是距离鼠标当前的位置 
    });
});

function doClickAction($this) {
    var fields = $this.data('fields');
    //参数
    var parameter = '';
    if (fields && fields.length > 0) {
        var fieldArr = fields.split(',');
        for (var i = 0; i < fieldArr.length; i++) {
            var detailArr = $('[name=' + fieldArr[i] + ']');
            //不对name容器标签进行限制，直接进行序列化
            //如果有特殊需求，可以再指定筛选器进行筛选
            parameter += ((parameter.length > 0 ? '&' : '') + detailArr.serialize());
        }
    }

    //method
    var cmethod = $this.data('method');
    var method = 'GET';
    if (cmethod && cmethod.length > 0) {
        method = cmethod;
    }

    //url
    var curl = $this.data('url') || $this.data('href');
    if (!curl || curl.length <= 0) {
        if ($this[0].tagName == 'A') {
            curl = $this.attr('href');
        }
    }
    doAction(method, curl, parameter);
}

//ajax请求 methodName 指定GET与POST
function doAction(methodName, actionUrl, actionParamter) {
    if (!methodName || methodName.length <= 0 || !actionUrl || actionUrl.length <= 0) {
        tips('请求参数异常，请保证请求的地址跟参数正确！', 0, 1000);
        return;
    }

    $.ajax({
        url: actionUrl,
        type: methodName,
        async: false,
        dataType: 'json',
        data: actionParamter,
        error: function (ex) {
            tips('请求异常！', 0, 1000);
            //console.log(ex);
        },
        beforeSend: function () {
            tips('正在操作中，请稍候...', 0, 2000);
        },
        success: function (s) {
            //console.log(s);
        },
        complete: function (result) {
            var rs = result.responseJSON;

            if (rs.message || rs.data) {
                tips(rs.message || rs.data, 0, 1000);
            }

            if (rs.url && rs.url.length > 0) {
                if (rs.url == '[refresh]') {
                    if (rs.time && +rs.time > 0) {
                        setTimeout(function () {
                            location.reload(false)
                        }, Math.min(+rs.time, 10) * 1000) //不能大于10秒，
                    } else {
                        //刷新页面但不重新加载页面的所有静态资源
                        location.reload(false);
                    }
                } else {
                    if (rs.time && +rs.time > 0) {
                        setTimeout(function () {
                            window.location.href = rs.url;
                        }, Math.min(+rs.time, 10) * 1000) //不能大于10秒，
                    } else {
                        window.location.href = rs.url;
                    }
                }
            }
        }
    });
}