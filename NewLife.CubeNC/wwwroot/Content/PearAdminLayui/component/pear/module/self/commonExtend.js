layui.define(['layer','sweetalert2'], function (exports) {
    "use strict";
    var $ = layui.jquery;
    var tmls, tool =
    {
        //表单对象 
        serializeObject:function (obj) {
            var a, o, h, i, e;
            a = $(obj).serializeArray();
            o = {};
            h = o.hasOwnProperty;
            for (i = 0; i < a.length; i++) {
                e = a[i];
                if (!h.call(o, e.name)) {
                    o[e.name] = e.value;
                }
            }
            return o;
        },
        //是否确定
        swalConfirm: function (option, func) {
            let title = typeof (option.title) == "undefined" ? "是否确定执行该操作，请慎重" : option.title;
            let type = typeof (option.type) == "undefined" ? "3" : option.type;
            if (type == "1") {
                type = "success";
            } else if (type == "2") {
                type = "error";
            } else {
                type = "warning";
            }
            let html = typeof (option.html) == "undefined" ? "" : option.html;
            Swal.fire({
                title: title,//标题
                html: html,  //内容
                type: type,  //类型
                cancelButtonColor: '取消',
                confirmButtonText: '确定',
                showLoaderOnConfirm: true,
                showCancelButton: true,
                allowOutsideClick: false,
                allowEscapeKey: false,
            }).then(function (result) {
                if (result.value) { //确定
                    if (typeof (func) != "undefined") {
                        func();
                    }
                }
            });
        },
        //成功
        swalSuccess: function (option, func) {

            let title = typeof (option.title) == "undefined" ? "" : option.title;
            let type = typeof (option.type) == "undefined" ? "1" : option.type;
            if (type == "1") {
                type = "success";
            } else if (type == "2") {
                type = "error";
            } else {
                type = "warning";
            }
            let timer = typeof (option.timer) == "undefined" ? 2 * 1000 : option.timer * 1000;//秒
            let html = typeof (option.html) == "undefined" ? "<b>" + timer +"</b>秒后页面自动关闭且刷新表格数据" : option.html;
            let timerInterval = {};
            Swal.fire({
                title: title,//标题
                html: html,  //内容
                timer: timer,//几秒后关闭
                type: type,  //类型
                timerProgressBar: true,
                onBeforeOpen: () => {
                    Swal.showLoading()
                    timerInterval = setInterval(() => {
                        const content = Swal.getContent()
                        if (content) {
                            const b = content.querySelector('b')
                            if (b) {
                                b.textContent = parseInt(Swal.getTimerLeft() / 1000)
                            }
                        }
                    }, 100)
                },
                onClose: () => {
                    clearInterval(timerInterval)
                }
            }).then((result) => {
                //if (result.dismiss === Swal.DismissReason.timer) {
                //    if (typeof (func) != "undefined") {
                //        func();
                //    }
                //}
                if (typeof (func) != "undefined") {
                    func();
                }
            });
        },
        //错误
        swalError: function (title, html) {
            Swal.fire({
                title: title,
                html: html,
                type: "error",
                confirmButtonText: '确认',
                confirmButtonColor: '#f27474',
            });
        },
        ajax: function (url, options, method = 'post', callFun = null) {
            var index = layer.load(2);
            //console.log(options);
            $.ajax({
                url: url,
                data: options,
                type: method, //HTTP请求类型
                //contentType: 'application/json',
                success: function (data) {
                    layer.close(index);
                    callFun(data);
                },
                error: function (e) {
                    layer.close(index);
                    //返回500错误 或者其他 http状态码错误时 需要在error 回调函数中处理了 并且返回的数据还不能直接alert，需要使用
                    //$.parseJSON 进行转译    res.msg 是自己组装的错误信息通用变量 
                    if (e.responseText == "") {
                        tool.swalError("url路径请求不对或者url请求格式不对请检查");
                    }
                    else {
                        var res = JSON.parse(e.responseText);
                        if (typeof (res.code) != "undefined" && res.code == 403) {
                            tool.swalError("请求异常--"+res.message);
                            return;
                        }
                        tool.swalError('连接异常，请稍后重试！');
                    }
                    let data = {code:1};//error
                    callFun(data);
                    return;
                }
            });
        },
        closeOpen: function () {
            layer.closeAll();
        },
        getUrlParam: function (name) {
            var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
            var r = window.location.search.substr(1).match(reg);
            if (r != null) return unescape(r[2]); return null;
        },
        formatdate: function (str) {
            if (str) {
                var d = eval('new ' + str.substr(1, str.length - 2));
                var ar_date = [
                    d.getFullYear(), d.getMonth() + 1, d.getDate()
                ];
                for (var i = 0; i < ar_date.length; i++) ar_date[i] = dFormat(ar_date[i]);
                return ar_date.slice(0, 3).join('-') + ' ' + ar_date.slice(3).join(':');

                function dFormat(i) { return i < 10 ? "0" + i.toString() : i; }
            } else {
                return "无信息";
            }
        },
        SetSession: function (key, options) {
            localStorage.setItem(key, JSON.stringify(options));
        },
        GetSession: function (key) {
            try {
                var obj = localStorage.getItem(key);
                if (obj == "" || obj == null || obj == undefined) {
                    return obj;
                }
                // console.log("jsonobj:" + JSON.parse(obj));
                return JSON.parse(obj);
            } catch (e) {
                console.log("获取session错误原因:" + e);
            }
        },
        /**
         * 删除键值对json
         * @param {key} key : 键
         */
        SessionRemove: function (key) {
            localStorage.removeItem(key);
        },
        isExtImage: function (name) {
            var imgExt = new Array(".png", ".jpg", ".jpeg", ".bmp", ".gif");
            name = name.toLowerCase();
            var i = name.lastIndexOf(".");
            var ext;
            if (i > -1) {
                ext = name.substring(i);
            }
            for (var i = 0; i < imgExt.length; i++) {
                if (imgExt[i] === ext)
                    return true;
            }
            return false;
        },
    };
    exports('commonExtend', tool);
});

