// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    function initSidebarReportSections() {
        var blocks = document.querySelectorAll('.sidebar-subsection-block[data-sidebar-section]');
        if (!blocks.length) {
            return;
        }

        blocks.forEach(function (block) {
            var key = block.getAttribute('data-sidebar-section');
            var hasActive = block.getAttribute('data-section-has-active') === 'true';
            var storageKey = 'pms-sidebar-section-' + key;
            var collapsed = false;

            if (hasActive) {
                collapsed = false;
                try { localStorage.setItem(storageKey, 'expanded'); } catch (e) { /* ignore */ }
            } else {
                try {
                    collapsed = localStorage.getItem(storageKey) === 'collapsed';
                } catch (e) {
                    collapsed = false;
                }
            }

            block.classList.toggle('is-collapsed', collapsed);
            var toggle = block.querySelector('.sidebar-subsection-toggle');
            if (toggle) {
                toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
            }
        });

        document.querySelectorAll('.sidebar-subsection-toggle').forEach(function (toggle) {
            if (toggle.dataset.sidebarSectionBound === 'true') {
                return;
            }
            toggle.dataset.sidebarSectionBound = 'true';

            toggle.addEventListener('click', function () {
                var block = toggle.closest('.sidebar-subsection-block');
                if (!block) {
                    return;
                }

                var key = block.getAttribute('data-sidebar-section');
                var willCollapse = !block.classList.contains('is-collapsed');
                block.classList.toggle('is-collapsed', willCollapse);
                toggle.setAttribute('aria-expanded', willCollapse ? 'false' : 'true');

                if (key) {
                    try {
                        localStorage.setItem('pms-sidebar-section-' + key, willCollapse ? 'collapsed' : 'expanded');
                    } catch (e) { /* ignore */ }
                }
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSidebarReportSections);
    } else {
        initSidebarReportSections();
    }
})();
