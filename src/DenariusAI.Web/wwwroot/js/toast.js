(() => {
    document.querySelectorAll('[data-app-toast]').forEach((toast, index) => {
        const close = () => { toast.classList.add('leaving'); window.setTimeout(() => toast.remove(), 260); };
        toast.querySelector('[data-toast-close]')?.addEventListener('click', close);
        window.setTimeout(close, 5000 + (index * 250));
    });
})();
