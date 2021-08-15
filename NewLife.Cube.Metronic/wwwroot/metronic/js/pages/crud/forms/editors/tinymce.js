"use strict";
// Class definition

var KTTinymce = function () {    
    // Private functions
    var demos = function () {
        
        tinymce.init({
			selector: '#kt-tinymce-1',
            toolbar: false,
            statusbar: false
		});

		tinymce.init({
			selector: '#kt-tinymce-2'
        });
        
        tinymce.init({
            selector: '#kt-tinymce-3',
            toolbar: 'advlist | autolink | link image | lists charmap | print preview', 
            plugins : 'advlist autolink link image lists charmap print preview'
        });
        
        tinymce.init({
            selector: '#kt-tinymce-4',
            menubar: false,
            toolbar: ['styleselect fontselect fontsizeselect',
                'undo redo | cut copy paste | bold italic | link image | alignleft aligncenter alignright alignjustify',
                'bullist numlist | outdent indent | blockquote subscript superscript | advlist | autolink | lists charmap | print preview |  code'], 
            plugins : 'advlist autolink link image lists charmap print preview code'
        });       
    }

    return {
        // public functions
        init: function() {
            demos(); 
        }
    };
}();

// Initialization
jQuery(document).ready(function() {
    KTTinymce.init();
});