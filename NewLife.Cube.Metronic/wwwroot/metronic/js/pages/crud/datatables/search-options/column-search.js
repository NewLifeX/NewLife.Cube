"use strict";
var KTDatatablesSearchOptionsColumnSearch = function() {

    $.fn.dataTable.Api.register('column().title()', function() {
        return $(this.header()).text().trim();
    });

    var initTable1 = function() {

        // begin first table
        var table = $('#kt_datatable').DataTable({
            responsive: true,

            // Pagination settings
            dom: `<'row'<'col-sm-12'tr>>
			<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7 dataTables_pager'lp>>`,
            // read more: https://datatables.net/examples/basic_init/dom.html

            lengthMenu: [5, 10, 25, 50],

            pageLength: 10,

            language: {
                'lengthMenu': 'Display _MENU_',
            },

            searchDelay: 500,
            processing: true,
            serverSide: true,
            ajax: {
                url: HOST_URL + '/api/datatables/demos/server.php',
                type: 'POST',
                data: {
                    // parameters for custom backend script demo
                    columnsDef: [
                        'RecordID', 'OrderID', 'Country', 'ShipCity', 'CompanyAgent',
                        'ShipDate', 'Status', 'Type', 'Actions',
                    ],
                },
            },
            columns: [{
                    data: 'RecordID'
                },
                {
                    data: 'OrderID'
                },
                {
                    data: 'Country'
                },
                {
                    data: 'ShipCity'
                },
                {
                    data: 'CompanyAgent'
                },
                {
                    data: 'ShipDate'
                },
                {
                    data: 'Status'
                },
                {
                    data: 'Type'
                },
                {
                    data: 'Actions',
                    responsivePriority: -1
                },
            ],
            initComplete: function() {
                var thisTable = this;
                var rowFilter = $('<tr class="filter"></tr>').appendTo($(table.table().header()));

                this.api().columns().every(function() {
                    var column = this;
                    var input;

                    switch (column.title()) {
                        case 'Record ID':
                        case 'Order ID':
                        case 'Ship City':
                        case 'Company Agent':
                            input = $(`<input type="text" class="form-control form-control-sm form-filter datatable-input" data-col-index="` + column.index() + `"/>`);
                            break;

                        case 'Country':
                            input = $(`<select class="form-control form-control-sm form-filter datatable-input" title="Select" data-col-index="` + column.index() + `">
										<option value="">Select</option></select>`);
                            column.data().unique().sort().each(function(d, j) {
                                $(input).append('<option value="' + d + '">' + d + '</option>');
                            });
                            break;

                        case 'Status':
                            var status = {
                                1: {
                                    'title': 'Pending',
                                    'class': 'label-light-primary'
                                },
                                2: {
                                    'title': 'Delivered',
                                    'class': ' label-light-danger'
                                },
                                3: {
                                    'title': 'Canceled',
                                    'class': ' label-light-primary'
                                },
                                4: {
                                    'title': 'Success',
                                    'class': ' label-light-success'
                                },
                                5: {
                                    'title': 'Info',
                                    'class': ' label-light-info'
                                },
                                6: {
                                    'title': 'Danger',
                                    'class': ' label-light-danger'
                                },
                                7: {
                                    'title': 'Warning',
                                    'class': ' label-light-warning'
                                },
                            };
                            input = $(`<select class="form-control form-control-sm form-filter datatable-input" title="Select" data-col-index="` + column.index() + `">
										<option value="">Select</option></select>`);
                            column.data().unique().sort().each(function(d, j) {
                                $(input).append('<option value="' + d + '">' + status[d].title + '</option>');
                            });
                            break;

                        case 'Type':
                            var status = {
                                1: {
                                    'title': 'Online',
                                    'state': 'danger'
                                },
                                2: {
                                    'title': 'Retail',
                                    'state': 'primary'
                                },
                                3: {
                                    'title': 'Direct',
                                    'state': 'success'
                                },
                            };
                            input = $(`<select class="form-control form-control-sm form-filter datatable-input" title="Select" data-col-index="` + column.index() + `">
										<option value="">Select</option></select>`);
                            column.data().unique().sort().each(function(d, j) {
                                $(input).append('<option value="' + d + '">' + status[d].title + '</option>');
                            });
                            break;

                        case 'Ship Date':
                            input = $(`
    							<div class="input-group date">
    								<input type="text" class="form-control form-control-sm datatable-input" readonly placeholder="From" id="kt_datepicker_1"
    								 data-col-index="` + column.index() + `"/>
    								<div class="input-group-append">
    									<span class="input-group-text"><i class="la la-calendar-o glyphicon-th"></i></span>
    								</div>
    							</div>
    							<div class="input-group date d-flex align-content-center">
    								<input type="text" class="form-control form-control-sm datatable-input" readonly placeholder="To" id="kt_datepicker_2"
    								 data-col-index="` + column.index() + `"/>
    								<div class="input-group-append">
    									<span class="input-group-text"><i class="la la-calendar-o glyphicon-th"></i></span>
    								</div>
    							</div>`);
                            break;

                        case 'Actions':
                            var search = $(`
                                <button class="btn btn-primary kt-btn btn-sm kt-btn--icon d-block">
							        <span>
							            <i class="la la-search"></i>
							            <span>Search</span>
							        </span>
							    </button>`);

                            var reset = $(`
                                <button class="btn btn-secondary kt-btn btn-sm kt-btn--icon">
							        <span>
							           <i class="la la-close"></i>
							           <span>Reset</span>
							        </span>
							    </button>`);

                            $('<th>').append(search).append(reset).appendTo(rowFilter);

                            $(search).on('click', function(e) {
                                e.preventDefault();
                                var params = {};
                                $(rowFilter).find('.datatable-input').each(function() {
                                    var i = $(this).data('col-index');
                                    if (params[i]) {
                                        params[i] += '|' + $(this).val();
                                    } else {
                                        params[i] = $(this).val();
                                    }
                                });
                                $.each(params, function(i, val) {
                                    // apply search params to datatable
                                    table.column(i).search(val ? val : '', false, false);
                                });
                                table.table().draw();
                            });

                            $(reset).on('click', function(e) {
                                e.preventDefault();
                                $(rowFilter).find('.datatable-input').each(function(i) {
                                    $(this).val('');
                                    table.column($(this).data('col-index')).search('', false, false);
                                });
                                table.table().draw();
                            });
                            break;
                    }

                    if (column.title() !== 'Actions') {
                        $(input).appendTo($('<th>').appendTo(rowFilter));
                    }
                });

                // hide search column for responsive table
                var hideSearchColumnResponsive = function() {
                    thisTable.api().columns().every(function() {
                        var column = this
                        if (column.responsiveHidden()) {
                            $(rowFilter).find('th').eq(column.index()).show();
                        } else {
                            $(rowFilter).find('th').eq(column.index()).hide();
                        }
                    })
                };

                // init on datatable load
                hideSearchColumnResponsive();
                // recheck on window resize
                window.onresize = hideSearchColumnResponsive;

                $('#kt_datepicker_1,#kt_datepicker_2').datepicker();
            },
            columnDefs: [{
                    targets: -1,
                    title: 'Actions',
                    orderable: false,
                    render: function(data, type, full, meta) {
                        return '\
							<div class="dropdown dropdown-inline">\
								<a href="javascript:;" class="btn btn-sm btn-clean btn-icon" data-toggle="dropdown">\
	                                <i class="la la-cog"></i>\
	                            </a>\
							  	<div class="dropdown-menu dropdown-menu-sm dropdown-menu-right">\
									<ul class="nav nav-hoverable flex-column">\
							    		<li class="nav-item"><a class="nav-link" href="#"><i class="nav-icon la la-edit"></i><span class="nav-text">Edit Details</span></a></li>\
							    		<li class="nav-item"><a class="nav-link" href="#"><i class="nav-icon la la-leaf"></i><span class="nav-text">Update Status</span></a></li>\
							    		<li class="nav-item"><a class="nav-link" href="#"><i class="nav-icon la la-print"></i><span class="nav-text">Print</span></a></li>\
									</ul>\
							  	</div>\
							</div>\
							<a href="javascript:;" class="btn btn-sm btn-clean btn-icon" title="Edit details">\
								<i class="la la-edit"></i>\
							</a>\
							<a href="javascript:;" class="btn btn-sm btn-clean btn-icon" title="Delete">\
								<i class="la la-trash"></i>\
							</a>\
						';
                    },
                },
                {
                    targets: 5,
                    width: '150px',
                },
                {
                    targets: 6,
                    width: '100px',
                    render: function(data, type, full, meta) {
                        var status = {
                            1: {
                                'title': 'Pending',
                                'class': 'label-light-primary'
                            },
                            2: {
                                'title': 'Delivered',
                                'class': ' label-light-danger'
                            },
                            3: {
                                'title': 'Canceled',
                                'class': ' label-light-primary'
                            },
                            4: {
                                'title': 'Success',
                                'class': ' label-light-success'
                            },
                            5: {
                                'title': 'Info',
                                'class': ' label-light-info'
                            },
                            6: {
                                'title': 'Danger',
                                'class': ' label-light-danger'
                            },
                            7: {
                                'title': 'Warning',
                                'class': ' label-light-warning'
                            },
                        };
                        if (typeof status[data] === 'undefined') {
                            return data;
                        }
                        return '<span class="label label-lg font-weight-bold' + status[data].class + ' label-inline">' + status[data].title + '</span>';
                    },
                },
                {
                    targets: 7,
                    width: '100px',
                    render: function(data, type, full, meta) {
                        var status = {
                            1: {
                                'title': 'Online',
                                'state': 'danger'
                            },
                            2: {
                                'title': 'Retail',
                                'state': 'primary'
                            },
                            3: {
                                'title': 'Direct',
                                'state': 'success'
                            },
                        };
                        if (typeof status[data] === 'undefined') {
                            return data;
                        }
                        return '<span class="label label-' + status[data].state + ' label-dot mr-2"></span>' +
                            '<span class="font-weight-bold text-' + status[data].state + '">' + status[data].title + '</span>';
                    },
                },
            ],
        });

    };

    return {

        //main function to initiate the module
        init: function() {
            initTable1();
        },

    };

}();

jQuery(document).ready(function() {
    KTDatatablesSearchOptionsColumnSearch.init();
});
