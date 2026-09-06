(() => {
    document.querySelectorAll('.import-simple-category').forEach(container => {
        const select = container.querySelector('.reconciliation-category');
        const summary = container.querySelector('.import-category-budget');
        if (!select || !summary) return;
        const amounts = summary.querySelector('dl');
        const status = summary.querySelector('[data-budget-status]');
        const update = () => {
            const option = select.selectedOptions[0];
            const available = Boolean(select.value && option?.dataset.budgeted && option?.dataset.executed);
            amounts.hidden = !available;
            status.hidden = available;
            summary.querySelector('[data-budgeted-value]').textContent = available ? `${option.dataset.budgeted} €` : '';
            summary.querySelector('[data-executed-value]').textContent = available ? `${option.dataset.executed} €` : '';
            status.textContent = select.value ? 'Valores indisponíveis para esta categoria.' : 'Selecione uma categoria.';
        };
        select.addEventListener('change', update);
        update();
    });
})();
