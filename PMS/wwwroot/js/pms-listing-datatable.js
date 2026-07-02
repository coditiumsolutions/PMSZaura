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
        }
    };
})(jQuery);
