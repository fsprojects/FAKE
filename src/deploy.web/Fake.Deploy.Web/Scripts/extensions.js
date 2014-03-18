/**
 * Date.parse with progressive enhancement for ISO 8601 <https://github.com/csnover/js-iso8601>
 * © 2011 Colin Snover <http://zetafleet.com>
 * Released under MIT license.
 */
(function (Date, undefined) {
    var origParse = Date.parse, numericKeys = [1, 4, 5, 6, 7, 10, 11];
    Date.parse = function (date) {
        var timestamp, struct, minutesOffset = 0;

        // ES5 §15.9.4.2 states that the string should attempt to be parsed as a Date Time String Format string
        // before falling back to any implementation-specific date parsing, so that’s what we do, even if native
        // implementations could be faster
        //              1 YYYY                2 MM       3 DD           4 HH    5 mm       6 ss        7 msec        8 Z 9 ±    10 tzHH    11 tzmm
        if ((struct = /^(\d{4}|[+\-]\d{6})(?:-(\d{2})(?:-(\d{2}))?)?(?:T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{3}))?)?(?:(Z)|([+\-])(\d{2})(?::(\d{2}))?)?)?$/.exec(date))) {
            // avoid NaN timestamps caused by “undefined” values being passed to Date.UTC
            for (var i = 0, k; (k = numericKeys[i]) ; ++i) {
                struct[k] = +struct[k] || 0;
            }

            // allow undefined days and months
            struct[2] = (+struct[2] || 1) - 1;
            struct[3] = +struct[3] || 1;

            if (struct[8] !== 'Z' && struct[9] !== undefined) {
                minutesOffset = struct[10] * 60 + struct[11];

                if (struct[9] === '+') {
                    minutesOffset = 0 - minutesOffset;
                }
            }

            timestamp = Date.UTC(struct[1], struct[2], struct[3], struct[4], struct[5] + minutesOffset, struct[6], struct[7]);
        }
        else {
            timestamp = origParse ? origParse(date) : NaN;
        }

        return timestamp;
    };
}(Date));

ko.bindingHandlers.date = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
        try {
            var jsonDate = ko.utils.unwrapObservable(valueAccessor());
            var d1;
            if (jsonDate == '' || jsonDate == undefined) {
                d1 = Date.now();
            }
            else {
                d1 = new Date(Date.parse(jsonDate));
            }
            var curr_year = d1.getFullYear();

            var curr_month = d1.getMonth() + 1; //Months are zero based
            if (curr_month < 10)
                curr_month = "0" + curr_month;

            var curr_date = d1.getDate();
            if (curr_date < 10)
                curr_date = "0" + curr_date;

            var curr_hour = d1.getHours();
            if (curr_hour < 10)
                curr_hour = "0" + curr_hour;

            var curr_min = d1.getMinutes();
            if (curr_min < 10)
                curr_min = "0" + curr_min;

            var curr_sec = d1.getSeconds();
            if (curr_sec < 10)
                curr_sec = "0" + curr_sec;

            var newtimestamp = curr_year + "-" + curr_month + "-" + curr_date + " " + curr_hour + ":" + curr_min + ":" + curr_sec;
            element.innerHTML = newtimestamp;

        }
        catch (exc) {
        }
    }
};