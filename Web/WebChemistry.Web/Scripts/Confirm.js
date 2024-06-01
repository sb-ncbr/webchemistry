var WebChem = WebChem || {};
(function (WebChem) {
    var onConfirmYes = function () { };
    var onConfirmNo = function () { };

    var $confirmModal = $("<div id='modal-confirm' class='modal hide'><div class='modal-header'><a href='#' class='close' data-dismiss='modal' aria-hidden='true'>&times;</a><h3><span id='modalHeader'></span></h3></div><div class='modal-body'><div id='modalBody'></div></div><div class='modal-footer'>      <a href='#' class='btn btn-danger' id='confirmYesBtn'>Yes</a><a href='#' class='btn' id='confirmNoBtn'>No</a></div></div>");
    var $confirmHeader = $confirmModal.find("#modalHeader");
    var $confirmBody = $confirmModal.find("#modalBody");

    var modalConfirm = $("#modal-confirm");

    $confirmModal.find("#confirmYesBtn").click(function (e) {
        e.preventDefault = true;
        $confirmModal.modal('hide');
        onConfirmYes();
    });

    $confirmModal.find("#confirmNoBtn").click(function (e) {
        e.preventDefault = true;
        $confirmModal.modal('hide');
        onConfirmNo();
    });

    WebChem.confirm = function (options) {
        if (options === undefined) option = {};
        if (options.header === undefined) options.header = "Header";
        if (options.message === undefined) options.message = "Body";
        if (options.onYes === undefined) options.onYes = function () { };
        if (options.onNo === undefined) options.onNo = function () { };

        $confirmHeader.html(options.header);
        $confirmBody.html(options.message);
        onConfirmYes = options.onYes;
        onConfirmNo = options.onNo;
        $confirmModal.modal('show');
    };


    WebChem.confirmFormAction = function (options) {

        if (options.header === undefined) options.header = "Confirm";

        WebChem.confirm(options);
    }
})(WebChem || (WebChem = {}));