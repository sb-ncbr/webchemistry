function PatternQueryExplorerStatusViewModel(vm, overlay, spinnerHost) {
    "use strict";

    var self = this,
        $overlay = $('#' + overlay),
        spinHostElement = document.querySelector('#' + spinnerHost),
        spinner = new Spinner({ hwaccel: true, radius: 50, length: 35, width: 10, lines: 20, color: '#fff' });

    this.message = ko.observable('');
    this.isBusy = ko.observable(false);

    this.setBusy = function (busy) {
        self.isBusy(busy);

        if (!busy) {
            spinner.stop();
            $overlay.hide();
            return;
        }

        $overlay.show();
        spinner.spin(spinHostElement);
    };
}