"use strict";

// Class definition
var KTAppChat = function () {
	var _chatAsideEl;
	var _chatAsideOffcanvasObj;
	var _chatContentEl;

	// Private functions
	var _initAside = function () {
		// Mobile offcanvas for mobile mode
		_chatAsideOffcanvasObj = new KTOffcanvas(_chatAsideEl, {
			overlay: true,
            baseClass: 'offcanvas-mobile',
            //closeBy: 'kt_chat_aside_close',
            toggleBy: 'kt_app_chat_toggle'
        });

		// User listing
		var cardScrollEl = KTUtil.find(_chatAsideEl, '.scroll');
		var cardBodyEl = KTUtil.find(_chatAsideEl, '.card-body');
		var searchEl = KTUtil.find(_chatAsideEl, '.input-group');

		if (cardScrollEl) {
			// Initialize perfect scrollbar(see:  https://github.com/utatti/perfect-scrollbar)
			KTUtil.scrollInit(cardScrollEl, {
				mobileNativeScroll: true,  // Enable native scroll for mobile
				desktopNativeScroll: false, // Disable native scroll and use custom scroll for desktop
				resetHeightOnDestroy: true,  // Reset css height on scroll feature destroyed
				handleWindowResize: true, // Recalculate hight on window resize
				rememberPosition: true, // Remember scroll position in cookie
				height: function() {  // Calculate height
					var height;

					if (KTUtil.isBreakpointUp('lg')) {
						height = KTLayoutContent.getHeight();
					} else {
						height = KTUtil.getViewPort().height;
					}

					if (_chatAsideEl) {
						height = height - parseInt(KTUtil.css(_chatAsideEl, 'margin-top')) - parseInt(KTUtil.css(_chatAsideEl, 'margin-bottom'));
						height = height - parseInt(KTUtil.css(_chatAsideEl, 'padding-top')) - parseInt(KTUtil.css(_chatAsideEl, 'padding-bottom'));
					}

					if (cardScrollEl) {
						height = height - parseInt(KTUtil.css(cardScrollEl, 'margin-top')) - parseInt(KTUtil.css(cardScrollEl, 'margin-bottom'));
					}

					if (cardBodyEl) {
						height = height - parseInt(KTUtil.css(cardBodyEl, 'padding-top')) - parseInt(KTUtil.css(cardBodyEl, 'padding-bottom'));
					}

					if (searchEl) {
						height = height - parseInt(KTUtil.css(searchEl, 'height'));
						height = height - parseInt(KTUtil.css(searchEl, 'margin-top')) - parseInt(KTUtil.css(searchEl, 'margin-bottom'));
					}

					// Remove additional space
					height = height - 2;

					return height;
				}
			});
		}
	}

	return {
		// Public functions
		init: function() {
			// Elements
			_chatAsideEl = KTUtil.getById('kt_chat_aside');
			_chatContentEl = KTUtil.getById('kt_chat_content');

			// Init aside and user list
			_initAside();

			// Init inline chat example
			KTLayoutChat.setup(KTUtil.getById('kt_chat_content'));

			// Trigger click to show popup modal chat on page load
			if (KTUtil.getById('kt_app_chat_toggle')) {
				setTimeout(function() {
					KTUtil.getById('kt_app_chat_toggle').click();
				}, 1000);
			}
		}
	};
}();

jQuery(document).ready(function() {
	KTAppChat.init();
});
