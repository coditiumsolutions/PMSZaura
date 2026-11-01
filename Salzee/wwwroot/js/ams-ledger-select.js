(function (window, $) {
    'use strict';

    if (!$ || !$.fn.select2) {
        return;
    }

    function initAmsLedgerSelect($el) {
        if ($el.data('select2')) {
            return;
        }

        var isSmall = $el.hasClass('form-select-sm');
        var placeholder = $el.data('placeholder') || 'Type to search ledger…';
        var allowClearAttr = $el.attr('data-allow-clear');
        var allowClear = allowClearAttr === 'false'
            ? false
            : (allowClearAttr === 'true' ? true : !$el.prop('required'));
        var $parent = $el.closest('.modal');
        if (!$parent.length) {
            $parent = $el.closest('table').length ? $('body') : $(document.body);
        }

        $el.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: placeholder,
            allowClear: allowClear,
            dropdownParent: $parent,
            minimumResultsForSearch: 0
        });

        if (isSmall) {
            $el.next('.select2-container').addClass('select2-container--sm');
        }
    }

    window.PmsAmsLedgerSelect = {
        init: function (root) {
            var $root = root ? $(root) : $(document);
            $root.find('select.ams-ledger-select').each(function () {
                initAmsLedgerSelect($(this));
            });
        },
        destroy: function (root) {
            var $root = root ? $(root) : $(document);
            $root.find('select.ams-ledger-select').each(function () {
                var $el = $(this);
                if ($el.data('select2')) {
                    $el.select2('destroy');
                }
            });
        }
    };

    $(function () {
        PmsAmsLedgerSelect.init();
    });
})(window, window.jQuery);
