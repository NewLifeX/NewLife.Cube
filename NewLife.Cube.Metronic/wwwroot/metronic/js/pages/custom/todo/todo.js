"use strict";

// Class definition
var KTAppTodo = function() {
    // Private properties
    var _asideEl;
    var _listEl;
    var _viewEl;
    var _replyEl;
    var _asideOffcanvas;

    // Private methods
    var _initEditor = function(form, editor) {
        // init editor
        var options = {
            modules: {
                toolbar: {}
            },
            placeholder: 'Type message...',
            theme: 'snow'
        };

        if (!KTUtil.getById(editor)) {
            return;
        }

        // Init editor
        var editor = new Quill('#' + editor, options);

        // Customize editor
        var toolbar = KTUtil.find(form, '.ql-toolbar');
        var editor = KTUtil.find(form, '.ql-editor');

        if (toolbar) {
            KTUtil.addClass(toolbar, 'px-5 border-top-0 border-left-0 border-right-0');
        }

        if (editor) {
            KTUtil.addClass(editor, 'px-8');
        }
    }

    var _initAttachments = function(elemId) {
        if (!KTUtil.getById(elemId)) {
            return;
        }

        var id = "#" + elemId;
        var previewNode = $(id + " .dropzone-item");
        previewNode.id = "";
        var previewTemplate = previewNode.parent('.dropzone-items').html();
        previewNode.remove();

        var myDropzone = new Dropzone(id, { // Make the whole body a dropzone
            url: "https://keenthemes.com/scripts/void.php", // Set the url for your upload script location
            parallelUploads: 20,
            maxFilesize: 1, // Max filesize in MB
            previewTemplate: previewTemplate,
            previewsContainer: id + " .dropzone-items", // Define the container to display the previews
            clickable: id + "_select" // Define the element that should be used as click trigger to select files.
        });

        myDropzone.on("addedfile", function(file) {
            // Hookup the start button
            $(document).find(id + ' .dropzone-item').css('display', '');
        });

        // Update the total progress bar
        myDropzone.on("totaluploadprogress", function(progress) {
            document.querySelector(id + " .progress-bar").style.width = progress + "%";
        });

        myDropzone.on("sending", function(file) {
            // Show the total progress bar when upload starts
            document.querySelector(id + " .progress-bar").style.opacity = "1";
        });

        // Hide the total progress bar when nothing's uploading anymore
        myDropzone.on("complete", function(progress) {
            var thisProgressBar = id + " .dz-complete";
            setTimeout(function() {
                $(thisProgressBar + " .progress-bar, " + thisProgressBar + " .progress").css('opacity', '0');
            }, 300)
        });
    }

    // Public methods
    return {
        // Public functions
        init: function() {
            // Init variables
            _asideEl = KTUtil.getById('kt_todo_aside');
            _listEl = KTUtil.getById('kt_todo_list');
            _viewEl = KTUtil.getById('kt_todo_view');
            _replyEl = KTUtil.getById('kt_todo_reply');

            // Init handlers
            KTAppTodo.initAside();
            KTAppTodo.initList();
            KTAppTodo.initView();
            KTAppTodo.initReply();
        },

        initAside: function() {
            // Mobile offcanvas for mobile mode
            _asideOffcanvas = new KTOffcanvas(_asideEl, {
                overlay: true,
                baseClass: 'offcanvas-mobile',
                //closeBy: 'kt_todo_aside_close',
                toggleBy: 'kt_subheader_mobile_toggle'
            });

            // View list
            KTUtil.on(_asideEl, '.list-item[data-action="list"]', 'click', function(e) {
                var type = KTUtil.attr(this, 'data-type');
                var listItemsEl = KTUtil.find(_listEl, '.kt-inbox__items');
                var navItemEl = this.closest('.kt-nav__item');
                var navItemActiveEl = KTUtil.find(_asideEl, '.kt-nav__item.kt-nav__item--active');

                // demo loading
                var loading = new KTDialog({
                    'type': 'loader',
                    'placement': 'top center',
                    'message': 'Loading ...'
                });
                loading.show();

                setTimeout(function() {
                    loading.hide();

                    KTUtil.css(_listEl, 'display', 'flex'); // show list
                    KTUtil.css(_viewEl, 'display', 'none'); // hide view

                    KTUtil.addClass(navItemEl, 'kt-nav__item--active');
                    KTUtil.removeClass(navItemActiveEl, 'kt-nav__item--active');

                    KTUtil.attr(listItemsEl, 'data-type', type);
                }, 600);
            });
        },

        initList: function() {
            // Group selection
            KTUtil.on(_listEl, '[data-inbox="group-select"] input', 'click', function() {
                var messages = KTUtil.findAll(_listEl, '[data-inbox="message"]');

                for (var i = 0, j = messages.length; i < j; i++) {
                    var message = messages[i];
                    var checkbox = KTUtil.find(message, '.checkbox input');
                    checkbox.checked = this.checked;

                    if (this.checked) {
                        KTUtil.addClass(message, 'active');
                    } else {
                        KTUtil.removeClass(message, 'active');
                    }
                }
            });

            // Individual selection
            KTUtil.on(_listEl, '[data-inbox="message"] [data-inbox="actions"] .checkbox input', 'click', function() {
                var item = this.closest('[data-inbox="message"]');

                if (item && this.checked) {
                    KTUtil.addClass(item, 'active');
                } else {
                    KTUtil.removeClass(item, 'active');
                }
            });
        },

        initView: function() {
            // Back to listing
            KTUtil.on(_viewEl, '[data-inbox="back"]', 'click', function() {
                // demo loading
                var loading = new KTDialog({
                    'type': 'loader',
                    'placement': 'top center',
                    'message': 'Loading ...'
                });

                loading.show();

                setTimeout(function() {
                    loading.hide();

                    KTUtil.addClass(_listEl, 'd-block');
                    KTUtil.removeClass(_listEl, 'd-none');

                    KTUtil.addClass(_viewEl, 'd-none');
                    KTUtil.removeClass(_viewEl, 'd-block');
                }, 700);
            });

            // Expand/Collapse reply
            KTUtil.on(_viewEl, '[data-inbox="message"]', 'click', function(e) {
                var message = this.closest('[data-inbox="message"]');

                var dropdownToggleEl = KTUtil.find(this, '[data-toggle="dropdown"]');
                var toolbarEl = KTUtil.find(this, '[data-inbox="toolbar"]');

                // skip dropdown toggle click
                if (e.target === dropdownToggleEl || (dropdownToggleEl && dropdownToggleEl.contains(e.target) === true)) {
                    return false;
                }

                // skip group actions click
                if (e.target === toolbarEl || (toolbarEl && toolbarEl.contains(e.target) === true)) {
                    return false;
                }

                if (KTUtil.hasClass(message, 'toggle-on')) {
                    KTUtil.addClass(message, 'toggle-off');
                    KTUtil.removeClass(message, 'toggle-on');
                } else {
                    KTUtil.removeClass(message, 'toggle-off');
                    KTUtil.addClass(message, 'toggle-on');
                }
            });
        },

        initReply: function() {
            _initEditor(_replyEl, 'kt_todo_reply_editor');
            _initAttachments('kt_todo_reply_attachments');
        }
    };
}();

// Class Initialization
jQuery(document).ready(function() {
    KTAppTodo.init();
});
