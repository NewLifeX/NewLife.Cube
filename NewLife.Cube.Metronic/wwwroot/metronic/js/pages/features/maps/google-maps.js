"use strict";

// Class definition
var KTGoogleMapsDemo = function() {

    // Private functions

    var demo1 = function() {
        var map = new GMaps({
            div: '#kt_gmap_1',
            lat: -12.043333,
            lng: -77.028333
        });
    }

    var demo2 = function() {
        var map = new GMaps({
            div: '#kt_gmap_2',
            zoom: 16,
            lat: -12.043333,
            lng: -77.028333,
            click: function(e) {
                alert('click');
            },
            dragend: function(e) {
                alert('dragend');
            }
        });
    }

    var demo3 = function() {
        var map = new GMaps({
            div: '#kt_gmap_3',
            lat: -51.38739,
            lng: -6.187181,
        });
        map.addMarker({
            lat: -51.38739,
            lng: -6.187181,
            title: 'Lima',
            details: {
                database_id: 42,
                author: 'HPNeo'
            },
            click: function(e) {
                if (console.log) console.log(e);
                alert('You clicked in this marker');
            }
        });
        map.addMarker({
            lat: -12.042,
            lng: -77.028333,
            title: 'Marker with InfoWindow',
            infoWindow: {
                content: '<span style="color:#000">HTML Content!</span>'
            }
        });
        map.setZoom(5);
    }

    var demo4 = function() {
        var map = new GMaps({
            div: '#kt_gmap_4',
            lat: -12.043333,
            lng: -77.028333
        });

        GMaps.geolocate({
            success: function(position) {
                map.setCenter(position.coords.latitude, position.coords.longitude);
            },
            error: function(error) {
                alert('Geolocation failed: ' + error.message);
            },
            not_supported: function() {
                alert("Your browser does not support geolocation");
            },
            always: function() {
                //alert("Geolocation Done!");
            }
        });
    }

    var demo5 = function() {
        var map = new GMaps({
            div: '#kt_gmap_5',
            lat: -12.043333,
            lng: -77.028333,
            click: function(e) {
                console.log(e);
            }
        });

        var path = [
            [-12.044012922866312, -77.02470665341184],
            [-12.05449279282314, -77.03024273281858],
            [-12.055122327623378, -77.03039293652341],
            [-12.075917129727586, -77.02764635449216],
            [-12.07635776902266, -77.02792530422971],
            [-12.076819390363665, -77.02893381481931],
            [-12.088527520066453, -77.0241058385925],
            [-12.090814532191756, -77.02271108990476]
        ];

        map.drawPolyline({
            path: path,
            strokeColor: '#131540',
            strokeOpacity: 0.6,
            strokeWeight: 6
        });
    }

    var demo6 = function() {
        var map = new GMaps({
            div: '#kt_gmap_6',
            lat: -12.043333,
            lng: -77.028333
        });

        var path = [
            [-12.040397656836609, -77.03373871559225],
            [-12.040248585302038, -77.03993927003302],
            [-12.050047116528843, -77.02448169303511],
            [-12.044804866577001, -77.02154422636042]
        ];

        var polygon = map.drawPolygon({
            paths: path,
            strokeColor: '#BBD8E9',
            strokeOpacity: 1,
            strokeWeight: 3,
            fillColor: '#BBD8E9',
            fillOpacity: 0.6
        });
    }

    var demo7 = function() {
        var map = new GMaps({
            div: '#kt_gmap_7',
            lat: -12.043333,
            lng: -77.028333
        });
        $('#kt_gmap_7_btn').click(function(e) {
            e.preventDefault();
            KTUtil.scrollTo('kt_gmap_7_btn', 400);
            map.travelRoute({
                origin: [-12.044012922866312, -77.02470665341184],
                destination: [-12.090814532191756, -77.02271108990476],
                travelMode: 'driving',
                step: function(e) {
                    $('#kt_gmap_7_routes').append('<li>' + e.instructions + '</li>');
                    $('#kt_gmap_7_routes li:eq(' + e.step_number + ')').delay(800 * e.step_number).fadeIn(500, function() {
                        map.setCenter(e.end_location.lat(), e.end_location.lng());
                        map.drawPolyline({
                            path: e.path,
                            strokeColor: '#131540',
                            strokeOpacity: 0.6,
                            strokeWeight: 6
                        });
                    });
                }
            });
        });
    }

    var demo8 = function() {
        var map = new GMaps({
            div: '#kt_gmap_8',
            lat: -12.043333,
            lng: -77.028333
        });

        var handleAction = function() {
            var text = $.trim($('#kt_gmap_8_address').val());
            GMaps.geocode({
                address: text,
                callback: function(results, status) {
                    if (status == 'OK') {
                        var latlng = results[0].geometry.location;
                        map.setCenter(latlng.lat(), latlng.lng());
                        map.addMarker({
                            lat: latlng.lat(),
                            lng: latlng.lng()
                        });
                        KTUtil.scrollTo('kt_gmap_8');
                    }
                }
            });
        }

        $('#kt_gmap_8_btn').click(function(e) {
            e.preventDefault();
            handleAction();
        });

        $("#kt_gmap_8_address").keypress(function(e) {
            var keycode = (e.keyCode ? e.keyCode : e.which);
            if (keycode == '13') {
                e.preventDefault();
                handleAction();
            }
        });
    }

    return {
        // public functions
        init: function() {
            // default charts
            demo1();
            demo2();
            demo3();
            demo4();
            demo5();
            demo6();
            demo7();
            demo8();
        }
    };
}();

jQuery(document).ready(function() {
    KTGoogleMapsDemo.init();
});