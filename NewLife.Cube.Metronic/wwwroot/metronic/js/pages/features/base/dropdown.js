"use strict";

// Class definition

var KTDropdownDemo = function () {
    
    // Private functions

    // basic demo
    var demo1 = function () {
        var output = $('#kt_dropdown_api_output');
        var dropdown1 = new KTDropdown('kt_dropdown_api_1');
        var dropdown2 = new KTDropdown('kt_dropdown_api_2');

        dropdown1.on('afterShow', function(dropdown) {
            output.append('<p>Dropdown 1: afterShow event fired</p>');
        });
        dropdown1.on('beforeShow', function(dropdown) {
            output.append('<p>Dropdown 1: beforeShow event fired</p>');
        });
        dropdown1.on('afterHide', function(dropdown) {
            output.append('<p>Dropdown 1: afterHide event fired</p>');
        });
        dropdown1.on('beforeHide', function(dropdown) {
            output.append('<p>Dropdown 1: beforeHide event fired</p>');
        });
    
        dropdown2.on('afterShow', function(dropdown) {
            output.append('<p>Dropdown 2: afterShow event fired</p>');
        });
        dropdown2.on('beforeShow', function(dropdown) {
            output.append('<p>Dropdown 2: beforeShow event fired</p>');
        });
        dropdown2.on('afterHide', function(dropdown) {
            output.append('<p>Dropdown 2: afterHide event fired</p>');
        });
        dropdown2.on('beforeHide', function(dropdown) {
            output.append('<p>Dropdown 2: beforeHide event fired</p>');
        });    
    }

    return {
        // public functions
        init: function() {
            demo1();
        }
    };
}();

jQuery(document).ready(function() {    
    KTDropdownDemo.init();
});