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
                '<a class="btn btn-primary" href="' + escapeHtml(data.detailUrl || ('/Car/CarDetail/' + data.carId)) + '">' +
                    (data.isOwner ? 'Quản lý xe này' : 'Xem chi tiết xe') +
                '</a>' +
            '</div>';

        summary.insertAdjacentElement('afterend', card);

        var heading = summary.querySelector('h2');
        if (heading) heading.textContent = data.displayName;

        document.querySelectorAll('.next-action-card a').forEach(function (link) {
            if (link.textContent.trim() === 'Xem thông tin' || link.getAttribute('href') === '#reservationSummary') {
                link.textContent = data.isOwner ? 'Quản lý xe này' : 'Xem chi tiết xe';
                link.href = data.detailUrl || ('/Car/CarDetail/' + data.carId);
            }
        });

        var nextTitle = document.querySelector('.next-action-card h3');
        var nextDescription = document.querySelector('.next-action-card p');
        if (nextTitle && nextTitle.textContent.trim() === 'Theo dõi đơn thuê') {
            nextTitle.textContent = 'Đang chờ chủ xe xác nhận';
        }
        if (nextDescription && nextDescription.textContent.indexOf('timeline') >= 0) {
            nextDescription.textContent = 'Chủ xe có tối đa 120 phút để phản hồi. Bạn chưa cần thanh toán ở bước này.';
        }
    }

    function updatePaymentHoldText() {
        document.querySelectorAll('.alert-info, .next-action-card, .action-box').forEach(function (element) {
            if (element.textContent.indexOf('15 phút') >= 0) {
                element.innerHTML = element.innerHTML.replace(/15 phút/g, '10 phút');
            }
        });
    }

    function configureCancellationReason() {
        var forms = Array.prototype.slice.call(document.querySelectorAll('form'));
        var form = forms.find(function (candidate) {
            var heading = candidate.querySelector('h5');
            return heading && heading.textContent.trim() === 'Hủy đơn thuê';
        });
        if (!form) return;

        var textarea = form.querySelector('textarea[name="reason"]');
        if (!textarea) return;

        textarea.value = '';
        textarea.required = true;
        textarea.minLength = 10;
        textarea.maxLength = 500;
        textarea.placeholder = 'Vui lòng mô tả lý do hủy đơn ít nhất 10 ký tự...';

        var label = textarea.closest('.form-group') && textarea.closest('.form-group').querySelector('label');
        if (label) label.textContent = 'Lý do hủy đơn (bắt buộc)';

        var select = document.createElement('select');
        select.className = 'form-control mb-2';
        select.setAttribute('aria-label', 'Chọn lý do hủy đơn');
        select.innerHTML =
            '<option value="">-- Chọn lý do --</option>' +
            '<option value="Thay đổi kế hoạch thuê xe.">Thay đổi kế hoạch</option>' +
            '<option value="Chọn nhầm xe hoặc thời gian thuê.">Chọn nhầm xe hoặc thời gian</option>' +
            '<option value="Không liên hệ được với chủ xe.">Không liên hệ được chủ xe</option>' +
            '<option value="Chi phí thuê xe không còn phù hợp.">Chi phí không phù hợp</option>' +
            '<option value="__other__">Lý do khác</option>';
        textarea.parentNode.insertBefore(select, textarea);

        select.addEventListener('change', function () {
            if (select.value === '__other__') {
                textarea.value = '';
                textarea.placeholder = 'Nhập lý do khác ít nhất 10 ký tự...';
                textarea.focus();
            } else {
                textarea.value = select.value;
                textarea.placeholder = 'Có thể bổ sung thêm chi tiết...';
            }
        });

        form.addEventListener('submit', function (event) {
            var value = textarea.value.trim();
            if (!select.value) {
                event.preventDefault();
                select.focus();
                window.alert('Vui lòng chọn lý do hủy đơn.');
                return;
            }
            if (value.length < 10) {
                event.preventDefault();
                textarea.focus();
                window.alert('Vui lòng nhập lý do hủy đơn ít nhất 10 ký tự.');
            }
        });
    }

    function handleMissingLocalQr() {
        var qr = document.querySelector('.payment-qr');
        if (!qr) return;
        qr.addEventListener('error', function () {
            var container = qr.parentElement;
            qr.style.display = 'none';
            if (container && !container.querySelector('.qr-local-warning')) {
                var warning = document.createElement('div');
                warning.className = 'alert alert-warning qr-local-warning text-left';
                warning.textContent = 'QR demo chưa được cài trên máy này. Vui lòng dùng đúng thông tin ngân hàng và nội dung chuyển khoản hiển thị bên cạnh.';
                container.appendChild(warning);
            }
        });
    }

    updatePaymentHoldText();
    configureCancellationReason();
    handleMissingLocalQr();

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
