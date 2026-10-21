$(function () {
    // Header cố định (fixed) khi cuộn: chừa đúng khoảng trống phía trên bằng chiều cao header thật
    var header = document.querySelector('header');
    if (header) {
        var syncHeaderHeight = function () {
            document.documentElement.style.setProperty('--header-height', header.offsetHeight + 'px');
        };
        syncHeaderHeight();
        new ResizeObserver(syncHeaderHeight).observe(header);
    }

    function refreshCartCount() {
        $.getJSON('/Cart/Count', function (data) {
            $('.cart-count-badge').text(data.count);
        });
    }
    refreshCartCount();

    // Thêm vào giỏ bằng AJAX (dùng ở trang danh mục/chi tiết sản phẩm)
    $(document).on('submit', '.add-to-cart-form', function (e) {
        e.preventDefault();
        var $form = $(this);
        $.post($form.attr('action'), $form.serialize(), function (data) {
            if (data.success) refreshCartCount();
        });
    });
});
