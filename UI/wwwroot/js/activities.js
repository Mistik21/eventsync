import { fetchApi, getSessionErrorMessage } from "./session.js";

const find = id => document.getElementById(id);
const eventId = window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/activities\/?$/i)?.[1];
const eventUrl = `/api/events/${eventId}`;
const activitiesUrl = `${eventUrl}/activities`;
const unlimited = 2147483647;
const dateFormat = new Intl.DateTimeFormat("ru-RU", { day: "numeric", month: "long" });
const fullDateFormat = new Intl.DateTimeFormat("ru-RU", { day: "numeric", month: "long", year: "numeric" });
const timeFormat = new Intl.DateTimeFormat("ru-RU", { hour: "2-digit", minute: "2-digit" });
const pluralRules = new Intl.PluralRules("ru-RU");

const status = find("activities-status");
const retry = find("activities-retry");
const addButton = find("activities-add");
const addEmptyButton = find("activities-add-empty");
const formDialog = find("activity-form-dialog");
const detailsDialog = find("activity-details-dialog");
const deleteDialog = find("activity-delete-dialog");
const form = find("activity-form");
const fields = find("activity-fields");
const titleInput = form.elements.title;
const startInput = form.elements.activityStartTime;
const endInput = form.elements.activityEndTime;
const limitInput = form.elements.maxParticipants;
const descriptionInput = form.elements.description;
const limitToggle = find("activity-use-limit");
const limitMinus = find("activity-minus");
const limitPlus = find("activity-plus");
const saveButton = find("activity-save");
const formClose = find("activity-form-close");
const formError = find("activity-form-error");
const editButton = find("activity-edit");
const deleteButton = find("activity-delete");
const deleteConfirm = find("activity-delete-confirm");
const deleteCancel = find("activity-delete-cancel");
const deleteError = find("activity-delete-error");
let isOrganizer = false;
let eventStart;
let selectedActivity;
let editingActivity;
let saving = false;

window.Telegram?.WebApp?.expand();

function showMessage(element, message) {
    element.textContent = message;
    element.hidden = !message;
}

function showStatus(message, { error = false, canRetry = false } = {}) {
    showMessage(status, message);
    status.dataset.error = String(error);
    retry.hidden = !canRetry;
}

function errorMessage(error, fallback) {
    const messages = {
        400: "Проверьте данные активности.",
        401: getSessionErrorMessage(),
        403: "У вас нет доступа к этому действию.",
        404: "Событие или активность не найдены.",
        409: "Данные изменились. Обновите список и попробуйте ещё раз.",
    };
    return messages[error.cause] ?? fallback;
}

