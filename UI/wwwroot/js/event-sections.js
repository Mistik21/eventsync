import { preloadEventSections } from "./event-section-preload.js";

const refreshAfter = 30_000;
const sections = new Map([
    ["items", { title: "Чек-лист", style: "/css/items.css", load: () => import("./items.js").then(module => module.mountItems) }],
    ["activities", { title: "Активности", style: "/css/activities.css?v=3", load: () => import("./activities.js?v=3").then(module => module.mountActivities) }],
    ["budget", { title: "Бюджет", style: "/css/budget.css", load: () => import("./budget.js").then(module => module.mountBudget) }],
]);

function loadStyle(href) {
    return new Promise((resolve, reject) => {
        const link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = href;
        link.addEventListener("load", resolve, { once: true });
        link.addEventListener("error", reject, { once: true });
        document.head.append(link);
    });
}

async function readSectionMarkup(name) {
    const response = await fetch(`/html/${name}.html`);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const page = new DOMParser().parseFromString(await response.text(), "text/html");
    if (!page.body.querySelector(`main.${name}-screen`)) throw new Error("Section markup is missing");
    return Array.from(page.body.querySelectorAll(":scope > :is(main, dialog, template)"));
}

function isPlainClick(click) {
    return !click.defaultPrevented && click.button === 0 && !click.metaKey && !click.ctrlKey && !click.shiftKey && !click.altKey;
}

export function mountEventSections(event, syncScreen, onBudgetDetails) {
    const host = document.getElementById("event-section-host");
    const placeholder = document.getElementById("event-section-placeholder");
    const title = document.getElementById("event-section-title");
    const status = document.getElementById("event-section-status");
    const fallback = document.getElementById("event-section-fallback");
    const views = new Map();
    const failed = new Set();
    const eventUrl = window.location.pathname + window.location.search;
    const eventTitle = document.title;
    const prepared = preloadEventSections(event);
    const nav = document.querySelector(".event-sections");
    let current;

    function standaloneUrl(name) {
        return `/event/${encodeURIComponent(event.id)}/${name}`;
    }

    function showActiveView() {
        const view = views.get(current);
        placeholder.hidden = Boolean(view);
        if (view) {
            view.element.hidden = false;
            const heading = view.element.querySelector("h1");
            if (heading) {
                heading.tabIndex = -1;
                heading.focus({ preventScroll: true });
            }
            if (Date.now() - view.lastRead >= refreshAfter) {
                view.lastRead = Date.now();
                view.refresh();
            }
            return;
        }

        title.textContent = sections.get(current).title;
        status.textContent = failed.has(current) ? "Не удалось открыть раздел." : "Загружаем раздел…";
        fallback.href = standaloneUrl(current);
        fallback.hidden = !failed.has(current);
        title.focus({ preventScroll: true });
    }

    function show(name) {
        const section = sections.get(name);
        const next = section ? name : undefined;
        if (current === next) return Boolean(section);

        const previous = views.get(current)?.element;
        if (previous) {
            for (const dialog of previous.querySelectorAll("dialog[open]")) dialog.close();
            previous.hidden = true;
        }

        current = next;
        host.hidden = !section;
        document.title = section ? `${section.title} · EventSync` : eventTitle;
        if (section) {
            window.scrollTo(0, 0);
            showActiveView();
        }
        return Boolean(section);
    }

    function back(click) {
        if (!isPlainClick(click)) return;
        click.preventDefault();
        if (window.history.state?.eventSection === current) {
            window.history.back();
        } else {
            window.history.replaceState(null, "", eventUrl);
            syncScreen();
        }
    }

    document.getElementById("event-section-back").href = eventUrl;
    document.getElementById("event-section-back").addEventListener("click", back);
    nav.addEventListener("click", click => {
        const link = click.target.closest("a[data-section]");
        if (!link || !sections.has(link.dataset.section) || !isPlainClick(click)) return;

        click.preventDefault();
        const name = link.dataset.section;
        window.history.pushState({ eventSection: name }, "", `${eventUrl}#${name}`);
        syncScreen();
    });

    async function prepare(name, section) {
        let view;
        try {
            const [markup, mount] = await Promise.all([readSectionMarkup(name), section.load(), loadStyle(section.style)]);
            view = document.createElement("div");
            view.hidden = true;
            view.append(...markup);
            host.append(view);
            const refresh = mount(view, event.id, prepared[name], onBudgetDetails);

            const link = view.querySelector(`#${name}-back`);
            link.href = eventUrl;
            link.addEventListener("click", back);
            views.set(name, { element: view, refresh, lastRead: Date.now() });
        } catch {
            view?.remove();
            failed.add(name);
        }

        if (current === name) showActiveView();
    }

    for (const [name, section] of sections) void prepare(name, section);
    return { show, get current() { return current; } };
}
