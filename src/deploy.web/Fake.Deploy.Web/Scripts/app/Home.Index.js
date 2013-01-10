function IndexViewModel() {
    var self = this;
    
    self.environments = ko.observableArray();

    self.build = function() {
        $.ajax({
            type: "GET",
            url: 'api/v1/environment/',
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            $.each(data, function (i, d) {
                var inst = ko.mapping.fromJS(d);
                self.environments.push(inst);
            });
            $('#agents li:nth-child(4n+1)').css('margin-left', '0');
            $('#envTabs a:first').tab('show');
        }).fail(function (msg) {
            toastr.error('Failed to get environments', 'Error');
        });
    };

}