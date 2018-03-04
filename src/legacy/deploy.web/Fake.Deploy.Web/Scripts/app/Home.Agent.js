var agentStatus = {
    Unknown: '#C0C0C0',
    Online: 'green',
    Offline: 'red'
};

function AgentStatusModel(available, msg) {
    return ko.mapping.fromJS({
        statusMessage: msg,
        available: available
    });
}

function AgentViewModel() {
    var self = this;

    self.agent = ko.observable();
    self.agentStatus = ko.observable(AgentStatusModel(agentStatus.Unknown, 'Querying status...'));
    self.deployments = ko.observableArray();
    self.agentDetails = ko.observable();

    self.recentMessages = ko.observableArray();

	self.updateMessagesFrom = function(data) {
         self.recentMessages([]);
         if (data != null) {
	        var response = data.xhr().response;
	        var messages = [];
	        if (response) messages = $.parseJSON(response);
            $.each(messages, function (i, msg) {
                self.recentMessages.push(msg);
            });
         }
	};

    self.getAgentDetails = function () {
        $.ajax({
            type: 'GET',
            url: '/api/v1/agent/details/' + self.agent().id(),
            dataType: 'json',
            contentType: 'application/json'
        }).done(function (data) {
            var a = ko.mapping.fromJS(data);
            self.agentDetails(a);
            self.agentStatus(AgentStatusModel(agentStatus.Online, 'Online'));
            self.refreshDeploymentsForAgent();
        }).fail(function (msg) {
            self.agentStatus(AgentStatusModel(agentStatus.Offline, 'Offline / Unreachable'));
        });
    };

    self.refreshDeploymentsForAgent = function () {
        if (self.agentStatus().available()) {
            $.ajax({
                type: "GET",
                url: '/api/v1/agent/deployments/' + self.agent().id(),
                dataType: 'json',
                contentType: 'application/json'
            }).done(function (data) {
                self.deployments([]);
                  
                $.each(data.fields || [], function (i, d) {
                    $.each(d, function (i2, dep) {
                        var deployment = ko.mapping.fromJS(dep);
                        self.deployments.push(deployment);
                    });
                });

            }).fail(function (msg) {
                toastr.error('Failed to get active deployments for agent ' + self.agent().name(), 'Error');
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
	            self.updateMessagesFrom(data);
                self.refreshDeploymentsForAgent();
            },
            fail: function (e, data) {
                $('#fileList').html('');
                $('#selectPackageBtn').removeClass('hide');
                $('#filePlaceHolder').modal('hide');
                toastr.error('Package deployment failed');
	            self.updateMessagesFrom(data);
            }
        });
        

        setInterval(function () { self.getAgentDetails(); }, 10000);
    };

    self.rollbackDeployment = function (form) {
        if (self.agentStatus().available()) {
            $('#rollbackDialog').modal('show');
            var data = $(form).serializeObject();
            var jsonStr = JSON.stringify(data);
            $.ajax({
                type: "POST",
                url: '/api/v1/package/rollback',
                dataType: 'json',
                data: jsonStr,
                contentType: 'application/json'
            }).done(function (d) {
                toastr.info('Rollback succeeded', 'Info');
                $('#rollbackDialog').modal('hide');
                self.recentMessages([]);
                if (d != null) {
                    $.each(d.Messages, function (i, msg) {
                        self.recentMessages.push(ko.mapping.fromJS(msg));
                    });
                }
                self.refreshDeploymentsForAgent();
            }).fail(function (msg) {
                var d = JSON.parse(msg.responseText);
                self.recentMessages([]);
                if (d != null) {
                    toastr.error(data.appName + ' rollback failed: ' + msg.statusText, 'Error');
                    $.each(d.Messages, function (i, msg) {
                        self.recentMessages.push(ko.mapping.fromJS(msg));
                    });

                    self.recentMessages.push(
                        ko.mapping.fromJS({
                            IsError: true,
                            Message: d.Exception.Message,
                            Timestamp: ''
                        })
                        );
                }
                $('#rollbackDialog').modal('hide');
            });
        }
    };
}