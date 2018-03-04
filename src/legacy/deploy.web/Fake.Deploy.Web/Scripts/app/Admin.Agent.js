function AgentAdminViewModel() {
	var self = this;

	self.environments = ko.observableArray();
	self.agents = ko.observableArray();

	self.getEnvironments = function () {
	    self.environments([]);
	    $.ajax({
	        type: "GET",
	        url: '/api/v1/environment/',
	        dataType: 'json',
	        contentType: 'application/json'
	    }).done(function (data) {
	        $.each(data, function (i, d) {
	            var inst = ko.mapping.fromJS(d);
	            self.environments.push(inst);
	        });
	    }).fail(function (msg) {
	        toastr.error('Failed to get environments ' + msg.statusText, 'Error');
	    });
	};

	self.getAgents = function () {
	    self.agents([]);
	    $.ajax({
	        type: "GET",
	        url: '/api/v1/agent/',
	        dataType: 'json',
	        contentType: 'application/json',
            cache: false
	    }).done(function (data) {
	        $.each(data, function (i, d) {
	            var inst = ko.mapping.fromJS(d);
	            self.agents.push(inst);
	        });
	    }).fail(function (msg) {
	        toastr.error('Failed to get agents ' + msg.statusText, 'Error');
	    });
	};

	self.build = function () {
	    self.getEnvironments();
	    self.getAgents();
	};

	self.registerAgent = function (form) {
		$.ajax({
			method: "POST",
			url: '/api/v1/agent/',
			data: $(form).serialize(),
			contentType: 'application/x-www-form-urlencoded'
		}).done(function (data) {
		    toastr.info('Successfully registered agent');
		    self.getAgents();
		}).fail(function (msg) {
		    toastr.error('Failed to register agent ' + msg.statusText, 'Error');
		});
	}

	self.deleteAgent = function (data) {
	    $.ajax({
	        method: "DELETE",
	        url: '/api/v1/agent/'+data.id(),
	        contentType: 'application/json'
	    }).done(function (d) {
	        toastr.info('Successfully deleted agent');
	        self.getAgents();
	    }).fail(function (msg) {
	        toastr.error('Failed to delete agent ' + msg.statusText, 'Error');
	    });
	}
}