function PatternQuerySupportThreadViewModel() {
    'use strict';

    var self = this;

    this.canRefresh = ko.observable(true);
    this.sendLabel = ko.observable("Send");
    this.canSend = ko.observable(true);   
    this.messages = ko.observableArray([]);
    this.message = ko.observable("");
    this.errorText = ko.observable("");

    this.refresh = function () {
        self.errorText("");
        self.canRefresh(false);
        $.ajax({ url: PatternQuerySupportActions.messagesAction, type: 'GET', dataType: 'json' })
       .done(function (result) {
           if (result.error) {               
               self.errorText(result.message);
               return;
           }
           self.canRefresh(true);
           self.messages.removeAll();
           _.forEach(result, function (m) { self.messages.push(m); });
           console.log(result);
       })
       .fail(function (jqXHR, textStatus, errorThrown) {
           console.log(errorThrown);
           self.errorText('Refresh failed. Please try again later.');
           self.canRefresh(true);
       });
    };

    function updateCanSend() {
        var msg = self.message().trim();

        self.errorText("");

        if (msg.length === 0) {
            self.sendLabel("Nothing to send.");
            self.canSend(false);
            return false;
        }
        if (msg.length > 5000) {
            self.sendLabel("Message too long.");
            self.canSend(false);
            return false;
        }

        self.sendLabel("Send");
        self.canSend(true);
        return true;
    }

    this.send = function () {
        if (!updateCanSend()) return;

        self.canSend(false);
        $.ajax({ url: PatternQuerySupportActions.sendAction, type: 'POST', data: { message: self.message() }, dataType: 'json' })
       .done(function (result) {
           if (result.error) {
               self.errorText(result.message);
               return;
           }
           self.message("");
           updateCanSend();
           self.canRefresh(true);
           self.messages.push(result);
       })
       .fail(function (jqXHR, textStatus, errorThrown) {
           console.log(errorThrown);
           self.errorText('Refresh failed. Please try again later.');
           self.canSend(true);
       });
    };

    this.message.subscribe(updateCanSend);

    this.refresh();
}

$(function () {
    ko.applyBindings(new PatternQuerySupportThreadViewModel(), document.getElementById('mq-support'));
    $("#resultUrl").click(function () { $(this).select(); });
});