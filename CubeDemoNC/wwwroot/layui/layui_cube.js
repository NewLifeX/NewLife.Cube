// 设置表格宽度
function setTable() {
    var bodys = $('.layui-table-box').find('.layui-table-body table');
    bodys.each(function () {
        // 暂时默认表格宽度始和头不保持一致
        var tds = $(this).find('tr:first td');
        tds.each(function (index) {
            var $this = $(this);
            var p = $this.parents('.layui-table-box');
            var $div = $this.children('div');
            var tdpaddings = $div.css('padding').split(' ');
            var leftpaddingNum = 0;
            // 去除左右padding值
            if (tdpaddings.length == 2) {
                leftpaddingNum = parseInt(tdpaddings[1].replace('px', '')) * 2;
            } else if (tdpaddings.length == 4) {
                leftpaddingNum = parseInt(tdpaddings[1].replace('px', '')) + parseInt(tdpaddings[3].replace('px', ''));
            }
            var tdwidth = $div[0].getBoundingClientRect().width - leftpaddingNum;

            var className = '.laytable-cell-' + $div.data('id');
            p.find('.layui-table-header').find(className).width(tdwidth);
        });
    });
}

// 设置滚动条
function setTableScroll() {
    $('.layui-table-box').find('.layui-table-body').on('scroll', function () {
        var $this = $(this);
        var left = $this.scrollLeft();
        var $header = $this.parents('.layui-table-box').find('.layui-table-header');

        $header.scrollLeft(left);
    });
}