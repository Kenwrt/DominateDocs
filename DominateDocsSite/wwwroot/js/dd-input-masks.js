// wwwroot/js/dd-input-masks.js
// Reusable input helpers for Blazor Server: phone, SSN, email
// Attaches listeners once, formats values, and triggers an "input" event so Blazor updates bound model.

window.DDInputMasks = window.DDInputMasks || (function () {

    function digitsOnly(s) {
        return (s || "").toString().replace(/\D/g, "");
    }

    function setValueAndNotify(el, value) {
        if (!el) return;
        if (el.value !== value) {
            el.value = value;
            el.dispatchEvent(new Event("input", { bubbles: true }));
        }
    }

    // -------------------
    // PHONE: (###) ###-####
    // -------------------
    function formatPhone(value) {
        const d = digitsOnly(value).slice(0, 10);
        if (d.length === 0) return "";
        if (d.length < 4) return "(" + d;
        if (d.length < 7) return "(" + d.slice(0, 3) + ") " + d.slice(3);
        return "(" + d.slice(0, 3) + ") " + d.slice(3, 6) + "-" + d.slice(6);
    }

    function attachPhone(el) {
        if (!el) return;

        if (el.dataset && el.dataset.ddPhoneAttached === "1") {
            // Still reformat on attach in case value came from DB after render
            setValueAndNotify(el, formatPhone(el.value));
            return;
        }
        if (el.dataset) el.dataset.ddPhoneAttached = "1";

        // Format existing DB value
        setValueAndNotify(el, formatPhone(el.value));

        let isFormatting = false;
        el.addEventListener("input", function () {
            if (isFormatting) return;
            isFormatting = true;

            const formatted = formatPhone(el.value);
            setValueAndNotify(el, formatted);

            isFormatting = false;
        });
    }

    // -------------------
    // SSN: ###-##-####
    // -------------------
    function formatSsn(value) {
        const d = digitsOnly(value).slice(0, 9);
        if (d.length === 0) return "";
        if (d.length <= 3) return d;
        if (d.length <= 5) return d.slice(0, 3) + "-" + d.slice(3);
        return d.slice(0, 3) + "-" + d.slice(3, 5) + "-" + d.slice(5);
    }

    function attachSsn(el) {
        if (!el) return;

        if (el.dataset && el.dataset.ddSsnAttached === "1") {
            setValueAndNotify(el, formatSsn(el.value));
            return;
        }
        if (el.dataset) el.dataset.ddSsnAttached = "1";

        setValueAndNotify(el, formatSsn(el.value));

        let isFormatting = false;
        el.addEventListener("input", function () {
            if (isFormatting) return;
            isFormatting = true;

            const formatted = formatSsn(el.value);
            setValueAndNotify(el, formatted);

            isFormatting = false;
        });
    }

    // -------------------
    // EMAIL: lowercase + trim (no aggressive "masking")
    // -------------------
    function normalizeEmail(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function attachEmail(el) {
        if (!el) return;

        if (el.dataset && el.dataset.ddEmailAttached === "1") {
            setValueAndNotify(el, normalizeEmail(el.value));
            return;
        }
        if (el.dataset) el.dataset.ddEmailAttached = "1";

        setValueAndNotify(el, normalizeEmail(el.value));

        let isNormalizing = false;
        el.addEventListener("blur", function () {
            if (isNormalizing) return;
            isNormalizing = true;

            const normalized = normalizeEmail(el.value);
            // blur-based normalization so we don't fight the user's typing
            setValueAndNotify(el, normalized);

            isNormalizing = false;
        });
    }

    return {
        attachPhone: attachPhone,
        attachSsn: attachSsn,
        attachEmail: attachEmail,
        formatPhone: formatPhone,
        formatSsn: formatSsn,
        normalizeEmail: normalizeEmail
    };
})();
