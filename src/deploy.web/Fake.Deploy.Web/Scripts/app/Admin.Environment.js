function EnvironmentAdminViewModel() {
    var self = this;

    self.saveEnvironment = function (form) {
        var data = $(form).serializeObject();
        data.Id = null;
        data.Agents = [];
        var jsonStr = JSON.stringify(data);
        $.ajax({
            type: "POST",
            url: '/api/v1/environment',
            dataType: 'json',
            data: jsonStr,
            contentType: 'application/json'
        }).done(function (data) {
            toastr.info('Environment saved', 'Info');
        }).fail(function (msg) {
            toastr.error('Failed to save environment', 'Error');
        });

        return false;
    };
}