(() => {
    function enhance(select) {
        if (!select || select.dataset.searchEnhanced === 'true' || select.disabled) return;
        select.dataset.searchEnhanced = 'true';
        const wrapper = document.createElement('div'); wrapper.className = 'searchable-select';
        const input = document.createElement('input'); input.type = 'search'; input.autocomplete = 'off'; input.className = 'searchable-select-input'; input.placeholder = select.dataset.searchPlaceholder || 'Procurar…'; input.setAttribute('role', 'combobox'); input.setAttribute('aria-autocomplete', 'list'); input.setAttribute('aria-expanded', 'false');
        const list = document.createElement('div'); list.className = 'searchable-select-list'; list.setAttribute('role', 'listbox'); list.hidden = true;
        select.parentNode.insertBefore(wrapper, select); wrapper.append(input, select); document.body.append(list); select.classList.add('searchable-select-native');
        let matches = []; let active = -1;
        const selectedText = () => select.selectedOptions[0]?.value ? select.selectedOptions[0].text.trim() : '';
        const close = () => { list.hidden = true; input.setAttribute('aria-expanded', 'false'); active = -1; };
        function position() { const rect = input.getBoundingClientRect(); const below = window.innerHeight - rect.bottom; const above = below < 240 && rect.top > below; list.style.left = `${rect.left}px`; list.style.width = `${rect.width}px`; list.style.top = above ? 'auto' : `${rect.bottom + 6}px`; list.style.bottom = above ? `${window.innerHeight - rect.top + 6}px` : 'auto'; }
        function choose(option) { select.value = option.value; input.value = option.text.trim(); select.dispatchEvent(new Event('change', { bubbles: true })); close(); }
        function render(query = '') {
            const term = query.trim().toLocaleLowerCase('pt-PT'); const includeEmpty = select.dataset.searchIncludeEmpty === 'true'; matches = [...select.options].filter(option => (option.value || (includeEmpty && !term)) && (!term || option.text.toLocaleLowerCase('pt-PT').includes(term))); list.replaceChildren(); active = -1;
            if (!matches.length) { const empty = document.createElement('span'); empty.className = 'searchable-select-empty'; empty.textContent = select.dataset.searchEmptyText || 'Nenhum resultado encontrado'; list.append(empty); }
            matches.forEach(option => { const button = document.createElement('button'); button.type = 'button'; button.setAttribute('role', 'option'); button.textContent = option.text.trim(); button.addEventListener('mousedown', event => { event.preventDefault(); choose(option); }); list.append(button); });
            position(); list.hidden = false; input.setAttribute('aria-expanded', 'true');
        }
        function setActive(next) { if (!matches.length) return; active = (next + matches.length) % matches.length; [...list.querySelectorAll('button')].forEach((button, index) => button.classList.toggle('active', index === active)); list.querySelectorAll('button')[active]?.scrollIntoView({ block: 'nearest' }); }
        input.value = selectedText(); input.addEventListener('focus', () => { input.select(); render(''); }); input.addEventListener('input', () => render(input.value));
        input.addEventListener('keydown', event => { if (event.key === 'ArrowDown') { event.preventDefault(); if (list.hidden) render(input.value); setActive(active + 1); } else if (event.key === 'ArrowUp') { event.preventDefault(); setActive(active - 1); } else if (event.key === 'Enter' && active >= 0) { event.preventDefault(); choose(matches[active]); } else if (event.key === 'Escape') { close(); input.value = selectedText(); } });
        input.addEventListener('blur', () => setTimeout(() => { close(); input.value = selectedText(); }, 100)); window.addEventListener('resize', close); document.querySelector('.content')?.addEventListener('scroll', close, { passive: true });
    }
    document.querySelectorAll('.searchable-select-control, .reconciliation-category').forEach(enhance);
})();
