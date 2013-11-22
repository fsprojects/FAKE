function IndexViewModel() {
    var self = this;

    self.getDynamicParameters = function(form, providerType, id) {
        var parameters = metaData[id + 's'][providerType];
        var dataProviderParameters = "";
        for (var i in parameters) {
            var p = id + '_' + parameters[i].ParameterName;
            dataProviderParameters += parameters[i].ParameterName + "=" + $('#' + p, form).val() + ";";
        }
        return dataProviderParameters;
    };

    self.addDynamicParameters = function(data, form) {
        var dataProvider = self.selectedDataProvider();
        data['DataProvider'] = dataProvider;
        data['DataProviderParameters'] = self.getDynamicParameters(form, dataProvider, 'dataProviderParameter');
        
        var membershipProvider = self.selectedMembershipProvider();
        data['MembershipProvider'] = membershipProvider;
        data['MembershipProviderParameters'] = self.getDynamicParameters(form, dataProvider, 'membershipProviderParameter');
    };

    self.setup = function(form) {
        $('#initDialog').modal('show');
        var data = $(form).serializeObject();
        self.addDynamicParameters(data, form);
        var jsonStr = JSON.stringify(data);
        $.ajax({
            type: "POST",
            url: '/Setup/SaveSetupInformation',
            data: jsonStr,
            contentType: 'application/json'
        }).done(function(d) {
            $('#initDialog').modal('hide');
            window.location.href = '/Home/Index';
        }).fail(function(msg) {
            toastr.error('Initialisation Failed', 'Error');
            $('#initDialog').modal('hide');
        });
    };

    self.dataProviders = ko.observableArray(metaData.dataProviders);
    self.membershipProviders = ko.observableArray(metaData.membershipProviders);
    self.selectedDataProvider = ko.observable("");
    self.selectedMembershipProvider = ko.observable("");

    self.providerParameters = function(provider, selection) {
        if (selection === "" || selection === undefined) {
            return [];
        } else {
            return metaData[provider][selection];
        }
    };
    
    self.dataProviderParameters = ko.computed(function () {
        var dp = self.selectedDataProvider();
        return self.providerParameters("dataProviderParameters", dp);
    });

    self.membershipProviderParameters = ko.computed(function () {
        var dp = self.selectedMembershipProvider();
        return self.providerParameters("membershipProviderParameters", dp);
    });
}