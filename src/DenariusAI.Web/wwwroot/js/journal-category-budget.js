(() => {
    const form = document.querySelector('#journal-entry-form');
    const budget = document.querySelector('#BudgetId');
    if (!form || !budget) return;

    const cache = new Map();
    let selectedBudgetId = '';
    let snapshot = null;
    let loading = false;
    let requestSequence = 0;

    const formatAmount = value => Number(value).toLocaleString('pt-PT', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function ensureSummary(select) {
        if (!select || select.dataset.budgetSummary === 'true') return;
        const summary = document.createElement('div');
        summary.className = 'category-budget-summary';
        summary.setAttribute('aria-live', 'polite');
        summary.setAttribute('aria-atomic', 'true');
        summary.innerHTML = '<dl hidden><div><dt>Orçamentado</dt><dd data-budgeted-value></dd></div><div><dt>Executado</dt><dd data-executed-value></dd></div></dl><small data-budget-status>Selecione uma categoria.</small>';
        const host = select.closest('.searchable-select') || select;
        host.insertAdjacentElement('afterend', summary);
        select.dataset.budgetSummary = 'true';
    }

    function summaryFor(select) {
        const host = select.closest('.searchable-select') || select;
        return host.nextElementSibling?.classList.contains('category-budget-summary') ? host.nextElementSibling : null;
    }

    function updateSummary(select) {
        ensureSummary(select);
        const summary = summaryFor(select);
        if (!summary) return;
        const amounts = summary.querySelector('dl');
        const status = summary.querySelector('[data-budget-status]');
        const categoryId = select.value?.toLowerCase();

        if (!categoryId) {
            amounts.hidden = true;
            status.hidden = false;
            status.textContent = 'Selecione uma categoria.';
            return;
        }
        if (!selectedBudgetId) {
            amounts.hidden = true;
            status.hidden = false;
            status.textContent = 'Selecione um orçamento para consultar os valores.';
            return;
        }
        if (loading) {
            amounts.hidden = true;
            status.hidden = false;
            status.textContent = 'A carregar valores do orçamento…';
            return;
        }
        if (!snapshot) {
            amounts.hidden = true;
            status.hidden = false;
            status.textContent = 'Valores indisponíveis para este orçamento.';
            return;
        }

        const execution = snapshot.get(categoryId);
        if (!execution) {
            amounts.hidden = true;
            status.hidden = false;
            status.textContent = 'Valores indisponíveis para esta categoria.';
            return;
        }

        summary.querySelector('[data-budgeted-value]').textContent = `${formatAmount(execution.budgeted)} €`;
        summary.querySelector('[data-executed-value]').textContent = `${formatAmount(execution.executed)} €`;
        amounts.hidden = false;
        status.hidden = true;
    }

    function updateAll() {
        form.querySelectorAll('.searchable-category').forEach(updateSummary);
    }

    async function loadBudgetSnapshot() {
        const budgetId = budget.value;
        selectedBudgetId = budgetId;
        requestSequence += 1;
        const requestId = requestSequence;

        if (!budgetId) {
            loading = false;
            snapshot = new Map();
            updateAll();
            return;
        }

        if (cache.has(budgetId)) {
            loading = false;
            snapshot = cache.get(budgetId);
            updateAll();
            return;
        }

        loading = true;
        snapshot = null;
        updateAll();
        try {
            const response = await fetch(`/JournalEntries/CategoryBudgetExecution?budgetId=${encodeURIComponent(budgetId)}`, {
                headers: { Accept: 'application/json' }
            });
            if (!response.ok) throw new Error(`Budget execution request failed with status ${response.status}.`);
            const items = await response.json();
            const nextSnapshot = new Map(items.map(item => [item.categoryId.toLowerCase(), item]));
            cache.set(budgetId, nextSnapshot);
            if (requestId !== requestSequence || selectedBudgetId !== budgetId) return;
            snapshot = nextSnapshot;
        } catch {
            if (requestId !== requestSequence || selectedBudgetId !== budgetId) return;
            snapshot = null;
        } finally {
            if (requestId === requestSequence && selectedBudgetId === budgetId) {
                loading = false;
                updateAll();
            }
        }
    }

    form.addEventListener('change', event => {
        if (event.target === budget) {
            loadBudgetSnapshot();
            return;
        }
        if (event.target.matches('.searchable-category')) updateSummary(event.target);
    });

    const observer = new MutationObserver(mutations => {
        const addedCategories = new Set();
        mutations.forEach(mutation => mutation.addedNodes.forEach(node => {
            if (!(node instanceof Element)) return;
            if (node.matches('.searchable-category')) addedCategories.add(node);
            node.querySelectorAll('.searchable-category').forEach(select => addedCategories.add(select));
        }));
        addedCategories.forEach(select => {
            ensureSummary(select);
            updateSummary(select);
        });
    });
    observer.observe(form, { childList: true, subtree: true });

    form.querySelectorAll('.searchable-category').forEach(ensureSummary);
    loadBudgetSnapshot();
})();
