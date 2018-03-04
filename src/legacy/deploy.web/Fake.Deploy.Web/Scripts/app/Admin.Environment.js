function EnvironmentAdminViewModel() {
    var self = this;

    self.environments = ko.observableArray();

    self.getEnvironments = function () {
        self.environments([]);
        $.ajax({
            type: "GET",
            url: '/api/v1/environment/',
            dataType: 'json',
            contentType: 'application/json',
            cache: false
        }).done(function (data) {
            $.each(data, function (i, d) {
                var inst = ko.mapping.fromJS(d);
                self.environments.push(inst);
            });
        }).fail(function (msg) {
            toastr.error('Failed to get environments', 'Error');
        });
    };

    self.build = function () {
        self.getEnvironments();
    };

    self.saveEnvironment = function (form) {
        var data = $(form).serializeObject();
        data.Id = null;
        data.Agents = [];
        var jsonStr = JSON.stringify(data);
        $.ajax({
            type: "POST",
            url: '/api/v1/environment',
            data: jsonStr,
            contentType: 'application/json'
        }).done(function (d) {
            toastr.info('Environment saved', 'Info');
            self.getEnvironments();
        }).fail(function (msg) {
            toastr.error('Failed to save environment', 'Error');
        });

        return false;
    };

    self.deleteEnvironment = function (data) {
        $.ajax({
            type: "DELETE",
            url: '/api/v1/environment/' + data.id(),
            contentType: 'application/json'
        }).done(function (d) {
            toastr.info('Successfully deleted environment');
            self.getEnvironments();
        }).fail(function (msg) {
            toastr.error('Failed to delete environment', 'Error');
        });
    }
}