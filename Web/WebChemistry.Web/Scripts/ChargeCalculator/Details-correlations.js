function initCorrelations(result) {
    "use strict";
    
    function createChart() {
        var chart = new Highcharts.Chart({
            title: { text: 'No data selected' },
            subtitle: { text: 'No details available' },
            chart: { type: 'scatter', renderTo: $("#details-correlation-plot")[0], animation: false },
            xAxis: { title: { text: '' }, gridLineWidth: 1 },
            yAxis: { title: { text: '' }, gridLineWidth: 1 },
            exporting: { enabled: true },
            credits: { enabled: false },
            tooltip: {
                crosshairs: [true, true],
                formatter: function () {
                    return '<b>' + this.point.label + '</b><br/><b>X:</b> ' + this.x.toFixed(5) + '<br/><b>Y:</b> ' + this.y.toFixed(5) + '</b><br/><b>diff:</b> ' + Math.abs(this.x - this.y).toFixed(5);
                }
            },
            legend: {
                enabled: false
            },
            plotOptions: {
                series: {
                    animation: false
                }
            }
        });
        return chart;
    }

    var maxAtomsInPlot = 2500;

    //function removeTheLimit() {
    //    maxAtomsInPlot = 10000000;
    //    updateChart(result);
    //};

    function filterData(data, corr) {
        var a = corr.A, b = corr.B, n = Math.sqrt(corr.A * corr.A + 1);

        function pointLineDistance(p) {
            return Math.abs(p.x - a * p.y - b) / n;
        }

        data.sort(function (x, y) {
            return Math.abs(y.x - y.y) - Math.abs(x.x - x.y) //pointLineDistance(y) - pointLineDistance(x);
        });
        
        return data.slice(0, maxAtomsInPlot);
    }

    function updateChart(result) {        
        var correlations = result.correlations;
        var chart = correlations.chart;

        if (!chart || result.computedChargeNames.length === 0) {
            return;
        }

        var x = correlations.xAxis(), y = correlations.yAxis();
        if (!x || !y) {
            chart.setTitle(
               { text: 'No data selected' },
               { text: 'No details available' });
            while (chart.series.length > 0) {
                chart.series[chart.series.length - 1].remove(false);
            }
            return;
        }

        if (x === y) {
            correlations.tooManyDataPoints(false);
            chart.setTitle(
               { text: 'X and Y axes are identical' },
               { text: 'No details available' });
            while (chart.series.length > 0) {
                chart.series[chart.series.length - 1].remove(false);
            }
            return;
        }
        
        var corr = getCorrelation(x, y);
                
        correlations.dataPointCount(corr.DataPointCount);

        while (chart.series.length > 0) {
            chart.series[chart.series.length - 1].remove(false);
        }
        
        var partition = result.Partitions[correlations.currentPartitionName()];
        var groups = partition.Groups;
        var xCharges = partition.Charges[x].GroupCharges,
            yCharges = partition.Charges[y].GroupCharges;

        chart.xAxis[0].setTitle({ text: x });
        chart.yAxis[0].setTitle({ text: y });
        
        var dataPoints = [];
        var minX = 10000000, maxX = -10000000;
        _.forEach(groups, function (group, id) {
            var xVal = xCharges[id], yVal = yCharges[id];
            if (!isNaN(xVal) && !isNaN(yVal)) {
                minX = Math.min(minX, xVal);
                maxX = Math.max(maxX, xVal);
                dataPoints.push({
                    x: xVal,
                    y: yVal,
                    label: group.Label
                });
            }
        });

        if (!correlations.ignoreMaxDataPoints()) {
            if (dataPoints.length > maxAtomsInPlot) {
                correlations.tooManyDataPoints(true);
                dataPoints = filterData(dataPoints, corr);
            } else {
                correlations.tooManyDataPoints(false);
            }
        }


        chart.setTitle(
            {
                text: "<b>" + result.Summary.Id + "</b>"
            },
            {
                text:
                  "Charges on " + correlations.currentPartitionName()
                + ", " + (correlations.tooManyDataPoints() ? "" + dataPoints.length + " of " + corr.DataPointCount : "" + corr.DataPointCount) + " data points"
                + "<br/>Pearson = " + corr.PearsonCoefficient.toFixed(3)
                + ", Spearman = " + corr.SpearmanCoefficient.toFixed(3)
                + ", RMSD = " + corr.Rmsd.toFixed(3)
                + ", Diff. = " + corr.AbsoluteDifferenceSum.toFixed(3)
            });

        chart.addSeries({
            name: "data",
            data: dataPoints,
            shadow: false,
            turboThreshold: 0,
            dashStyle: "Solid",
            color: "#7CB5EC",
            animation: false,
            marker: { symbol: 'circle' }
        }, false);

        //chart.addSeries({
        //    color: "#000000",
        //    type: 'line',
        //    animation: false,
        //    name: 'Regression Line',
        //    lineWidth: 1,
        //    data: [[minX, corr.A * minX + corr.B], [maxX, corr.A * maxX + corr.B]],
        //    marker: {
        //        enabled: false
        //    },
        //    states: {
        //        hover: {
        //            lineWidth: 0
        //        }
        //    },
        //    enableMouseTracking: false
        //}, false);
        
        chart.redraw();
    }

    var correlations = {
        currentPartitionName: ko.observable(),

        customAxisShowUpdate: ko.observable(false),
        customAxisUpdateLabel: ko.observable("Update Axis Range"),
        customAxisUpdateEnable: ko.observable(true),
        customXAxis: ko.observable(false),
        customXAxisMin: ko.observable("-1"),
        customXAxisMax: ko.observable("1"),
        customYAxis: ko.observable(false),
        customYAxisMin: ko.observable("-1"),
        customYAxisMax: ko.observable("1"),

        updateRanges: function () {
            if (correlations.customXAxis()) {
                correlations.chart.xAxis[0].setExtremes(+correlations.customXAxisMin(), +correlations.customXAxisMax())
            } else {
                correlations.chart.xAxis[0].setExtremes(null, null)
            }

            if (correlations.customYAxis()) {
                correlations.chart.yAxis[0].setExtremes(+correlations.customYAxisMin(), +correlations.customYAxisMax())
            } else {
                correlations.chart.yAxis[0].setExtremes(null, null)
            }

            correlations.customAxisShowUpdate(false);
        },

        xAxis: ko.observable(),
        yAxis: ko.observable(),
        rawData: ko.observable(),
        maxDataPoints: maxAtomsInPlot,
        ignoreMaxDataPoints: ko.observable(false),
        removeTheLimit: function () {
            correlations.ignoreMaxDataPoints(true),
            updateChart(result);
        },
        addTheLimit: function () {
            correlations.ignoreMaxDataPoints(false),
            updateChart(result);
        },
        tooManyDataPoints: ko.observable(false),
        dataPointCount: ko.observable(0),
        chart: undefined,
        init: function () {
            if (result.computedChargeNames.length > 1) {
                this.yAxis(result.computedChargeNames[1]);
            }
            this.chart = createChart();
            updateChart(result);


            var $plot = $("#details-correlation-plot"), $wrap = $(".details-plot-wrap");
            var oldW = 0, oldH;
            var onResize = function () {
                var w = $wrap.width(), h = $wrap.height();
                if (oldW === w && oldH === h) return;
                oldW = w;
                oldH = h;
                if (w > h - 40) {
                    $plot.width(h - 40);
                } else {
                    $plot.width(w);
                }
                $(window).resize();
            };

            window.addEventListener('resize', onResize);
            onResize();
        }
    };

    function customRangesUpdated() {
        if (correlations.customXAxis()) {
            var l = correlations.customXAxisMin().trim(), h = correlations.customXAxisMax().trim();
            if (isNaN(+l)) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("'" + l + "' is not a valid range value.");
                return;
            }

            if (isNaN(+h)) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("'" + h + "' is not a valid range value.");
                return;
            }

            if (+l >= +h) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("Low value must be < high value.");
                return;
            }
        }

        if (correlations.customYAxis()) {
            var l = correlations.customYAxisMin().trim(), h = correlations.customYAxisMax().trim();
            if (isNaN(+l)) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("'" + l + "' is not a valid range value.");
                return;
            }

            if (isNaN(+h)) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("'" + h + "' is not a valid range value.");
                return;
            }

            if (+l >= +h) {
                correlations.customAxisShowUpdate(true);
                correlations.customAxisUpdateEnable(false);
                correlations.customAxisUpdateLabel("Low value must be < high value.");
                return;
            }
        }

        correlations.customAxisShowUpdate(true);
        correlations.customAxisUpdateEnable(true);
        correlations.customAxisUpdateLabel("Update Axis Range");
    }

    correlations.customXAxis.subscribe(customRangesUpdated);
    correlations.customXAxisMin.subscribe(customRangesUpdated);
    correlations.customXAxisMax.subscribe(customRangesUpdated);
    correlations.customYAxis.subscribe(customRangesUpdated);
    correlations.customYAxisMin.subscribe(customRangesUpdated);
    correlations.customYAxisMax.subscribe(customRangesUpdated);
    
    function getCorrelation(x, y) {
        var corrs = result.Partitions[correlations.currentPartitionName()].Correlations;
        var corr;
        if (corrs[x] && corrs[x][y]) {
            corr = corrs[x][y];
        } else {
            corr = $.extend({}, corrs[y][x]);
            var a = 1 / (corr.A - corr.B), b = corr.B / (corr.B - corr.A);
            corr.A = a;
            corr.B = b;
            var t = corr.IndependentId;
            corr.IndependentId = corr.DependentId;
            corr.DependentId = t;
        }
        return corr;
    }

    function updateRawData() {
        var xName = correlations.xAxis();
        if (!xName) {
            return;
        }

        var raw = [];
        $.each(result.computedChargeNames, function (i, charges) {
            if (xName === charges) { return; }
            raw.push(getCorrelation(xName, charges));
        });
        correlations.rawData(raw);
    }

    correlations.currentPartitionName.subscribe(function (name) {
        updateRawData();
        updateChart(result);
    });

    correlations.xAxis.subscribe(function (name) {
        updateRawData();
        updateChart(result);
    });

    correlations.yAxis.subscribe(function (name) {
        updateChart(result);
    });

    result.correlations = correlations;
}