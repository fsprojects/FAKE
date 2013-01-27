function IndexViewModel() {
    var self = this;

    self.setup = function (form) {
        $('#initDialog').modal('show');
        var data = $(form).serializeObject();
        var jsonStr = JSON.stringify(data);
        $.ajax({
            type: "POST",
            url: '/Setup/SaveSetupInformation',
            data: jsonStr,
            contentType: 'application/json'
        }).done(function (d) {
            $('#initDialog').modal('hide');
            window.navigate('/Home/Index');
        }).fail(function (msg) {
            toastr.error('Initialisation Failed', 'Error');
            $('#initDialog').modal('hide');
        });
    }
}