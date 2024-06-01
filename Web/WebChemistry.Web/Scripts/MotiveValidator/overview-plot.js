function makeOverviewPlot(summaryData, targetId, explicit, onBarClick) {
    var total = (summaryData.Analyzed || 0) + (summaryData.NotAnalyzed || 0),
        columns = [], names = [],
        doOnClick = !!onBarClick;
    
    var csv = '"Name","Header","Value","Percent"\n';

    _.forEach(MotiveValidatorGlobalsColumnOrdering, function (c) {       
        var value = summaryData[c];
        var percent = (100.0 * value / total);
        var props = MotiveValidatorGlobals[c];

        if (isNaN(value)) {
            return;
        }

        if (value > 0) {
            csv += '"' + c + '","' + props.name + '",' + value + ',' + percent.toFixed(2) + '\n';
        }

        if (c.indexOf("HasAll_GoodChirality") === 0 && c !== 'HasAll_GoodChirality') {
            if (summaryData["HasAll_GoodChirality"] === summaryData[c]) {
                return;
            }
        }

        //if (c.indexOf("HasAll_BadChirality") === 0 && c !== 'HasAll_BadChirality') {
        //    if (summaryData["HasAll_BadChirality"] === summaryData[c]) {
        //        return;
        //    }
        //}
        
        if ((explicit && value === 0) || (!explicit && percent < 0.5)) {
            return;
        }

        names.push(props.name);
        columns.push({ y: percent, tooltip: props.tooltip, value: value, total: total, color: props.color, categoryName: c });
    });

    var link = "data:text/csv;charset=UTF-8," + encodeURIComponent(csv);

    $("#" + targetId).highcharts({
        title: { text: '' },
        chart: { type: 'bar' },
        tooltip: { useHTML: true, pointFormat: '{point.tooltip}<br/><b>{point.value} of {point.total} ({point.y:.2f}%)</b><br/>', followPointer: true },
        xAxis: { categories: names },
        yAxis: { min: 0, max: 100, title: { text: '% of residues' }, labels: { enabled: true } },
        series: [{ name: 'percent values', data: columns }],
        legend: { enabled: false },
        exporting: { enabled: false },
        credits: { enabled: false },
        plotOptions: doOnClick 
            ? { 
                series: {
                    cursor: 'pointer',
                    point: {
                        events: { 
                            click: function () {
                                onBarClick(this.categoryName);
                            }
                        }
                    }
                }
            }
            : {}
    });

    $("#" + targetId)
        .css({ position: 'relative' })
        .append($("<a>")
            .attr({
                'href': link,
                'download': 'overview.csv',
                'title': 'Plot data in CSV format. Includes all non-zero categories (the plot displays only data with at least 0.5% occurrences).',
                'target': '_blank'
            })
            .text("CSV")
            .css({
                position: 'absolute',
                bottom: '15px',
                right: '10px',
                'font-size': '8pt'
            }));
}