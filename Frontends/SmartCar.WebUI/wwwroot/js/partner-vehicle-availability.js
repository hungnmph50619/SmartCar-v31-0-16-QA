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

    function showMessage(type, message) {
        var section = document.getElementById('vehicles');
        if (!section) return;
        var old = section.querySelector('.availability-inline-message');
        if (old) old.remove();
        var alert = document.createElement('div');
        alert.className = 'alert alert-' + type + ' availability-inline-message';
        alert.setAttribute('role', type === 'success' ? 'status' : 'alert');
        alert.textContent = message;
        var body = section.querySelector('.card-body');
        if (body) body.insertBefore(alert, body.firstChild);
        alert.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    document.querySelectorAll('#vehicleList .vehicle-card h3').forEach(function (heading) {
        heading.textContent = normalizeVehicleName(heading.textContent);
    });

    document.querySelectorAll('#vehicleList form[action*="UpdateAvailability"]').forEach(function (form) {
        form.addEventListener('submit', async function (event) {
            event.preventDefault();
            var button = form.querySelector('button[type="submit"]');
            if (!button || button.disabled) return;

            var confirmation = button.getAttribute('data-confirm');
            if (confirmation && !window.confirm(confirmation)) return;

            var originalText = button.textContent;
            button.disabled = true;
            button.textContent = 'Đang cập nhật...';

            try {
                var response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    showMessage('danger', 'Không thể cập nhật trạng thái xe. Vui lòng thử lại.');
                    button.disabled = false;
                    button.textContent = originalText;
                    return;
                }

                var html = await response.text();
                var documentResult = new DOMParser().parseFromString(html, 'text/html');
                var error = documentResult.querySelector('.alert-danger');
                if (error) {
                    showMessage('danger', error.textContent.trim() || 'Không thể cập nhật trạng thái xe.');
                    button.disabled = false;
                    button.textContent = originalText;
                    return;
                }

                var success = documentResult.querySelector('.alert-success');
                showMessage('success', success ? success.textContent.trim() : 'Đã cập nhật trạng thái nhận đơn của xe.');
                window.setTimeout(function () {
                    window.location.href = '/VehiclePartner/Dashboard#vehicles';
                    window.location.reload();
                }, 500);
            } catch (error) {
                showMessage('danger', 'Không kết nối được máy chủ khi cập nhật trạng thái xe.');
                button.disabled = false;
                button.textContent = originalText;
            }
        });

        var button = form.querySelector('button[type="submit"]');
        if (button) button.removeAttribute('data-confirm');
    });
})();