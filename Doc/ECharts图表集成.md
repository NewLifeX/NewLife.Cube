# 第21章 ECharts 图表集成

> 本章介绍魔方中 ECharts 图表的集成方法，实现数据可视化展示。
> 
> 详细教程：https://newlifex.com/cube/echarts

---

## 21.1 ECharts 加载方法

### 通过 CDN 加载

```html
<!-- 通过 CDN 加载 -->
<script src="https://cdn.jsdelivr.net/npm/echarts@5/dist/echarts.min.js"></script>

<!-- 本地文件加载 -->
<script src="~/lib/echarts/echarts.min.js"></script>
```

### 基础用法

```html
<div id="myChart" style="width: 600px; height: 400px;"></div>

<script>
    var chart = echarts.init(document.getElementById('myChart'));
    chart.setOption({
        title: { text: '销售统计' },
        xAxis: { type: 'category', data: ['周一', '周二', '周三', '周四', '周五'] },
        yAxis: { type: 'value' },
        series: [{ data: [120, 200, 150, 80, 70], type: 'bar' }]
    });
</script>
```

---

## 21.2 常见图表类型

### 折线图

```javascript
option = {
    xAxis: { type: 'category', data: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'] },
    yAxis: { type: 'value' },
    series: [{
        data: [150, 230, 224, 218, 135],
        type: 'line',
        smooth: true
    }]
};
```

### 柱状图

```javascript
option = {
    xAxis: { type: 'category', data: ['产品A', '产品B', '产品C'] },
    yAxis: { type: 'value' },
    series: [{
        data: [120, 200, 150],
        type: 'bar',
        barWidth: '60%'
    }]
};
```

### 饼图

```javascript
option = {
    series: [{
        type: 'pie',
        radius: '50%',
        data: [
            { value: 1048, name: '搜索引擎' },
            { value: 735, name: '直接访问' },
            { value: 580, name: '邮件营销' }
        ]
    }]
};
```

---

## 21.3 数据源与动态加载

### 后端数据提供接口

```csharp
public ActionResult GetSalesData()
{
    var data = Order.FindAll(Order._.CreateTime >= DateTime.Today.AddDays(-7))
        .GroupBy(e => e.CreateTime.Date)
        .Select(g => new
        {
            date = g.Key.ToString("MM-dd"),
            amount = g.Sum(e => e.Amount)
        })
        .OrderBy(e => e.date)
        .ToList();
    
    return Json(new { code = 0, data });
}
```

### 前端动态加载数据

```javascript
fetch('/Admin/Report/GetSalesData')
    .then(res => res.json())
    .then(res => {
        if (res.code === 0) {
            chart.setOption({
                xAxis: { data: res.data.map(d => d.date) },
                series: [{ data: res.data.map(d => d.amount) }]
            });
        }
    });
```

---

## 21.4 仪表板图表展示

```html
<div class="row">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">销售趋势</h3>
            </div>
            <div class="card-body">
                <div id="salesTrend" style="height: 300px;"></div>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <h3 class="card-title">订单状态</h3>
            </div>
            <div class="card-body">
                <div id="orderStatus" style="height: 300px;"></div>
            </div>
        </div>
    </div>
</div>

<script>
    // 销售数据趋势图
    var salesTrend = echarts.init(document.getElementById('salesTrend'));
    salesTrend.setOption({
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: @Html.Raw(ViewBag.Dates.ToJson()) },
        yAxis: { type: 'value' },
        series: [{
            name: '销售额',
            type: 'line',
            smooth: true,
            data: @Html.Raw(ViewBag.Sales.ToJson()),
            areaStyle: {}
        }]
    });
    
    // 订单状态饼图
    var orderStatus = echarts.init(document.getElementById('orderStatus'));
    orderStatus.setOption({
        tooltip: { trigger: 'item' },
        series: [{
            type: 'pie',
            radius: ['40%', '70%'],
            data: @Html.Raw(ViewBag.StatusData.ToJson())
        }]
    });
    
    // 响应式
    window.addEventListener('resize', function() {
        salesTrend.resize();
        orderStatus.resize();
    });
</script>
```

---

## 21.5 图表导出

```javascript
// 导出为图片
function exportChart(chart, filename) {
    var url = chart.getDataURL({
        type: 'png',
        pixelRatio: 2,
        backgroundColor: '#fff'
    });
    
    var link = document.createElement('a');
    link.download = filename + '.png';
    link.href = url;
    link.click();
}
```

---

## 21.6 高级配置示例

### 多系列图表

```javascript
option = {
    tooltip: { trigger: 'axis' },
    legend: { data: ['产品A', '产品B'] },
    xAxis: { type: 'category', data: ['1月', '2月', '3月', '4月'] },
    yAxis: { type: 'value' },
    series: [
        {
            name: '产品A',
            data: [120, 200, 150, 180],
            type: 'line'
        },
        {
            name: '产品B',
            data: [90, 150, 200, 160],
            type: 'line'
        }
    ]
};
```

### 主题和样式

```javascript
// 深色主题
var chart = echarts.init(document.getElementById('myChart'), 'dark');

// 自定义颜色
chart.setOption({
    color: ['#E74C3C', '#3498DB', '#2ECC71', '#F39C12'],
    // ... 其他配置
});
```

### 交互功能

```javascript
chart.setOption({
    toolbox: {
        feature: {
            saveAsImage: {},
            dataZoom: {},
            restore: {}
        }
    },
    dataZoom: [
        {
            type: 'slider',
            show: true,
            yAxisIndex: [0],
            left: '93%'
        }
    ]
});
```

---

## 本章小结

通过本章学习，你应该掌握了：

1. **ECharts 加载**：CDN 和本地加载方法
2. **常见图表**：折线图、柱状图、饼图的配置
3. **数据加载**：后端数据接口与前端动态渲染
4. **仪表板**：多图表组合展示
5. **图表导出**：导出为图片文件

---

## 参考资源

- [ECharts 在魔方中的应用](https://newlifex.com/cube/echarts)
- [ECharts 官方文档](https://echarts.apache.org/)
