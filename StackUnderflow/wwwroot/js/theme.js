// Run before styles load so the first paint uses the selected theme.
(() => {
    const storageKey = 'stackunderflow.theme';
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
    const normalize = (value) => value === 'light' || value === 'dark' ? value : 'system';
    let preference = 'system';

    try {
        preference = normalize(window.localStorage.getItem(storageKey));
    } catch {
        // The theme still works when the browser blocks storage.
    }

    const resolvedTheme = () => preference === 'system'
        ? (systemTheme.matches ? 'dark' : 'light')
        : preference;

    const applyTheme = () => {
        const theme = resolvedTheme();
        document.documentElement.setAttribute('data-bs-theme', theme);

        const picker = document.getElementById('theme-preference');
        if (picker) {
            const label = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
            picker.setAttribute('aria-label', label);
            picker.setAttribute('title', label);
        }
    };

    applyTheme();
    systemTheme.addEventListener('change', applyTheme);

    // Keep open tabs in sync, including when the saved preference is cleared.
    window.addEventListener('storage', (event) => {
        if (event.key !== storageKey && event.key !== null) return;
        try {
            if (event.storageArea !== window.localStorage) return;
        } catch {
            return;
        }
        preference = normalize(event.newValue);
        applyTheme();
    });

    document.addEventListener('DOMContentLoaded', () => {
        const picker = document.getElementById('theme-preference');
        if (!picker) return;

        applyTheme();
        picker.disabled = false;
        picker.addEventListener('click', () => {
            preference = resolvedTheme() === 'dark' ? 'light' : 'dark';
            applyTheme();
            try {
                window.localStorage.setItem(storageKey, preference);
            } catch {
                // Retain the selection for this page even without persistence.
            }
        });
    });
})();