async function request(url, options) {
    const response = await fetchApi(url, options);
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`, { cause: response.status });
    }
    return response;
}

function parseDate(value) {
    const date = new Date(value ?? NaN);
    return Number.isFinite(date.getTime()) ? date : null;
}

function localDateTime(date) {
    if (!date) return "";
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

function localDay(date) {
    return localDateTime(date).slice(0, 10);
}

function dayLabel(date) {
    if (!date) return "Дата не указана";
    const formatter = date.getFullYear() === new Date().getFullYear() ? dateFormat : fullDateFormat;
    return formatter.format(date);
}

function normalizeActivity(activity) {
    const participants = activity.participants;
    const limit = activity.maxParticipants;
    return {
        id: activity.id,
        title: activity.title,
        description: activity.description ?? "",
        // The current controller serializes entity properties with a Cyrillic «а».
        start: parseDate(activity.activityStartTime ?? activity["\u0430ctivityStartTime"]),
        end: parseDate(activity.activityEndTime ?? activity["\u0430ctivityEndTime"]),
        limit: limit > 0 && limit < unlimited ? limit : null,
        participants: participants ?? [],
        count: activity.participantsCount ?? participants?.length ?? null,
    };
}

async function readActivities() {
    const response = await request(activitiesUrl, { cache: "no-store" });
    return (await response.json()).map(normalizeActivity);
}

function seatsLabel({ count, limit }) {
    if (limit === null) return "Без лимита";
    if (count === null) return `Лимит · ${limit}`;
    return count >= limit ? "Мест нет" : `Свободно ${limit - count} из ${limit}`;
}

function activityCard(activity) {
    const row = find("activity-card-template").content.firstElementChild.cloneNode(true);
    const description = row.querySelector(".activity-card-description");
    const { start, end } = activity;
    const crossesMidnight = start && end && localDay(start) !== localDay(end);
    const time = [start && timeFormat.format(start), end && (crossesMidnight
        ? `${dayLabel(end)}, ${timeFormat.format(end)}` : timeFormat.format(end))].filter(Boolean).join("–");
    row.querySelector(".activity-card-time").textContent = time || "Время не указано";
    row.querySelector(".activity-card-seats").textContent = seatsLabel(activity);
    row.querySelector(".activity-card-title").textContent = activity.title;
    showMessage(description, activity.description);
    row.querySelector(".activity-card-role").hidden = !isOrganizer;
    row.querySelector("button").addEventListener("click", () => showDetails(activity));
    return row;
}

function renderActivities(activities) {
    const groups = new Map();
    const ordered = [...activities].sort((left, right) =>
        (left.start?.getTime() ?? Infinity) - (right.start?.getTime() ?? Infinity));
    for (const activity of ordered) {
        const key = localDay(activity.start);
        if (!groups.has(key)) groups.set(key, []);
        groups.get(key).push(activity);
    }

    const sections = Array.from(groups.values(), group => {
        const section = document.createElement("section");
        section.className = "activity-day";
        const header = document.createElement("header");
        const heading = document.createElement("h2");
        heading.textContent = dayLabel(group[0].start);
        const count = document.createElement("span");
        count.className = "activity-day-count";
        const noun = { one: "активность", few: "активности", many: "активностей" };
        count.textContent = `${group.length} ${noun[pluralRules.select(group.length)]}`;
        header.append(heading, count);
        const list = document.createElement("ul");
        list.append(...group.map(activityCard));
        section.append(header, list);
        return section;
    });

    find("activities-list").replaceChildren(...sections);
    find("activities-empty").hidden = activities.length !== 0;
    addEmptyButton.hidden = !isOrganizer;
    addButton.hidden = !isOrganizer || activities.length === 0;
}

async function loadPage() {
    showStatus("Загружаем активности…");
    try {
        const [event, activities] = await Promise.all([
            request(eventUrl, { cache: "no-store" }).then(response => response.json()),
            readActivities(),
        ]);
        isOrganizer = event.isOrganizer === true;
        eventStart = parseDate(event.eventStartTime);
        renderActivities(activities);
        showStatus("");
    } catch (error) {
        showStatus(errorMessage(error, "Не удалось загрузить активности."), {
            error: true,
            canRetry: ![401, 403, 404].includes(error.cause),
        });
    }
}

function showDetails(activity) {
    if (saving) return;
    selectedActivity = activity;
    find("activity-details-title").textContent = activity.title;
    const showDate = localDay(activity.start) !== localDay(activity.end);
    const displayTime = date => date ? `${showDate ? `${dayLabel(date)}, ` : ""}${timeFormat.format(date)}` : "—";
    find("activity-details-start").textContent = displayTime(activity.start);
    find("activity-details-end").textContent = displayTime(activity.end);
    showMessage(find("activity-details-description"), activity.description);

    const { count, limit, participants } = activity;
    const total = limit === null ? "" : ` из ${limit}`;
    find("activity-participants-title").textContent = count === null ? "Записались" : `Записались · ${count}${total}`;
    const list = find("activity-participants");
    const names = participants.filter(user => user.name).map(user => {
        const row = document.createElement("li");
        row.textContent = user.name;
        return row;
    });
    list.replaceChildren(...names);
    list.hidden = names.length === 0;
    showMessage(find("activity-participants-note"), list.hidden
        ? (count === 0 ? "Пока никто не записан" : "Список участников недоступен") : "");
    find("activity-details-actions").hidden = !isOrganizer;
    detailsDialog.showModal();
}

function updateStepper() {
    const value = limitInput.valueAsNumber;
    find("activity-stepper").hidden = !limitToggle.checked;
    limitInput.disabled = !limitToggle.checked;
    limitMinus.disabled = !(value > 1);
    limitPlus.disabled = value >= Number(limitInput.max);
}

function openForm(activity = null) {
    if (!isOrganizer || saving) return;
    editingActivity = activity;
    form.reset();
    if (activity) {
        titleInput.value = activity.title;
        startInput.value = localDateTime(activity.start);
        endInput.value = localDateTime(activity.end);
        descriptionInput.value = activity.description;
        limitToggle.checked = activity.limit !== null;
        limitInput.value = activity.limit ?? 1;
        detailsDialog.close();
    }
    updateTimeBounds();
    updateStepper();
    find("activity-form-title").textContent = activity ? "Редактирование активности" : "Новая активность";
    saveButton.textContent = activity ? "Сохранить" : "Создать";
    showMessage(formError, "");
    formDialog.showModal();
}

function returnToDetails(dialog, button) {
    if (saving) return;
    dialog.close();
    showDetails(selectedActivity);
    button.focus();
}

function closeForm() {
    if (editingActivity) {
        returnToDetails(formDialog, editButton);
    } else if (!saving) {
        formDialog.close();
    }
}

function readTime(input, original) {
    return original && input.value === localDateTime(original) ? original : parseDate(input.value);
}

function updateTimeBounds() {
    const start = readTime(startInput, editingActivity?.start);
    const end = readTime(endInput, editingActivity?.end);
    startInput.min = localDateTime(eventStart);
    endInput.min = localDateTime(start ?? eventStart);
    startInput.setCustomValidity(start && eventStart && start < eventStart
        ? "Начало активности не может быть раньше начала события." : "");
    endInput.setCustomValidity(start && end && end <= start
        ? "Укажите время окончания позже времени начала." : "");
    return { start, end };
}

function matchesChanges(activity, payload) {
    return activity.title === payload.title && activity.description === payload.description
        && activity.start?.toISOString() === payload.activityStartTime
        && activity.end?.toISOString() === payload.activityEndTime
        && activity.limit === (payload.maxParticipants === unlimited ? null : payload.maxParticipants);
}

async function refreshAfterMutation(dialog, errorField, verify) {
    let activities;
    try {
        activities = await readActivities();
    } catch (error) {
        dialog.close();
        showStatus(errorMessage(error, "Не удалось обновить список. Проверьте результат действия."), { error: true, canRetry: true });
        return;
    }

    renderActivities(activities);
    showStatus("");
    const message = verify?.(activities);
    if (message) {
        showMessage(errorField, message);
        return;
    }
    dialog.close();
    find("activities-title").focus();
}

addButton.addEventListener("click", () => openForm());
addEmptyButton.addEventListener("click", () => openForm());
editButton.addEventListener("click", () => openForm(selectedActivity));
formClose.addEventListener("click", closeForm);
formDialog.addEventListener("cancel", event => {
    if (saving || editingActivity) {
        event.preventDefault();
        closeForm();
    }
});

startInput.addEventListener("input", updateTimeBounds);
endInput.addEventListener("input", updateTimeBounds);
limitToggle.addEventListener("change", updateStepper);
limitInput.addEventListener("input", updateStepper);
limitMinus.addEventListener("click", () => {
    limitInput.stepDown();
    updateStepper();
});
limitPlus.addEventListener("click", () => {
    limitInput.stepUp();
    updateStepper();
});

form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!formDialog.open || saving || !isOrganizer) return;
    const title = titleInput.value.trim();
    const activity = editingActivity;
    const { start, end } = updateTimeBounds();
    if (!title) {
        showMessage(formError, "Введите название активности.");
        titleInput.focus();
        return;
    }
    if (!startInput.reportValidity() || !endInput.reportValidity() || !start || !end) return;

    const payload = {
        title,
        description: descriptionInput.value.trim(),
        activityStartTime: start.toISOString(),
        activityEndTime: end.toISOString(),
        maxParticipants: limitToggle.checked ? limitInput.valueAsNumber : (activity ? unlimited : null),
    };
    if (activity && matchesChanges(activity, payload)) {
        closeForm();
        return;
    }

    saving = fields.disabled = saveButton.disabled = formClose.disabled = true;
    saveButton.textContent = "Сохраняем…";
    showMessage(formError, "");
    try {
        await request(activity ? `${activitiesUrl}/${encodeURIComponent(activity.id)}` : activitiesUrl, {
            method: activity ? "PATCH" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload),
        });
        await refreshAfterMutation(formDialog, formError, activity && (activities =>
            activities.some(item => item.id === activity.id && matchesChanges(item, payload))
                ? "" : "Сервер не сохранил изменения активности."));
    } catch (error) {
        showMessage(formError, errorMessage(error, activity ? "Не удалось сохранить изменения." : "Не удалось создать активность."));
    } finally {
        saving = fields.disabled = saveButton.disabled = formClose.disabled = false;
        saveButton.textContent = activity ? "Сохранить" : "Создать";
    }
});

deleteButton.addEventListener("click", () => {
    if (saving || !isOrganizer) return;
    find("activity-delete-title").textContent = `Вы уверены, что хотите удалить активность: ${selectedActivity.title}?`;
    showMessage(deleteError, "");
    detailsDialog.close();
    deleteDialog.showModal();
});

deleteCancel.addEventListener("click", () => returnToDetails(deleteDialog, deleteButton));
deleteDialog.addEventListener("cancel", event => {
    event.preventDefault();
    returnToDetails(deleteDialog, deleteButton);
});

deleteConfirm.addEventListener("click", async () => {
    if (!deleteDialog.open || saving || !isOrganizer) return;
    const { id } = selectedActivity;
    saving = deleteConfirm.disabled = deleteCancel.disabled = true;
    deleteConfirm.textContent = "Удаляем…";
    showMessage(deleteError, "");
    try {
        await request(`${activitiesUrl}/${encodeURIComponent(id)}`, { method: "DELETE" });
        await refreshAfterMutation(deleteDialog, deleteError, activities =>
            activities.some(item => item.id === id) ? "Сервер не удалил активность." : "");
    } catch (error) {
        showMessage(deleteError, errorMessage(error, "Не удалось удалить активность."));
    } finally {
        saving = deleteConfirm.disabled = deleteCancel.disabled = false;
        deleteConfirm.textContent = "Удалить";
    }
});

retry.addEventListener("click", loadPage);
if (eventId) {
    find("activities-back").href = `/html/event.html?id=${encodeURIComponent(eventId)}`;
    void loadPage();
} else {
    showStatus("Событие не выбрано. Вернитесь к списку.", { error: true });
}
