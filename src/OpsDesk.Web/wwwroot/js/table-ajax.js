/**
 * OpsDesk Table AJAX — Reactive live search, filter, and pagination
 * Provides smooth instant updates for table containers without full-page reloads.
 */
(function () {
    'use strict';

    function initAjaxTable() {
        const forms = document.querySelectorAll('form[data-ajax-form]');

        forms.forEach(form => {
            const containerId = form.getAttribute('data-ajax-target') || 'table-container';
            const container = document.getElementById(containerId);
            if (!container) return;

            let debounceTimer = null;

            // Function to perform AJAX fetch and DOM swap
            async function performSearch(url) {
                try {
                    container.style.opacity = '0.5';
                    container.style.pointerEvents = 'none';

                    const response = await fetch(url, {
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });

                    if (!response.ok) throw new Error('Network response was not ok');

                    const html = await response.text();
                    container.innerHTML = html;
                    window.history.pushState(null, '', url);

                    // Re-bind confirmation modals if needed
                    if (window.bindConfirmModals) {
                        window.bindConfirmModals();
                    }
                } catch (err) {
                    console.error('AJAX table update failed:', err);
                } finally {
                    container.style.opacity = '1';
                    container.style.pointerEvents = 'auto';
                }
            }

            // Build request URL from current form inputs
            function getFormUrl(pageOverride) {
                const formData = new FormData(form);
                const params = new URLSearchParams();

                for (const [key, value] of formData.entries()) {
                    if (value && value.trim() !== '') {
                        if (pageOverride && key.toLowerCase() === 'page') continue;
                        params.append(key, value.trim());
                    }
                }

                if (pageOverride) {
                    params.append('page', pageOverride);
                }

                const action = form.getAttribute('action') || window.location.pathname;
                const qs = params.toString();
                return qs ? `${action}?${qs}` : action;
            }

            // Live Search with Debounce (300ms)
            const searchInputs = form.querySelectorAll('input[type="search"], input[name*="search" i]');
            searchInputs.forEach(input => {
                input.addEventListener('input', () => {
                    clearTimeout(debounceTimer);
                    debounceTimer = setTimeout(() => {
                        const url = getFormUrl(1);
                        performSearch(url);
                    }, 300);
                });
            });

            // Instant Filter on Dropdown Selects / Checkboxes
            const filterInputs = form.querySelectorAll('select, input[type="checkbox"], input[type="radio"], input[type="date"]');
            filterInputs.forEach(select => {
                select.addEventListener('change', () => {
                    const url = getFormUrl(1);
                    performSearch(url);
                });
            });

            // Prevent traditional form submit postback
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                clearTimeout(debounceTimer);
                const url = getFormUrl(1);
                performSearch(url);
            });

            // Intercept Pagination Clicks inside container
            container.addEventListener('click', (e) => {
                const link = e.target.closest('a.page-link');
                if (link && link.href) {
                    const pageItem = link.closest('.page-item');
                    if (pageItem && pageItem.classList.contains('disabled')) {
                        e.preventDefault();
                        return;
                    }

                    e.preventDefault();
                    performSearch(link.href);
                }
            });
        });
    }

    // Support browser back/forward navigation
    window.addEventListener('popstate', () => {
        window.location.reload();
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAjaxTable);
    } else {
        initAjaxTable();
    }
})();
