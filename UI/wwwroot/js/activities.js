import { fetchCurrentUser } from "./current-user.js";
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
const membershipDialog = find("activity-membership-dialog");
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
const membershipConfirm = find("activity-membership-confirm");
const membershipCancel = find("activity-membership-cancel");
const membershipError = find("activity-membership-error");
let isOrganizer = false;
let currentUserId;
let eventStart;
let selectedActivity;
let editingActivity;
let membershipActivity;
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
        creatorId: activity.creatorId ?? activity.creator,
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
    return count >= limit ? `Занято ${count} из ${limit}` : `Свободно ${limit - count} из ${limit}`;
}

function isActivityCreator(activity) {
    return Boolean(currentUserId && currentUserId === activity?.creatorId);
}

function isActivityParticipant(activity) {
    return activity.participants.some(user => user.id === currentUserId);
}

function isActivityFull({ count, limit }) {
    return count !== null && limit !== null && count >= limit;
}

function activityCard(activity) {
    const row = find("activity-card-template").content.firstElementChild.cloneNode(true);
    const description = row.querySelector(".activity-card-description");
    const { start, end } = activity;
    const crossesMidnight = start && end && localDay(start) !== localDay(end);
    const time = [start && timeFormat.format(start), end && (crossesMidnight
        ? `${dayLabel(end)}, ${timeFormat.format(end)}` : timeFormat.format(end))].filter(Boolean).join("–");
    row.querySelector(".activity-card-time").textContent = time || "Время не указано";
    const full = isActivityFull(activity);
    const seats = row.querySelector(".activity-card-seats");
    seats.textContent = seatsLabel(activity);
    seats.dataset.full = String(full);
    row.querySelector(".activity-card-title").textContent = activity.title;
    showMessage(description, activity.description);
    const isCreator = isActivityCreator(activity);
    row.querySelector(".activity-card-role").hidden = !isCreator;
    row.querySelector(".activity-card-open").addEventListener("click", () => showDetails(activity));

    if (currentUserId && !isCreator) {
        const joined = isActivityParticipant(activity);
        const button = row.querySelector(".activity-card-membership");
        button.id = `activity-membership-${activity.id}`;
        button.dataset.joined = String(joined);
        button.textContent = joined ? "Вы записаны · Отказаться" : (full ? "Мест нет" : "Записаться");
        button.hidden = false;
        button.disabled = !joined && full;
        button.addEventListener("click", () => openMembershipDialog(activity));
    }
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
        const [event, activities, viewer] = await Promise.all([
            request(eventUrl, { cache: "no-store" }).then(response => response.json()),
            readActivities(),
            fetchCurrentUser().then(response => response.ok ? response.json() : null).catch(() => null),
        ]);
        isOrganizer = event.isOrganizer === true;
        currentUserId = viewer?.id ?? (isOrganizer ? event.organizerId : null);
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
    find("activity-details-actions").hidden = !isActivityCreator(activity);
    detailsDialog.showModal();
}

function updateStepper() {
    const value = limitInput.valueAsNumber;
    const minimum = Number(limitInput.min);
    find("activity-stepper").hidden = !limitToggle.checked;
    limitInput.disabled = !limitToggle.checked;
    limitMinus.disabled = !(value > minimum);
    limitPlus.disabled = value >= Number(limitInput.max);
    limitInput.setCustomValidity(limitToggle.checked && value < minimum
        ? `Лимит участников должен быть не меньше ${minimum}.` : "");
}

function openForm(activity = null) {
    if (saving || !(activity ? isActivityCreator(activity) : isOrganizer)) return;
    editingActivity = activity;
    form.reset();
    const minimum = Math.max(1, activity?.count ?? 0);
    limitInput.min = String(minimum);
    limitInput.value = String(activity?.limit ?? minimum);
    if (activity) {
        titleInput.value = activity.title;
        startInput.value = localDateTime(activity.start);
        endInput.value = localDateTime(activity.end);
        descriptionInput.value = activity.description;
        limitToggle.checked = activity.limit !== null;
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

async function refreshAfterMutation(dialog, errorField, verify, focusId = "activities-title") {
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
    (find(focusId) ?? find("activities-title")).focus();
}

function openMembershipDialog(activity) {
    if (saving) return;
    membershipActivity = activity;
    const joined = isActivityParticipant(activity);
    const prompt = joined ? "отказаться от активности" : "записаться на активность";
    find("activity-membership-title").textContent = `Вы уверены, что хотите ${prompt} «${activity.title}»?`;
    membershipConfirm.textContent = joined ? "Отказаться" : "Записаться";
    showMessage(membershipError, "");
    membershipDialog.showModal();
}

membershipCancel.addEventListener("click", () => membershipDialog.close());
membershipDialog.addEventListener("cancel", event => {
    if (saving) event.preventDefault();
});

membershipConfirm.addEventListener("click", async () => {
    if (saving || !membershipDialog.open) return;
    const { id } = membershipActivity;
    const joining = !isActivityParticipant(membershipActivity);
    const action = joining ? "join" : "leave";
    const failure = joining ? "Не удалось записаться на активность." : "Не удалось отказаться от активности.";

    saving = membershipConfirm.disabled = membershipCancel.disabled = true;
    membershipConfirm.textContent = joining ? "Записываем…" : "Отменяем запись…";
    showMessage(membershipError, "");
    try {
        await request(`${activitiesUrl}/${encodeURIComponent(id)}/${action}`, { method: "POST" }).catch(error => {
            if (error.cause !== 409) throw error;
        });
        await refreshAfterMutation(membershipDialog, membershipError, activities => {
            const updated = activities.find(activity => activity.id === id);
            if (!updated) return "Активность больше недоступна.";
            if (isActivityParticipant(updated) === joining) return "";
            if (joining && isActivityFull(updated)) return "Свободных мест больше нет.";
            return joining ? "Сервер не подтвердил запись." : "Сервер не отменил запись.";
        }, `activity-membership-${id}`);
    } catch (error) {
        showMessage(membershipError, errorMessage(error, failure));
    } finally {
        saving = membershipConfirm.disabled = membershipCancel.disabled = false;
        membershipConfirm.textContent = joining ? "Записаться" : "Отказаться";
    }
});

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
    const activity = editingActivity;
    if (!formDialog.open || saving || !(activity ? isActivityCreator(activity) : isOrganizer)) return;
    const title = titleInput.value.trim();
    const { start, end } = updateTimeBounds();
    if (!title) {
        showMessage(formError, "Введите название активности.");
        titleInput.focus();
        return;
    }
    if (!startInput.reportValidity() || !endInput.reportValidity() || !start || !end) return;
    updateStepper();
    if (!limitInput.reportValidity()) return;

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
    if (saving || !isActivityCreator(selectedActivity)) return;
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
    if (!deleteDialog.open || saving || !isActivityCreator(selectedActivity)) return;
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
