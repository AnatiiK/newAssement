// Order line popup on the Orders grid: one modal used for both "Add line" and "Edit".
(function () {
    const modal = document.getElementById('lineModal');
    if (!modal) return;

    const form = document.getElementById('lineForm');
    const title = document.getElementById('lineModalTitle');
    const field = name => form.querySelector(`[name="Line.${name}"]`);

    function resetValidation() {
        const validator = $(form).data('validator');
        if (validator) validator.resetForm();
        form.querySelectorAll('.input-validation-error').forEach(el => el.classList.remove('input-validation-error'));
        form.querySelectorAll('.field-validation-error').forEach(el => {
            el.textContent = '';
            el.classList.replace('field-validation-error', 'field-validation-valid');
        });
    }

    modal.addEventListener('show.bs.modal', event => {
        const trigger = event.relatedTarget;
        const data = trigger && trigger.classList.contains('js-edit-line') ? trigger.dataset : null;

        resetValidation();
        title.textContent = data ? `Edit line ${data.lineNumber}` : 'Add line';
        field('LineNumber').value = data ? data.lineNumber : '0';
        field('ProductCode').value = data ? data.productCode : '';
        field('ProductType').value = data ? data.productType : '0';
        field('CostPrice').value = data ? data.costPrice : '';
        field('SalesPrice').value = data ? data.salesPrice : '';
        field('Quantity').value = data ? data.quantity : '1';
    });

    modal.addEventListener('shown.bs.modal', () => field('ProductCode').focus());
})();
