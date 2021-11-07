(function () {
    'use strict';

    describe('Sample Suite', function () {
        var success;

        beforeAll(function () {
            success = true;
        });

        it('should have a successful test', function () {
            expect(success).toBeTruthy();
        });

        it('should have a failing test', function () {
            expect(success).toBeFalsy();
        });
    });
})();
