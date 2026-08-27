(() => {
    const widget = document.querySelector('.asset-balances-widget');
    const toggle = widget?.querySelector('[data-asset-widget-toggle]');
    if (!widget || !toggle) return;

    const storageKey = 'denarius.asset-balances.minimized';
    const setMinimized = minimized => {
        widget.classList.toggle('is-minimized', minimized);
        toggle.setAttribute('aria-expanded', String(!minimized));
        toggle.setAttribute('aria-label', minimized ? 'Maximizar resumo patrimonial' : 'Minimizar resumo patrimonial');
        toggle.setAttribute('title', minimized ? 'Maximizar resumo patrimonial' : 'Minimizar resumo patrimonial');
        toggle.querySelector('span').textContent = minimized ? '+' : '−';
    };

    setMinimized(localStorage.getItem(storageKey) === 'true');
    toggle.addEventListener('click', () => {
        const minimized = !widget.classList.contains('is-minimized');
        setMinimized(minimized);
        localStorage.setItem(storageKey, String(minimized));
    });
})();
