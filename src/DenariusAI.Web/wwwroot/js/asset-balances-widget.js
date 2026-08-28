(() => {
    const widget = document.querySelector('.asset-balances-widget');
    const toggle = widget?.querySelector('[data-asset-widget-toggle]');
    if (!widget || !toggle) return;

    const storageKey = 'denarius.asset-balances.minimized';
    const positionKey = 'denarius.asset-balances.position.v2';
    const header = widget.querySelector('header');
    const margin = 8;

    const clampPosition = (left, top) => {
        const bounds = widget.getBoundingClientRect();
        return {
            left: Math.min(Math.max(margin, left), Math.max(margin, window.innerWidth - bounds.width - margin)),
            top: Math.min(Math.max(margin, top), Math.max(margin, window.innerHeight - bounds.height - margin))
        };
    };

    const applyPosition = (left, top, persist = false) => {
        const position = clampPosition(left, top);
        widget.style.left = `${position.left}px`;
        widget.style.top = `${position.top}px`;
        widget.style.right = 'auto';
        widget.style.bottom = 'auto';
        if (persist) localStorage.setItem(positionKey, JSON.stringify(position));
    };

    const restorePosition = () => {
        try {
            const position = JSON.parse(localStorage.getItem(positionKey));
            if (Number.isFinite(position?.left) && Number.isFinite(position?.top)) applyPosition(position.left, position.top);
        } catch { localStorage.removeItem(positionKey); }
    };

    const setMinimized = minimized => {
        widget.classList.toggle('is-minimized', minimized);
        toggle.setAttribute('aria-expanded', String(!minimized));
        toggle.setAttribute('aria-label', minimized ? 'Maximizar resumo patrimonial' : 'Minimizar resumo patrimonial');
        toggle.setAttribute('title', minimized ? 'Maximizar resumo patrimonial' : 'Minimizar resumo patrimonial');
        toggle.querySelector('span').textContent = minimized ? '+' : '−';
        requestAnimationFrame(() => {
            if (widget.style.left) applyPosition(parseFloat(widget.style.left), parseFloat(widget.style.top), true);
        });
    };

    setMinimized(localStorage.getItem(storageKey) === 'true');
    restorePosition();
    toggle.addEventListener('click', () => {
        const minimized = !widget.classList.contains('is-minimized');
        setMinimized(minimized);
        localStorage.setItem(storageKey, String(minimized));
    });

    header?.addEventListener('pointerdown', event => {
        if (event.button !== 0 || event.target.closest('button, form, a, input')) return;
        const bounds = widget.getBoundingClientRect();
        const offsetX = event.clientX - bounds.left;
        const offsetY = event.clientY - bounds.top;
        header.setPointerCapture(event.pointerId);
        widget.classList.add('is-dragging');

        const move = moveEvent => applyPosition(moveEvent.clientX - offsetX, moveEvent.clientY - offsetY);
        const stop = stopEvent => {
            header.releasePointerCapture(stopEvent.pointerId);
            header.removeEventListener('pointermove', move);
            header.removeEventListener('pointerup', stop);
            header.removeEventListener('pointercancel', stop);
            widget.classList.remove('is-dragging');
            const finalBounds = widget.getBoundingClientRect();
            applyPosition(finalBounds.left, finalBounds.top, true);
        };
        header.addEventListener('pointermove', move);
        header.addEventListener('pointerup', stop);
        header.addEventListener('pointercancel', stop);
    });

    window.addEventListener('resize', () => {
        if (!widget.style.left) return;
        applyPosition(parseFloat(widget.style.left), parseFloat(widget.style.top), true);
    });
})();
