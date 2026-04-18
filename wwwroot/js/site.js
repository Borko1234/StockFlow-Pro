// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
    });

    // Close sidebar when clicking outside on mobile
    $(document).on('click', function (e) {
        if ($(window).width() <= 768) {
            if (!$(e.target).closest('#sidebar, #sidebarCollapse').length) {
                $('#sidebar').removeClass('active');
            }
        }
    });
});
