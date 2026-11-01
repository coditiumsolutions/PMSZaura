// Global Session Validation for AJAX Requests
$(document).ready(function() {
    // Override jQuery's ajaxSetup to handle session expiration globally
    $.ajaxSetup({
        statusCode: {
            401: function(xhr) {
                try {
                    var response = JSON.parse(xhr.responseText);
                    if (response.sessionExpired) {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Session Expired',
                            text: response.message || 'Your session has expired. Please login again.',
                            confirmButtonText: 'Login',
                            allowOutsideClick: false,
                            allowEscapeKey: false
                        }).then((result) => {
                            if (result.isConfirmed) {
                                window.location.href = '/Login/Index';
                            }
                        });
                    }
                } catch (e) {
                    // If response is not JSON, show generic session expired message
                    Swal.fire({
                        icon: 'warning',
                        title: 'Session Expired',
                        text: 'Your session has expired. Please login again.',
                        confirmButtonText: 'Login',
                        allowOutsideClick: false,
                        allowEscapeKey: false
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.href = '/Login/Index';
                        }
                    });
                }
            }
        }
    });

    // Global function to check session validity
    window.checkSession = function() {
        return $.ajax({
            url: '/Login/CheckSession',
            type: 'GET',
            cache: false
        });
    };

    // Global function to handle session expiration
    window.handleSessionExpiration = function(message) {
        Swal.fire({
            icon: 'warning',
            title: 'Session Expired',
            text: message || 'Your session has expired. Please login again.',
            confirmButtonText: 'Login',
            allowOutsideClick: false,
            allowEscapeKey: false
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = '/Login/Index';
            }
        });
    };
});
