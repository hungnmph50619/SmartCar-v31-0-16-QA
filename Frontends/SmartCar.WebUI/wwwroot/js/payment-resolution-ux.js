(function () {
    'use strict';

    if (!/\/ReservationLookup\/Details\//i.test(window.location.pathname)) return;

    function money(value) {
        var number = Number(String(value || '').replace(/[^0-9.-]/g, '')) || 0;
        return new Intl.NumberFormat('vi-VN').format(number) + ' đ';
    }

    function addStyles() {
        if (document.getElementById('sc-payment-resolution-styles')) return;
        var style = document.createElement('style');
        style.id = 'sc-payment-resolution-styles';
        style.textContent = [
            '.sc-payment-review{border:1px solid #d9e4ee;border-radius:14px;background:#f8fbfe;padding:18px;margin-top:16px}',
            '.sc-payment-review-title{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;margin-bottom:14px}',
            '.sc-payment-review-title h5{margin:0;font-size:1.05rem}.sc-payment-review-title p{margin:.25rem 0 0;color:#64748b}',
            '.sc-payment-summary{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin-bottom:16px}',
            '.sc-payment-metric{background:#fff;border:1px solid #e2e8f0;border-radius:10px;padding:12px}',
            '.sc-payment-metric small{display:block;color:#64748b;margin-bottom:4px}.sc-payment-metric strong{font-size:1rem;color:#0f172a}',
            '.sc-difference.is-match{color:#15803d}.sc-difference.is-mismatch{color:#b91c1c}',
            '.sc-review-actions{display:flex;gap:10px;flex-wrap:wrap}.sc-review-actions .btn{min-height:42px}',
            '.sc-review-help{border-left:4px solid #f59e0b;background:#fff7df;padding:12px 14px;border-radius:8px;margin:12px 0;color:#6b4c00}',
            '.sc-payment-status{border-radius:12px;padding:14px 16px;margin:12px 0;font-weight:600}',
            '.sc-payment-status.warning{background:#fff7df;border:1px solid #f4d58d;color:#6b4c00}',
            '.sc-payment-status.danger{background:#fff0f0;border:1px solid #f5b8b8;color:#8f1d1d}',
            '.sc-qr-guidance{font-size:.86rem;color:#475569;margin-top:10px;line-height:1.45}',
            '@media(max-width:767px){.sc-payment-summary{grid-template-columns:1fr}.sc-payment-review{padding:14px}.sc-review-actions .btn{width:100%}}'
        ].join('');
        document.head.appendChild(style);
    }

    function replaceDemoText() {
        document.querySelectorAll('.small.text-warning, .text-warning').forEach(function (element) {
            var text = (element.textContent || '').trim();
            if (/QR demo|trình diễn đồ án|thay bằng QR ngân hàng/i.test(text)) {
                element.className = 'sc-qr-guidance';
                element.innerHTML = 'Vui lòng kiểm tra đúng <strong>số tài khoản, số tiền và nội dung chuyển khoản</strong> trước khi xác nhận.';
            }
        });
    }

    function improveConfirmationForm(form, container) {
        if (!form || form.dataset.scEnhanced === 'true') return;
        form.dataset.scEnhanced = 'true';
        form.classList.add('sc-payment-review');

        var amountInput = form.querySelector('[name="amount"]');
        var transactionInput = form.querySelector('[name="transactionCode"]');
        var providerInput = form.querySelector('[name="provider"]');
        var expected = amountInput ? Number(amountInput.value || 0) : 0;
        var transferContent = '';
        var contentMatch = (container.textContent || '').match(/SC\d{6}(?:-[A-Z0-9]+)?/i);
        if (contentMatch) transferContent = contentMatch[0];

        var heading = document.createElement('div');
        heading.className = 'sc-payment-review-title';
        heading.innerHTML = '<div><h5>Đối chiếu giao dịch trên sao kê</h5><p>Chỉ xác nhận khi tiền đã thực sự vào tài khoản SmartCar.</p></div><span class="badge badge-warning p-2">Chờ xác nhận</span>';
        form.insertBefore(heading, form.firstChild);

        var summary = document.createElement('div');
        summary.className = 'sc-payment-summary';
        summary.innerHTML =
            '<div class="sc-payment-metric"><small>Số tiền cần nhận</small><strong>' + money(expected) + '</strong></div>' +
            '<div class="sc-payment-metric"><small>Nội dung yêu cầu</small><strong>' + (transferContent || 'Theo mã đơn') + '</strong></div>' +
            '<div class="sc-payment-metric"><small>Chênh lệch</small><strong class="sc-difference is-match">0 đ</strong></div>';
        heading.insertAdjacentElement('afterend', summary);

        var difference = summary.querySelector('.sc-difference');
        function updateDifference() {
            var actual = Number(amountInput && amountInput.value || 0);
            var diff = actual - expected;
            difference.textContent = (diff > 0 ? '+' : '') + money(diff);
            difference.classList.toggle('is-match', diff === 0);
            difference.classList.toggle('is-mismatch', diff !== 0);
        }
        if (amountInput) {
            amountInput.setAttribute('inputmode', 'numeric');
            amountInput.addEventListener('input', updateDifference);
            updateDifference();
        }
        if (transactionInput) transactionInput.placeholder = 'Ví dụ: FT240724001';
        if (providerInput && !providerInput.value) providerInput.value = 'Agribank';

        var help = document.createElement('div');
        help.className = 'sc-review-help';
        help.innerHTML = '<strong>Kiểm tra trước khi xác nhận:</strong> đúng số tiền, mã giao dịch chưa dùng cho đơn khác, tiền đã vào sao kê và nội dung có thể đối chiếu với khách.';
        var submit = form.querySelector('button[type="submit"], button:not([type])');
        if (submit) {
            submit.textContent = 'Xác nhận đã nhận đủ';
            submit.classList.add('btn-lg');
            submit.parentNode.insertBefore(help, submit);
        }
    }

    function improveReviewForm(form) {
        if (!form || form.dataset.scEnhanced === 'true') return;
        form.dataset.scEnhanced = 'true';
        form.action = '/PaymentResolution/Review';
        form.classList.add('sc-payment-review');

        var select = form.querySelector('[name="decision"]');
        var note = form.querySelector('[name="note"]');
        var button = form.querySelector('button[type="submit"], button:not([type])');
        if (!select || !button) return;

        select.innerHTML = [
            '<option value="">-- Chọn kết quả đối chiếu --</option>',
            '<optgroup label="Cho khách xử lý lại trong 15 phút">',
            '<option>Chưa tìm thấy giao dịch</option>',
            '<option>Thanh toán thiếu</option>',
            '<option>Sai nội dung</option>',
            '<option>Chuyển sai tài khoản</option>',
            '</optgroup>',
            '<optgroup label="Đóng đơn và giải phóng lịch xe">',
            '<option>Thanh toán muộn</option>',
            '<option>Giao dịch không hợp lệ</option>',
            '<option>Bị từ chối</option>',
            '</optgroup>'
        ].join('');
        select.required = true;
        if (note) {
            note.minLength = 10;
            note.maxLength = 500;
            note.placeholder = 'Nêu rõ vấn đề và hướng dẫn khách cần làm gì tiếp theo (ít nhất 10 ký tự)...';
        }

        var heading = document.createElement('div');
        heading.className = 'sc-payment-review-title';
        heading.innerHTML = '<div><h5>Giao dịch chưa đạt yêu cầu</h5><p>Chọn xử lý lại nếu khách còn khả năng khắc phục; chỉ từ chối khi giao dịch không hợp lệ hoặc đã quá hạn.</p></div>';
        form.insertBefore(heading, form.firstChild);

        var help = document.createElement('div');
        help.className = 'sc-review-help';
        help.textContent = 'Xử lý lại: giữ lịch thêm 15 phút. Từ chối: đóng bước thanh toán và mở lịch xe để nhận đơn khác.';
        button.parentNode.insertBefore(help, button);

        function updateButton() {
            var terminal = /Thanh toán muộn|Giao dịch không hợp lệ|Bị từ chối/.test(select.value);
            button.textContent = terminal ? 'Từ chối xác nhận thanh toán' : 'Yêu cầu khách xử lý lại';
            button.className = terminal ? 'btn btn-danger btn-lg' : 'btn btn-warning btn-lg';
        }
        select.addEventListener('change', updateButton);
        updateButton();
    }

    function addRoleGuidance() {
        document.querySelectorAll('.action-box').forEach(function (box) {
            var text = box.textContent || '';
            if (/Cần khách xử lý|Chưa tìm thấy giao dịch|Thanh toán thiếu|Sai nội dung|Chuyển sai tài khoản/.test(text) && !box.querySelector('.sc-payment-status')) {
                var notice = document.createElement('div');
                notice.className = 'sc-payment-status warning';
                notice.innerHTML = 'Thanh toán đang cần xử lý lại. Lịch xe chỉ được giữ tạm thời; <strong>không giao xe</strong> cho đến khi SmartCar xác nhận đã nhận đủ tiền.';
                box.insertBefore(notice, box.firstChild);
            }
            if (/Bị từ chối|Giao dịch không hợp lệ|Thanh toán muộn/.test(text) && !box.querySelector('.sc-payment-status')) {
                var rejected = document.createElement('div');
                rejected.className = 'sc-payment-status danger';
                rejected.textContent = 'Thanh toán không được xác nhận. Lịch xe đã hoặc sẽ được giải phóng để nhận đơn khác.';
                box.insertBefore(rejected, box.firstChild);
            }
        });
    }

    function enhance() {
        addStyles();
        replaceDemoText();
        document.querySelectorAll('form[action*="ConfirmPayment"]').forEach(function (form) {
            improveConfirmationForm(form, form.closest('.action-box') || form.parentElement);
        });
        document.querySelectorAll('form[action*="ReviewPayment"], form[action*="PaymentResolution/Review"]').forEach(improveReviewForm);
        addRoleGuidance();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', enhance);
    else enhance();
})();
