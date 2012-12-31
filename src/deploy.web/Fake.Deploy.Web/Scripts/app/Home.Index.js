function IndexViewModel() {
    var self = this;
    
    self.selectedAgent = ko.observable();
    self.deployments = ko.observableArray();
    self.environments = ko.observableArray();

    self.agentSelected = function (agent) {
        self.selectedAgent(agent);
        $.ajax({
            type: "GET",
            url: agent.Address() + 'fake/deployments?status=active',
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
        $('#agentDialog').modal('show');
    };

    self.agentDeselected = function () {
        self.selectedAgent(null);
        self.deployments([]);
        $('#agentDialog').modal('hide');
    };

    self.rollbackDeployment = function (app) {
        if (self.selectedAgent() == null) {
            toastr.error('No agent selected for rollback', 'Error');
        } else {
            var agent = self.selectedAgent();
            $.ajax({
                type: "PUT",
                url: agent.Address() + '/fake/deployments/' + app.Id() + '?version=HEAD~1',
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (data) {
                toastr.info(app.Id() + ' rolled back', 'Info');
            }).fail(function (msg) {
                toastr.error('Failed to rollback ' + agent.Name() + ' - ' + app.Id(), 'Error');
                console.log(msg.responseText);
            });
        }
    };

    self.build = function() {
        $.ajax({
            type: "GET",
            url: 'api/v1/environment/',
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            $.each(data, function (i, d) {
                var inst = ko.mapping.fromJS(d)
                self.environments.push(inst);
            });
            $('#agents li:nth-child(4n+1)').css('margin-left', '0');
        }).fail(function (msg) {
            toastr.error('Failed to get environments', 'Error');
        });
    };

}