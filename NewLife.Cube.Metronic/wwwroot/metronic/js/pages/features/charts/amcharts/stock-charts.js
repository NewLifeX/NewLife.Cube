"use strict";

// Class definition
var KTamChartsStockChartsDemo = function() {

    // Private functions
    var demo1 = function() {
        var chartData1 = [];
        var chartData2 = [];
        var chartData3 = [];
        var chartData4 = [];

        generateChartData();

        function generateChartData() {
            var firstDate = new Date();
            firstDate.setDate(firstDate.getDate() - 500);
            firstDate.setHours(0, 0, 0, 0);

            for (var i = 0; i < 500; i++) {
                var newDate = new Date(firstDate);
                newDate.setDate(newDate.getDate() + i);

                var a1 = Math.round(Math.random() * (40 + i)) + 100 + i;
                var b1 = Math.round(Math.random() * (1000 + i)) + 500 + i * 2;

                var a2 = Math.round(Math.random() * (100 + i)) + 200 + i;
                var b2 = Math.round(Math.random() * (1000 + i)) + 600 + i * 2;

                var a3 = Math.round(Math.random() * (100 + i)) + 200;
                var b3 = Math.round(Math.random() * (1000 + i)) + 600 + i * 2;

                var a4 = Math.round(Math.random() * (100 + i)) + 200 + i;
                var b4 = Math.round(Math.random() * (100 + i)) + 600 + i;

                chartData1.push({
                    "date": newDate,
                    "value": a1,
                    "volume": b1
                });
                chartData2.push({
                    "date": newDate,
                    "value": a2,
                    "volume": b2
                });
                chartData3.push({
                    "date": newDate,
                    "value": a3,
                    "volume": b3
                });
                chartData4.push({
                    "date": newDate,
                    "value": a4,
                    "volume": b4
                });
            }
        }

        var chart = AmCharts.makeChart("kt_amcharts_1", {
            "rtl": KTUtil.isRTL(),
            "type": "stock",
            "theme": "light",
            "dataSets": [{
                "title": "first data set",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],
                "dataProvider": chartData1,
                "categoryField": "date"
            }, {
                "title": "second data set",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],
                "dataProvider": chartData2,
                "categoryField": "date"
            }, {
                "title": "third data set",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],
                "dataProvider": chartData3,
                "categoryField": "date"
            }, {
                "title": "fourth data set",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],
                "dataProvider": chartData4,
                "categoryField": "date"
            }],

            "panels": [{
                "showCategoryAxis": false,
                "title": "Value",
                "percentHeight": 70,
                "stockGraphs": [{
                    "id": "g1",
                    "valueField": "value",
                    "comparable": true,
                    "compareField": "value",
                    "balloonText": "[[title]]:<b>[[value]]</b>",
                    "compareGraphBalloonText": "[[title]]:<b>[[value]]</b>"
                }],
                "stockLegend": {
                    "periodValueTextComparing": "[[percents.value.close]]%",
                    "periodValueTextRegular": "[[value.close]]"
                }
            }, {
                "title": "Volume",
                "percentHeight": 30,
                "stockGraphs": [{
                    "valueField": "volume",
                    "type": "column",
                    "showBalloon": false,
                    "fillAlphas": 1
                }],
                "stockLegend": {
                    "periodValueTextRegular": "[[value.close]]"
                }
            }],

            "chartScrollbarSettings": {
                "graph": "g1"
            },

            "chartCursorSettings": {
                "valueBalloonsEnabled": true,
                "fullWidth": true,
                "cursorAlpha": 0.1,
                "valueLineBalloonEnabled": true,
                "valueLineEnabled": true,
                "valueLineAlpha": 0.5
            },

            "periodSelector": {
                "position": "left",
                "periods": [{
                    "period": "MM",
                    "selected": true,
                    "count": 1,
                    "label": "1 month"
                }, {
                    "period": "YYYY",
                    "count": 1,
                    "label": "1 year"
                }, {
                    "period": "YTD",
                    "label": "YTD"
                }, {
                    "period": "MAX",
                    "label": "MAX"
                }]
            },

            "dataSetSelector": {
                "position": "left"
            },

            "export": {
                "enabled": true
            }
        });
    }

    var demo2 = function() {
        var chartData = [];
        generateChartData();

        function generateChartData() {
            var firstDate = new Date(2012, 0, 1);
            firstDate.setDate(firstDate.getDate() - 500);
            firstDate.setHours(0, 0, 0, 0);

            for (var i = 0; i < 500; i++) {
                var newDate = new Date(firstDate);
                newDate.setDate(newDate.getDate() + i);

                var a = Math.round(Math.random() * (40 + i)) + 100 + i;
                var b = Math.round(Math.random() * 100000000);

                chartData.push({
                    "date": newDate,
                    "value": a,
                    "volume": b
                });
            }
        }

        var chart = AmCharts.makeChart("kt_amcharts_2", {
            "type": "stock",
            "theme": "light",
            "dataSets": [{
                "color": "#b0de09",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],
                "dataProvider": chartData,
                "categoryField": "date",
                // EVENTS
                "stockEvents": [{
                    "date": new Date(2010, 8, 19),
                    "type": "sign",
                    "backgroundColor": "#85CDE6",
                    "graph": "g1",
                    "text": "S",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2010, 10, 19),
                    "type": "flag",
                    "backgroundColor": "#FFFFFF",
                    "backgroundAlpha": 0.5,
                    "graph": "g1",
                    "text": "F",
                    "description": "Some longer\ntext can also\n be added"
                }, {
                    "date": new Date(2010, 11, 10),
                    "showOnAxis": true,
                    "backgroundColor": "#85CDE6",
                    "type": "pin",
                    "text": "X",
                    "graph": "g1",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2010, 11, 26),
                    "showOnAxis": true,
                    "backgroundColor": "#85CDE6",
                    "type": "pin",
                    "text": "Z",
                    "graph": "g1",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 0, 3),
                    "type": "sign",
                    "backgroundColor": "#85CDE6",
                    "graph": "g1",
                    "text": "U",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 1, 6),
                    "type": "sign",
                    "graph": "g1",
                    "text": "D",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 3, 5),
                    "type": "sign",
                    "graph": "g1",
                    "text": "L",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 3, 5),
                    "type": "sign",
                    "graph": "g1",
                    "text": "R",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 5, 15),
                    "type": "arrowUp",
                    "backgroundColor": "#00CC00",
                    "graph": "g1",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 6, 25),
                    "type": "arrowDown",
                    "backgroundColor": "#CC0000",
                    "graph": "g1",
                    "description": "This is description of an event"
                }, {
                    "date": new Date(2011, 8, 1),
                    "type": "text",
                    "graph": "g1",
                    "text": "Longer text can\nalso be displayed",
                    "description": "This is description of an event"
                }]
            }],


            "panels": [{
                "title": "Value",
                "stockGraphs": [{
                    "id": "g1",
                    "valueField": "value"
                }],
                "stockLegend": {
                    "valueTextRegular": " ",
                    "markerType": "none"
                }
            }],

            "chartScrollbarSettings": {
                "graph": "g1"
            },

            "chartCursorSettings": {
                "valueBalloonsEnabled": true,
                "graphBulletSize": 1,
                "valueLineBalloonEnabled": true,
                "valueLineEnabled": true,
                "valueLineAlpha": 0.5
            },

            "periodSelector": {
                "periods": [{
                    "period": "DD",
                    "count": 10,
                    "label": "10 days"
                }, {
                    "period": "MM",
                    "count": 1,
                    "label": "1 month"
                }, {
                    "period": "YYYY",
                    "count": 1,
                    "label": "1 year"
                }, {
                    "period": "YTD",
                    "label": "YTD"
                }, {
                    "period": "MAX",
                    "label": "MAX"
                }]
            },

            "panelsSettings": {
                "usePrefixes": true
            },
            "export": {
                "enabled": true
            }
        });
    }

    var demo3 = function() {
        var chartData = generateChartData();

        function generateChartData() {
            var chartData = [];
            var firstDate = new Date(2012, 0, 1);
            firstDate.setDate(firstDate.getDate() - 500);
            firstDate.setHours(0, 0, 0, 0);

            for (var i = 0; i < 500; i++) {
                var newDate = new Date(firstDate);
                newDate.setDate(newDate.getDate() + i);

                var value = Math.round(Math.random() * (40 + i)) + 100 + i;

                chartData.push({
                    "date": newDate,
                    "value": value
                });
            }
            return chartData;
        }


        var chart = AmCharts.makeChart("kt_amcharts_3", {
            "type": "stock",
            "theme": "light",
            "dataSets": [{
                "color": "#b0de09",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }],
                "dataProvider": chartData,
                "categoryField": "date"
            }],

            "panels": [{
                "showCategoryAxis": true,
                "title": "Value",
                "eraseAll": false,
                "allLabels": [{
                    "x": 0,
                    "y": 115,
                    "text": "Click on the pencil icon on top-right to start drawing",
                    "align": "center",
                    "size": 16
                }],

                "stockGraphs": [{
                    "id": "g1",
                    "valueField": "value",
                    "useDataSetColors": false
                }],

                "stockLegend": {
                    "valueTextRegular": " ",
                    "markerType": "none"
                },

                "drawingIconsEnabled": true
            }],

            "chartScrollbarSettings": {
                "graph": "g1"
            },
            "chartCursorSettings": {
                "valueBalloonsEnabled": true
            },
            "periodSelector": {
                "position": "bottom",
                "periods": [{
                    "period": "DD",
                    "count": 10,
                    "label": "10 days"
                }, {
                    "period": "MM",
                    "count": 1,
                    "label": "1 month"
                }, {
                    "period": "YYYY",
                    "count": 1,
                    "label": "1 year"
                }, {
                    "period": "YTD",
                    "label": "YTD"
                }, {
                    "period": "MAX",
                    "label": "MAX"
                }]
            }
        });
    }

    var demo4 = function() {
        var chartData = generateChartData();

        function generateChartData() {
            var chartData = [];
            var firstDate = new Date(2012, 0, 1);
            firstDate.setDate(firstDate.getDate() - 1000);
            firstDate.setHours(0, 0, 0, 0);

            for (var i = 0; i < 1000; i++) {
                var newDate = new Date(firstDate);
                newDate.setHours(0, i, 0, 0);

                var a = Math.round(Math.random() * (40 + i)) + 100 + i;
                var b = Math.round(Math.random() * 100000000);

                chartData.push({
                    "date": newDate,
                    "value": a,
                    "volume": b
                });
            }
            return chartData;
        }

        var chart = AmCharts.makeChart("kt_amcharts_4", {
            "type": "stock",
            "theme": "light",
            "categoryAxesSettings": {
                "minPeriod": "mm"
            },

            "dataSets": [{
                "color": "#b0de09",
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }],

                "dataProvider": chartData,
                "categoryField": "date"
            }],

            "panels": [{
                "showCategoryAxis": false,
                "title": "Value",
                "percentHeight": 70,

                "stockGraphs": [{
                    "id": "g1",
                    "valueField": "value",
                    "type": "smoothedLine",
                    "lineThickness": 2,
                    "bullet": "round"
                }],


                "stockLegend": {
                    "valueTextRegular": " ",
                    "markerType": "none"
                }
            }, {
                "title": "Volume",
                "percentHeight": 30,
                "stockGraphs": [{
                    "valueField": "volume",
                    "type": "column",
                    "cornerRadiusTop": 2,
                    "fillAlphas": 1
                }],

                "stockLegend": {
                    "valueTextRegular": " ",
                    "markerType": "none"
                }
            }],

            "chartScrollbarSettings": {
                "graph": "g1",
                "usePeriod": "10mm",
                "position": "top"
            },

            "chartCursorSettings": {
                "valueBalloonsEnabled": true
            },

            "periodSelector": {
                "position": "top",
                "dateFormat": "YYYY-MM-DD JJ:NN",
                "inputFieldWidth": 150,
                "periods": [{
                    "period": "hh",
                    "count": 1,
                    "label": "1 hour",
                    "selected": true
                }, {
                    "period": "hh",
                    "count": 2,
                    "label": "2 hours"
                }, {
                    "period": "hh",
                    "count": 5,
                    "label": "5 hour"
                }, {
                    "period": "hh",
                    "count": 12,
                    "label": "12 hours"
                }, {
                    "period": "MAX",
                    "label": "MAX"
                }]
            },

            "panelsSettings": {
                "usePrefixes": true
            },

            "export": {
                "enabled": true,
                "position": "bottom-right"
            }
        });
    }

    var demo5 = function() {
        var chartData = [];
        generateChartData();


        function generateChartData() {
            var firstDate = new Date();
            firstDate.setHours(0, 0, 0, 0);
            firstDate.setDate(firstDate.getDate() - 2000);

            for (var i = 0; i < 2000; i++) {
                var newDate = new Date(firstDate);

                newDate.setDate(newDate.getDate() + i);

                var open = Math.round(Math.random() * (30) + 100);
                var close = open + Math.round(Math.random() * (15) - Math.random() * 10);

                var low;
                if (open < close) {
                    low = open - Math.round(Math.random() * 5);
                } else {
                    low = close - Math.round(Math.random() * 5);
                }

                var high;
                if (open < close) {
                    high = close + Math.round(Math.random() * 5);
                } else {
                    high = open + Math.round(Math.random() * 5);
                }

                var volume = Math.round(Math.random() * (1000 + i)) + 100 + i;
                var value = Math.round(Math.random() * (30) + 100);

                chartData[i] = ({
                    "date": newDate,
                    "open": open,
                    "close": close,
                    "high": high,
                    "low": low,
                    "volume": volume,
                    "value": value
                });
            }
        }

        var chart = AmCharts.makeChart("kt_amcharts_5", {
            "type": "stock",
            "theme": "light",
            "dataSets": [{
                "fieldMappings": [{
                    "fromField": "open",
                    "toField": "open"
                }, {
                    "fromField": "close",
                    "toField": "close"
                }, {
                    "fromField": "high",
                    "toField": "high"
                }, {
                    "fromField": "low",
                    "toField": "low"
                }, {
                    "fromField": "volume",
                    "toField": "volume"
                }, {
                    "fromField": "value",
                    "toField": "value"
                }],
                "color": "#7f8da9",
                "dataProvider": chartData,
                "title": "West Stock",
                "categoryField": "date"
            }, {
                "fieldMappings": [{
                    "fromField": "value",
                    "toField": "value"
                }],
                "color": "#fac314",
                "dataProvider": chartData,
                "compared": true,
                "title": "East Stock",
                "categoryField": "date"
            }],


            "panels": [{
                    "title": "Value",
                    "showCategoryAxis": false,
                    "percentHeight": 70,
                    "valueAxes": [{
                        "id": "v1",
                        "dashLength": 5
                    }],

                    "categoryAxis": {
                        "dashLength": 5
                    },

                    "stockGraphs": [{
                        "type": "candlestick",
                        "id": "g1",
                        "openField": "open",
                        "closeField": "close",
                        "highField": "high",
                        "lowField": "low",
                        "valueField": "close",
                        "lineColor": "#7f8da9",
                        "fillColors": "#7f8da9",
                        "negativeLineColor": "#db4c3c",
                        "negativeFillColors": "#db4c3c",
                        "fillAlphas": 1,
                        "useDataSetColors": false,
                        "comparable": true,
                        "compareField": "value",
                        "showBalloon": false,
                        "proCandlesticks": true
                    }],

                    "stockLegend": {
                        "valueTextRegular": undefined,
                        "periodValueTextComparing": "[[percents.value.close]]%"
                    }
                },

                {
                    "title": "Volume",
                    "percentHeight": 30,
                    "marginTop": 1,
                    "showCategoryAxis": true,
                    "valueAxes": [{
                        "dashLength": 5
                    }],

                    "categoryAxis": {
                        "dashLength": 5
                    },

                    "stockGraphs": [{
                        "valueField": "volume",
                        "type": "column",
                        "showBalloon": false,
                        "fillAlphas": 1
                    }],

                    "stockLegend": {
                        "markerType": "none",
                        "markerSize": 0,
                        "labelText": "",
                        "periodValueTextRegular": "[[value.close]]"
                    }
                }
            ],

            "chartScrollbarSettings": {
                "graph": "g1",
                "graphType": "line",
                "usePeriod": "WW"
            },

            "chartCursorSettings": {
                "valueLineBalloonEnabled": true,
                "valueLineEnabled": true
            },

            "periodSelector": {
                "position": "bottom",
                "periods": [{
                    "period": "DD",
                    "count": 10,
                    "label": "10 days"
                }, {
                    "period": "MM",
                    "selected": true,
                    "count": 1,
                    "label": "1 month"
                }, {
                    "period": "YYYY",
                    "count": 1,
                    "label": "1 year"
                }, {
                    "period": "YTD",
                    "label": "YTD"
                }, {
                    "period": "MAX",
                    "label": "MAX"
                }]
            },
            "export": {
                "enabled": true
            }
        });
    }
    return {
        // public functions
        init: function() {
            demo1();
            demo2();
            demo3();
            demo4();
            demo5();
        }
    };
}();

jQuery(document).ready(function() {
    KTamChartsStockChartsDemo.init();
});