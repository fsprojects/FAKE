function AgentViewModel() {
    var self = this;

    self.agent = ko.observable();
    self.deployments = ko.observableArray();
    self.environments = ko.observableArray();

    self.refreshDeploymentsForAgent = function () {
        $.ajax({
            type: "GET",
            url: self.agent().Address() + 'fake/deployments?status=active',
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
            toastr.error('Failed to get active deployments for agent ' + self.agent().Name(), 'Error');
        });
    };

    self.createAgentView = function (agentId) {
        $.ajax({
            type: "GET",
            url: '/api/v1/agent/' + agentId,
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            var a = ko.mapping.fromJS(data)
            self.agent(a);
            self.refreshDeploymentsForAgent();
        }).fail(function (msg) {
            toastr.error('Failed to get agent ' + agentId, 'Error');
        });
    };

    self.createRegisterAgentView = function() {
        $.ajax({
            type: "GET",
            url: '/api/v1/environment/',
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            $.each(data, function (i, d) {
                var inst = ko.mapping.fromJS(d)
                self.environments.push(inst);
            });
        }).fail(function (msg) {
            toastr.error('Failed to get environments', 'Error');
        });
    };

    self.registerAgent = function (form) {
        $.ajax({
            type: "POST",
            url: '/api/v1/agent/',
            dataType: 'json',
            data: $(form).serialize(),
            contentType: 'application/x-www-form-urlencoded'
        }).done(function (data) {
            toastr.info('Successfully registered agent')
        }).fail(function (msg) {
            toastr.error('Failed to register agent', 'Error');
        });
    }

    self.deployPackage = function (form) {
        return true;
    }

    self.rollbackDeployment = function (data) {
            $.ajax({
                type: "PUT",
                url: agent.Address() + '/fake/deployments/' + data.Name() + '?version=HEAD~1',
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (data) {
                toastr.info(app + ' rolled back', 'Info');
            }).fail(function (msg) {
                toastr.error('Failed to rollback ' + agent.Name() + ' - ' + data.Name(), 'Error');
                console.log(msg.responseText);
            });
    };
}