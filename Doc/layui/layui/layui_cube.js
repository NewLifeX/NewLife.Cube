 //JS
 layui.use(['element_cube', 'layer', 'util'], function() {
     var cube = layui.element_cube,
         layer = layui.layer,
         util = layui.util,
         $ = layui.$;

     //头部事件
     util.event('lay-header-event', {
         //左侧菜单事件
         menuLeft: function(othis) {
             $('.layui-index-shade').addClass('layui-layer-shade');
             $('.layui-layout').removeClass('layui-layout-admin');
         },
         menuRight: function() {
             layer.open({
                 type: 1,
                 content: '<div style="padding: 15px;">处理右侧面板的操作</div>',
                 area: ['260px', '100%'],
                 offset: 'rt', //右上角
                 anim: 5,
                 shadeClose: true
             });
         }
     });

     // 遮罩层事件
     util.event('layui-layer-shade-event', {
         shadeClose: function(othis) {
             $('.layui-index-shade').removeClass('layui-layer-shade');
             $('.layui-layout').addClass('layui-layout-admin');
         }
     });

     // 左侧菜单
     util.event('cube-leftmenu', {
         openmenu: function(othis) {
             // 设置URL和title
             var url = othis.data('url');
             var title = othis.data('title');

             var li = $('.cube-tab-title').children('ul').children('li[lay-id="' + url + '"]');
             if (li && li.length > 0) {
                 cube.tabChangeCube('cube-layout-tabs', url);

                 return;
             }

             cube.tabAddCube('cube-layout-tabs', {
                 title: title,
                 content: '<iframe src="' + url + '" frameborder="0" class="cube-iframe">',
                 id: url
             });
         }
     });
 });