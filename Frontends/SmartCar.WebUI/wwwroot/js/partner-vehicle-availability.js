(function () {
    'use strict';

    function normalizeVehicleName(value) {
        var tokens = (value || '').trim().split(/\s+/).filter(Boolean);
        var normalized = [];
        tokens.forEach(function (token) {
            if (!normalized.length || normalized[normalized.length - 1].toLowerCase() !== token.toLowerCase()) {
                normalized.push(token);
            }
        });
        return normalized.join(' ');
    }

    document.querySelectorAll('#vehicleList .vehicle-card h3').forEach(function (heading) {
        heading.textContent = normalizeVehicleName(heading.textContent);
    });

    document.querySelectorAll('#vehicleList form[action*="UpdateAvailability"]').forEach(function (form) {
        var oldButton = form.querySelector('button[type="submit"]');
        var activeInput = form.querySelector('input[name="isActive"]');
        if (!oldButton || !activeInput) return;

        // Thay nút để loại bỏ các click listener xác nhận đã được script cũ gắn trước đó.
        var button = oldButton.cloneNode(true);
        oldButton.replaceWith(button);
        button.removeAttribute('data-confirm');

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            if (button.disabled) return;

            var enabling = button.textContent.trim().toLowerCase().indexOf('bật nhận đơn') === 0;
            var confirmation = enabling
                ? 'Bật nhận đơn mới cho xe này ngay bây giờ?'
                : 'Xác nhận ngừng nhận đơn mới cho xe này?';
            if (!window.confirm(confirmation)) return;

            // Gửi giá trị lowercase rõ ràng để ASP.NET model binder nhận đúng trạng thái.
            activeInput.value = enabling ? 'true' : 'false';
            button.disabled = true;
            button.textContent = 'Đang cập nhật...';

            // Dùng POST điều hướng bình thường thay cho fetch/AJAX. Server sẽ tự xử lý
            // redirect và TempData; tránh tình trạng request đứng ở 'Đang cập nhật...'.
            window.setTimeout(function () {
                HTMLFormElement.prototype.submit.call(form);
            }, 50);
        });
    });
})();
