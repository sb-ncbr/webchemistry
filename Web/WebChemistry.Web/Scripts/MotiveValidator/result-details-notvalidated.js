var notValidatedDetailsModalModel = {
    entry: ko.observable(null),
    status: ko.observable(""),
    current: ko.observable(null),
    visualizer: null
};

var notValidatedDetailsVisualization = null;

$(function () {
    ko.applyBindings(notValidatedDetailsModalModel, document.getElementById("notValidatedDetailsModal"));

    $('#notValidatedDetailsModal').on('hide', function () {
        if (notValidatedDetailsVisualization !== null) {
            notValidatedDetailsVisualization.dispose();
            notValidatedDetailsVisualization = null;
        }
    });
});

var notValidatedDetailsModelCache = {};

function createNotValidatedModelVisualization(sid, modelName) {
    var xhr = null;
    var cancel = function () {
        if (xhr !== null) {
            xhr.abort();
            xhr = null;
        }
    };

    var visualizer = createVisualizer("model3d-notValidated"); //notValidatedDetailsModalModel.visualizer;

    notValidatedDetailsModalModel.status("");
    if (!visualizer) {
        return;
    }

    var cacheKey = modelName + "-" + sid;

    if (notValidatedDetailsModelCache[cacheKey]) {
        setTimeout(function () {
            try {
                var result = notValidatedDetailsModelCache[cacheKey];
                var mol = jsonToChemDoodleMoleculeNamed(result);// molReader.read(result, 1);
                visualizer.clear();
                visualizer.loadContent([mol], []);
            } catch (ex) {
                visualizer.clear();
                notValidatedDetailsModalModel.status("Failed to load the structure.");
            }
        }, 50);
    } else {
        notValidatedDetailsModalModel.status("Loading...");
        xhr = $.ajax({
            url: validatorParams.structureAction,
            data: { model: modelName, sid: sid, type: "notvalidatedjson" },
            cache: false,
            dataType: 'json'
        })
        .done(function (result) {
            notValidatedDetailsModalModel.status("");
            xhr = null;
            try {
                var mol = jsonToChemDoodleMoleculeNamed(result); //molReader.read(result, 1);
                visualizer.clear();
                visualizer.loadContent([mol], []);
                notValidatedDetailsModelCache[cacheKey] = result;
            } catch (ex) {
                visualizer.clear();
                notValidatedDetailsModalModel.status("Failed to load the structure.");
            }
        })
        .fail(function () { notValidatedDetailsModalModel.status("Failed to load the structure."); xhr = null; });
    }

    return {
        dispose: function () {
            cancel();
            if (visualizer) {
                visualizer.clear();
                visualizer = null;
                $("#model3d-notValidated-wrap > canvas").empty().remove();
            }
        }
    };
}

function showNotValidatedDetails(entry) {
    notValidatedDetailsVisualization = createNotValidatedModelVisualization(entry.Id, entry.ModelName);
    notValidatedDetailsModalModel.entry(entry);
    $('#notValidatedDetailsModal').modal('show');
}