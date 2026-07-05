// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Highlight all code blocks on the page. highlightAll will handle <pre><code> blocks
if (window.hljs) {
    hljs.highlightAll();

    // Additionally, ensure any code elements inside post-content are highlighted (defensive)
    document.querySelectorAll('div.post-content pre code').forEach((block) => {
        try { hljs.highlightElement(block); } catch (e) { /* ignore */ }
    });
}

// -----------------------------
// Live preview for answer textarea
// -----------------------------
function htmlEncode(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;')
              .replace(/</g, '&lt;')
              .replace(/>/g, '&gt;')
              .replace(/"/g, '&quot;')
              .replace(/'/g, '&#39;');
}

function renderCodeFencePreview(text) {
    if (text == null) return '';

    const fenceRegex = /```(?:([^\r\n]+)\r?\n)?([\s\S]*?)```/g;

    let lastIndex = 0;
    let out = '';
    let m;

    while ((m = fenceRegex.exec(text)) !== null) {
        // Append the plain text before this match, HTML-encoded
        const before = text.slice(lastIndex, m.index);
        out += htmlEncode(before);

        const lang = m[1] ? m[1].trim() : null;
        const code = m[2] || '';
        const encodedCode = htmlEncode(code);

        if (lang) {
            const cls = htmlEncode(lang);
            out += `<pre><code class="language-${cls}">${encodedCode}</code></pre>`;
        } else {
            out += `<pre><code>${encodedCode}</code></pre>`;
        }

        lastIndex = fenceRegex.lastIndex;
    }

    // Append remaining tail
    out += htmlEncode(text.slice(lastIndex));

    return out;
}

// Debounce helper
function debounce(fn, wait) {
    let t = null;
    return function(...args) {
        clearTimeout(t);
        t = setTimeout(() => fn.apply(this, args), wait);
    };
}

function attachLivePreview(textareaSelector, previewSelector) {
    const ta = document.querySelector(textareaSelector);
    const preview = document.querySelector(previewSelector);
    if (!ta || !preview) return;

    const update = () => {
        const rendered = renderCodeFencePreview(ta.value);
        preview.innerHTML = rendered;

        // highlight any code inside the preview
        preview.querySelectorAll('pre code').forEach((block) => {
            try { hljs.highlightElement(block); } catch (e) { /* ignore */ }
        });
    };

    const debounced = debounce(update, 150);
    ta.addEventListener('input', debounced);

    // initialize preview with current value (if any)
    update();
}

// Attach to the answer textarea preview
document.addEventListener('DOMContentLoaded', function() {
    attachLivePreview('#answer-content', '#answer-preview');
    attachLivePreview('#thread-content', '#thread-preview');
    attachQuestionFilters();
    attachAnswerSort();
    attachProfileCards();
});

// -----------------------------
// Profile hover cards
// -----------------------------
function attachProfileCards() {
    const triggers = Array.from(document.querySelectorAll('[data-profile-card-user-id]'));
    if (triggers.length === 0) return;

    const card = document.createElement('div');
    card.className = 'profile-hover-card';
    card.setAttribute('role', 'dialog');
    card.setAttribute('aria-label', 'Profile card');
    card.hidden = true;
    document.body.appendChild(card);

    const cache = new Map();
    let activeTrigger = null;
    let hideTimer = null;
    let requestId = 0;

    const positionCard = (trigger) => {
        const margin = 10;
        const rect = trigger.getBoundingClientRect();
        const cardRect = card.getBoundingClientRect();
        const viewportWidth = document.documentElement.clientWidth;
        const viewportHeight = document.documentElement.clientHeight;

        let left = rect.right + margin;
        let top = rect.top;

        if (left + cardRect.width + margin > viewportWidth) {
            left = rect.left - cardRect.width - margin;
        }

        left = Math.max(margin, Math.min(left, viewportWidth - cardRect.width - margin));
        top = Math.max(margin, Math.min(top, viewportHeight - cardRect.height - margin));

        card.style.left = `${left}px`;
        card.style.top = `${top}px`;
    };

    const showCard = async (trigger) => {
        const userId = trigger.dataset.profileCardUserId;
        if (!userId) return;

        activeTrigger = trigger;
        clearTimeout(hideTimer);
        const currentRequest = ++requestId;

        card.hidden = false;
        card.classList.add('is-loading');
        card.innerHTML = '<div class="profile-hover-card-loading">Loading profile...</div>';
        positionCard(trigger);

        try {
            if (!cache.has(userId)) {
                const response = await fetch(`/Profile/InfoCard/${encodeURIComponent(userId)}`, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    throw new Error('Profile card request failed.');
                }

                cache.set(userId, await response.text());
            }

            if (currentRequest !== requestId || activeTrigger !== trigger) return;

            card.classList.remove('is-loading');
            card.innerHTML = cache.get(userId);
            positionCard(trigger);
        } catch {
            if (currentRequest !== requestId || activeTrigger !== trigger) return;

            card.classList.remove('is-loading');
            card.innerHTML = '<div class="profile-hover-card-loading">Profile unavailable.</div>';
            positionCard(trigger);
        }
    };

    const scheduleHide = () => {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(() => {
            activeTrigger = null;
            card.hidden = true;
            card.innerHTML = '';
        }, 180);
    };

    triggers.forEach((trigger) => {
        trigger.addEventListener('mouseenter', () => showCard(trigger));
        trigger.addEventListener('focus', () => showCard(trigger));
        trigger.addEventListener('mouseleave', scheduleHide);
        trigger.addEventListener('blur', scheduleHide);
    });

    card.addEventListener('mouseenter', () => clearTimeout(hideTimer));
    card.addEventListener('mouseleave', scheduleHide);

    window.addEventListener('scroll', () => {
        if (!card.hidden && activeTrigger) {
            positionCard(activeTrigger);
        }
    }, { passive: true });

    window.addEventListener('resize', () => {
        if (!card.hidden && activeTrigger) {
            positionCard(activeTrigger);
        }
    });
}

// -----------------------------
// Home page question filters
// -----------------------------
function attachQuestionFilters() {
    const filterBar = document.querySelector('.question-tabs');
    const questionList = document.querySelector('.question-list');
    if (!filterBar || !questionList) return;

    const filterButtons = Array.from(filterBar.querySelectorAll('[data-question-filter]'));
    const questions = Array.from(questionList.querySelectorAll('.question-summary'));
    const emptyState = document.querySelector('.question-filter-empty');
    if (filterButtons.length === 0 || questions.length === 0) return;

    const numberFromData = (question, name) => Number(question.dataset[name]) || 0;
    const createdAt = (question) => Date.parse(question.dataset.createdAt) || 0;
    const newestFirst = (left, right) => createdAt(right) - createdAt(left);

    const filters = {
        new: {
            includes: () => true,
            compare: newestFirst
        },
        trending: {
            includes: (question) => numberFromData(question, 'recentAnswers') > 0,
            compare: (left, right) =>
                numberFromData(right, 'recentAnswers') - numberFromData(left, 'recentAnswers')
                || numberFromData(right, 'upvotes') - numberFromData(left, 'upvotes')
                || newestFirst(left, right)
        },
        viewed: {
            includes: () => true,
            compare: (left, right) =>
                numberFromData(right, 'views') - numberFromData(left, 'views')
                || newestFirst(left, right)
        },
        upvoted: {
            includes: () => true,
            compare: (left, right) =>
                numberFromData(right, 'upvotes') - numberFromData(left, 'upvotes')
                || newestFirst(left, right)
        }
    };

    const applyFilter = (filterName) => {
        const selectedFilter = filters[filterName];
        if (!selectedFilter) return;

        const visibleQuestions = questions.filter(selectedFilter.includes).sort(selectedFilter.compare);
        const visibleSet = new Set(visibleQuestions);

        questions.forEach((question) => {
            question.hidden = !visibleSet.has(question);
        });
        visibleQuestions.forEach((question) => questionList.appendChild(question));

        if (emptyState) {
            emptyState.hidden = visibleQuestions.length !== 0;
        }
        questionList.hidden = visibleQuestions.length === 0;

        filterButtons.forEach((button) => {
            const isActive = button.dataset.questionFilter === filterName;
            button.classList.toggle('active', isActive);
            button.setAttribute('aria-pressed', String(isActive));
        });
    };

    filterBar.addEventListener('click', (event) => {
        const button = event.target.closest('[data-question-filter]');
        if (!button || !filterBar.contains(button)) return;

        applyFilter(button.dataset.questionFilter);
    });

    const initialFilter = filterButtons.find((button) => button.classList.contains('active'));
    applyFilter(initialFilter?.dataset.questionFilter || 'new');
}

// -----------------------------
// Thread detail answer sorting
// -----------------------------
function attachAnswerSort() {
    const sortSelect = document.querySelector('.answer-sort');
    const answerList = document.querySelector('.answer-list');
    if (!sortSelect || !answerList) return;

    const answers = Array.from(answerList.querySelectorAll('.answer-post'));
    if (answers.length === 0) return;

    const score = (answer) => Number(answer.dataset.answerScore) || 0;
    const createdAt = (answer) => Date.parse(answer.dataset.answerCreatedAt) || 0;
    const isAccepted = (answer) => answer.dataset.answerAccepted === 'true';
    const acceptedFirst = (left, right) => Number(isAccepted(right)) - Number(isAccepted(left));

    const comparators = {
        score: (left, right) =>
            acceptedFirst(left, right)
            || score(right) - score(left)
            || createdAt(left) - createdAt(right),
        newest: (left, right) =>
            acceptedFirst(left, right)
            || createdAt(right) - createdAt(left),
        oldest: (left, right) =>
            acceptedFirst(left, right)
            || createdAt(left) - createdAt(right)
    };

    const sortAnswers = () => {
        const compare = comparators[sortSelect.value] || comparators.score;
        answers.sort(compare).forEach((answer) => answerList.appendChild(answer));
    };

    sortSelect.addEventListener('change', sortAnswers);
    sortAnswers();
}
