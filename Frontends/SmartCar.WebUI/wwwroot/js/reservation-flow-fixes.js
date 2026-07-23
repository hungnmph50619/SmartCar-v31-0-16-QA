(function () {
    'use strict';

    function normalizeTime(value) {
        if (!value) return '';
        var match = String(value).trim().match(/^(\d{1,2}):(\d{2})/);
        if (!match) return '';
        var hour = Math.min(23, Math.max(0, parseInt(match[1], 10)));
        var minute = Math.min(59, Math.max(0, parseInt(match[2], 10)));
        return String(hour).padStart(2, '0') + ':' + String(minute).padStart(2, '0');
    }

    function build24HourSelect(input) {
        if (!input || input.tagName === 'SELECT' || input.dataset.smartcar24h === 'true') return input;

        var selectedValue = normalizeTime(input.value) || '09:00';
        var select = document.createElement('select');

        Array.from(input.attributes).forEach(function (attribute) {
            if (attribute.name !== 'type' && attribute.name !== 'step' && attribute.name !== 'value') {
                select.setAttribute(attribute.name, attribute.value);
            }
        });

        select.dataset.smartcar24h = 'true';
        select.className = input.className;

        for (var hour = 0; hour < 24; hour++) {
            for (var minute = 0; minute < 60; minute += 30) {
                var value = String(hour).padStart(2, '0') + ':' + String(minute).padStart(2, '0');
                var option = document.createElement('option');
                option.value = value;
                option.textContent = value;
                option.selected = value === selectedValue;
                select.appendChild(option);
            }
        }

        input.replaceWith(select);
        return select;
    }

    function replaceNativeTimeInputs() {
        document.querySelectorAll('input[type="time"]').forEach(build24HourSelect);
    }

    // Chạy ngay nếu DOM đã sẵn sàng, đồng thời chạy lại khi DOMContentLoaded
    // để không phụ thuộc thứ tự nạp script của từng layout.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', replaceNativeTimeInputs, { once: true });
    } else {
        replaceNativeTimeInputs();
    }
})();
