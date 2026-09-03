(() => {
    const storageKey = 'denarius.cookieConsent.v1';

    const initialise = () => {
        const consent = document.querySelector('[data-cookie-consent]');
        if (!consent) return;

        let accepted = false;
        try {
            accepted = window.localStorage.getItem(storageKey) === 'accepted';
        } catch {
            accepted = false;
        }

        if (accepted) {
            consent.hidden = true;
            return;
        }

        consent.hidden = false;

        const acceptButton = consent.querySelector('[data-cookie-consent-accept]');
        if (!acceptButton) return;

        acceptButton.addEventListener('click', (event) => {
            event.preventDefault();

            try {
                window.localStorage.setItem(storageKey, 'accepted');
            } catch {
                // Storage can be unavailable in restricted browser modes.
            }

            consent.hidden = true;
            consent.style.display = 'none';
        });
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialise, { once: true });
    } else {
        initialise();
    }
})();
