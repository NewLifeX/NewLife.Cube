// Class definition

var KTBootstrapDatetimepicker = function () {
    // Private functions
    var baseDemos = function () {
        // Demo 1
        $('#kt_datetimepicker_1').datetimepicker();

        // Demo 2
        $('#kt_datetimepicker_2').datetimepicker({
            locale: 'de'
        });

        // Demo 3
        $('#kt_datetimepicker_3').datetimepicker({
            format: 'L'
        });

        // Demo 4
        $('#kt_datetimepicker_4').datetimepicker({
            format: 'LT'
        });

        // Demo 5
        $('#kt_datetimepicker_5').datetimepicker();

        // Demo 6
        $('#kt_datetimepicker_6').datetimepicker({
            defaultDate: '11/1/2020',
            disabledDates: [
                moment('12/25/2020'),
                new Date(2020, 11 - 1, 21),
                '11/22/2022 00:53'
            ]
        });

        // Demo 7
        $('#kt_datetimepicker_7_1').datetimepicker();
        $('#kt_datetimepicker_7_2').datetimepicker({
            useCurrent: false
        });

        $('#kt_datetimepicker_7_1').on('change.datetimepicker', function (e) {
            $('#kt_datetimepicker_7_2').datetimepicker('minDate', e.date);
        });
        $('#kt_datetimepicker_7_2').on('change.datetimepicker', function (e) {
            $('#kt_datetimepicker_7_1').datetimepicker('maxDate', e.date);
        });

        // Demo 8
        $('#kt_datetimepicker_8').datetimepicker({
            inline: true,
        });
    }

    var modalDemos = function () {
        // Demo 9
        $('#kt_datetimepicker_9').datetimepicker();

        // Demo 10
        $('#kt_datetimepicker_10').datetimepicker({
            locale: 'de'
        });

        // Demo 11
        $('#kt_datetimepicker_11').datetimepicker({
            format: 'L'
        });
    }

    var validationDemos = function () {
        // Demo 12
        $('#kt_datetimepicker_12').datetimepicker();

        // Demo 13
        $('#kt_datetimepicker_13').datetimepicker();
    }

    return {
        // Public functions
        init: function() {
            baseDemos();
            modalDemos();
            validationDemos();
        }
    };
}();

jQuery(document).ready(function() {
    KTBootstrapDatetimepicker.init();
});
