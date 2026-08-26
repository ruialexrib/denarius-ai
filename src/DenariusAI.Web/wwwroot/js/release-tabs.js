(() => {
    const tabs = [...document.querySelectorAll('[data-release-tab]')];
    const panels = [...document.querySelectorAll('[data-release-panel]')];
    if (!tabs.length) return;

    const select = (selectedIndex, focus = false) => {
        tabs.forEach((tab, index) => {
            const active = index === selectedIndex;
            tab.setAttribute('aria-selected', active.toString());
            tab.tabIndex = active ? 0 : -1;
            panels[index].hidden = !active;
        });
        if (focus) tabs[selectedIndex].focus();
    };

    tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => select(index));
        tab.addEventListener('keydown', event => {
            if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
            event.preventDefault();
            const target = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 :
                (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
            select(target, true);
        });
    });
})();
