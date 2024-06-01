function initAggregates(result) {
    "use strict";

    function createChart() {
        var chart = new Highcharts.Chart({
            title: { text: 'No data selected' },
            subtitle: { text: 'No details available' },
            chart: { type: 'column', renderTo: $("#details-aggregate-plot")[0] },
            //xAxis: { categories: [] },
            xAxis: { categies: [], title: { text: 'Group Property' } },
            yAxis: { title: { text: 'Property Value' } },
            exporting: { enabled: true },
            credits: { enabled: false },
            plotOptions: {
                series: {
                    animation: false
                }
            }
        });
        return chart;
    }

    function updateChart(result) {
        var aggregates = result.aggregates;
        var chart = aggregates.chart;

        if (!chart || result.computedChargeNames.length === 0) {
            return;
        }


        var title = "<b>" + aggregates.currentProperty().name + "</b> of <b>" + aggregates.currentPartitionName() + "</b> according to <b>" + aggregates.currentCluster() + "</b>";
        chart.setTitle( { text: "<b>" + result.Summary.Id  + "</b>" }, { text: title });

        var clusters = result.Partitions[aggregates.currentPartitionName()].Clusters[aggregates.currentCluster()].Clusters;
        var categories = clusters.map(function (c) { return c.Key; });
        chart.xAxis[0].setCategories(categories, false);

        while (chart.series.length > 0) {
            chart.series[chart.series.length - 1].remove(false);
        }

        var prop = aggregates.currentProperty().value;
        var colors = Highcharts.getOptions().colors;
        $.each(result.computedChargeNames, function (i, charges) {
            var p = result.Partitions[aggregates.currentPartitionName()];
            if (!p || !p.Charges[charges]) {
                return;
            }
            var stats = p.Charges[charges].ClusterStats[aggregates.currentCluster()];

            chart.addSeries({
                color: colors[i % colors.length],
                name: charges,
                data: stats.Stats.map(function (s) { return s[prop]; })
            }, false);
        });

        chart.redraw();
    }

    var aggregates = {
        currentPartitionName: ko.observable(),
        clusterList: ko.observable(),
        currentCluster: ko.observable(),
        propertyList: [
            { value: "MinCharge", name: "Minimum Charge" },
            { value: "MaxCharge", name: "Maximum Charge" },
            { value: "Average", name: "Average Charge" },
            { value: "AbsAverage", name: "Average Absolute Charge" },
            { value: "Median", name: "Median Charge" },
            { value: "AbsMedian", name: "Median Absolute Charge" },
            { value: "Sigma", name: "Standard Charge Deviation" },
            { value: "AbsSigma", name: "Standard Abs. Charge Deviation" }
        ],
        currentProperty: ko.observable(),
        currentDetailsName: ko.observable(),
        currentDetails: ko.observable(),
        chart: undefined,
        init: function () {
            this.chart = createChart();
            updateChart(result);
        }
    };
    
    function updateDetails() {
        var name = aggregates.currentDetailsName();
        if (!name) {
            return;
        }
        var p = result.Partitions[aggregates.currentPartitionName()];
        var charges = p.Charges[name];
        var stats = charges.ClusterStats[aggregates.currentCluster()];
        aggregates.currentDetails(stats);
    }

    aggregates.currentPartitionName.subscribe(function (name) {
        var p = result.Partitions[name];
        var clusters = Object.keys(p.Clusters);
        aggregates.clusterList(clusters);
        aggregates.currentCluster(clusters[0]);
        updateDetails();
        updateChart(result);
    });

    aggregates.currentCluster.subscribe(function (name) {
        updateDetails();
        updateChart(result);
    });

    aggregates.currentProperty.subscribe(function (name) {
        updateChart(result);
    });

    aggregates.currentDetailsName.subscribe(function (name) {
        updateDetails();
    });
    
    result.aggregates = aggregates;    
}