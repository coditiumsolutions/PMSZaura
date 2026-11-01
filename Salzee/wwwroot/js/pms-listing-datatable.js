(function ($) {
    'use strict';

    window.PmsListingDataTable = {
        defaults: {
            pageLength: 20,
            lengthMenu: [[20, 50, 100], [20, 50, 100]],
            paging: true,
            scrollX: false,
            autoWidth: false,
            language: {
                search: 'Search:',
                lengthMenu: 'Show _MENU_ per page',
                info: 'Showing _START_ to _END_ of _TOTAL_ records',
                infoEmpty: 'No records',
                infoFiltered: '(filtered from _MAX_)',
                paginate: {
                    first: 'First',
                    last: 'Last',
                    next: 'Next',
                    previous: 'Previous'
                }
            }
        },
        init: function (selector, options) {
            var $table = $(selector);
            if (!$table.length || !$.fn.DataTable) {
                return null;
            }

            if ($.fn.DataTable.isDataTable($table)) {
                return $table.DataTable();
            }

            var settings = $.extend(true, {}, this.defaults, options || {});
            return $table.DataTable(settings);
        },
        bindRowNavigation: function (tableSelector, options) {
            var opts = options || {};
            var rowSelector = opts.rowSelector || 'tr[data-href]';
            var ignoreSelector = opts.ignoreSelector || 'a, button, form, input, select, label, textarea, .pms-saas-td-check';
            $(tableSelector + ' tbody').on('click', rowSelector, function (e) {
                if ($(e.target).closest(ignoreSelector).length) {
                    return;
                }
                var href = $(this).data('href');
                if (href) {
                    window.location.href = href;
                }
            });
        },
        bindModalRowOpen: function (tableSelector, rowSelector, modalPrefix) {
            $(tableSelector + ' tbody').on('click', rowSelector, function (e) {
                if ($(e.target).closest('a, button, form, input, select, label, textarea, .btn-group-vertical').length) {
                    return;
                }
                var id = $(this).data('row-id');
                if (!id) {
                    return;
                }
                var modalEl = document.getElementById(modalPrefix + id);
                if (!modalEl || !window.bootstrap) {
                    return;
                }
                window.bootstrap.Modal.getOrCreateInstance(modalEl, { backdrop: false }).show();
            });
        }
    };
})(jQuery);
