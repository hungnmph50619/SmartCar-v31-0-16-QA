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

    function addActionButtons() {
        var confirmForm = findConfirmForm();
        var reviewForm = findReviewForm();
        if (!confirmForm || !reviewForm || document.querySelector('.sc-payment-resolution-actions')) return;

        var submit = confirmForm.querySelector('button[type="submit"], button:not([type])');
        if (!submit) return;

        var actions = document.createElement('div');
        actions.className = 'sc-payment-resolution-actions';
        actions.innerHTML =
            '<button type="button" class="btn btn-warning btn-lg sc-request-retry">Yêu cầu khách xử lý lại</button>' +
            '<button type="button" class="btn btn-outline-danger btn-lg sc-reject-payment">Từ chối xác nhận</button>';

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
        addActionButtons();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.setTimeout(init, 300);
})();
