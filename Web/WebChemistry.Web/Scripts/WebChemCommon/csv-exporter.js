
var WebChemUtils;
(function (WebChemUtils) {
    "use strict";

    WebChemUtils.toCsv = function (data, columns, separator, delimiter) {

        console.log(columns);

        separator = separator || ",";
        delimiter = delimiter || '"';

        var ret = [],
            delim = new RegExp(delimiter, 'g'), doubleDelim = delimiter + delimiter;

        ret.push(_.map(columns, function (c) {
            return delimiter + c.header.replace(delim, doubleDelim) + delimiter;
        }).join(separator));

        _.forEach(data, function (row) {
            var csv = _.map(columns, function (c) {
                if (c.isNumeric) return c.getter(row);
                var val = c.getter(row), str;
                if (val === undefined || val === null) {
                    str = "";
                } else {
                    str = val.toString();
                }
                console.log(c.header, str);
                return delimiter + str.replace(delim, doubleDelim) + delimiter;
            }).join(separator);
            ret.push(csv);
        });

        return ret.join('\n');
    };
})(WebChemUtils || (WebChemUtils = {}));