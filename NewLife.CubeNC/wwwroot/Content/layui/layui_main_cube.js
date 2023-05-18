//JS
layui.use(['element_cube', 'layer', 'util'], function () {
    var cube = layui.element_cube,
        layer = layui.layer,
        util = layui.util,
        $ = layui.$;

    //头部事件
    util.event('lay-header-event', {
        // 左侧菜单
        menuLeft: function (othis) {
            $('.layui-index-shade').addClass('layui-layer-shade');
            $('.layui-layout').removeClass('cube-layout-admin');
        },
        // 右侧菜单
        menuRight: function () {
            layer.open({
                type: 1,
                content: '<div style="padding: 15px;">处理右侧面板的操作</div>',
                area: ['260px', '100%'],
                offset: 'rt', //右上角
                anim: 5,
                shadeClose: true
            });
        },
        // 设置菜单
        menuSetting: function (othis) {
            var url = othis.data('url');
            var title = othis.data('title');
            var isout = othis.data('loginout');
            // 过滤登出菜单操作
            if (isout) {
                console.log('open');
                location.href = url;
                return;
            }

            cubeAddTab(url, title);
        }
    });

    // 遮罩层事件
    util.event('layui-layer-shade-event', {
        shadeClose: function (othis) {
            $('.layui-index-shade').removeClass('layui-layer-shade');
            $('.layui-layout').addClass('cube-layout-admin');
        }
    });

    // 左侧菜单
    util.event('cube-leftmenu', {
        openmenu: function (othis) {
            // 设置URL和title
            var url = othis.data('url');
            var title = othis.data('title');

            cubeAddTab(url, title);
        }
    });

    // 菜单统一添加方法
    function cubeAddTab(url, title, isRandom) {
        var idmark = url;
        if (isRandom) idmark = url + Math.random();

        var li = $('.cube-tab-title').children('ul').children('li[lay-id="' + idmark + '"]');

        if (li && li.length > 0) {
            cube.tabChangeCube('cube-layout-tabs', url);
            return;
        }

        cube.tabAddCube('cube-layout-tabs', {
            title: title,
            content: '<iframe src="' + url + '" frameborder="0" class="cube-iframe">',
            id: idmark
        });
    }

    // 添加消息监听
    window.addEventListener('message', function (event) {

        if (event.origin != this.location.origin) return false;

        switch (event.data.kind) {
            case 'tab':
                cubeAddTab(event.data.url, event.data.title, true);
                break;
            default:
        }
    });
});

