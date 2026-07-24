(function () {
    'use strict';

    if (!/\/ReservationLookup\/Details\//i.test(window.location.pathname)) return;

    function findReviewForm() {
        return document.querySelector(
            'form[action*="PaymentResolution/Review"], form[action*="ReviewPayment"]'
        );
    }

    function findConfirmForm() {
        return document.querySelector('form[action*="ConfirmPayment"]');
    }

    function formatInteger(value) {
        var digits = String(value == null ? '' : value).replace(/\D/g, '');
        if (!digits) return '';
        return Number(digits).toLocaleString('en-US');
    }

    function enhanceAmountInput(confirmForm) {
        if (!confirmForm || confirmForm.dataset.scAmountFormatted === 'true') return;

        var rawInput = confirmForm.querySelector('input[name="amount"]');
        if (!rawInput) return;

        confirmForm.dataset.scAmountFormatted = 'true';

        var displayInput = document.createElement('input');
        displayInput.type = 'text';
        displayInput.className = rawInput.className || 'form-control';
        displayInput.inputMode = 'numeric';
        displayInput.autocomplete = 'off';
        displayInput.required = rawInput.required;
        displayInput.setAttribute('aria-label', 'Số tiền thực nhận');
        displayInput.placeholder = 'Ví dụ: 45,000';

        var initialAmount = String(rawInput.value || '').split('.')[0].replace(/\D/g, '');
        rawInput.value = initialAmount;
        displayInput.value = formatInteger(initialAmount);

        rawInput.type = 'hidden';
        rawInput.required = false;
        rawInput.insertAdjacentElement('beforebegin', displayInput);

        function syncAmount() {
            var digits = displayInput.value.replace(/\D/g, '');
            displayInput.value = formatInteger(digits);
            rawInput.value = digits;
            rawInput.dispatchEvent(new Event('input', { bubbles: true }));
        }

        displayInput.addEventListener('input', syncAmount);
        displayInput.addEventListener('blur', syncAmount);

        confirmForm.addEventListener('submit', function (event) {
            syncAmount();
            if (!rawInput.value || Number(rawInput.value) <= 0) {
                event.preventDefault();
                displayInput.setCustomValidity('Vui lòng nhập số tiền thực nhận hợp lệ.');
                displayInput.reportValidity();
                displayInput.focus();
                return;
            }
            displayInput.setCustomValidity('');
        });
    }

    function openReviewForm(reviewForm, preferredDecision) {
        if (!reviewForm) return;

        reviewForm.classList.add('is-open');
        reviewForm.style.display = 'block';

        var select = reviewForm.querySelector('[name="decision"]');
        if (select && preferredDecision) {
            var matchingOption = Array.prototype.find.call(select.options, function (option) {
                return option.text.trim() === preferredDecision;
            });
            if (matchingOption) {
                select.value = matchingOption.value;
                select.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }

        reviewForm.scrollIntoView({ behavior: 'smooth', block: 'center' });
        var note = reviewForm.querySelector('[name="note"]');
        if (note) window.setTimeout(function () { note.focus(); }, 350);
    }

    function removeRedundantProblemToggle() {
        document.querySelectorAll('.sc-payment-problem-toggle').forEach(function (button) {
            button.remove();
        });
    }

    function addActionButtons() {
        var confirmForm = findConfirmForm();
        var reviewForm = findReviewForm();
        if (!confirmForm || !reviewForm || document.querySelector('.sc-payment-resolution-actions')) return;

        var submit = confirmForm.querySelector('button[type="submit"], button:not([type])');
        if (!submit) return;

        var actions = document.createElement('div');
        actions.className = 'sc-payment-resolution-actions';
        actions.innerHTML =
            '<button type="button" class="btn btn-warning btn-lg sc-request-retry">Yêu cầu khách bổ sung/chuyển lại</button>' +
            '<button type="button" class="btn btn-outline-danger btn-lg sc-reject-payment">Từ chối giao dịch và giải phóng xe</button>';

        submit.insertAdjacentElement('afterend', actions);

        actions.querySelector('.sc-request-retry').addEventListener('click', function () {
            openReviewForm(reviewForm, 'Chưa tìm thấy giao dịch');
        });

        actions.querySelector('.sc-reject-payment').addEventListener('click', function () {
            openReviewForm(reviewForm, 'Giao dịch không hợp lệ');
        });
    }

    function addStyles() {
        if (document.getElementById('sc-payment-resolution-actions-style')) return;
        var style = document.createElement('style');
        style.id = 'sc-payment-resolution-actions-style';
        style.textContent =
            '.sc-payment-resolution-actions{display:flex;gap:10px;flex-wrap:wrap;margin-top:12px}' +
            '.sc-payment-resolution-actions .btn{min-height:46px}' +
            '@media(max-width:767px){.sc-payment-resolution-actions .btn{width:100%}}';
        document.head.appendChild(style);
    }

    function init() {
        addStyles();
        var confirmForm = findConfirmForm();
        enhanceAmountInput(confirmForm);
        addActionButtons();
        removeRedundantProblemToggle();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.setTimeout(init, 300);
})();