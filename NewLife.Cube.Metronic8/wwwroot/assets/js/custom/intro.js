"use strict";

// Class definition
var KTIntro = function () {
    // Private functions
    var handleIntro = function(id, calback, timeout) {
        var date = new Date();

        var currentTime = date.getTime();

        var time2days = 1000 * 60 * 60 * 24 * 2; // 2 days
        var time7days = 1000 * 60 * 60 * 24 * 7; // 7 days
        var time15days = 1000 * 60 * 60 * 24 * 15; // 15 days
        var time21days = 1000 * 60 * 60 * 24 * 21; // 21 days
        var time30days = 1000 * 60 * 60 * 24 * 30; // 30 days        

        if (!KTCookie.get(id + '_counter') || parseInt(KTCookie.get(id + '_counter')) < 3) {
            // Initial display
            if (!KTCookie.get(id + '_counter')) {
                setTimeout(calback, timeout); // Display intro in 5 seconds
                
                KTCookie.set(id + '_show_1', '1', {expires: new Date(date.getTime() + time2days)});

                KTCookie.set(id + '_counter', '1', {expires: new Date(date.getTime() + time30days)});

                return true;
            } 

            // 2 display
            if (KTCookie.get(id + '_counter') == '1' && !KTCookie.get(id + '_show_1')) {
                setTimeout(calback, timeout); // Display intro in 5 seconds
                
                KTCookie.set(id + '_show_2', '1', {expires: new Date(date.getTime() + time7days)});
                
                KTCookie.set(id + '_counter', '2', {expires: new Date(date.getTime() + time21days)});

                return true;
            }

            // 3 display
            if (KTCookie.get(id + '_counter') == '2' && !KTCookie.get(id + '_show_2')) {
                setTimeout(calback, timeout); // Display intro in 5 seconds
                
                KTCookie.set(id + '_show_3', '1', {expires: new Date(date.getTime() + time15days)});
                
                KTCookie.set(id + '_counter', '3', {expires: new Date(date.getTime() + time15days)});

                return true;
            }

            return false;
        }
    }

    var showtIntro1 = function() {
        var element = document.querySelector('#kt_header_search_toggle');

        if (!element) {
            return;
        }

        var options = {
            customClass: 'popover-dark',
            container: 'body',
            trigger: 'manual',
            boundary: 'window',
            placement: 'left',
            dismiss: true,
            html: true,
            title: 'Quick Search',
            content: 'Fully functional search with advance options and preferences setup'
        }

        // Initialize popover
        var popover = KTApp.initBootstrapPopover(element, options);
        popover.show();

        // Auto remove
        setTimeout(function() {
            if (popover) {
                popover.dispose();
            } 
        }, 1000 * 10); // 10 seconds

        // Hide popover on element click
        element.addEventListener("click", function(e) {
            popover.dispose();
        });
    }

    var showtIntro2 = function() {
        var element = document.querySelector('#kt_toolbar_primary_button');

        if (!element) {
            return;
        }

        var options = {
            customClass: 'popover-dark',
            container: 'body',
            boundary: 'window',
            trigger: 'manual',
            placement: 'left',
            dismiss: true,
            html: true,
            title: 'Quick Notifications',
            content: 'Seamless access to updates and notifications in various formats'
        }

        // Initialize popover
        var popover = KTApp.initBootstrapPopover(element, options);
        popover.show();

        // Remove
        setTimeout(function() {
            if (popover) {
                popover.dispose();
            } 
        }, 1000 * 10); // 10 seconds

        // Hide popover on element click
        element.addEventListener("click", function(e) {
            popover.dispose();
        });
    }

    var showtIntro3 = function() {
        var element = document.querySelector('#kt_header_user_menu_toggle');

        if (!element) {
            return;
        }

        var options = {
            customClass: 'popover-dark',
            container: 'body',
            boundary: 'window',
            placement: 'left',
            trigger: 'manual',
            dismiss: true,
            html: true,
            title: 'Advanced User Menu',
            content: 'With quick links to user profile and account settings pages'
        }

        // Initialize popover
        var popover = KTApp.initBootstrapPopover(element, options);
        popover.show();

        // Remove
        setTimeout(function() {
            if (popover) {
                popover.dispose();
            } 
        }, 1000 * 10); // 10 seconds

        // Hide popover on element click
        element.addEventListener("click", function(e) {
            popover.dispose();
        });
    }

    var initIntro = function(product) {
        // Handle intro popovers displays
        if (KTUtil.inIframe() === false) {                
            if (handleIntro('kt_' + product + '_intro_1', showtIntro1, 1000 * 5)) {
            } else if (handleIntro('kt_' + product + '_intro_2', showtIntro2, 1000 * 5)) {
            } else if (handleIntro('kt_' + product + '_intro_3', showtIntro3, 1000 * 5)) {
            }
        } 
    }

    // Public methods
    return {
        init: function () {
            initIntro('metronic');
        }   
    }
}();

// Webpack support
if (typeof module !== 'undefined') {
    module.exports = KTIntro;
}

// On document ready
KTUtil.onDOMContentLoaded(function() {
    KTIntro.init();
});
