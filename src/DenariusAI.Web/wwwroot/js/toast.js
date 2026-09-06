(() => {
    const toasts = Array.from(document.querySelectorAll('[data-app-toast]'));

    const layoutToasts = () => {
        const baseTop = window.matchMedia('(max-width: 600px)').matches ? 70 : 82;
        let offset = baseTop;

        toasts.filter(toast => document.body.contains(toast)).forEach(toast => {
            toast.style.top = `${offset}px`;
            offset += toast.offsetHeight + 12;
        });
    };

    toasts.forEach((toast, index) => {
        const close = () => {
            toast.classList.add('leaving');
            window.setTimeout(() => {
                toast.remove();
                layoutToasts();
            }, 260);
        };

        toast.querySelector('[data-toast-close]')?.addEventListener('click', close);

        if (toast.dataset.toastPersistent !== 'true') {
            window.setTimeout(close, 5000 + (index * 250));
        }
    });

    layoutToasts();
    window.addEventListener('resize', layoutToasts);
})();
