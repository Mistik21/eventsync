import { fetchCurrentUser } from "./current-user.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";
import { takePreloadedEventSection } from "./event-section-preload.js";

const find = id => document.getElementById(id);
const eventId = window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/items\/?$/i)?.[1];
const api = `/api/events/${eventId}`;
const itemsApi = `${api}/items`;
const itemUrl = itemId => `${itemsApi}/${encodeURIComponent(itemId)}`;
const status = find("items-status");
const retry = find("items-retry");
const empty = find("items-empty");
const content = find("items-content");
const list = find("items-list");
const add = find("items-add");
const formDialog = find("item-form-dialog");
const detailsDialog = find("item-details-dialog");
const deleteDialog = find("item-delete-dialog");
const form = find("item-form");
const fields = find("item-fields");
const title = find("item-title");
const link = find("item-url");
const count = find("item-count");
const notes = find("item-description");
const minus = find("item-count-minus");
const plus = find("item-count-plus");
const close = find("item-form-close");
const submit = find("item-form-submit");
const formError = find("item-form-error");
const editButton = find("item-edit");
const deleteButton = find("item-delete");
const deleteCancel = find("item-delete-cancel");
const deleteConfirm = find("item-delete-confirm");
const deleteError = find("item-delete-error");
let detailsItem;
let editingItem;

window.Telegram?.WebApp?.expand();

function showStatus(message, { error = false, canRetry = false } = {}) {
    status.textContent = message;
    status.hidden = !message;
    status.dataset.error = String(error);
    retry.hidden = !canRetry;
}

