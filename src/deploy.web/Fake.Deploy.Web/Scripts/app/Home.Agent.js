function AgentStatusModel(available, msg) {
    var self = this;

    self.statusMessage = ko.observable(msg);
    self.available = ko.observable(available);
}

function AgentViewModel() {
    var self = this;

    self.agent = ko.observable();
    self.agentStatus = ko.observable(new AgentStatusModel(false, 'Offline / Unreachable'));
    self.deployments = ko.observableArray();
    self.agentDetails = ko.observable();

    self.getAgentDetails = function () {
        $.ajax({
            type: "GET",
            url: self.agent().Address() + 'fake/statistics',
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            var a = ko.mapping.fromJS(data)
            self.agentDetails(a);
            self.agentStatus(new AgentStatusModel(true, 'Online'));
            self.refreshDeploymentsForAgent();
        }).fail(function (msg) {
            self.agentStatus(new AgentStatusModel(false, 'Offline / Unreachable'));
        });
    };

    self.refreshDeploymentsForAgent = function () {
        if (self.agentStatus().available()) {
            $.ajax({
                type: "GET",
                url: self.agent().Address() + 'fake/deployments?status=active',
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (data) {
                self.deployments([]);
                $.each(data.values, function (i, d) {
                    $.each(d, function (i2, dep) {
                        var deployment = ko.mapping.fromJS(dep);
                        self.deployments.push(deployment);
                    });
                });
                
            }).fail(function (msg) {
                toastr.error('Failed to get active deployments for agent ' + self.agent().Name(), 'Error');
            });
        }
    };

    self.build = function (agentId) {
        $.ajax({
            type: "GET",
            url: '/api/v1/agent/' + agentId,
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            var a = ko.mapping.fromJS(data);
            self.agent(a);
            self.getAgentDetails();
        }).fail(function (msg) {
            toastr.error('Failed to get agent ' + agentId, 'Error');
        });

        $('#fileupload').fileupload({
            dataType: 'json',
            redirect: '@Url.Action("Agent", new { agentId = Model })',
            add: function (e, data) {
                $('#selectPackageBtn').addClass('hide');
                $('#filePlaceHolder').modal('show');
                $.each(data.files, function (i, file) {
                    $('#fileName').html('<h4> Deploying: ' + file.name + '</h4>');
                });

                data.submit();
            },
            progressall: function (e, data) {
                var progress = parseInt(data.loaded / data.total * 100, 10);
                $('#progress .bar').css(
                    'width',
                    progress + '%'
                );
            },
            done: function (e, data) {
                $('#fileList').html('');
                $('#filePlaceHolder').modal('hide');
                $('#selectPackageBtn').removeClass('hide');
                toastr.info('Package deployed');
                self.refreshDeploymentsForAgent();
            },
            fail: function (e, data) {
                $('#fileList').html('');
                $('#selectPackageBtn').removeClass('hide');
                $('#filePlaceHolder').modal('hide');
                toastr.error('Package deployment failed');
            }
        });

        setInterval(function () { self.getAgentDetails(); }, 10000);
    };

    self.rollbackDeployment = function (data) {
        if (self.agentStatus().available()) {
            $('#rollbackDialog').modal('show');
            $.ajax({
                type: "PUT",
                url: self.agent().Address() + 'fake/deployments/' + data.Id() + '?version=HEAD~1',
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (d) {
                toastr.info('Rollback succeeded', 'Info');
                $('#rollbackDialog').modal('hide');
                self.refreshDeploymentsForAgent();
            }).fail(function (msg) {
                toastr.error('Failed to rollback ' + self.agent().Name() + ' - ' + data.Id() + ' ' + msg.statusText, 'Error');
                $('#rollbackDialog').modal('hide');
                console.log(msg.responseText);
            });
        } 
    };
}