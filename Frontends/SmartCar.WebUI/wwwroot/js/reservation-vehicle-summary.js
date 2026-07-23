(function () {
    'use strict';

    var match = window.location.pathname.match(/^\/ReservationLookup\/Details\/(\d+)\/?$/i);
    if (!match) return;

    var reservationId = match[1];
    var summary = document.getElementById('reservationSummary');
    if (!summary) return;

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function buildVehicleCard(data) {
        var image = data.imageUrl
            ? '<img class="reservation-vehicle-image" src="' + escapeHtml(data.imageUrl) + '" alt="Ảnh ' + escapeHtml(data.displayName) + '">'
            : '<div class="reservation-vehicle-placeholder" aria-hidden="true">🚗</div>';
        var plate = data.maskedLicensePlate || 'Chưa cập nhật';
        var owner = data.ownerName || 'Đối tác chủ xe';
        var ownerPhone = data.ownerPhone ? ' · ' + escapeHtml(data.ownerPhone) : '';
        var carCode = 'CAR-' + String(data.carId).padStart(4, '0');
        var partnerCode = 'PV-' + String(data.partnerVehicleId || 0).padStart(4, '0');

        var card = document.createElement('section');
        card.id = 'reservationVehicleSummary';
        card.className = 'reservation-vehicle-card mb-4';
        card.setAttribute('aria-label', 'Thông tin xe đang thuê');
        card.innerHTML =
            '<div class="reservation-vehicle-media">' + image + '</div>' +
            '<div class="reservation-vehicle-content">' +
                '<div class="reservation-vehicle-eyebrow">XE TRONG ĐƠN #' + escapeHtml(reservationId) + '</div>' +
                '<h3>' + escapeHtml(data.displayName) + '</h3>' +
                '<div class="reservation-vehicle-tags">' +
                    '<span>Biển số ' + escapeHtml(plate) + '</span>' +
                    '<span>' + escapeHtml(carCode) + '</span>' +
                    '<span>' + escapeHtml(partnerCode) + '</span>' +
                '</div>' +
                '<div class="reservation-vehicle-specs">' +
                    '<span>' + escapeHtml(data.seat) + ' chỗ</span>' +
                    '<span>' + escapeHtml(data.transmission) + '</span>' +
                    '<span>' + escapeHtml(data.fuel) + '</span>' +
                '</div>' +
                '<div class="reservation-vehicle-meta"><strong>Chủ xe:</strong> ' + escapeHtml(owner) + ownerPhone + '</div>' +
                '<div class="reservation-vehicle-meta"><strong>Điểm giao xe:</strong> ' + escapeHtml(data.locationName || 'Chưa cập nhật') + '</div>' +
            '</div>' +
            '<div class="reservation-vehicle-action">' +
                '<a class="btn btn-primary" href="' + escapeHtml(data.detailUrl || ('/Car/CarDetail/' + data.carId)) + '">Xem chi tiết xe</a>' +
            '</div>';

        summary.insertAdjacentElement('afterend', card);

        var heading = summary.querySelector('h2');
        if (heading) heading.textContent = data.displayName;

        var nextCard = document.querySelector('.next-action-card');
        var nextTitle = nextCard ? nextCard.querySelector('h3') : null;
        var nextDescription = nextCard ? nextCard.querySelector('p') : null;
        var nextLink = nextCard ? nextCard.querySelector('a') : null;
        if (nextLink && nextLink.textContent.trim() === 'Xem thông tin') {
            nextLink.textContent = 'Xem chi tiết xe';
            nextLink.href = data.detailUrl || ('/Car/CarDetail/' + data.carId);
        }
        if (nextTitle && nextTitle.textContent.trim() === 'Theo dõi đơn thuê') {
            nextTitle.textContent = 'Đang chờ chủ xe xác nhận';
            if (nextDescription) {
                nextDescription.textContent = 'Chủ xe có tối đa 120 phút để phản hồi. Bạn chưa cần thanh toán ở bước này.';
            }
        }
    }

    fetch('/ReservationVehicleInfo/Get?reservationId=' + encodeURIComponent(reservationId), {
        credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(function (response) {
            if (!response.ok) throw new Error('Không tải được thông tin xe.');
            return response.json();
        })
        .then(buildVehicleCard)
        .catch(function () {
            var fallback = document.createElement('div');
            fallback.className = 'alert alert-warning mb-4';
            fallback.textContent = 'Chưa tải được thông tin nhận diện xe. Bạn có thể tải lại trang hoặc quay lại danh sách đơn.';
            summary.insertAdjacentElement('afterend', fallback);
        });
})();
