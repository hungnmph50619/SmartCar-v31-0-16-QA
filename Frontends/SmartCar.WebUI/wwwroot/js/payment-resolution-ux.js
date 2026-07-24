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
            '.sc-payment-review{border:1px solid #d9e4ee;border-radius:14px;background:#f8fbfe;padding:20px;margin-top:16px}',
            '.sc-payment-review-title{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;margin-bottom:16px}',
            '.sc-payment-review-title h5{margin:0;font-size:1.15rem}.sc-payment-review-title p{margin:.3rem 0 0;color:#64748b}',
            '.sc-payment-summary{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;margin-bottom:18px}',
            '.sc-payment-metric{background:#fff;border:1px solid #e2e8f0;border-radius:12px;padding:15px}',
            '.sc-payment-metric small{display:block;color:#64748b;margin-bottom:5px}.sc-payment-metric strong{font-size:1.15rem;color:#0f172a;word-break:break-word}',
            '.sc-difference.is-match{color:#15803d}.sc-difference.is-mismatch{color:#b91c1c}',
            '.sc-review-actions{display:flex;gap:10px;flex-wrap:wrap}.sc-review-actions .btn{min-height:44px}',
            '.sc-review-help{border-left:4px solid #f59e0b;background:#fff7df;padding:13px 15px;border-radius:9px;margin:14px 0;color:#6b4c00}',
            '.sc-payment-status{border-radius:12px;padding:14px 16px;margin:12px 0;font-weight:600}',
            '.sc-payment-status.warning{background:#fff7df;border:1px solid #f4d58d;color:#6b4c00}',
            '.sc-payment-status.danger{background:#fff0f0;border:1px solid #f5b8b8;color:#8f1d1d}',
            '.sc-qr-guidance{font-size:.86rem;color:#475569;margin-top:10px;line-height:1.45}',
            '.sc-staff-payment-focus{display:grid;grid-template-columns:minmax(0,2.15fr) minmax(280px,.85fr);gap:24px;align-items:start;margin-bottom:24px}',
            '.sc-staff-payment-main>.card,.sc-staff-payment-aside>.card{margin-bottom:18px!important}',
            '.sc-staff-payment-main #paymentSection{font-size:1.45rem;font-weight:700;margin-bottom:4px}',
            '.sc-staff-payment-main .card-body{padding:28px!important}',
            '.sc-staff-payment-main .action-box{border:0;padding:0;background:transparent}',
            '.sc-staff-payment-main .sc-payment-review{background:#f8fbff;border-color:#cfe1f3}',
            '.sc-staff-payment-main form .row{margin-left:-6px;margin-right:-6px}',
            '.sc-staff-payment-main form .row>[class*=col-]{padding-left:6px;padding-right:6px}',
            '.sc-staff-payment-main .form-control{min-height:46px;border-radius:9px}',
            '.sc-staff-payment-main label{font-weight:600;color:#334155}',
            '.sc-staff-payment-main .btn-success{min-width:230px}',
            '.sc-staff-payment-aside .card-body{padding:20px!important}',
            '.sc-staff-payment-aside h4{font-size:1.08rem;margin-bottom:14px}',
            '.sc-payment-task-header{background:linear-gradient(135deg,#eef7ff,#fff);border:1px solid #d7e9fb;border-left:5px solid #1089ff;border-radius:14px;padding:18px 20px;margin-bottom:20px}',
            '.sc-payment-task-header h3{font-size:1.3rem;margin:0 0 6px}.sc-payment-task-header p{margin:0;color:#64748b}',
            '.sc-payment-problem-toggle{margin-top:14px}',
            '.sc-payment-problem-panel{display:none;margin-top:14px}.sc-payment-problem-panel.is-open{display:block}',
            '.sc-hidden-for-payment-review{display:none!important}',
            '@media(max-width:991px){.sc-staff-payment-focus{grid-template-columns:1fr}.sc-staff-payment-aside{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.sc-staff-payment-aside>.card{margin-bottom:0!important}}',
            '@media(max-width:767px){.sc-payment-summary{grid-template-columns:1fr}.sc-payment-review{padding:14px}.sc-review-actions .btn{width:100%}.sc-staff-payment-main .card-body{padding:18px!important}.sc-staff-payment-aside{grid-template-columns:1fr}.sc-staff-payment-focus{gap:14px}}'
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

    function replaceProviderWithSelect(input) {
        if (!input || input.tagName === 'SELECT' || input.dataset.scProviderEnhanced === 'true') return input;
        var select = document.createElement('select');
        Array.prototype.forEach.call(input.attributes, function (attribute) {
            if (attribute.name !== 'type' && attribute.name !== 'value') select.setAttribute(attribute.name, attribute.value);
        });
        select.dataset.scProviderEnhanced = 'true';
        select.className = input.className || 'form-control';
        select.required = true;
        var current = (input.value || 'Agribank').trim();
        var banks = ['Agribank', 'Vietcombank', 'BIDV', 'VietinBank', 'MB Bank', 'Techcombank', 'ACB', 'Sacombank', 'TPBank', 'VPBank', 'Khác'];
        select.innerHTML = '<option value="">-- Chọn ngân hàng/kênh nhận --</option>' + banks.map(function (bank) {
            return '<option value="' + bank + '"' + (bank.toLowerCase() === current.toLowerCase() ? ' selected' : '') + '>' + bank + '</option>';
        }).join('');
        if (!select.value && current) {
            var option = document.createElement('option');
            option.value = current;
            option.textContent = current;
            option.selected = true;
            select.appendChild(option);
        }
        input.replaceWith(select);
        return select;
    }

    function improveConfirmationForm(form, container) {
        if (!form || form.dataset.scEnhanced === 'true') return;
        form.dataset.scEnhanced = 'true';
        form.classList.add('sc-payment-review');

        var amountInput = form.querySelector('[name="amount"]');
        var transactionInput = form.querySelector('[name="transactionCode"]');
        var providerInput = replaceProviderWithSelect(form.querySelector('[name="provider"]'));
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
        help.innerHTML = '<strong>Kiểm tra trước khi xác nhận:</strong> đúng số tiền, đúng nội dung, mã giao dịch chưa dùng cho đơn khác và giao dịch đã xuất hiện trên sao kê.';
        var submit = form.querySelector('button[type="submit"], button:not([type])');
        if (submit) {
            submit.textContent = 'Xác nhận đã nhận đủ';
            submit.className = 'btn btn-success btn-lg';
            submit.parentNode.insertBefore(help, submit);
        }
    }

    function improveReviewForm(form) {
        if (!form || form.dataset.scEnhanced === 'true') return;
        form.dataset.scEnhanced = 'true';
        form.action = '/PaymentResolution/Review';
        form.classList.add('sc-payment-review', 'sc-payment-problem-panel');

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
        heading.innerHTML = '<div><h5>Giao dịch chưa đạt yêu cầu</h5><p>Chỉ mở phần này khi giao dịch có vấn đề cần khách xử lý hoặc cần từ chối.</p></div>';
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

    function addProblemToggle(paymentBox) {
        var reviewForm = paymentBox && paymentBox.querySelector('form[action*="PaymentResolution/Review"], form[action*="ReviewPayment"]');
        if (!reviewForm || paymentBox.querySelector('.sc-payment-problem-toggle')) return;
        var toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'btn btn-outline-warning btn-lg sc-payment-problem-toggle';
        toggle.textContent = 'Giao dịch có vấn đề';
        toggle.setAttribute('aria-expanded', 'false');
        toggle.addEventListener('click', function () {
            var open = reviewForm.classList.toggle('is-open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            toggle.textContent = open ? 'Đóng phần xử lý lỗi' : 'Giao dịch có vấn đề';
            if (open) reviewForm.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        });
        reviewForm.parentNode.insertBefore(toggle, reviewForm);
    }

    function cardByHeading(root, pattern) {
        var cards = root ? root.querySelectorAll('.card') : [];
        for (var i = 0; i < cards.length; i++) {
            var heading = cards[i].querySelector('h4,h3,h5');
            if (heading && pattern.test((heading.textContent || '').trim())) return cards[i];
        }
        return null;
    }

    function buildStaffPaymentLayout() {
        var confirmForm = document.querySelector('form[action*="ConfirmPayment"]');
        if (!confirmForm || document.querySelector('.sc-staff-payment-focus')) return;

        var shell = document.querySelector('.detail-shell');
        var row = shell && shell.querySelector(':scope > .row');
        var paymentCard = document.getElementById('paymentSection');
        paymentCard = paymentCard && paymentCard.closest('.card');
        if (!shell || !row || !paymentCard) return;

        var leftColumn = row.querySelector('.col-lg-8');
        var rightColumn = row.querySelector('.col-lg-4');
        var scheduleCard = cardByHeading(leftColumn, /Thông tin chuyến thuê/i);
        var priceCard = cardByHeading(rightColumn, /Chi tiết giá/i);

        var taskHeader = document.createElement('div');
        taskHeader.className = 'sc-payment-task-header';
        taskHeader.innerHTML = '<h3>Đối chiếu thanh toán đơn thuê</h3><p>Kiểm tra giao dịch trên sao kê, sau đó xác nhận đủ tiền hoặc mở phần xử lý khi giao dịch có vấn đề.</p>';

        var focus = document.createElement('div');
        focus.className = 'sc-staff-payment-focus';
        var main = document.createElement('div');
        main.className = 'sc-staff-payment-main';
        var aside = document.createElement('aside');
        aside.className = 'sc-staff-payment-aside';
        focus.appendChild(main);
        focus.appendChild(aside);

        main.appendChild(paymentCard);
        if (scheduleCard) aside.appendChild(scheduleCard);
        if (priceCard) aside.appendChild(priceCard);

        row.parentNode.insertBefore(taskHeader, row);
        row.parentNode.insertBefore(focus, row);

        var nextAction = shell.querySelector('.next-action-card');
        if (nextAction) nextAction.classList.add('sc-hidden-for-payment-review');

        if (leftColumn) leftColumn.classList.add('sc-hidden-for-payment-review');
        if (rightColumn) rightColumn.classList.add('sc-hidden-for-payment-review');
        row.classList.add('sc-hidden-for-payment-review');

        addProblemToggle(paymentCard);
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
        buildStaffPaymentLayout();
        addRoleGuidance();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', enhance);
    else enhance();
})();