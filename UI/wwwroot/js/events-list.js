import { fetchApi, getSessionErrorMessage } from "./session.js";
import { lookupAddress, parseCoordinates } from "./two-gis.js";

const dateFormat = new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "long",
    timeStyle: "short",
});

export function canDisplayCover(value) {
    if (!value) {
        return false;
    }
    if (/^data:image\/(jpeg|png|webp|gif|avif);base64,/i.test(value)) {
        return true;
    }
    try {
        const { protocol } = new URL(value, window.location.href);
        return protocol === "https:" || protocol === "http:";
    } catch {
        return false;
    }
}

function displayLocation(card, event) {
    const address = card.querySelector(".event-card-address");
    address.textContent = event.eventLocation.address?.trim() || event.resolvedAddress ||
        (event.point ? (event.resolvedAddress === undefined ? "Определяем адрес…" : "Не удалось определить адрес")
            : "Место не указано");
}

function renderCard(template, event) {
    const card = template.content.cloneNode(true);
    card.querySelector(".event-card-summary").href = `/html/event.html?id=${encodeURIComponent(event.id)}`;
    const cover = card.querySelector(".event-card-cover");
    if (canDisplayCover(event.mainImageUrl)) {
        cover.addEventListener("error", () => { cover.hidden = true; }, { once: true });
        cover.src = event.mainImageUrl;
        cover.hidden = false;
    }

    card.querySelector(".event-card-title").textContent = event.title;
    const role = card.querySelector(".event-card-role");
    role.textContent = event.isOrganizer ? "Вы организатор" : "Вы участник";
    role.classList.toggle("event-card-role--organizer", event.isOrganizer);

    const date = card.querySelector(".event-card-date");
    date.dateTime = event.eventStartTime;
    date.textContent = dateFormat.format(event.start);

    displayLocation(card, event);
    return card;
}

function showEvents(events) {
    const groups = [...document.querySelectorAll("[data-group]")];
    const list = document.querySelector(".event-list");
    const empty = document.querySelector(".empty-events");
    const emptyTitle = empty.querySelector("h2");
    const emptyHint = empty.querySelector("p");
    const search = document.querySelector(".event-search input");
    const filter = document.querySelector(".event-filter");
    const order = document.querySelector("#event-order");
    const template = document.querySelector("#event-card-template");
    const emptyTitles = {
        active: "Нет активных событий",
        organizing: "Нет событий, которые вы организуете",
        archive: "В архиве пока пусто",
    };
    const observedEvents = new WeakMap();
    const addressObserver = new IntersectionObserver(entries => {
        for (const { target, isIntersecting } of entries) {
            if (!isIntersecting || !target.isConnected) {
                continue;
            }
            addressObserver.unobserve(target);
            const event = observedEvents.get(target);
            event.addressLookup ??= lookupAddress(event.point).catch(() => "");
            void event.addressLookup.then(address => {
                event.resolvedAddress = address;
                displayLocation(target, event);
            });
        }
    });
    let selected = "active";

    function render() {
        const now = Date.now();
        const query = search.value.trim().toLocaleLowerCase("ru");
        const active = events.filter(event => event.start >= now);
        const collections = {
            active,
            organizing: active.filter(event => event.isOrganizer),
            archive: events.filter(event => event.start < now),
        };
        groups[0].textContent = `Активные · ${collections.active.length}`;
        groups[1].textContent = `Я организую · ${collections.organizing.length}`;

        const visible = collections[selected].filter(event =>
            [event.title, event.eventLocation.address, event.resolvedAddress ?? ""]
                .some(value => value.toLocaleLowerCase("ru").includes(query)));
        const direction = order.value === "ascending" ? 1 : -1;
        visible.sort((first, second) => direction * (first.start - second.start));

        addressObserver.disconnect();
        list.replaceChildren(...visible.map(event => {
            const card = renderCard(template, event);
            if (event.point && event.resolvedAddress === undefined) {
                observedEvents.set(card.firstElementChild, event);
                addressObserver.observe(card.firstElementChild);
            }
            return card;
        }));
        list.hidden = visible.length === 0;
        empty.hidden = !list.hidden;
        emptyHint.hidden = events.length > 0 || Boolean(query);
        emptyTitle.textContent = query
            ? "Событий не найдено"
            : events.length ? emptyTitles[selected] : "Событий пока нет";
    }

    groups.forEach(button => button.addEventListener("click", () => {
        selected = button.dataset.group;
        groups.forEach(group => group.setAttribute("aria-pressed", String(group === button)));
        order.value = selected === "archive" ? "descending" : "ascending";
        render();
    }));
    search.addEventListener("input", render);
    order.addEventListener("change", () => {
        filter.open = false;
        render();
    });
    search.disabled = false;
    filter.hidden = false;
    render();
}

export async function loadMyEvents() {
    const status = document.querySelector(".events-status");
    try {
        const response = await fetchApi("/api/events", { cache: "no-store" });
        if (response.status === 401) {
            status.textContent = getSessionErrorMessage();
            return;
        }
        if (!response.ok) {
            throw new Error("Не удалось получить события.");
        }
        const events = await response.json();
        showEvents(events.map(event => ({
            ...event,
            start: new Date(event.eventStartTime),
            point: event.eventLocation.address?.trim() ? null : parseCoordinates(event.eventLocation.coordinates),
        })));
        status.hidden = true;
    } catch {
        status.textContent = "Не удалось загрузить события. Обновите страницу.";
    }
}
