function AgentAdminViewModel() {
	var self = this;

	self.environments = ko.observableArray();

	self.build = function () {
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
}