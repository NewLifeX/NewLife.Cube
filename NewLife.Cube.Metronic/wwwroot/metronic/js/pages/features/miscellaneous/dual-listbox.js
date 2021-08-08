'use strict';

// Class definition
var KTDualListbox = function () {
    // Private functions
    var demo1 = function () {
        // Dual Listbox
        var _this = document.getElementById('kt_dual_listbox_1');

        // init dual listbox
        var dualListBox = new DualListbox(_this, {
            addEvent: function (value) {
                console.log(value);
            },
            removeEvent: function (value) {
                console.log(value);
            },
            availableTitle: 'Available options',
            selectedTitle: 'Selected options',
            addButtonText: 'Add',
            removeButtonText: 'Remove',
            addAllButtonText: 'Add All',
            removeAllButtonText: 'Remove All'
        });
    };

    var demo2 = function () {
        // Dual Listbox
        var _this = document.getElementById('kt_dual_listbox_2');

        // init dual listbox
        var dualListBox = new DualListbox(_this, {
            addEvent: function (value) {
                console.log(value);
            },
            removeEvent: function (value) {
                console.log(value);
            },
            availableTitle: "Source Options",
            selectedTitle: "Destination Options",
            addButtonText: "<i class='flaticon2-next'></i>",
            removeButtonText: "<i class='flaticon2-back'></i>",
            addAllButtonText: "<i class='flaticon2-fast-next'></i>",
            removeAllButtonText: "<i class='flaticon2-fast-back'></i>"
        });
    };

    var demo3 = function () {
        // Dual Listbox
        var _this = document.getElementById('kt_dual_listbox_3');

        // init dual listbox
        var dualListBox = new DualListbox(_this, {
            addEvent: function (value) {
                console.log(value);
            },
            removeEvent: function (value) {
                console.log(value);
            },
            availableTitle: 'Available options',
            selectedTitle: 'Selected options',
            addButtonText: 'Add',
            removeButtonText: 'Remove',
            addAllButtonText: 'Add All',
            removeAllButtonText: 'Remove All'
        });
    };

    var demo4 = function () {
        // Dual Listbox
        var _this = document.getElementById('kt_dual_listbox_4');

        // init dual listbox
        var dualListBox = new DualListbox(_this, {
            addEvent: function (value) {
                console.log(value);
            },
            removeEvent: function (value) {
                console.log(value);
            },
            availableTitle: 'Available options',
            selectedTitle: 'Selected options',
            addButtonText: 'Add',
            removeButtonText: 'Remove',
            addAllButtonText: 'Add All',
            removeAllButtonText: 'Remove All'
        });

        // hide search
        dualListBox.search.classList.add('dual-listbox__search--hidden');
    };

    return {
        // public functions
        init: function () {
            demo1();
            demo2();
            demo3();
            demo4();
        },
    };
}();

window.addEventListener('load', function(){
    KTDualListbox.init();
});
