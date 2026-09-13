// Run with: node --test StackUnderflow.Tests/theme.test.mjs
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { runInNewContext } from 'node:vm';

const script = readFileSync(new URL('../StackUnderflow/wwwroot/js/theme.js', import.meta.url), 'utf8');

function createPage({ saved = null, dark = false, blockedStorage = false } = {}) {
    const documentEvents = {};
    const windowEvents = {};
    const attributes = {};
    let systemChange;
    let ready = false;
    const picker = {
        disabled: true,
        attributes: {},
        events: {},
        addEventListener(name, listener) { this.events[name] = listener; },
        setAttribute(name, value) { this.attributes[name] = value; }
    };
    const media = {
        matches: dark,
        addEventListener: (name, listener) => { systemChange = listener; }
    };
    const storage = {
        getItem: () => saved,
        setItem: (key, value) => { saved = value; }
    };
    const window = {
        matchMedia: () => media,
        addEventListener: (name, listener) => { windowEvents[name] = listener; },
        get localStorage() {
            if (blockedStorage) throw new Error('Storage unavailable');
            return storage;
        }
    };
    const document = {
        documentElement: { setAttribute: (name, value) => { attributes[name] = value; } },
        getElementById: () => ready ? picker : null,
        addEventListener: (name, listener) => { documentEvents[name] = listener; }
    };
    runInNewContext(script, { window, document });

    return {
        get theme() { return attributes['data-bs-theme']; },
        get saved() { return saved; },
        picker,
        mount() { ready = true; documentEvents.DOMContentLoaded(); },
        click() { picker.events.click(); },
        changeSystem(value) { media.matches = value; systemChange(); },
        changeStorage(value, key = 'stackunderflow.theme', storageArea = storage) {
            windowEvents.storage({ key, newValue: value, storageArea });
        }
    };
}

test('first paint follows the system before the picker exists', () => {
    for (const dark of [false, true]) {
        const page = createPage({ dark });
        assert.equal(page.theme, dark ? 'dark' : 'light');
        page.mount();
        assert.equal(page.picker.disabled, false);
    }
});

test('system changes update an open page in both directions', () => {
    const page = createPage();
    page.mount();
    page.changeSystem(true);
    assert.equal(page.theme, 'dark');
    page.changeSystem(false);
    assert.equal(page.theme, 'light');
});

test('saved overrides apply before first paint and ignore system changes', () => {
    for (const saved of ['light', 'dark']) {
        const page = createPage({ saved, dark: saved === 'light' });
        assert.equal(page.theme, saved);
        page.mount();
        page.changeSystem(false);
        page.changeSystem(true);
        assert.equal(page.theme, saved);
    }
});

test('one click switches to the opposite theme and repeated clicks toggle back', () => {
    for (const dark of [false, true]) {
        const page = createPage({ dark });
        page.mount();
        page.click();
        assert.equal(page.theme, dark ? 'light' : 'dark');
        assert.equal(page.saved, page.theme);
        const nextPage = createPage({ saved: page.saved, dark });
        assert.equal(nextPage.theme, page.theme);
        page.click();
        assert.equal(page.theme, dark ? 'dark' : 'light');
    }
});

test('click toggles the current theme after a system change', () => {
    const page = createPage();
    page.mount();
    page.changeSystem(true);
    page.click();
    assert.equal(page.theme, 'light');
    page.changeSystem(false);
    page.changeSystem(true);
    assert.equal(page.theme, 'light');
});

test('unavailable storage does not break the toggle', () => {
    const page = createPage({ blockedStorage: true, dark: true });
    page.mount();
    assert.equal(page.theme, 'dark');
    page.click();
    assert.equal(page.theme, 'light');
    page.click();
    assert.equal(page.theme, 'dark');
});

test('invalid saved values fall back to system appearance', () => {
    const page = createPage({ saved: 'invalid', dark: true });
    page.mount();
    assert.equal(page.theme, 'dark');
});

test('other tabs synchronize choices and clearing storage restores System', () => {
    const page = createPage({ dark: true });
    page.mount();
    page.changeStorage('light');
    assert.equal(page.theme, 'light');
    page.changeStorage(null);
    assert.equal(page.theme, 'dark');
    page.changeStorage('light');
    page.changeStorage(null, null);
    assert.equal(page.theme, 'dark');
});

test('unrelated storage events leave the preference unchanged', () => {
    const page = createPage({ saved: 'light' });
    page.mount();
    page.changeStorage('dark', 'unrelated');
    page.changeStorage('dark', 'stackunderflow.theme', {});
    assert.equal(page.theme, 'light');
});

test('icon button labels describe what clicking will do', () => {
    const page = createPage();
    page.mount();
    assert.equal(page.picker.attributes['aria-label'], 'Switch to dark mode');
    page.changeSystem(true);
    assert.equal(page.picker.attributes['aria-label'], 'Switch to light mode');
    page.click();
    assert.equal(page.picker.attributes['aria-label'], 'Switch to dark mode');
    assert.equal(page.picker.attributes.title, 'Switch to dark mode');
    page.click();
    assert.equal(page.picker.attributes['aria-label'], 'Switch to light mode');
});
