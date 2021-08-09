"use strict";

var KTCardTools = function () {
    // Toastr
    var initToastr = function() {
        toastr.options.showDuration = 1000;
    }

    // Demo 1
    var demo1 = function() {
        // This card is lazy initialized using data-card="true" attribute. You can access to the card object as shown below and override its behavior
        var card = new KTCard('kt_card_1');

        // Toggle event handlers
        card.on('beforeCollapse', function(card) {
            setTimeout(function() {
                toastr.info('Before collapse event fired!');
            }, 100);
        });

        card.on('afterCollapse', function(card) {
            setTimeout(function() {
                toastr.warning('Before collapse event fired!');
            }, 2000);
        });

        card.on('beforeExpand', function(card) {
            setTimeout(function() {
                toastr.info('Before expand event fired!');
            }, 100);
        });

        card.on('afterExpand', function(card) {
            setTimeout(function() {
                toastr.warning('After expand event fired!');
            }, 2000);
        });

        // Remove event handlers
        card.on('beforeRemove', function(card) {
            toastr.info('Before remove event fired!');

            return confirm('Are you sure to remove this card ?');  // remove card after user confirmation
        });

        card.on('afterRemove', function(card) {
            setTimeout(function() {
                toastr.warning('After remove event fired!');
            }, 2000);
        });

        // Reload event handlers
        card.on('reload', function(card) {
            toastr.info('Leload event fired!');

            KTApp.block(card.getSelf(), {
                overlayColor: '#ffffff',
                type: 'loader',
                state: 'primary',
                opacity: 0.3,
                size: 'lg'
            });

            // update the content here

            setTimeout(function() {
                KTApp.unblock(card.getSelf());
            }, 2000);
        });
    }

    // Demo 2
    var demo2 = function() {
        // This card is lazy initialized using data-card="true" attribute. You can access to the card object as shown below and override its behavior
        var card = new KTCard('kt_card_2');

        // Toggle event handlers
        card.on('beforeCollapse', function(card) {
            setTimeout(function() {
                toastr.info('Before collapse event fired!');
            }, 100);
        });

        card.on('afterCollapse', function(card) {
            setTimeout(function() {
                toastr.warning('Before collapse event fired!');
            }, 2000);
        });

        card.on('beforeExpand', function(card) {
            setTimeout(function() {
                toastr.info('Before expand event fired!');
            }, 100);
        });

        card.on('afterExpand', function(card) {
            setTimeout(function() {
                toastr.warning('After expand event fired!');
            }, 2000);
        });

        // Remove event handlers
        card.on('beforeRemove', function(card) {
            toastr.info('Before remove event fired!');

            return confirm('Are you sure to remove this card ?');  // remove card after user confirmation
        });

        card.on('afterRemove', function(card) {
            setTimeout(function() {
                toastr.warning('After remove event fired!');
            }, 2000);
        });

        // Reload event handlers
        card.on('reload', function(card) {
            toastr.info('Leload event fired!');

            KTApp.block(card.getSelf(), {
                overlayColor: '#000000',
                type: 'spinner',
                state: 'primary',
                opacity: 0.05,
                size: 'lg'
            });

            // update the content here

            setTimeout(function() {
                KTApp.unblock(card.getSelf());
            }, 2000);
        });
    }

    // Demo 3
    var demo3 = function() {
        // This card is lazy initialized using data-card="true" attribute. You can access to the card object as shown below and override its behavior
        var card = new KTCard('kt_card_3');

        // Toggle event handlers
        card.on('beforeCollapse', function(card) {
            setTimeout(function() {
                toastr.info('Before collapse event fired!');
            }, 100);
        });

        card.on('afterCollapse', function(card) {
            setTimeout(function() {
                toastr.warning('Before collapse event fired!');
            }, 2000);
        });

        card.on('beforeExpand', function(card) {
            setTimeout(function() {
                toastr.info('Before expand event fired!');
            }, 100);
        });

        card.on('afterExpand', function(card) {
            setTimeout(function() {
                toastr.warning('After expand event fired!');
            }, 2000);
        });

        // Remove event handlers
        card.on('beforeRemove', function(card) {
            toastr.info('Before remove event fired!');

            return confirm('Are you sure to remove this card ?');  // remove card after user confirmation
        });

        card.on('afterRemove', function(card) {
            setTimeout(function() {
                toastr.warning('After remove event fired!');
            }, 2000);
        });

        // Reload event handlers
        card.on('reload', function(card) {
            toastr.info('Leload event fired!');

            KTApp.block(card.getSelf(), {
                type: 'loader',
                state: 'success',
                message: 'Please wait...'
            });

            // update the content here

            setTimeout(function() {
                KTApp.unblock(card.getSelf());
            }, 2000);
        });
    }

    // Demo 4
    var demo4 = function() {
        // This card is lazy initialized using data-card="true" attribute. You can access to the card object as shown below and override its behavior
        var card = new KTCard('kt_card_4');

        // Toggle event handlers
        card.on('beforeCollapse', function(card) {
            setTimeout(function() {
                toastr.info('Before collapse event fired!');
            }, 100);
        });

        card.on('afterCollapse', function(card) {
            setTimeout(function() {
                toastr.warning('Before collapse event fired!');
            }, 2000);
        });

        card.on('beforeExpand', function(card) {
            setTimeout(function() {
                toastr.info('Before expand event fired!');
            }, 100);
        });

        card.on('afterExpand', function(card) {
            setTimeout(function() {
                toastr.warning('After expand event fired!');
            }, 2000);
        });

        // Remove event handlers
        card.on('beforeRemove', function(card) {
            toastr.info('Before remove event fired!');

            return confirm('Are you sure to remove this card ?');  // remove card after user confirmation
        });

        card.on('afterRemove', function(card) {
            setTimeout(function() {
                toastr.warning('After remove event fired!');
            }, 2000);
        });

        // Reload event handlers
        card.on('reload', function(card) {
            toastr.info('Leload event fired!');

            KTApp.block(card.getSelf(), {
                type: 'loader',
                state: 'primary',
                message: 'Please wait...'
            });

            // update the content here

            setTimeout(function() {
                KTApp.unblock(card.getSelf());
            }, 2000);
        });
    }

    return {
        //main function to initiate the module
        init: function () {
            initToastr();

            // init demos
            demo1();
            demo2();
            demo3();
            demo4();
        }
    };
}();

jQuery(document).ready(function() {
    KTCardTools.init();
});
