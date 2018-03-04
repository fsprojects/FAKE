function UserAdminViewModel() {
	var self = this;

	self.users = ko.observableArray();

	self.getUsers = function () {
	    self.users([]);
	    $.ajax({
	        type: "GET",
	        url: '/api/v1/user/',
	        dataType: 'json',
	        contentType: 'application/json'
	    }).done(function (data) {
	        $.each(data, function (i, d) {
	            var inst = ko.mapping.fromJS(d);
	            self.users.push(inst);
	        });
	    }).fail(function (msg) {
	        toastr.error('Failed to get users ' + msg.statusText, 'Error');
	    });
	};

	self.build = function () {
	    self.getUsers();
	};

	self.registerUser = function (form) {
	    var data = $(form).serializeObject();
	    var jsonStr = JSON.stringify(data);
		$.ajax({
			type: "POST",
			url: '/api/v1/user/',
			data: jsonStr,
			contentType: 'application/json'
		}).done(function (data) {
		    toastr.info('Successfully registered user');
		    self.getUsers();
		}).fail(function (msg) {
		    toastr.error('Failed to register user ' + msg.statusText, 'Error');
		});
	}

	self.deleteUser = function (data) {
	    $.ajax({
	        type: "DELETE",
	        url: '/api/v1/user/'+data.username(),
	        contentType: 'application/json'
	    }).done(function (d) {
	        toastr.info('Successfully deleted user');
	        self.getUsers();
	    }).fail(function (msg) {
	        toastr.error('Failed to delete user ' + msg.statusText, 'Error');
	    });
	}
}