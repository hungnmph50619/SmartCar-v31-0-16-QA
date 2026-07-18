(function () {
    "use strict";

    function getVietnameseMessage(element) {
        const validity = element.validity;
        const label = element.getAttribute("data-field-name") ||
            (element.labels && element.labels.length ? element.labels[0].innerText.trim() : "trường này");

        if (validity.valueMissing) {
            return "Vui lòng nhập hoặc chọn " + label.toLowerCase() + ".";
        }

        if (validity.typeMismatch) {
            if (element.type === "email") {
                return "Địa chỉ email không hợp lệ.";
            }
            if (element.type === "url") {
                return "Địa chỉ liên kết không hợp lệ.";
            }
            return "Giá trị đã nhập không đúng định dạng.";
        }

        if (validity.badInput) {
            if (element.type === "date") {
                return "Ngày đã chọn không hợp lệ.";
            }
            if (element.type === "time") {
                return "Giờ đã chọn không hợp lệ.";
            }
            if (element.type === "number") {
                return "Vui lòng nhập một số hợp lệ.";
            }
            return "Giá trị đã nhập không hợp lệ.";
        }

        if (validity.rangeUnderflow) {
            return "Giá trị phải lớn hơn hoặc bằng " + element.min + ".";
        }

        if (validity.rangeOverflow) {
            return "Giá trị phải nhỏ hơn hoặc bằng " + element.max + ".";
        }

        if (validity.stepMismatch) {
            return element.type === "time"
                ? "Giờ đã chọn không hợp lệ. Vui lòng chọn lại giờ."
                : "Giá trị đã nhập không đúng bước cho phép.";
        }

        if (validity.tooShort) {
            return "Nội dung phải có ít nhất " + element.minLength + " ký tự.";
        }

        if (validity.tooLong) {
            return "Nội dung không được vượt quá " + element.maxLength + " ký tự.";
        }

        if (validity.patternMismatch) {
            return "Giá trị đã nhập không đúng định dạng yêu cầu.";
        }

        return "Giá trị đã nhập không hợp lệ.";
    }

    document.addEventListener("invalid", function (event) {
        const element = event.target;
        if (!(element instanceof HTMLInputElement) &&
            !(element instanceof HTMLSelectElement) &&
            !(element instanceof HTMLTextAreaElement)) {
            return;
        }

        element.setCustomValidity("");
        element.setCustomValidity(getVietnameseMessage(element));
    }, true);

    ["input", "change"].forEach(function (eventName) {
        document.addEventListener(eventName, function (event) {
            const element = event.target;
            if (element && typeof element.setCustomValidity === "function") {
                element.setCustomValidity("");
            }
        }, true);
    });

    // Việt hóa thông báo của jQuery Validation nếu thư viện được dùng trên trang.
    document.addEventListener("DOMContentLoaded", function () {
        if (window.jQuery && window.jQuery.validator) {
            window.jQuery.extend(window.jQuery.validator.messages, {
                required: "Vui lòng nhập thông tin này.",
                remote: "Vui lòng sửa thông tin này.",
                email: "Vui lòng nhập địa chỉ email hợp lệ.",
                url: "Vui lòng nhập địa chỉ liên kết hợp lệ.",
                date: "Vui lòng nhập ngày hợp lệ.",
                dateISO: "Vui lòng nhập ngày hợp lệ theo định dạng năm-tháng-ngày.",
                number: "Vui lòng nhập số hợp lệ.",
                digits: "Vui lòng chỉ nhập chữ số.",
                equalTo: "Giá trị xác nhận chưa trùng khớp.",
                maxlength: window.jQuery.validator.format("Vui lòng không nhập quá {0} ký tự."),
                minlength: window.jQuery.validator.format("Vui lòng nhập ít nhất {0} ký tự."),
                rangelength: window.jQuery.validator.format("Vui lòng nhập từ {0} đến {1} ký tự."),
                range: window.jQuery.validator.format("Vui lòng nhập giá trị từ {0} đến {1}."),
                max: window.jQuery.validator.format("Vui lòng nhập giá trị nhỏ hơn hoặc bằng {0}."),
                min: window.jQuery.validator.format("Vui lòng nhập giá trị lớn hơn hoặc bằng {0}.")
            });
        }
    });
})();
