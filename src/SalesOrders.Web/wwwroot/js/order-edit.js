// Create / Edit order screen: add and remove line rows client-side.
// Rows use non-sequential "Lines.Index" keys, so removing a row never requires renaming the others.
(function () {
    const table = document.getElementById('linesTable');
    const tbody = table.querySelector('tbody');
    const template = document.getElementById('lineRowTemplate');
    const form = document.getElementById('orderForm');
    const firstNewLine = parseInt(table.dataset.nextLine, 10) || 1;
    let keyCounter = 0;

    // Existing lines keep their number; new lines (LineNumber 0) preview the number the
    // server will assign: continuing the sequence after the order's highest line.
    function renumber() {
        let next = firstNewLine;
        tbody.querySelectorAll('tr.line-row').forEach(row => {
            if (row.dataset.lineNumber === '0') {
                row.querySelector('.line-number').textContent = next++;
            }
        });
    }

    function reparseValidation() {
        $(form).removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse(form);
    }

    document.getElementById('addLine').addEventListener('click', () => {
        const key = 'n' + (++keyCounter) + '_' + Date.now();
        const html = template.innerHTML.replaceAll('__key__', key);
        tbody.insertAdjacentHTML('beforeend', html);
        const row = tbody.lastElementChild;
        row.querySelector('[name$=".Quantity"]').value = '1';
        renumber();
        reparseValidation();
        row.querySelector('[name$=".ProductCode"]').focus();
    });

    tbody.addEventListener('click', event => {
        const button = event.target.closest('.js-remove-line');
        if (!button) return;
        button.closest('tr').remove();
        renumber();
        reparseValidation();
    });

    renumber();
})();
