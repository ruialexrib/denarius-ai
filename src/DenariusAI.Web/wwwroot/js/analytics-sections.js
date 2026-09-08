(() => {
    const input = document.querySelector('#analysisSearch');
    const cards = [...document.querySelectorAll('[data-analysis-card]')];
    const count = document.querySelector('[data-analysis-count]');
    const empty = document.querySelector('[data-analysis-empty]');
    const clear = document.querySelector('[data-analysis-search-clear]');

    if (!input || !cards.length) return;

    const normalize = value => value
        .toLocaleLowerCase('pt-PT')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .trim();

    const apply = () => {
        const term = normalize(input.value);
        let visible = 0;

        cards.forEach(card => {
            const haystack = normalize(`${card.dataset.search ?? ''} ${card.textContent ?? ''}`);
            const matches = !term || term.split(/\s+/).every(token => haystack.includes(token));
            card.hidden = !matches;
            if (matches) visible += 1;
        });

        if (count) count.textContent = `${visible} ${visible === 1 ? 'área' : 'áreas'}`;
        if (empty) empty.hidden = visible !== 0;
        if (clear) clear.hidden = !term;
    };

    input.addEventListener('input', apply);
    input.addEventListener('search', apply);
    clear?.addEventListener('click', () => {
        input.value = '';
        input.focus();
        apply();
    });
})();
