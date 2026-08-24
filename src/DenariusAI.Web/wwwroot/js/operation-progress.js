(() => {
  const overlay = document.getElementById('operation-progress');
  if (!overlay) return;
  const message = overlay.querySelector('[data-progress-text]');
  const showProgress = text => {
    message.textContent = text;
    overlay.hidden = false;
    document.body.classList.add('operation-pending');
  };

  document.addEventListener('click', event => {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    const link = event.target.closest('.sidebar a.nav-item, .user-dropdown > a, .brand-area a.brand');
    if (!(link instanceof HTMLAnchorElement) || link.target === '_blank' || link.hasAttribute('download') || link.dataset.noProgress === 'true') return;
    const destination = new URL(link.href, window.location.href);
    if (destination.origin !== window.location.origin || (destination.pathname === window.location.pathname && destination.search === window.location.search && destination.hash)) return;
    const label = link.textContent.replace(/\s+/g, ' ').trim();
    showProgress(label ? `A abrir ${label}…` : 'A carregar a página…');
  });

  document.addEventListener('submit', event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.dataset.noProgress === 'true' || event.defaultPrevented) return;
    if (!form.checkValidity()) return;
    const submitter = event.submitter;
    if (submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement) {
      if (submitter.name) {
        const submittedValue = document.createElement('input');
        submittedValue.type = 'hidden';
        submittedValue.name = submitter.name;
        submittedValue.value = submitter.value;
        form.append(submittedValue);
      }
      submitter.disabled = true;
    }
    showProgress(form.dataset.progressMessage || submitter?.dataset.progressMessage || 'A executar a operação…');
  });
  window.addEventListener('pageshow', () => {
    overlay.hidden = true;
    document.body.classList.remove('operation-pending');
  });
})();
