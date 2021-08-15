"use strict";

// Class definition
var KTAppInbox = function() {
    // Private properties
    var _asideEl;
    var _listEl;
    var _viewEl;
    var _composeEl;
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

    var _initForm = function(formEl) {
        var formEl = KTUtil.getById(formEl);

        // Init autocompletes
        var toEl = KTUtil.find(formEl, '[name=compose_to]');
        var tagifyTo = new Tagify(toEl, {
            delimiters: ", ", // add new tags when a comma or a space character is entered
            maxTags: 10,
            blacklist: ["fuck", "shit", "pussy"],
            keepInvalidTags: true, // do not remove invalid tags (but keep them marked as invalid)
            whitelist: [{
                value: 'Chris Muller',
                email: 'chris.muller@wix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_11.jpg',
                class: 'tagify__tag--primary'
            }, {
                value: 'Nick Bold',
                email: 'nick.seo@gmail.com',
                initials: 'SS',
                initialsState: 'warning',
                pic: ''
            }, {
                value: 'Alon Silko',
                email: 'alon@keenthemes.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_6.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_8.jpg'
            }, {
                value: 'Sara Loran',
                email: 'sara.loran@tilda.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_9.jpg'
            }, {
                value: 'Eric Davok',
                email: 'davok@mix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Lina Nilson',
                email: 'lina.nilson@loop.com',
                initials: 'LN',
                initialsState: 'danger',
                pic: './assets/media/users/100_15.jpg'
            }],
            templates: {
                dropdownItem: function(tagData) {
                    try {
                        var html = '';

                        html += '<div class="tagify__dropdown__item">';
                        html += '   <div class="d-flex align-items-center">';
                        html += '       <span class="symbol sumbol-' + (tagData.initialsState ? tagData.initialsState : '') + ' mr-2">';
                        html += '           <span class="symbol-label" style="background-image: url(\''+ (tagData.pic ? tagData.pic : '') + '\')">' + (tagData.initials ? tagData.initials : '') + '</span>';
                        html += '       </span>';
                        html += '       <div class="d-flex flex-column">';
                        html += '           <a href="#" class="text-dark-75 text-hover-primary font-weight-bold">'+ (tagData.value ? tagData.value : '') + '</a>';
                        html += '           <span class="text-muted font-weight-bold">' + (tagData.email ? tagData.email : '') + '</span>';
                        html += '       </div>';
                        html += '   </div>';
                        html += '</div>';

                        return html;
                    } catch (err) {}
                }
            },
            transformTag: function(tagData) {
                tagData.class = 'tagify__tag tagify__tag--primary';
            },
            dropdown: {
                classname: "color-blue",
                enabled: 1,
                maxItems: 5
            }
        });

        var ccEl = KTUtil.find(formEl, '[name=compose_cc]');
        var tagifyCc = new Tagify(ccEl, {
            delimiters: ", ", // add new tags when a comma or a space character is entered
            maxTags: 10,
            blacklist: ["fuck", "shit", "pussy"],
            keepInvalidTags: true, // do not remove invalid tags (but keep them marked as invalid)
            whitelist: [{
                value: 'Chris Muller',
                email: 'chris.muller@wix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_11.jpg',
                class: 'tagify__tag--primary'
            }, {
                value: 'Nick Bold',
                email: 'nick.seo@gmail.com',
                initials: 'SS',
                initialsState: 'warning',
                pic: ''
            }, {
                value: 'Alon Silko',
                email: 'alon@keenthemes.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_6.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_8.jpg'
            }, {
                value: 'Sara Loran',
                email: 'sara.loran@tilda.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_9.jpg'
            }, {
                value: 'Eric Davok',
                email: 'davok@mix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Lina Nilson',
                email: 'lina.nilson@loop.com',
                initials: 'LN',
                initialsState: 'danger',
                pic: './assets/media/users/100_15.jpg'
            }],
            templates: {
                dropdownItem: function(tagData) {
                    try {
                        var html = '';

                        html += '<div class="tagify__dropdown__item">';
                        html += '   <div class="d-flex align-items-center">';
                        html += '       <span class="symbol sumbol-' + (tagData.initialsState ? tagData.initialsState : '') + ' mr-2" style="background-image: url(\''+ (tagData.pic ? tagData.pic : '') + '\')">';
                        html += '           <span class="symbol-label">' + (tagData.initials ? tagData.initials : '') + '</span>';
                        html += '       </span>';
                        html += '       <div class="d-flex flex-column">';
                        html += '           <a href="#" class="text-dark-75 text-hover-primary font-weight-bold">'+ (tagData.value ? tagData.value : '') + '</a>';
                        html += '           <span class="text-muted font-weight-bold">' + (tagData.email ? tagData.email : '') + '</span>';
                        html += '       </div>';
                        html += '   </div>';
                        html += '</div>';

                        return html;
                    } catch (err) {}
                }
            },
            transformTag: function(tagData) {
                tagData.class = 'tagify__tag tagify__tag--primary';
            },
            dropdown: {
                classname: "color-blue",
                enabled: 1,
                maxItems: 5
            }
        });

        var bccEl = KTUtil.find(formEl, '[name=compose_bcc]');
        var tagifyBcc = new Tagify(bccEl, {
            delimiters: ", ", // add new tags when a comma or a space character is entered
            maxTags: 10,
            blacklist: ["fuck", "shit", "pussy"],
            keepInvalidTags: true, // do not remove invalid tags (but keep them marked as invalid)
            whitelist: [{
                value: 'Chris Muller',
                email: 'chris.muller@wix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_11.jpg',
                class: 'tagify__tag--primary'
            }, {
                value: 'Nick Bold',
                email: 'nick.seo@gmail.com',
                initials: 'SS',
                initialsState: 'warning',
                pic: ''
            }, {
                value: 'Alon Silko',
                email: 'alon@keenthemes.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_6.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_8.jpg'
            }, {
                value: 'Sara Loran',
                email: 'sara.loran@tilda.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_9.jpg'
            }, {
                value: 'Eric Davok',
                email: 'davok@mix.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Sam Seanic',
                email: 'sam.senic@loop.com',
                initials: '',
                initialsState: '',
                pic: './assets/media/users/100_13.jpg'
            }, {
                value: 'Lina Nilson',
                email: 'lina.nilson@loop.com',
                initials: 'LN',
                initialsState: 'danger',
                pic: './assets/media/users/100_15.jpg'
            }],
            templates: {
                dropdownItem: function(tagData) {
                    try {
                        var html = '';

                        html += '<div class="tagify__dropdown__item">';
                        html += '   <div class="d-flex align-items-center">';
                        html += '       <span class="symbol sumbol-' + (tagData.initialsState ? tagData.initialsState : '') + ' mr-2" style="background-image: url(\''+ (tagData.pic ? tagData.pic : '') + '\')">';
                        html += '           <span class="symbol-label">' + (tagData.initials ? tagData.initials : '') + '</span>';
                        html += '       </span>';
                        html += '       <div class="d-flex flex-column">';
                        html += '           <a href="#" class="text-dark-75 text-hover-primary font-weight-bold">'+ (tagData.value ? tagData.value : '') + '</a>';
                        html += '           <span class="text-muted font-weight-bold">' + (tagData.email ? tagData.email : '') + '</span>';
                        html += '       </div>';
                        html += '   </div>';
                        html += '</div>';

                        return html;
                    } catch (err) {}
                }
            },
            transformTag: function(tagData) {
                tagData.class = 'tagify__tag tagify__tag--primary';
            },
            dropdown: {
                classname: "color-blue",
                enabled: 1,
                maxItems: 5
            }
        });

        // CC input show
        KTUtil.on(formEl, '[data-inbox="cc-show"]', 'click', function(e) {
            var inputEl = KTUtil.find(formEl, '.inbox-to-cc');
            KTUtil.removeClass(inputEl, 'd-none');
            KTUtil.addClass(inputEl, 'd-flex');
            KTUtil.find(formEl, "[name=compose_cc]").focus();
        });

        // CC input hide
        KTUtil.on(formEl, '[data-inbox="cc-hide"]', 'click', function(e) {
            var inputEl = KTUtil.find(formEl, '.inbox-to-cc');
            KTUtil.removeClass(inputEl, 'd-flex');
            KTUtil.addClass(inputEl, 'd-none');
        });

        // BCC input show
        KTUtil.on(formEl, '[data-inbox="bcc-show"]', 'click', function(e) {
            var inputEl = KTUtil.find(formEl, '.inbox-to-bcc');
            KTUtil.removeClass(inputEl, 'd-none');
            KTUtil.addClass(inputEl, 'd-flex');
            KTUtil.find(formEl, "[name=compose_bcc]").focus();
        });

        // BCC input hide
        KTUtil.on(formEl, '[data-inbox="bcc-hide"]', 'click', function(e) {
            var inputEl = KTUtil.find(formEl, '.inbox-to-bcc');
            KTUtil.removeClass(inputEl, 'd-flex');
            KTUtil.addClass(inputEl, 'd-none');
        });
    }

    var _initAttachments = function(elemId) {
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
            _asideEl = KTUtil.getById('kt_inbox_aside');
            _listEl = KTUtil.getById('kt_inbox_list');
            _viewEl = KTUtil.getById('kt_inbox_view');
            _composeEl = KTUtil.getById('kt_inbox_compose');
            _replyEl = KTUtil.getById('kt_inbox_reply');

            // Init handlers
            KTAppInbox.initAside();
            KTAppInbox.initList();
            KTAppInbox.initView();
            KTAppInbox.initReply();
            KTAppInbox.initCompose();
        },

        initAside: function() {
            // Mobile offcanvas for mobile mode
            _asideOffcanvas = new KTOffcanvas(_asideEl, {
                overlay: true,
                baseClass: 'offcanvas-mobile',
                //closeBy: 'kt_inbox_aside_close',
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
            // View message
            KTUtil.on(_listEl, '[data-inbox="message"]', 'click', function(e) {
                var actionsEl = KTUtil.find(this, '[data-inbox="actions"]');

                // skip actions click
                if (e.target === actionsEl || (actionsEl && actionsEl.contains(e.target) === true)) {
                    return false;
                }

                // Demo loading
                var loading = new KTDialog({
                    'type': 'loader',
                    'placement': 'top center',
                    'message': 'Loading ...'
                });
                loading.show();

                setTimeout(function() {
                    loading.hide();

                    KTUtil.addClass(_listEl, 'd-none');
                    KTUtil.removeClass(_listEl, 'd-block');

                    KTUtil.addClass(_viewEl, 'd-block');
                    KTUtil.removeClass(_viewEl, 'd-none');
                }, 700);
            });

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
            _initEditor(_replyEl, 'kt_inbox_reply_editor');
            _initAttachments('kt_inbox_reply_attachments');
            _initForm('kt_inbox_reply_form');
        },

        initCompose: function() {
            _initEditor(_composeEl, 'kt_inbox_compose_editor');
            _initAttachments('kt_inbox_compose_attachments');
            _initForm('kt_inbox_compose_form');

            // Remove reply form
            KTUtil.on(_composeEl, '[data-inbox="dismiss"]', 'click', function(e) {
                swal.fire({
                    text: "Are you sure to discard this message ?",
                    type: "danger",
                    buttonsStyling: false,
                    confirmButtonText: "Discard draft",
                    confirmButtonClass: "btn btn-danger",
                    showCancelButton: true,
                    cancelButtonText: "Cancel",
                    cancelButtonClass: "btn btn-light-primary"
                }).then(function(result) {
                    $(_composeEl).modal('hide');
                });
            });
        }
    };
}();

// Class Initialization
jQuery(document).ready(function() {
    KTAppInbox.init();
});
