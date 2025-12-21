// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

/*// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.target.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});*/


// POST Button Logic (Unblock / Delete)
$(document).on('click', 'button[data-post]', function (e) {
    // Stop the button from doing anything automatically (fix cancel also submit form)
    e.preventDefault();
    e.stopPropagation();

    // 1. Get the custom message, or use a default if missing
    var message = $(this).data('confirm') || "Are you sure?";

    // 2. Show the popup
    if (confirm(message)) {
        // 3. If clicked OK, submit the form
        var url = $(this).data('post');
        var form = $('<form action="' + url + '" method="post"></form>');

        // Add CSRF token if your app uses it (Standard ASP.NET Core)
        // var token = $('input[name="__RequestVerificationToken"]').val();
        // if (token) {
        //     form.append('<input type="hidden" name="__RequestVerificationToken" value="' + token + '" />');
        // }

        $('body').append(form);
        form.submit();
    }
    // Nothing happens if cancel is clicked
});


// Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$('[type=reset]').on('click', e => {
    e.preventDefault();
    location = location;
});

// Check all checkboxes
$('[data-check]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a')) return;

    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview
$('.upload input').on('change', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;

    if (f && f.type.startsWith('image/')) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    // Trigger input validation
    $(e.target).valid();
});
$(document).ready(function () {
    if ($.fn.select2) {

        // Initialize Product Search Dropdown
        $('#ProductSearch').select2({
            placeholder: "- Type to Search -",
            allowClear: true,
            width: '100%'
        });

        // Initialize Supplier Search Dropdown
        $('#SupplierSearch').select2({
            placeholder: "- Type to Search -",
            allowClear: true,
            width: '100%',
        });
    }
});