function showDialogError(element, message) {
    element.textContent = message;
    element.hidden = !message;
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
    detailsItem = item;
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

function renderItem(item, participants, currentUserId) {
    const responsibleId = getResponsibleId(item);
    const isMine = Boolean(responsibleId && responsibleId === currentUserId);
    const responsibleName = responsibleId ? participants.get(responsibleId) || "Участник события" : null;
    const card = find("item-card-template").content.firstElementChild.cloneNode(true);
    card.dataset.itemId = item.id;
    card.dataset.assigned = String(Boolean(responsibleId));
    card.querySelector("strong").textContent = item.title;
    card.querySelector("small").textContent = `${item.count} шт.`;
    const owner = card.querySelector(".item-owner");
    if (responsibleId && !isMine) {
        owner.textContent = `✓ Берёт ${responsibleName}`;
        owner.hidden = false;
    }
    if (!responsibleId || isMine) {
        const button = card.querySelector(".item-action");
        const action = isMine ? "unassign" : "assign";
        button.dataset.action = action;
        button.textContent = isMine ? "Не принесу" : "Взять";
        button.setAttribute("aria-label", `${button.textContent}: ${item.title}`);
        button.hidden = false;
        button.addEventListener("click", () => void changeAssignment(item.id, action));
    }
    card.querySelector(".item-open").addEventListener("click", () => openDetails(item, isMine ? "Вы" : responsibleName));
    return card;
}

async function loadItems({ fallback = "Не удалось загрузить чек-лист.", mutationError = "", prepared } = {}) {
    empty.hidden = true;
    content.hidden = true;
    if (!prepared) {
        showStatus("Загружаем чек-лист…");
    }
    try {
        let items = prepared?.items;
        if (!items) {
            const response = await fetchApi(itemsApi, { cache: "no-store" });
            if (!response.ok) {
                showStatus(getRequestError(response, fallback), {
                    error: true,
                    canRetry: ![401, 403, 404].includes(response.status),
                });
                return;
            }
            items = await response.json();
        }

        const taken = items.filter(getResponsibleId).length;
        if (items.length) {
            let participants = new Map();
            let currentUser = null;
            if (prepared) {
                participants = new Map(prepared.participants.map(({ id, name }) => [id, name]));
                currentUser = prepared.viewer;
            } else if (taken) {
                [participants, currentUser] = await Promise.all([
                    getParticipantNames(),
                    fetchCurrentUser().then(response => response.ok ? response.json() : null).catch(() => null),
                ]);
            }
            list.replaceChildren(...items.map(item => renderItem(item, participants, currentUser?.id)));
            const subject = items.length === 1 ? "предмета" : "предметов";
            find("items-progress-label").textContent = `Взято ${taken} из ${items.length} ${subject}`;
            find("items-progress-percent").textContent = `${Math.round(taken / items.length * 100)}%`;
            const progress = find("items-progress");
            progress.max = items.length;
            progress.value = taken;
        }
        empty.hidden = items.length !== 0;
        content.hidden = items.length === 0;
        showStatus(mutationError, { error: Boolean(mutationError) });
    } catch {
        showStatus(fallback, { error: true, canRetry: true });
    }
}

function focusList(itemId, selector = ".item-open") {
    if (!content.hidden) {
        const card = itemId ? list.querySelector(`[data-item-id="${CSS.escape(itemId)}"]`) : null;
        (card?.querySelector(selector) ?? card?.querySelector(".item-open") ?? add).focus();
    } else if (!retry.hidden) {
        retry.focus();
    } else if (!empty.hidden) {
        empty.querySelector("[data-add-item]").focus();
    }
}

async function changeAssignment(itemId, action) {
    const failure = action === "unassign" ? "Не удалось снять назначение." : "Не удалось взять предмет.";
    list.inert = true;
    add.disabled = true;
    showStatus("Сохраняем…");
    try {
        const response = await fetchApi(`${itemUrl(itemId)}/${action}`, { method: "POST" });
        if (response.ok) {
            await loadItems({ fallback: "Изменение сохранено. Не удалось обновить список." });
        } else if (response.status === 400 || response.status === 409) {
            const message = response.status === 409 ? "Назначение уже изменилось." : failure;
            await loadItems({ fallback: `${message} Не удалось обновить список.`, mutationError: message });
        } else {
            showStatus(getRequestError(response, failure), { error: true });
        }
    } catch {
        showStatus(failure, { error: true });
    } finally {
        list.inert = false;
        add.disabled = false;
        focusList(itemId, ".item-action:not([hidden])");
    }
}

function openDeleteConfirmation() {
    find("item-delete-title").textContent = `Вы уверены, что хотите удалить из списка предмет: ${detailsItem.title}?`;
    showDialogError(deleteError, "");
    detailsDialog.close();
    deleteDialog.showModal();
}

function cancelDeleteConfirmation() {
    if (deleteConfirm.disabled) return;

    deleteDialog.close();
    detailsDialog.showModal();
    deleteButton.focus();
}

function setDeleting(deleting) {
    deleteCancel.disabled = deleting;
    deleteConfirm.disabled = deleting;
    deleteConfirm.textContent = deleting ? "Удаляем…" : "Удалить";
}

async function deleteItem() {
    if (!deleteDialog.open || deleteConfirm.disabled) return;

    const fallback = "Не удалось удалить предмет. Попробуйте ещё раз.";
    showDialogError(deleteError, "");
    setDeleting(true);
    try {
        const response = await fetchApi(itemUrl(detailsItem.id), { method: "DELETE" });
        if (!response.ok) {
            showDialogError(deleteError, getRequestError(response, fallback));
            return;
        }
        deleteDialog.close();
        await loadItems({ fallback: "Предмет удалён. Не удалось обновить список." });
        focusList();
    } catch {
        showDialogError(deleteError, fallback);
    } finally {
        setDeleting(false);
    }
}

function updateStepper() {
    if (count.validity.rangeUnderflow) {
        count.value = count.min;
    }
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
    if (editingItem) {
        submit.textContent = saving ? "Сохраняем…" : "Сохранить";
    } else {
        submit.textContent = saving ? "Создаём…" : "Создать";
    }
}

function openItemForm(item = null) {
    editingItem = item;
    form.reset();
    find("item-form-title").textContent = item ? "Редактировать предмет" : "Добавить";
    if (item) {
        title.value = item.title;
        link.value = item.urlShop ?? "";
        count.value = String(item.count);
        notes.value = item.description ?? "";
    }
    showDialogError(formError, "");
    setSubmitting(false);
    updateStepper();
    formDialog.showModal();
}

for (const button of document.querySelectorAll("[data-add-item]")) {
    button.addEventListener("click", () => openItemForm());
}

editButton.addEventListener("click", () => {
    detailsDialog.close();
    openItemForm(detailsItem);
});
deleteButton.addEventListener("click", openDeleteConfirmation);
deleteCancel.addEventListener("click", cancelDeleteConfirmation);
deleteConfirm.addEventListener("click", deleteItem);
deleteDialog.addEventListener("cancel", event => {
    event.preventDefault();
    cancelDeleteConfirmation();
});

count.addEventListener("input", updateStepper);
minus.addEventListener("click", () => stepCount(-1));
plus.addEventListener("click", () => stepCount(1));
close.addEventListener("click", () => formDialog.close());
formDialog.addEventListener("cancel", event => {
    if (submit.disabled) event.preventDefault();
});

form.addEventListener("submit", async event => {
    event.preventDefault();
    if (submit.disabled) return;

    const name = title.value.trim();
    if (!name) {
        showDialogError(formError, "Введите название предмета.");
        title.focus();
        return;
    }
    const savedUrl = editingItem?.urlShop ?? "";
    const url = link.value === savedUrl ? savedUrl : link.value.trim();
    if (url && url !== savedUrl && !getLink(url)) {
        showDialogError(formError, "Введите ссылку, начинающуюся с http:// или https://.");
        link.focus();
        return;
    }

    const item = {
        title: name,
        description: notes.value.trim(),
        count: count.valueAsNumber,
        urlShop: url || null,
    };
    const itemId = editingItem?.id;
    const fallback = itemId ? "Не удалось сохранить изменения." : "Не удалось создать предмет.";
    showDialogError(formError, "");
    setSubmitting(true);
    try {
        const response = await fetchApi(itemId ? itemUrl(itemId) : itemsApi, {
            method: itemId ? "PATCH" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(item),
        });
        if (!response.ok) {
            showDialogError(formError, getRequestError(response, fallback));
            return;
        }
        formDialog.close();
        const saved = itemId ? "Изменения сохранены." : "Предмет создан.";
        await loadItems({ fallback: `${saved} Не удалось обновить список.` });
        focusList(itemId);
    } catch {
        showDialogError(formError, fallback);
    } finally {
        setSubmitting(false);
    }
});

retry.addEventListener("click", () => void loadItems());
if (eventId) {
    find("items-back").href = `/html/event.html?id=${eventId}`;
    void loadItems({ prepared: takePreloadedEventSection(eventId, "items") });
} else {
    showStatus("Событие не выбрано. Вернитесь к списку событий.", { error: true });
}
