"use strict";

// Class Definition
var KTSignupComingSoon = function() {
    // Elements
    var form;
    var submitButton;
	var validator; 

    var counterDays;
    var counterHours;
    var counterMinutes;
    var counterSeconds;

    var handleForm = function(e) {
        var validation;		 

        if( !form ) {
            return;
        }        

        // Init form validation rules. For more info check the FormValidation plugin's official documentation:https://formvalidation.io/
        validator = FormValidation.formValidation(
			form,
			{
				fields: {					
					'email': {
                        validators: {
                            regexp: {
                                regexp: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                                message: 'The value is not a valid email address',
                            },
							notEmpty: {
								message: 'Email address is required'
							}
						}
					} 
				},
				plugins: {
					trigger: new FormValidation.plugins.Trigger(),
					bootstrap: new FormValidation.plugins.Bootstrap5({
                        rowSelector: '.fv-row',
                        eleInvalidClass: '',
                        eleValidClass: ''
                    })
				}
			}
		);		

        submitButton.addEventListener('click', function (e) {
            e.preventDefault();

            // Validate form
            validator.validate().then(function (status) {
                if (status == 'Valid') {
                    // Show loading indication
                    submitButton.setAttribute('data-kt-indicator', 'on');

                    // Disable button to avoid multiple click 
                    submitButton.disabled = true;

                    // Simulate ajax request
                    setTimeout(function() {
                        // Hide loading indication
                        submitButton.removeAttribute('data-kt-indicator');

                        // Enable button
                        submitButton.disabled = false;

                        // Show message popup. For more info check the plugin's official documentation: https://sweetalert2.github.io/
                        Swal.fire({
                            text: "We have received your request. You will be notified once we go live.",
                            icon: "success",
                            buttonsStyling: false,
                            confirmButtonText: "Ok, got it!",
                            customClass: {
                                confirmButton: "btn btn-primary"
                            }
                        }).then(function (result) {
                            if (result.isConfirmed) { 
                                form.querySelector('[name="email"]').value= "";                            
                                //form.submit();
                            }
                        });
                    }, 2000);   						
                } else {
                    // Show error popup. For more info check the plugin's official documentation: https://sweetalert2.github.io/
                    Swal.fire({
                        text: "Sorry, looks like there are some errors detected, please try again.",
                        icon: "error",
                        buttonsStyling: false,
                        confirmButtonText: "Ok, got it!",
                        customClass: {
                            confirmButton: "btn btn-primary"
                        }
                    });
                }
            });
		});
    }

    var initCounter = function() {
        // Set the date we're counting down to
        var currentTime = new Date(); 
        var countDownDate = new Date(currentTime.getTime() + 1000 * 60 * 60 * 24 * 15 + 1000 * 60 * 60 * 10 + 1000 * 60 * 15).getTime();

        var count = function() {
            // Get todays date and time
            var now = new Date().getTime();

            // Find the distance between now an the count down date
            var distance = countDownDate - now;

            // Time calculations for days, hours, minutes and seconds
            var days = Math.floor(distance / (1000 * 60 * 60 * 24));
            var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
            var seconds = Math.floor((distance % (1000 * 60)) / 1000);

            // Display the result
            if(counterDays) counterDays.innerHTML = days; 
            if(counterHours) counterHours.innerHTML = hours;
            if(counterMinutes) counterMinutes.innerHTML = minutes;
            if(counterSeconds) counterSeconds.innerHTML = seconds;
        };

        // Update the count down every 1 second
        var x = setInterval(count, 1000);

        // Initial count
        count();
    }

    // Public Functions
    return {
        // public functions
        init: function() {
            form = document.querySelector('#kt_coming_soon_form');
            submitButton = document.querySelector('#kt_coming_soon_submit');
           
            handleForm();

            // Uncomment to active coming soon counter
            //counterDays = document.querySelector('#kt_coming_soon_counter_days');
            //counterHours = document.querySelector('#kt_coming_soon_counter_hours');
            //counterMinutes = document.querySelector('#kt_coming_soon_counter_minutes');
            //counterSeconds = document.querySelector('#kt_coming_soon_counter_seconds');
            //initCounter();
        }
    };
}();

// On document ready
KTUtil.onDOMContentLoaded(function() {
    KTSignupComingSoon.init();
});
