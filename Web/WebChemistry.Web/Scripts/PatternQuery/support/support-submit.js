function PatternQuerySupportSubmitViewModel() {
    "use strict";

    var self = this,
        emailCheck = /^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/i,
        ansVal;

    function getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function makeQuestion(token) {
        var num = parseInt(atob(token)),
            a = getRandomInt(10, num / 2),
            b = num - a;
        ansVal = num;
        return "How much is " + a + " + " + b + "?";
    }

    this.errorText = ko.observable("");
    this.email = ko.observable("");
    this.title = ko.observable("");
    this.message = ko.observable("");
    this.answer = ko.observable("");
    this.question = ko.observable(makeQuestion(PatternQuerySupportActions.token));
    this.canSubmit = ko.observable(true);
    this.submitLabel = ko.observable("Submit");

    function updateCanSubmit() {
        var email = self.email().trim(),
            title = self.title().trim(),
            msg = self.message().trim(),
            ans = +self.answer().trim();
        
        self.errorText("");

        if (email.length > 0 && !emailCheck.test(email)) {
            self.submitLabel("Invalid email address.");
            self.canSubmit(false);
            return false;
        }

        if (title.length === 0) {
            self.submitLabel("Please enter a title.");
            self.canSubmit(false);
            return false;
        }

        if (title.length > 50) {
            self.submitLabel("The title is too long.");
            self.canSubmit(false);
            return false;
        }

        if (msg.length === 0) {
            self.submitLabel("Please enter your message.");
            self.canSubmit(false);
            return false;
        }

        if (msg.length > 5000) {
            self.submitLabel("The message is too long.");
            self.canSubmit(false);
            return false;
        }

        if (isNaN(ans) || ans !== ansVal) {
            self.submitLabel("Invalid question answer.");
            self.canSubmit(false);
            return false;
        }

        self.canSubmit(true);
        self.submitLabel("Submit");
        return true;
    }

    this.email.subscribe(updateCanSubmit);
    this.title.subscribe(updateCanSubmit);
    this.message.subscribe(updateCanSubmit);
    this.answer.subscribe(updateCanSubmit);

    updateCanSubmit();

    this.submit = function () {
        if (!updateCanSubmit()) return;

        var submittedTitle = self.title().trim();

        self.canSubmit(false);
        $.ajax({
            url: PatternQuerySupportActions.submitAction,
            data: { email: self.email().trim(), answer: self.answer().trim(), message: self.message().trim(), title: self.title().trim() },
            type: 'POST',
            dataType: 'json'
        })
        .done(function (result) {
            if (result.error) {
                if (result.type === 'captcha') {
                    self.question(makeQuestion(result.token));
                }
                self.errorText(result.message);
                return;
            }
            self.submitLabel('Redirecting...');
            RecentlySubmittedComputations.submit("PatternQuerySupport", result.id, submittedTitle);
            location.href = PatternQuerySupportActions.supportAction.replace('-id-', result.id);
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            console.log(errorThrown);
            self.errorText('Submit failed. Please try again later.');
            self.canSubmit(true);
        });
    };
}