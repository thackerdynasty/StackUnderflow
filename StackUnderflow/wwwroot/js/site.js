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
    attachThreadPagination();
    attachAnswerSort();
    attachAnswerPagination();
    attachProfileCards();
});

// Re-applies the currently active home-page filter. Replaced with a real
// implementation by attachQuestionFilters; pagination calls it after appending
// freshly loaded cards so they participate in the active filter/sort.
let reapplyActiveQuestionFilter = () => {};

// -----------------------------
// Profile hover cards
// -----------------------------
function attachProfileCards() {
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

    // Delegate to the document so triggers added later (paginated threads,
    // "Load more" answers) get hover cards without re-binding listeners.
    const triggerFrom = (target) =>
        target instanceof Element ? target.closest('[data-profile-card-user-id]') : null;

    document.addEventListener('mouseover', (event) => {
        const trigger = triggerFrom(event.target);
        if (!trigger) return;
        if (trigger === activeTrigger && !card.hidden) {
            clearTimeout(hideTimer);
            return;
        }
        showCard(trigger);
    });

    document.addEventListener('mouseout', (event) => {
        const trigger = triggerFrom(event.target);
        if (!trigger || trigger.contains(event.relatedTarget)) return;
        scheduleHide();
    });

    document.addEventListener('focusin', (event) => {
        const trigger = triggerFrom(event.target);
        if (trigger) showCard(trigger);
    });

    document.addEventListener('focusout', (event) => {
        if (triggerFrom(event.target)) scheduleHide();
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
    const emptyState = document.querySelector('.question-filter-empty');
    if (filterButtons.length === 0) return;

    // Read the cards fresh each time so cards appended by "Load More" are included.
    const getQuestions = () => Array.from(questionList.querySelectorAll('.question-summary'));

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

    let activeFilterName = 'new';

    const applyFilter = (filterName) => {
        const selectedFilter = filters[filterName];
        if (!selectedFilter) return;
        activeFilterName = filterName;

        const questions = getQuestions();
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

    // Expose a re-apply hook for the pagination loader.
    reapplyActiveQuestionFilter = () => applyFilter(activeFilterName);

    const initialFilter = filterButtons.find((button) => button.classList.contains('active'));
    applyFilter(initialFilter?.dataset.questionFilter || 'new');
}

// -----------------------------
// Home page paginated thread loading (page-based)
// -----------------------------
function attachThreadPagination() {
    const questionList = document.querySelector('.question-list');
    const nav = document.querySelector('.question-pagination');
    if (!questionList || !nav) return;

    const pageSize = Number(nav.dataset.pageSize) || 20;
    const searchQuery = nav.dataset.search || '';
    const totalPages = Number(nav.dataset.totalPages) || 1;
    let currentPage = Number(nav.dataset.currentPage) || 1;
    let loading = false;

    if (totalPages <= 1) return;

    const buildCard = (thread) => {
        const score = (thread.upvoteCount || 0) - (thread.downvoteCount || 0);
        const answerCount = thread.answerCount || 0;
        const views = thread.viewCount || 0;

        const article = document.createElement('article');
        article.className = 'question-summary';
        article.dataset.createdAt = new Date(thread.createdAt).toISOString();
        article.dataset.recentAnswers = String(thread.recentAnswerCount || 0);
        article.dataset.views = String(views);
        article.dataset.upvotes = String(thread.upvoteCount || 0);

        const answerStateClass = thread.isSolved
            ? 'answered accepted'
            : answerCount > 0 ? 'answered' : '';

        const excerpt = (thread.content || '').length > 220
            ? `${thread.content.slice(0, 220)}...`
            : (thread.content || '');

        const askedDate = new Date(thread.createdAt).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
        });

        const detailUrl = `/Thread/${encodeURIComponent(thread.id)}`;
        const profileUrl = `/Profile/Details/${encodeURIComponent(thread.userId || '')}`;

        article.innerHTML = `
            <div class="question-stats" aria-label="Question stats">
                <div class="stat">
                    <strong>${score}</strong>
                    <span>votes</span>
                </div>
                <div class="stat answer-stat ${answerStateClass}">
                    <strong>${answerCount}</strong>
                    <span>${answerCount === 1 ? 'answer' : 'answers'}</span>
                </div>
                <div class="stat muted">
                    <strong>${views}</strong>
                    <span>views</span>
                </div>
            </div>
            <div class="question-content">
                <h2>
                    <a href="${detailUrl}"></a>
                </h2>
                <p></p>
                <div class="question-meta">
                    ${thread.isSolved ? '<span class="status-pill">Solved</span>' : ''}
                    <span>asked ${htmlEncode(askedDate)}</span>
                    <span>by <a href="${profileUrl}" data-profile-card-user-id="${htmlEncode(String(thread.userId || ''))}"></a></span>
                </div>
            </div>`;

        // Set user-controlled text via textContent to avoid HTML injection.
        article.querySelector('.question-content h2 a').textContent = thread.title || '';
        article.querySelector('.question-meta a').textContent = thread.authorName || 'unknown user';

        // Render fenced code blocks the same way the server does (ContentParser.RenderCodeBlocks).
        // renderCodeFencePreview HTML-escapes all non-code text, so this stays injection-safe.
        const excerptParagraph = article.querySelector('.question-content > p');
        excerptParagraph.innerHTML = renderCodeFencePreview(excerpt);
        excerptParagraph.querySelectorAll('pre code').forEach((block) => {
            try { if (window.hljs) hljs.highlightElement(block); } catch (e) { /* ignore */ }
        });

        return article;
    };

    // Build the list of page numbers to show, with ellipses around large gaps.
    const pageWindow = () => {
        const delta = 2;
        const left = Math.max(2, currentPage - delta);
        const right = Math.min(totalPages - 1, currentPage + delta);
        const pages = [1];
        if (left > 2) pages.push('…');
        for (let page = left; page <= right; page++) pages.push(page);
        if (right < totalPages - 1) pages.push('…');
        if (totalPages > 1) pages.push(totalPages);
        return pages;
    };

    const makeButton = (label, page, { disabled = false, active = false, ariaLabel } = {}) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'page-button' + (active ? ' active' : '');
        button.textContent = label;
        if (ariaLabel) button.setAttribute('aria-label', ariaLabel);
        if (active) button.setAttribute('aria-current', 'page');
        button.disabled = disabled || loading;
        if (!disabled && !active) {
            button.addEventListener('click', () => goToPage(page));
        }
        return button;
    };

    const renderControls = () => {
        nav.replaceChildren();
        nav.appendChild(makeButton('Previous', currentPage - 1, {
            disabled: currentPage <= 1,
            ariaLabel: 'Previous page'
        }));

        pageWindow().forEach((page) => {
            if (page === '…') {
                const ellipsis = document.createElement('span');
                ellipsis.className = 'page-ellipsis';
                ellipsis.textContent = '…';
                nav.appendChild(ellipsis);
                return;
            }
            nav.appendChild(makeButton(String(page), page, {
                active: page === currentPage,
                ariaLabel: `Page ${page}`
            }));
        });

        nav.appendChild(makeButton('Next', currentPage + 1, {
            disabled: currentPage >= totalPages,
            ariaLabel: 'Next page'
        }));
    };

    const goToPage = async (page) => {
        if (loading || page === currentPage || page < 1 || page > totalPages) return;
        loading = true;
        nav.classList.add('loading');
        renderControls();

        try {
            const searchParam = searchQuery ? `&search=${encodeURIComponent(searchQuery)}` : '';
            const response = await fetch(`/api/Thread/paginated?page=${page}&pageSize=${pageSize}${searchParam}`, {
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error(`Request failed: ${response.status}`);

            const result = await response.json();
            const threads = result.data || [];
            questionList.replaceChildren(...threads.map(buildCard));

            currentPage = result.currentPage || page;
            nav.dataset.currentPage = String(currentPage);

            // Let the freshly rendered cards participate in the active filter/sort.
            reapplyActiveQuestionFilter();

            const shell = document.querySelector('.questions-shell');
            if (shell) shell.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } catch (error) {
            console.error('Failed to load page', error);
        } finally {
            loading = false;
            nav.classList.remove('loading');
            renderControls();
        }
    };

    renderControls();
}

// -----------------------------
// Thread detail answer sorting
// -----------------------------
// Re-applies the current answer sort. Replaced by attachAnswerSort; the pagination
// loader calls it after appending freshly loaded answers so they sort into place.
let reapplyActiveAnswerSort = () => {};

function attachAnswerSort() {
    const sortSelect = document.querySelector('.answer-sort');
    const answerList = document.querySelector('.answer-list');
    if (!sortSelect || !answerList) return;

    // Read answers fresh each time so cards appended by "Load more" are included.
    const getAnswers = () => Array.from(answerList.querySelectorAll('.answer-post'));

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
        getAnswers().sort(compare).forEach((answer) => answerList.appendChild(answer));
    };

    sortSelect.addEventListener('change', sortAnswers);
    reapplyActiveAnswerSort = sortAnswers;
    sortAnswers();
}

// -----------------------------
// Thread detail answer pagination (Load more)
// -----------------------------
function attachAnswerPagination() {
    const answerList = document.querySelector('.answer-list');
    const button = document.querySelector('[data-load-more-answers]');
    if (!answerList || !button) return;

    const threadId = button.dataset.threadId;
    const pageSize = Number(button.dataset.pageSize) || 5;
    const totalPages = Number(button.dataset.totalPages) || 1;
    let currentPage = Number(button.dataset.currentPage) || 1;
    let loading = false;

    const loadNextPage = async () => {
        if (loading || currentPage >= totalPages) return;
        loading = true;
        button.disabled = true;
        const originalLabel = button.textContent;
        button.textContent = 'Loading…';

        try {
            const nextPage = currentPage + 1;
            const response = await fetch(
                `/Thread/${encodeURIComponent(threadId)}/Answers?page=${nextPage}&pageSize=${pageSize}`,
                { headers: { 'Accept': 'text/html' } }
            );
            if (!response.ok) throw new Error(`Request failed: ${response.status}`);

            // The endpoint returns rendered answer markup; parse and append it.
            const html = await response.text();
            const fragment = document.createElement('div');
            fragment.innerHTML = html;

            Array.from(fragment.querySelectorAll('.answer-post')).forEach((article) => {
                answerList.appendChild(article);
                article.querySelectorAll('pre code').forEach((block) => {
                    try { if (window.hljs) hljs.highlightElement(block); } catch (e) { /* ignore */ }
                });
            });

            currentPage = nextPage;
            button.dataset.currentPage = String(currentPage);

            // Let the freshly loaded answers participate in the active sort.
            reapplyActiveAnswerSort();

            if (currentPage >= totalPages) {
                (button.closest('.answers-pagination') || button).hidden = true;
            }
        } catch (error) {
            console.error('Failed to load more answers', error);
            button.textContent = 'Try again';
            button.disabled = false;
            loading = false;
            return;
        }

        button.textContent = originalLabel;
        button.disabled = false;
        loading = false;
    };

    button.addEventListener('click', loadNextPage);
}
