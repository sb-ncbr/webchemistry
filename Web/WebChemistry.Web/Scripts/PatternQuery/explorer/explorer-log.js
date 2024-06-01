function PatternQueryExplorerLogViewModel(vm, host) {
    "use strict";

    var self = this,
        $host = $('#' + host);

    this.messages = ko.observableArray([]);
    
    this.toggleScroll = function (element, type) {
        var $e = $(element),
            $item = $e.closest('.mq-log-item-content');
        
        if (type === "warnings") {
            $item.find('.mq-log-item-warnings').toggle();
            $item.find('.mq-log-item-errors').hide();
        } else {
            $item.find('.mq-log-item-errors').toggle();
            $item.find('.mq-log-item-warnings').hide();
        }

        $host.animate({
            scrollTop: $item.position().top
        }, 200);
    };

    function scrollToBottom() {
        $host.scrollTop($host[0].scrollHeight);
    }

    this.error = function (message) {
        self.messages.push({ log: self, timestamp: new Date().toLocaleTimeString(), type: 'error', text: message });
        scrollToBottom();
    };

    this.errorSet = function (message, errors) {
        self.messages.push({ log: self, timestamp: new Date().toLocaleTimeString(), type: 'errorSet', text: message, errors: errors });
        scrollToBottom();
    };

    this.message = function (message) {
        self.messages.push({ log: self, timestamp: new Date().toLocaleTimeString(), type: 'message', text: message });
        scrollToBottom();
    };
    
    this.addStructures = function (result) {
        self.messages.push({ log: self, timestamp: new Date().toLocaleTimeString(), type: 'structures', structures: result.NewIdentifiers, warnings: result.Warnings, errors: result.Errors });
        scrollToBottom();
    };

    this.addMotifs = function (result) {
        self.messages.push({
            log: self,
            timestamp: new Date().toLocaleTimeString(),
            type: 'motifs',
            timing: mqResultUtils.formatMsTime(result.QueryTimeMs),
            count: result.Patterns.length,
            structureCount: result.StructureCount,
            warnings: result.Warnings,
            errors: result.Errors
        });
        scrollToBottom();
    };

    this.structureWarnings = function (s) {
        self.messages.push({ log: self, timestamp: new Date().toLocaleTimeString(), type: 'structure-warnings', id: s.Id, warnings: s.Warnings });
        scrollToBottom();
    };

    this.removeItem = function (item) {
        self.messages.remove(item);
    };

    this.clear = function () {
        self.messages.removeAll();
    };
}