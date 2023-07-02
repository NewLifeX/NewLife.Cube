// 以下时间用于魔方判断是否需要更新脚本
// 2020-02-04 00:00:00

$(function () {

    window.infoDialog = parent['infoDialog'] || function (title, msg) { alert(msg); };
    window.confirmDialog = parent['confirmDialog'] || function (msg, func) { if (confirm(msg)) func(); };
    window.tips = parent['tips'] || function (msg, modal, time, jumpUrl) { alert(msg); location.reload(); };

    // 根据data-action的值确定操作类型 action为请求后端执行业务操作，url为直接跳转指定url地址
    // 按钮请求action
    $(document).on('click',
        'button[data-action="action"], input[data-action="action"], a[data-action="action"]',
        function (e) {
            $this = $(this);
            //动态设置标签参数
            var url = $this.data('url');
            if (!url || url == '') {
                url = $this.attr('href');
            }
            //if (url && url.length > 0) {
            //    $this.data('url', url);
            //    // 避免请求失败导致无法进行后续我。
            //    $this.attr('href', 'javascript:void(0);');
            //}

            var cf = $this.data('confirm');

            if (cf && cf.length > 0) {
                confirmDialog(cf, function () {
                    doClickAction($this)
                });
                return false;
            }

            doClickAction($this);
            // 阻止按钮本身的事件冒泡
            return false;
        });

    // 直接执行Url地址
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

    // 多标签页打开请求地址，否则新页打开
    $(document).on('click'
        , 'a[target="_blank"]'
        , function (data) {

            $this = $(this);
            // 动态设置标签参数
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
            }

            // 判断当前是否在容器当中，如果当前页面在容器中则使用容器标签，否则直接当前页面进行跳转
            if (window.frames.length == parent.frames.length) {
                //window.location.href = url;
                return true;
            }

            // 获取框架名称
            //var parentName = window.parent.frameName;

            var title = $this.data('title') ?? $this.attr('title');
            if (!title || title.length <= 0) {
                title = $this.html();
            }

            // 外部框架自行定义cubeAddTab方法，用于打开标签页
            return window.parent.cubeAddTab(url, title, true);
        }
    )

    // 多标签页打开请求地址，否则本页打开
    $(document).on('click'
        , 'a[target="_frame"]'
        , function (data) {

            $this = $(this);
            // 动态设置标签参数
            var url = $this.attr('href');
            if (url && url.length > 0) {
                $this.data('url', url);
            }

            // 判断当前是否在容器当中，如果当前页面在容器中则使用容器标签，否则直接当前页面进行跳转
            if (window.frames.length == parent.frames.length) {
                window.location.href = url;
                return true;
            }

            var title = $this.data('title') ?? $this.attr('title');
            if (!title || title.length <= 0) {
                title = $this.html();
            }

            // 外部框架自行定义cubeAddTab方法，用于打开标签页
            return window.parent.cubeAddTab(url, title, true);
        }
    )
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
            //tips('正在操作中，请稍候...', 0, 2000);
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

//// 发送消息到框架页-执行打开标签操作
//function sendEventToParent(data) {
//    // 只向同域框架发送消息，避免消息干扰
//    window.parent.postMessage(data, location.origin);
//}