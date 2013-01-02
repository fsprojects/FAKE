function AgentViewModel() {
    var self = this;

    self.deployments = ko.observableArray();

    self.build = function (url) {
        $.ajax({
            type: "GET",
            url: url + 'fake/deployments?status=active',
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            self.deployments([]);
            $.each(data.values, function (i, d) {
                $.each(d, function (i, dep) {
                    var deployment = ko.mapping.fromJS(dep)
                    self.deployments.push(deployment);
                });
            });
        }).fail(function (msg) {
            toastr.error('Failed to get active deployments for agent ' + agent.Name(), 'Error');
        });
    };

    self.rollbackDeployment = function (url, app) {
            $.ajax({
                type: "PUT",
                url: url + '/fake/deployments/' + app + '?version=HEAD~1',
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (data) {
                toastr.info(app + ' rolled back', 'Info');
            }).fail(function (msg) {
                toastr.error('Failed to rollback ' + agent.Name() + ' - ' + app, 'Error');
                console.log(msg.responseText);
            });
    };
}