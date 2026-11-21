import { fetchApi, getSessionErrorMessage } from "./session.js";

const find = id => document.getElementById(id);
const eventId = window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/items\/?$/i)?.[1];
const api = `/api/events/${eventId}`;
const status = find("items-status");
const retry = find("items-retry");
const empty = find("items-empty");
const content = find("items-content");
const list = find("items-list");
const add = find("items-add");
const createDialog = find("item-create-dialog");
const detailsDialog = find("item-details-dialog");
const form = find("item-create-form");
const fields = find("item-fields");
const title = find("item-title");
const link = find("item-url");
const count = find("item-count");
const minus = find("item-count-minus");
const plus = find("item-count-plus");
const close = find("item-create-close");
const submit = find("item-create-submit");
const createError = find("item-create-error");

window.Telegram?.WebApp?.expand();

function showStatus(message, { error = false, canRetry = false } = {}) {
    status.textContent = message;
    status.hidden = !message;
    status.dataset.error = String(error);
    retry.hidden = !canRetry;
}

function getRequestError(response, fallback) {
    if (response.status === 401) return getSessionErrorMessage();
    if (response.status === 403) return "Чек-лист доступен участникам события.";
    if (response.status === 404) return "Событие не найдено.";
    return fallback;
}

function getLink(value) {
    try {
        const url = new URL(value);
        return url.protocol === "https:" || url.protocol === "http:" ? url.href : null;
    } catch {
        return null;
    }
}

function getResponsibleId(item) {
    return item.responsibleID ?? item.responsibleId;
}

async function getParticipantNames() {
    try {
        const response = await fetchApi(`${api}/participants`, { cache: "no-store" });
        if (response.ok) {
            const participants = await response.json();
            return new Map(participants.map(({ id, name }) => [id, name]));
        }
    } catch {
        // The list can still be shown if participant names are unavailable.
    }
    return new Map();
}

function openDetails(item, responsibleName) {
    find("item-details-title").textContent = item.title;
    find("item-details-count").textContent = `${item.count} шт.`;
    find("item-details-owner").textContent = responsibleName || "Свободно";

    const url = getLink(item.urlShop);
    const linkElement = find("item-details-link");
    find("item-details-no-link").hidden = Boolean(url);
    linkElement.hidden = !url;
    linkElement.textContent = url ?? "";
    linkElement.href = url ?? "";

    const description = find("item-details-description");
    description.textContent = item.description ?? "";
    description.hidden = !description.textContent.trim();
    find("item-details-description-label").hidden = description.hidden;
    detailsDialog.showModal();
}

function renderItem(item, responsibleName) {
    const card = find("item-card-template").content.firstElementChild.cloneNode(true);
    card.dataset.assigned = String(Boolean(responsibleName));
    card.querySelector("strong").textContent = item.title;
    card.querySelector("small").textContent = `${item.count} шт.`;
    const owner = card.querySelector(".item-owner");
    if (responsibleName) {
        owner.textContent = `✓ Берёт ${responsibleName}`;
        owner.hidden = false;
        card.querySelector(".item-take").hidden = true;
    }
    card.querySelector(".item-open").addEventListener("click", () => openDetails(item, responsibleName));
    return card;
}

async function loadItems({ afterCreate = false } = {}) {
    empty.hidden = true;
    content.hidden = true;
    showStatus("Загружаем чек-лист…");
    const fallback = afterCreate ? "Предмет создан. Не удалось обновить список." : "Не удалось загрузить чек-лист.";
    try {
        const response = await fetchApi(`${api}/items`, { cache: "no-store" });
        if (!response.ok) {
            showStatus(getRequestError(response, fallback), {
                error: true,
                canRetry: ![401, 403, 404].includes(response.status),
            });
            return;
        }

        const items = await response.json();
        const taken = items.filter(getResponsibleId).length;
        if (items.length) {
            const participants = taken ? await getParticipantNames() : new Map();
            list.replaceChildren(...items.map(item => {
                const id = getResponsibleId(item);
                return renderItem(item, id ? participants.get(id) || "Участник события" : null);
            }));
            const subject = items.length === 1 ? "предмета" : "предметов";
            find("items-progress-label").textContent = `Взято ${taken} из ${items.length} ${subject}`;
            find("items-progress-percent").textContent = `${Math.round(taken / items.length * 100)}%`;
            const progress = find("items-progress");
            progress.max = items.length;
            progress.value = taken;
        }
        empty.hidden = items.length !== 0;
        content.hidden = items.length === 0;
        showStatus("");
    } catch {
        showStatus(fallback, { error: true, canRetry: true });
    }
}

function updateStepper() {
    const value = count.valueAsNumber;
    minus.disabled = Number.isNaN(value) || value <= Number(count.min);
    plus.disabled = value >= Number(count.max);
}

function stepCount(change) {
    const value = count.valueAsNumber;
    const next = (Number.isFinite(value) ? Math.trunc(value) : 0) + change;
    count.value = String(Math.max(Number(count.min), Math.min(Number(count.max), next)));
    updateStepper();
}

function setSubmitting(saving) {
    fields.disabled = saving;
    close.disabled = saving;
    submit.disabled = saving;
    submit.textContent = saving ? "Создаём…" : "Создать";
}

function showCreateError(message) {
    createError.textContent = message;
    createError.hidden = !message;
}

for (const button of document.querySelectorAll("[data-add-item]")) {
    button.addEventListener("click", () => {
        form.reset();
        showCreateError("");
        updateStepper();
        createDialog.showModal();
    });
}

count.addEventListener("input", () => {
    if (count.validity.rangeUnderflow) {
        count.value = count.min;
    }
    updateStepper();
});
minus.addEventListener("click", () => stepCount(-1));
plus.addEventListener("click", () => stepCount(1));
close.addEventListener("click", () => createDialog.close());
createDialog.addEventListener("cancel", event => {
    if (submit.disabled) event.preventDefault();
});

form.addEventListener("submit", async event => {
    event.preventDefault();
    if (submit.disabled) return;

    const name = title.value.trim();
    if (!name) {
        showCreateError("Введите название предмета.");
        title.focus();
        return;
    }
    const url = link.value.trim();
    if (url && !getLink(url)) {
        showCreateError("Введите ссылку, начинающуюся с http:// или https://.");
        link.focus();
        return;
    }

    const item = {
        title: name,
        description: find("item-description").value.trim(),
        count: count.valueAsNumber,
        urlShop: url || null,
    };
    showCreateError("");
    setSubmitting(true);
    const fallback = "Не удалось создать предмет. Попробуйте ещё раз.";
    try {
        const response = await fetchApi(`${api}/items`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(item),
        });
        if (!response.ok) {
            showCreateError(getRequestError(response, fallback));
            return;
        }
        createDialog.close();
        await loadItems({ afterCreate: true });
        if (!content.hidden) add.focus();
        else if (!retry.hidden) retry.focus();
    } catch {
        showCreateError(fallback);
    } finally {
        setSubmitting(false);
    }
});

retry.addEventListener("click", () => void loadItems());
if (eventId) {
    find("items-back").href = `/html/event.html?id=${eventId}`;
    void loadItems();
} else {
    showStatus("Событие не выбрано. Вернитесь к списку событий.", { error: true });
}
