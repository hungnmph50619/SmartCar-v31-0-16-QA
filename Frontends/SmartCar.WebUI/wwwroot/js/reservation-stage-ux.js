(function () {
    'use strict';

    if (!/\/ReservationLookup\/Details\//i.test(window.location.pathname)) return;

    function normalize(value) {
        return (value || '').toLowerCase().replace(/\s+/g, ' ').trim();
    }

    function findCard(title) {
        return Array.from(document.querySelectorAll('.detail-card')).find(function (card) {
            var heading = card.querySelector('h4');
            return heading && normalize(heading.textContent).indexOf(normalize(title)) >= 0;
        });
    }

    function getOperationCards() {
        return {
            handover: document.getElementById('handover') || findCard('Biên bản giao'),
            incident: document.getElementById('incident') || findCard('Sự cố và hỗ trợ'),
            dispute: document.getElementById('dispute') || findCard('Khiếu nại và tranh chấp'),
            traffic: findCard('Phạt nguội')
        };
    }

    function pageState() {
        var summary = document.getElementById('reservationSummary');
        var source = normalize((summary ? summary.textContent : '') + ' ' + document.body.textContent);

        if (/chờ chủ xe xác nhận/.test(source)) return 'owner-pending';
        if (/chờ nhân viên xác nhận|chờ đối chiếu|đang đối chiếu/.test(source)) return 'payment-review';
        if (/chờ thanh toán|thanh toán chuyển khoản thủ công|thời gian giữ xe còn lại/.test(source)) return 'payment';
        if (/chờ giao xe|đã thanh toán giữ chỗ/.test(source)) return 'handover';
        if (/đang thuê/.test(source)) return 'renting';
        if (/trả xe và đối soát|đã trả xe/.test(source)) return 'returning';
        if (/hoàn thành/.test(source)) return 'completed';
        return 'unknown';
    }

    function hasMeaningfulData(card) {
        if (!card) return false;
        var text = normalize(card.textContent);
        var emptyPhrases = [
            'chưa có biên bản',
            'chưa ghi nhận khoản phạt nào',
            'chưa có sự cố',
            'chưa có yêu cầu',
            'không có sự cố',
            'không có tranh chấp'
        ];
        return !emptyPhrases.some(function (phrase) { return text.indexOf(phrase) >= 0; });
    }

    function restoreCardsFromLegacyGroup(cards) {
        var group = document.querySelector('.smart-operations-group');
        var summary = document.querySelector('.smart-post-rental-summary');
        if (!group && !summary) return;

        var parent = (summary && summary.parentNode) || (group && group.parentNode);
        var marker = summary || group;
        Object.keys(cards).forEach(function (key) {
            var card = cards[key];
            if (card && parent) parent.insertBefore(card, marker);
        });
        if (group) group.remove();
        if (summary) summary.remove();
    }

    function hide(card) {
        if (card) {
            card.style.display = 'none';
            card.setAttribute('aria-hidden', 'true');
        }
    }

    function show(card) {
        if (card) {
            card.style.display = '';
            card.removeAttribute('aria-hidden');
        }
    }

    function addPaymentHelp() {
        if (document.getElementById('scPaymentHelp')) return;
        var payment = document.getElementById('paymentSection') || Array.from(document.querySelectorAll('.detail-card')).find(function (card) {
            var heading = card.querySelector('h4');
            return heading && normalize(heading.textContent).indexOf('thanh toán') >= 0;
        });
        if (!payment || !payment.parentNode) return;

        var help = document.createElement('details');
        help.id = 'scPaymentHelp';
        help.className = 'sc-payment-help';
        help.innerHTML = [
            '<summary>Cần hỗ trợ thanh toán?</summary>',
            '<div class="sc-payment-help-body">',
            '<p>Chọn tình huống đang gặp để biết cách xử lý. Không bấm “Tôi đã chuyển khoản” nếu ngân hàng chưa báo giao dịch thành công.</p>',
            '<ul>',
            '<li><strong>Chuyển khoản không thành công:</strong> kiểm tra số dư, hạn mức và kết nối ngân hàng rồi thử lại.</li>',
            '<li><strong>Chuyển sai số tiền hoặc nội dung:</strong> giữ lại mã giao dịch và chờ nhân viên SmartCar hướng dẫn đối chiếu.</li>',
            '<li><strong>Chuyển nhầm tài khoản:</strong> liên hệ ngân hàng để tra soát; SmartCar chỉ xác nhận tiền thực nhận đúng tài khoản.</li>',
            '</ul>',
            '</div>'
        ].join('');
        payment.insertAdjacentElement('afterend', help);
    }

    function addStyles() {
        if (document.getElementById('scReservationStageStyles')) return;
        var style = document.createElement('style');
        style.id = 'scReservationStageStyles';
        style.textContent = [
            '.sc-payment-help{margin:0 0 1.5rem;border:1px solid #dce6ef;border-radius:12px;background:#fff;box-shadow:0 5px 18px rgba(20,60,90,.05)}',
            '.sc-payment-help>summary{cursor:pointer;list-style:none;padding:14px 18px;font-weight:700;color:#087be0}',
            '.sc-payment-help>summary::-webkit-details-marker{display:none}',
            '.sc-payment-help>summary:after{content:"＋";float:right;color:#64748b}',
            '.sc-payment-help[open]>summary:after{content:"−"}',
            '.sc-payment-help-body{padding:0 18px 16px;color:#566474;line-height:1.55}',
            '.sc-payment-help-body p{margin:0 0 10px}',
            '.sc-payment-help-body ul{margin:0;padding-left:20px}',
            '.sc-payment-help-body li+li{margin-top:7px}'
        ].join('');
        document.head.appendChild(style);
    }

    function applyStageVisibility() {
        addStyles();
        var cards = getOperationCards();
        restoreCardsFromLegacyGroup(cards);
        var state = pageState();

        Object.keys(cards).forEach(function (key) { hide(cards[key]); });

        if (state === 'payment' || state === 'payment-review' || state === 'owner-pending') {
            addPaymentHelp();
            return;
        }

        if (state === 'handover') {
            show(cards.handover);
            return;
        }

        if (state === 'renting') {
            show(cards.handover);
            show(cards.incident);
            if (hasMeaningfulData(cards.dispute)) show(cards.dispute);
            return;
        }

        if (state === 'returning' || state === 'completed') {
            show(cards.handover);
            show(cards.incident);
            show(cards.dispute);
            if (hasMeaningfulData(cards.traffic)) show(cards.traffic);
            return;
        }

        Object.keys(cards).forEach(function (key) {
            if (hasMeaningfulData(cards[key])) show(cards[key]);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyStageVisibility, { once: true });
    } else {
        applyStageVisibility();
    }
})();
