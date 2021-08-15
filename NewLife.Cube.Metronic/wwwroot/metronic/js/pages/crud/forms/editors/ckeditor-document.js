"use strict";
// Class definition

var KTCkeditorDocument = function () {    
    // Private functions
    var demos = function () {
        DecoupledEditor
            .create( document.querySelector( '#kt-ckeditor-1' ) )
            .then( editor => {
                const toolbarContainer = document.querySelector( '#kt-ckeditor-1-toolbar' );

                toolbarContainer.appendChild( editor.ui.view.toolbar.element );
            } )
            .catch( error => {
                console.error( error );
            } );

        DecoupledEditor
            .create( document.querySelector( '#kt-ckeditor-2' ) )
            .then( editor => {
                const toolbarContainer = document.querySelector( '#kt-ckeditor-2-toolbar' );

                toolbarContainer.appendChild( editor.ui.view.toolbar.element );
            } )
            .catch( error => {
                console.error( error );
            } );

        DecoupledEditor
            .create( document.querySelector( '#kt-ckeditor-3' ) )
            .then( editor => {
                const toolbarContainer = document.querySelector( '#kt-ckeditor-3-toolbar' );

                toolbarContainer.appendChild( editor.ui.view.toolbar.element );
            } )
            .catch( error => {
                console.error( error );
            } );
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
    KTCkeditorDocument.init();
});