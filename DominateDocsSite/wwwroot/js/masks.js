window.masks = (function () {
    function onlyDigits(s) { return (s || '').replace(/\D/g, ''); }

    function formatSSN(v) {
        const d = onlyDigits(v).slice(0, 9);
        if (d.length <= 3) return d;
        if (d.length <= 5) return d.slice(0, 3) + '-' + d.slice(3);
        return d.slice(0, 3) + '-' + d.slice(3, 5) + '-' + d.slice(5, 9);
    }

    function formatEIN(v) {
        const d = onlyDigits(v).slice(0, 9);
        if (d.length <= 2) return d;
        return d.slice(0, 2) + '-' + d.slice(2, 9);
    }

    function formatPhone(v) {
        const d = onlyDigits(v).slice(0, 10);
        if (d.length === 0) return '';
        if (d.length <= 3) return '(' + d;
        if (d.length <= 6) return '(' + d.slice(0, 3) + ') ' + d.slice(3);
        return '(' + d.slice(0, 3) + ') ' + d.slice(3, 6) + '-' + d.slice(6, 10);
    }

    function isValidSSN(v) { return /^\d{3}-\d{2}-\d{4}$/.test(v); }
    function isValidEIN(v) { return /^\d{2}-\d{7}$/.test(v); }
    function isValidPhone(v) { return /^\(\d{3}\) \d{3}-\d{4}$/.test(v); }

    function setValidation(el, valid, allowEmpty) {
        if (allowEmpty && el.value === '') {
            el.style.outline = '';
            el.title = '';
            return;
        }
        if (valid) {
            el.style.outline = '';
            el.title = '';
        } else {
            el.style.outline = '2px solid #f44336';
            const m = el.getAttribute('data-mask');
            el.title = m === 'ssn' ? 'Format: 333-33-3333'
                : m === 'ein' ? 'Format: 34-3333333'
                    : m === 'phone' ? 'Format: (949) 500-7417'
                        : '';
        }
    }

    function attachMask(input) {
        if (!input || input.__mask_init) return;
        input.__mask_init = true;

        const mask = input.getAttribute('data-mask');
        const formatter = mask === 'ssn' ? formatSSN
            : mask === 'ein' ? formatEIN
                : mask === 'phone' ? formatPhone
                    : null;
        const validator = mask === 'ssn' ? isValidSSN
            : mask === 'ein' ? isValidEIN
                : mask === 'phone' ? isValidPhone
                    : null;
        if (!formatter) return;

        function applyFormat() {
            const start = input.selectionStart;
            const old = input.value;
            const next = formatter(old);
            if (old !== next) {
                input.value = next;
                const diff = next.length - old.length;
                try { input.setSelectionRange(start + diff, start + diff); } catch (e) { }
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
            if (validator) setValidation(input, validator(input.value), true);
        }

        input.addEventListener('input', applyFormat);
        input.addEventListener('paste', () => setTimeout(applyFormat, 0));
        input.addEventListener('blur', () => {
            if (validator) setValidation(input, validator(input.value), input.value === '');
        });

        applyFormat();
    }

    function scanAndAttach() {
        document.querySelectorAll('input').forEach(el => {
            const id = (el.id || '').toLowerCase();
            const placeholder = (el.placeholder || '').toLowerCase();
            const ariaLabel = (el.getAttribute('aria-label') || '').toLowerCase();
            const label = (el.closest('.mud-input-control')?.querySelector('label')?.innerText || '').toLowerCase();

            let mask = null;
            if (id.includes('ssn') || placeholder.includes('ssn') || ariaLabel.includes('ssn') || label.includes('ssn')) mask = 'ssn';
            else if (id.includes('ein') || placeholder.includes('ein') || ariaLabel.includes('ein') || label.includes('ein')) mask = 'ein';
            else if (id.includes('phone') || placeholder.includes('phone') || ariaLabel.includes('phone') || label.includes('phone')) mask = 'phone';

            if (mask) {
                if (!el.getAttribute('data-mask')) el.setAttribute('data-mask', mask);
                attachMask(el);
            }
        });
    }

    scanAndAttach();
    setInterval(scanAndAttach, 300);

    return {
        applyAll: scanAndAttach,
        validateAll: () => {
            let ok = true;
            document.querySelectorAll('input[data-mask]').forEach(el => {
                const m = el.getAttribute('data-mask');
                const v = m === 'ssn' ? isValidSSN : m === 'ein' ? isValidEIN : m === 'phone' ? isValidPhone : null;
                if (v && el.value !== '') {
                    const valid = v(el.value);
                    setValidation(el, valid, false);
                    if (!valid) ok = false;
                }
            });
            return ok;
        }
    };
})();
