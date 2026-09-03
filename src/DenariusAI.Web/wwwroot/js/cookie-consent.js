(() => {
    const consent = document.querySelector('[data-cookie-consent]');
    if (!consent) return;

    const storageKey = 'denarius.cookieConsent.v1';
    let accepted = false;

    try {
        accepted = window.localStorage.getItem(storageKey) === 'accepted';
    } catch {
        accepted = false;
    }

    if (accepted) return;

    consent.hidden = false;
    const acceptButton = consent.querySelector('[data-cookie-consent-accept]');
    acceptButton?.addEventListener('click', () => {
        try {
            window.localStorage.setItem(storageKey, 'accepted');
        } catch {
            // If storage is unavailable, hide the notice for the current page only.
        }
        consent.hidden = true;
    });
})();
