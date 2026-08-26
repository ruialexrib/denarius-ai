(() => {
    const form = document.querySelector('#journal-entry-form'); if (!form) return;
    const body = document.querySelector('#journal-lines'); const template = document.querySelector('#journal-line-template'); const save = document.querySelector('#save-journal');
    const number = value => Number.parseFloat(value) || 0; const format = value => value.toLocaleString('pt-PT', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function renumber() { [...body.querySelectorAll('.journal-line')].forEach((row, index) => row.querySelectorAll('[name]').forEach(input => input.name = input.name.replace(/Lines\[\d+\]/, `Lines[${index}]`))); }
    function update() {
        const rows = [...body.querySelectorAll('.journal-line')]; const debit = rows.reduce((sum, row) => sum + number(row.querySelector('.line-debit').value), 0); const credit = rows.reduce((sum, row) => sum + number(row.querySelector('.line-credit').value), 0); const difference = debit - credit;
        const validValues = rows.every(row => { const d = number(row.querySelector('.line-debit').value); const c = number(row.querySelector('.line-credit').value); return (d > 0) !== (c > 0); }); const accounts = rows.map(row => row.querySelector('.line-account').value).filter(Boolean); const balanced = Math.abs(difference) < 0.005 && debit > 0; const valid = rows.length >= 2 && accounts.length === rows.length && new Set(accounts).size >= 2 && validValues && balanced;
        document.querySelector('#total-debit').textContent = format(debit); document.querySelector('#total-credit').textContent = format(credit); document.querySelector('#journal-difference').textContent = format(difference); document.querySelector('#balance-state').textContent = balanced ? 'Movimento equilibrado.' : 'O movimento tem de ficar equilibrado.'; document.querySelector('.journal-balance').classList.toggle('balanced', balanced); save.disabled = !valid; body.querySelectorAll('.remove-line').forEach(button => button.disabled = rows.length <= 2);
    }
    document.querySelector('#add-line').addEventListener('click', () => { body.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', body.children.length)); update(); });
    body.addEventListener('click', event => { const button = event.target.closest('.remove-line'); if (!button || body.children.length <= 2) return; button.closest('.journal-line').remove(); renumber(); update(); });
    body.addEventListener('input', event => { if (event.target.matches('.line-debit') && number(event.target.value) > 0) event.target.closest('tr').querySelector('.line-credit').value = '0'; if (event.target.matches('.line-credit') && number(event.target.value) > 0) event.target.closest('tr').querySelector('.line-debit').value = '0'; update(); }); body.addEventListener('change', update); update();

    const simple = document.querySelector('.simple-entry-panel');
    if (!simple) return;
    const advanced = document.querySelector('#advanced-entry'); const amount = document.querySelector('#simple-amount'); const source = document.querySelector('#simple-source'); const destination = document.querySelector('#simple-destination'); const category = document.querySelector('#simple-category'); const categoryWrap = document.querySelector('#simple-category-wrap'); const destinationWrap = document.querySelector('#simple-destination-wrap'); const summary = document.querySelector('#simple-entry-summary');
    const catalogs = { expense: JSON.parse(document.querySelector('#expense-categories').textContent), income: JSON.parse(document.querySelector('#income-categories').textContent) };
    let type = 'expense'; let simpleMode = true;
    function optionList(items) { category.innerHTML = '<option value="">Selecionar categoria</option>' + items.map(item => `<option value="${item.value}">${item.text}</option>`).join(''); }
    function setRows(lines) {
        body.innerHTML = '';
        lines.forEach((line, index) => { body.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', index)); const row = body.lastElementChild; row.querySelector('.line-account').value = line.account; row.querySelector('[name$=".CategoryId"]').value = line.category || ''; row.querySelector('[name$=".Description"]').value = line.description || ''; row.querySelector('.line-debit').value = line.debit; row.querySelector('.line-credit').value = line.credit; });
        update();
    }
    function syncSimple() {
        if (!simpleMode) return;
        const value = number(amount.value); const account = source.value; const target = destination.value; const selectedCategory = category.value; const description = document.querySelector('#Description').value;
        if (type === 'expense') setRows([{ account: simple.dataset.expenseAccount, category: selectedCategory, description, debit: value, credit: 0 }, { account, description: 'Pagamento', debit: 0, credit: value }]);
        else if (type === 'income') setRows([{ account, description: 'Recebimento', debit: value, credit: 0 }, { account: simple.dataset.incomeAccount, category: selectedCategory, description, debit: 0, credit: value }]);
        else setRows([{ account: target, description: 'Conta de destino', debit: value, credit: 0 }, { account, description: 'Conta de origem', debit: 0, credit: value }]);
        const sourceName = source.selectedOptions[0]?.text || 'a conta selecionada'; const targetName = destination.selectedOptions[0]?.text || 'a conta de destino'; const categoryName = category.selectedOptions[0]?.text || 'a categoria selecionada';
        summary.textContent = value > 0 && account && (type === 'transfer' ? target && target !== account : selectedCategory) ? (type === 'expense' ? `Serão retirados ${format(value)} € de ${sourceName} e registados em ${categoryName}.` : type === 'income' ? `Serão recebidos ${format(value)} € em ${sourceName} e registados em ${categoryName}.` : `Serão transferidos ${format(value)} € de ${sourceName} para ${targetName}.`) : 'Preencha os campos apresentados para preparar o movimento.';
    }
    function selectType(nextType) {
        type = nextType; document.querySelectorAll('[data-entry-type]').forEach(button => button.classList.toggle('active', button.dataset.entryType === type));
        destinationWrap.hidden = type !== 'transfer'; categoryWrap.hidden = type === 'transfer'; document.querySelector('label[for="simple-source"]').textContent = type === 'expense' ? 'Pago com' : type === 'income' ? 'Recebido em' : 'Transferido de';
        if (type !== 'transfer') optionList(catalogs[type]);
        syncSimple();
    }
    document.querySelectorAll('[data-entry-type]').forEach(button => button.addEventListener('click', () => selectType(button.dataset.entryType)));
    [amount, source, destination, category, document.querySelector('#Description')].forEach(field => { field.addEventListener('input', syncSimple); field.addEventListener('change', syncSimple); });
    document.querySelector('#show-advanced').addEventListener('click', () => { simpleMode = false; simple.hidden = true; advanced.hidden = false; });
    document.querySelector('#show-simple').addEventListener('click', () => { simpleMode = true; advanced.hidden = true; simple.hidden = false; syncSimple(); });
    window.applySimpleJournalSuggestion = suggestion => {
        const expense = suggestion.lines.find(line => line.accountId.toLowerCase() === simple.dataset.expenseAccount.toLowerCase()); const income = suggestion.lines.find(line => line.accountId.toLowerCase() === simple.dataset.incomeAccount.toLowerCase());
        const nextType = expense ? 'expense' : income ? 'income' : 'transfer'; selectType(nextType);
        const value = Math.max(...suggestion.lines.map(line => Number(line.debit) || Number(line.credit) || 0)); amount.value = value;
        if (expense) { source.value = suggestion.lines.find(line => line !== expense)?.accountId || ''; category.value = expense.categoryId || ''; }
        else if (income) { source.value = suggestion.lines.find(line => line !== income)?.accountId || ''; category.value = income.categoryId || ''; }
        else { const debitLine = suggestion.lines.find(line => Number(line.debit) > 0); const creditLine = suggestion.lines.find(line => Number(line.credit) > 0); source.value = creditLine?.accountId || ''; destination.value = debitLine?.accountId || ''; }
        simpleMode = true; simple.hidden = false; advanced.hidden = true; syncSimple();
    };
    selectType('expense');
})();
