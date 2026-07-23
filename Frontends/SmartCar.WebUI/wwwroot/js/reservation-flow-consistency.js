(function () {
    'use strict';

    var storageKey = 'smartcar_reservation_schedule';

    function normalizeTime(value) {
        if (!value) return '';
        var match = String(value).trim().match(/^(\d{1,2}):(\d{2})/);
        if (!match) return '';
        var hour = Math.min(23, Math.max(0, parseInt(match[1], 10)));
        var minute = Math.min(59, Math.max(0, parseInt(match[2], 10)));
        return String(hour).padStart(2, '0') + ':' + String(minute).padStart(2, '0');
    }

    function readScheduleFromUrl(url) {
        var params = new URL(url || window.location.href, window.location.origin).searchParams;
        return {
            pickUpDate: params.get('pickUpDate') || '',
            dropOffDate: params.get('dropOffDate') || '',
            pickUpTime: normalizeTime(params.get('pickUpTime')),
            dropOffTime: normalizeTime(params.get('dropOffTime'))
        };
    }

    function hasSchedule(schedule) {
        return !!(schedule.pickUpDate || schedule.dropOffDate || schedule.pickUpTime || schedule.dropOffTime);
    }

    function saveSchedule(schedule) {
        if (!hasSchedule(schedule)) return;
        try { sessionStorage.setItem(storageKey, JSON.stringify(schedule)); } catch (_) { }
    }

    function loadSchedule() {
        var fromUrl = readScheduleFromUrl(window.location.href);
        if (hasSchedule(fromUrl)) {
            saveSchedule(fromUrl);
            return fromUrl;
        }
        try {
            return JSON.parse(sessionStorage.getItem(storageKey) || '{}');
        } catch (_) {
            return {};
        }
    }

    function findField(name) {
        return document.querySelector('[name="' + name + '"], [name="' + name.charAt(0).toUpperCase() + name.slice(1) + '"]');
    }

    function setField(name, value) {
        if (!value) return;
        var field = findField(name);
        if (!field) return;
        var normalizedValue = name.toLowerCase().includes('time') ? normalizeTime(value) : value;
        if (!normalizedValue) return;
        field.value = normalizedValue;
        field.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function enforce24HourControls() {
        document.documentElement.lang = 'vi';
        document.querySelectorAll('input[type="time"]').forEach(function (input) {
            input.lang = 'vi';
            input.step = input.step && input.step !== 'any' ? input.step : '1800';
            if (input.value) input.value = normalizeTime(input.value);
        });
        document.querySelectorAll('select').forEach(function (select) {
            if (!/time|giờ/i.test((select.name || '') + ' ' + (select.id || ''))) return;
            Array.from(select.options).forEach(function (option) {
                var normalized = normalizeTime(option.value || option.textContent);
                if (normalized) {
                    option.value = normalized;
                    option.textContent = normalized;
                }
            });
        });
    }

    function appendScheduleToReservationLinks() {
        var schedule = loadSchedule();
        document.querySelectorAll('a[href*="/Reservation/Index"], a[href*="Reservation/Index"]').forEach(function (link) {
            try {
                var url = new URL(link.href, window.location.origin);
                ['pickUpDate', 'dropOffDate', 'pickUpTime', 'dropOffTime'].forEach(function (key) {
                    if (!url.searchParams.get(key) && schedule[key]) url.searchParams.set(key, schedule[key]);
                });
                link.href = url.pathname + url.search + url.hash;
            } catch (_) { }
        });
    }

    function restoreReservationFormSchedule() {
        if (!/\/Reservation\/Index/i.test(window.location.pathname)) return;
        var schedule = loadSchedule();
        setField('pickUpDate', schedule.pickUpDate);
        setField('dropOffDate', schedule.dropOffDate);
        setField('pickUpTime', schedule.pickUpTime);
        setField('dropOffTime', schedule.dropOffTime);
    }

    function parseUtcTimestamp(raw) {
        if (!raw) return NaN;
        var text = raw.trim();
        if (!/(Z|[+-]\d{2}:?\d{2})$/i.test(text)) text += 'Z';
        return Date.parse(text);
    }

    function repairPaymentCountdown() {
        var countdown = document.getElementById('countdown');
        if (!countdown) return;
        var statusText = document.body.textContent || '';
        if (!/Chờ thanh toán|Chờ khách đặt cọc|Chờ khách thanh toán giữ chỗ/i.test(statusText)) return;

        var expiry = NaN;
        document.querySelectorAll('script').forEach(function (script) {
            if (!isNaN(expiry)) return;
            var match = (script.textContent || '').match(/new Date\('([^']+)'\)/);
            if (match) expiry = parseUtcTimestamp(match[1]);
        });
        if (isNaN(expiry)) return;

        function tick() {
            var remaining = expiry - Date.now();
            if (remaining <= 0) {
                countdown.textContent = 'Đã hết thời gian';
                return;
            }
            var minutes = Math.floor(remaining / 60000);
            var seconds = Math.floor((remaining % 60000) / 1000);
            countdown.textContent = String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0');
            window.setTimeout(tick, 1000);
        }
        tick();
    }

    function repairReservationCopy() {
        document.querySelectorAll('.alert, p, div').forEach(function (element) {
            if (element.children.length > 4) return;
            var text = element.textContent || '';
            if (text.includes('Frontends/SmartCar.WebUI/appsettings.Local.json')) {
                element.textContent = 'Thanh toán hiện đang tạm thời chưa khả dụng. Vui lòng liên hệ SmartCar hoặc thử lại sau.';
            }
            if (text.includes('xe được giữ 15 phút')) {
                element.innerHTML = element.innerHTML.replace('15 phút', '10 phút');
            }
        });
    }

    function normalizeVehicleNames() {
        document.querySelectorAll('h1, h2, h3, h4, h5').forEach(function (heading) {
            heading.textContent = heading.textContent
                .replace(/\bToyota\s+Vios\s+Vios\s+G\b/gi, 'Toyota Vios G')
                .replace(/\bToyota\s+Toyota\s+Vios\b/gi, 'Toyota Vios');
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var currentSchedule = readScheduleFromUrl(window.location.href);
        if (hasSchedule(currentSchedule)) saveSchedule(currentSchedule);
        enforce24HourControls();
        appendScheduleToReservationLinks();
        restoreReservationFormSchedule();
        repairPaymentCountdown();
        repairReservationCopy();
        normalizeVehicleNames();
    });
})();
