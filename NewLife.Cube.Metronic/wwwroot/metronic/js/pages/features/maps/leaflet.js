"use strict";

// Class definition
var KTLeaflet = function () {

	// Private functions
	var demo1 = function () {
		// define leaflet
		var leaflet = L.map('kt_leaflet_1', {
			center: [-37.8179793, 144.9671293],
			zoom: 11
		});

		// set leaflet tile layer
		L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
			attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
		}).addTo(leaflet);

		// set custom SVG icon marker
		var leafletIcon = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// bind marker with popup
		var marker = L.marker([-37.8179793, 144.9671293], { icon: leafletIcon }).addTo(leaflet);
		marker.bindPopup("<b>Flinder's Station</b><br/>Melbourne, Victoria").openPopup();
	}

	var demo2 = function () {
		// define leaflet
		var leaflet = L.map('kt_leaflet_2', {
			center: [51.5073219, -0.1276474],
			zoom: 11
		})

		// set leaflet tile layer
		L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
			attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
		}).addTo(leaflet);

		// set custom SVG icon marker
		var leafletIcon = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// bind marker with popup
		var marker = L.marker([51.5073219, -0.1276474], { icon: leafletIcon }).addTo(leaflet);
		marker.bindPopup("<b>City of London</b>").openPopup();

		// set circle polygon
		var circle = L.circle([51.5073219, -0.1276474], {
			color: '#4A7DFF',
			fillColor: '#6993FF',
			fillOpacity: 0.5,
			radius: 700
		}).addTo(leaflet);
	}

	var demo3 = function () {
		// define leaflet
		var leaflet = L.map('kt_leaflet_3', {
			center: [47.339, 11.602],
			zoom: 3
		})

		// set leaflet tile layer
		L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
			attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
		}).addTo(leaflet);


		// set custom SVG icon marker
		var leafletIcon1 = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		var leafletIcon2 = L.divIcon({
			html: `<span class="svg-icon svg-icon-primary svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		var leafletIcon3 = L.divIcon({
			html: `<span class="svg-icon svg-icon-warning svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		var leafletIcon4 = L.divIcon({
			html: `<span class="svg-icon svg-icon-success svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// bind markers with popup
		var marker1 = L.marker([39.3262345, -4.8380649], { icon: leafletIcon1 }).addTo(leaflet);
		var marker2 = L.marker([41.804, 13.843], { icon: leafletIcon2 }).addTo(leaflet);
		var marker3 = L.marker([51.11, 10.371], { icon: leafletIcon3 }).addTo(leaflet);
		var marker4 = L.marker([46.74, 2.417], { icon: leafletIcon4 }).addTo(leaflet);

		marker1.bindPopup("Spain", { closeButton: false });
		marker2.bindPopup("Italy", { closeButton: false });
		marker3.bindPopup("Germany", { closeButton: false });
		marker4.bindPopup("France", { closeButton: false });

		L.control.scale().addTo(leaflet);
	}

	var demo4 = function () {
		// define leaflet
		var leaflet = L.map('kt_leaflet_4', {
			center: [51.505, -0.09],
			zoom: 13
		})

		// set leaflet tile layer
		L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
			attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
		}).addTo(leaflet);

		// set custom SVG icon marker
		var leafletIcon = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// bind marker with popup
		var marker = L.marker([51.5, -0.09], { icon: leafletIcon }).addTo(leaflet);

		// set circle polygon
		var circle = L.circle([51.508, -0.11], {
			color: '#4A7DFF',
			fillColor: '#6993FF',
			fillOpacity: 0.5,
			radius: 700
		}).addTo(leaflet);

		// set polygon
		var polygon = L.polygon([
			[51.509, -0.08],
			[51.503, -0.06],
			[51.51, -0.047]
		]).addTo(leaflet);
	}

	var demo5 = function () {
		// Define Map Location
		var leaflet = L.map('kt_leaflet_5', {
			center: [40.725, -73.985],
			zoom: 13
		});

		// Init Leaflet Map
		L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
			attribution: '&copy; <a href="https://osm.org/copyright">OpenStreetMap</a> contributors'
		}).addTo(leaflet);

		// Set Geocoding
		var geocodeService;
		if (typeof L.esri.Geocoding === 'undefined') {
			geocodeService = L.esri.geocodeService();
		} else {
			geocodeService = L.esri.Geocoding.geocodeService();
		}

		// Define Marker Layer
		var markerLayer = L.layerGroup().addTo(leaflet);

		// Set Custom SVG icon marker
		var leafletIcon = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// Map onClick Action
		leaflet.on('click', function (e) {
			geocodeService.reverse().latlng(e.latlng).run(function (error, result) {
				if (error) {
					return;
				}
				markerLayer.clearLayers(); // remove this line to allow multi-markers on click
				L.marker(result.latlng, { icon: leafletIcon }).addTo(markerLayer).bindPopup(result.address.Match_addr, { closeButton: false }).openPopup();
				alert(`You've clicked on the following address: ${result.address.Match_addr}`);
			});
		});
	}

	var demo6 = function () {
		// add sample location data
		var data = [
			{ "loc": [41.575330, 13.102411], "title": "aquamarine" },
			{ "loc": [41.575730, 13.002411], "title": "black" },
			{ "loc": [41.807149, 13.162994], "title": "blue" },
			{ "loc": [41.507149, 13.172994], "title": "chocolate" },
			{ "loc": [41.847149, 14.132994], "title": "coral" },
			{ "loc": [41.219190, 13.062145], "title": "cyan" },
			{ "loc": [41.344190, 13.242145], "title": "darkblue" },
			{ "loc": [41.679190, 13.122145], "title": "Darkred" },
			{ "loc": [41.329190, 13.192145], "title": "Darkgray" },
			{ "loc": [41.379290, 13.122545], "title": "dodgerblue" },
			{ "loc": [41.409190, 13.362145], "title": "gray" },
			{ "loc": [41.794008, 12.583884], "title": "green" },
			{ "loc": [41.805008, 12.982884], "title": "greenyellow" },
			{ "loc": [41.536175, 13.273590], "title": "red" },
			{ "loc": [41.516175, 13.373590], "title": "rosybrown" },
			{ "loc": [41.507175, 13.273690], "title": "royalblue" },
			{ "loc": [41.836175, 13.673590], "title": "salmon" },
			{ "loc": [41.796175, 13.570590], "title": "seagreen" },
			{ "loc": [41.436175, 13.573590], "title": "seashell" },
			{ "loc": [41.336175, 13.973590], "title": "silver" },
			{ "loc": [41.236175, 13.273590], "title": "skyblue" },
			{ "loc": [41.546175, 13.473590], "title": "yellow" },
			{ "loc": [41.239290, 13.032145], "title": "white" }
		];

		// init leaflet map
		var leaflet = new L.Map('kt_leaflet_6', { zoom: 10, center: new L.latLng(data[0].loc) });

		leaflet.addLayer(new L.TileLayer('http://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'));

		// add scale layer
		L.control.scale().addTo(leaflet);

		// set custom SVG icon marker
		var leafletIcon = L.divIcon({
			html: `<span class="svg-icon svg-icon-danger svg-icon-3x"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="24px" height="24px" viewBox="0 0 24 24" version="1.1"><g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd"><rect x="0" y="24" width="24" height="0"/><path d="M5,10.5 C5,6 8,3 12.5,3 C17,3 20,6.75 20,10.5 C20,12.8325623 17.8236613,16.03566 13.470984,20.1092932 C12.9154018,20.6292577 12.0585054,20.6508331 11.4774555,20.1594925 C7.15915182,16.5078313 5,13.2880005 5,10.5 Z M12.5,12 C13.8807119,12 15,10.8807119 15,9.5 C15,8.11928813 13.8807119,7 12.5,7 C11.1192881,7 10,8.11928813 10,9.5 C10,10.8807119 11.1192881,12 12.5,12 Z" fill="#000000" fill-rule="nonzero"/></g></svg></span>`,
			bgPos: [10, 10],
			iconAnchor: [20, 37],
			popupAnchor: [0, -37],
			className: 'leaflet-marker'
		});

		// set markers
		data.forEach(function (item) {
			var marker = L.marker(item.loc, { icon: leafletIcon }).addTo(leaflet);
			marker.bindPopup(item.title, { closeButton: false });
		})
	}

	return {
		// public functions
		init: function () {
			// default charts
			demo1();
			demo2();
			demo3();
			demo4();
			demo5();
			demo6();
		}
	};
}();

jQuery(document).ready(function () {
	KTLeaflet.init();
});
