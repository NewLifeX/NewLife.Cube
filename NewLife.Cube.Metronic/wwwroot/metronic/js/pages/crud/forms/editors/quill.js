// Class definition
var KTQuilDemos = function() {

    // Private functions
    var demo1 = function() {
        var quill = new Quill('#kt_quil_1', {
            modules: {
                toolbar: [
                    [{
                        header: [1, 2, false]
                    }],
                    ['bold', 'italic', 'underline'],
                    ['image', 'code-block']
                ]
            },
            placeholder: 'Type your text here...',
            theme: 'snow' // or 'bubble'
        });
    }

    var demo2 = function() {
        var Delta = Quill.import('delta');
        var quill = new Quill('#kt_quil_2', {
            modules: {
                toolbar: true
            },
            placeholder: 'Type your text here...',
            theme: 'snow'
        });

        // Store accumulated changes
        var change = new Delta();
        quill.on('text-change', function(delta) {
            change = change.compose(delta);
        });

        // Save periodically
        setInterval(function() {
            if (change.length() > 0) {
                console.log('Saving changes', change);
                /*
                Send partial changes
                $.post('/your-endpoint', {
                  partial: JSON.stringify(change)
                });

                Send entire document
                $.post('/your-endpoint', {
                  doc: JSON.stringify(quill.getContents())
                });
                */
                change = new Delta();
            }
        }, 5 * 1000);

        // Check for unsaved data
        window.onbeforeunload = function() {
            if (change.length() > 0) {
                return 'There are unsaved changes. Are you sure you want to leave?';
            }
        }
    }

    return {
        // public functions
        init: function() {
            demo1();
            demo2();
        }
    };
}();

jQuery(document).ready(function() {
    KTQuilDemos.init();
});
