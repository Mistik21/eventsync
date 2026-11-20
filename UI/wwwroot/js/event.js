import { canDisplayCover } from "./events-list.js";
import { fetchCurrentUser, getInitials } from "./current-user.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";
import { loadMapGl, lookupAddress, parseCoordinates } from "./two-gis.js";

const find = id => document.getElementById(id);
const dialog = document.querySelector(".event-dialog");
const dialogContent = find("event-dialog-content");
const deleteDialog = find("event-delete-dialog");
const deleteConfirm = find("event-delete-confirm");
const deleteCancel = find("event-delete-cancel");
const deleteStatus = find("event-delete-status");
const inviteButton = find("event-invite");
const inviteDialog = find("event-invite-dialog");
const inviteCode = find("event-invite-code");
const inviteStatus = find("event-invite-status");
const status = find("event-status");
const dateFormat = new Intl.DateTimeFormat("ru-RU", { dateStyle: "long", timeStyle: "short" });
const moneyFormat = new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 });
const participantFormat = new Intl.PluralRules("ru-RU");
const participantLabels = { one: "участник", few: "участника", many: "участников" };
const collectionNotStarted = 0;
let eventDetails;
let previewMap;
let participantsLoading = false;

window.Telegram?.WebApp?.expand();

function paragraph(text) {
    const element = document.createElement("p");
    element.textContent = text;
    return element;
}

function openPanel(title, ...content) {
    find("event-dialog-title").textContent = title;
    dialogContent.replaceChildren(...content);
    dialog.showModal();
}

async function renderMap(point) {
    const container = find("event-map");
    try {
        const { mapgl, apiKey } = await loadMapGl();
        const center = [point.lng, point.lat];
        container.hidden = false;
        const map = new mapgl.Map(container, {
            key: apiKey, center, zoom: 15, lang: "ru", disableZoomOnScroll: true,
        });
        previewMap = map;
        map.once("styleloaderror", () => { container.hidden = true; });
        new mapgl.Marker(map, { coordinates: center, interactive: false });
    } catch {
        container.hidden = true;
    }
}

function renderEvent(event) {
    eventDetails = event;
    inviteButton.hidden = event.collectionStatus !== collectionNotStarted;
    document.title = `${event.title} · EventSync`;
    find("event-title").textContent = event.title;
    const cover = find("event-cover");
    if (canDisplayCover(event.mainImageUrl)) {
        cover.addEventListener("error", () => { cover.hidden = true; }, { once: true });
        cover.src = event.mainImageUrl;
        cover.hidden = false;
    }

    const date = find("event-date");
    const start = new Date(event.eventStartTime);
    date.dateTime = event.eventStartTime;
    date.textContent = Number.isNaN(start.getTime()) ? "Дата не указана" : dateFormat.format(start);
    const count = event.participantsCount;
    find("event-participants").textContent = `${count} ${participantLabels[participantFormat.select(count)] ?? "участника"}`;
    const point = parseCoordinates(event.eventLocation?.coordinates);
    const address = find("event-address");
    const savedAddress = event.eventLocation?.address?.trim();
    address.textContent = savedAddress || (point ? "Определяем адрес…" : "Место не указано");
    find("event-menu").hidden = !event.isOrganizer;
    const membershipAction = find("event-membership-action");
    membershipAction.textContent = event.isOrganizer ? "Удалить событие" : "Покинуть событие";
    membershipAction.disabled = !event.isOrganizer;
    membershipAction.hidden = !event.isOrganizer && !event.isParticipant;
    find("event-membership-hint").hidden = event.isOrganizer || membershipAction.hidden;
    find("event-content").hidden = false;
    status.hidden = true;
    if (point) {
        void renderMap(point);
        if (!savedAddress) {
            void lookupAddress(point).catch(() => "").then(resolvedAddress => {
                address.textContent = resolvedAddress || "Не удалось определить адрес";
                find("event-info-address").textContent = address.textContent;
            });
        }
    }
    find("event-info-address").textContent = address.textContent;
    find("event-info-description").textContent = event.description || "Описание пока не добавлено";
    renderInformationDate(start);
    showEventScreen();
}

function renderInformationDate(start) {
    if (Number.isNaN(start.getTime())) {
        find("event-info-date").textContent = "Дата не указана";
        find("event-info-time").textContent = "—";
        return;
    }

    find("event-info-date").textContent = start.toLocaleDateString("ru-RU", { dateStyle: "long" });
    find("event-info-time").textContent = start.toLocaleTimeString("ru-RU", { timeStyle: "short" });
    find("event-info-month").textContent = start.toLocaleDateString("ru-RU", { month: "long", year: "numeric" });
    const dayOffset = (start.getDay() + 6) % 7;
    const week = Array.from({ length: 7 }, (_, index) => {
        const day = new Date(start);
        day.setDate(start.getDate() + index - dayOffset);
        const cell = document.createElement("div");
        const weekday = document.createElement("small");
        weekday.textContent = day.toLocaleDateString("ru-RU", { weekday: "short" });
        if (index === dayOffset) {
            cell.className = "event-info-day-selected";
            cell.setAttribute("aria-current", "date");
        }
        cell.append(weekday, String(day.getDate()));
        return cell;
    });
    find("event-info-week").replaceChildren(...week);
    find("event-info-calendar").hidden = false;
}

function renderParticipant(user, currentUserId) {
    const row = document.createElement("li");
    const initials = document.createElement("span");
    initials.className = "event-info-avatar";
    initials.setAttribute("aria-hidden", "true");
    initials.textContent = getInitials(user.name);
    const label = document.createElement("div");
    label.textContent = user.id === currentUserId ? "Вы" : user.name;
    if (user.id === eventDetails.organizerId) {
        const role = document.createElement("small");
        role.textContent = "Организатор";
        label.append(role);
    }
    row.append(initials, label);
    if (eventDetails.isOrganizer && eventDetails.collectionStatus === collectionNotStarted
        && user.id !== currentUserId && user.id !== eventDetails.organizerId) {
        const remove = document.createElement("button");
        remove.type = "button";
        remove.textContent = "Удалить";
        remove.title = "Удаление участников пока недоступно";
        remove.setAttribute("aria-label", `Удалить участника: ${user.name}`);
        remove.disabled = true;
        row.append(remove);
    }
    return row;
}

async function loadParticipants() {
    if (participantsLoading) {
        return;
    }
    participantsLoading = true;
    const message = find("event-info-participants-status");
    const retry = find("event-info-retry");
    const list = find("event-info-people");
    retry.hidden = true;
    list.hidden = true;
    message.hidden = false;
    message.textContent = "Загружаем участников…";
    try {
        const [response, currentUser] = await Promise.all([
            fetchApi(`/api/events/${encodeURIComponent(eventDetails.id)}/participants`, { cache: "no-store" }),
            fetchCurrentUser().then(result => result.ok ? result.json() : null).catch(() => null),
        ]);
        if (!response.ok) {
            message.textContent = response.status === 401 ? getSessionErrorMessage()
                : response.status === 403 ? "У вас нет доступа к списку участников."
                : "Не удалось загрузить участников.";
            retry.hidden = response.status === 401 || response.status === 403;
            return;
        }
        const users = await response.json();
        const currentUserId = currentUser?.id ?? (eventDetails.isOrganizer ? eventDetails.organizerId : null);
        const rows = users.map(user => renderParticipant(user, currentUserId));
        list.replaceChildren(...rows);
        list.hidden = rows.length === 0;
        message.hidden = !list.hidden;
        message.textContent = "Участников пока нет.";
    } catch {
        message.textContent = "Не удалось загрузить участников.";
        retry.hidden = false;
    } finally {
        participantsLoading = false;
    }
}

function showEventScreen() {
    if (!eventDetails) {
        return;
    }
    const isInformation = window.location.hash === "#information";
    const overview = find("event-overview");
    const information = find("event-information");
    if (!isInformation && information.hidden) {
        return;
    }
    overview.hidden = isInformation;
    information.hidden = !isInformation;
    find(isInformation ? "event-info-location" : "event-map-preview").append(find("event-map"));
    previewMap?.invalidateSize();
    window.scrollTo(0, 0);
    find(isInformation ? "event-info-title" : "event-info").focus();
    if (isInformation) {
        void loadParticipants();
    }
}

find("event-info").addEventListener("click", () => { window.location.hash = "information"; });
find("event-info-retry").addEventListener("click", loadParticipants);
window.addEventListener("hashchange", showEventScreen);

inviteButton.addEventListener("click", () => {
    if (inviteButton.hidden) {
        return;
    }
    inviteCode.textContent = eventDetails.id;
    inviteStatus.hidden = true;
    inviteDialog.showModal();
});

find("event-invite-copy").addEventListener("click", async () => {
    try {
        await navigator.clipboard.writeText(eventDetails.id);
        inviteStatus.textContent = "Код скопирован.";
    } catch {
        inviteStatus.textContent = "Выделите и скопируйте код вручную.";
    }
    inviteStatus.hidden = false;
});

find("event-invite-share").addEventListener("click", () => {
    const shareUrl = new URL("https://t.me/share/url");
    shareUrl.searchParams.set("url", "https://t.me/EventSync1_bot");
    shareUrl.searchParams.set("text", `Приглашение на событие «${eventDetails.title}»\nКод приглашения: ${eventDetails.id}`);
    const telegram = window.Telegram?.WebApp;
    if (telegram?.initData && telegram.openTelegramLink) {
        telegram.openTelegramLink(shareUrl.href);
    } else {
        window.open(shareUrl.href, "_blank", "noopener,noreferrer");
    }
});

find("event-membership-action").addEventListener("click", () => {
    if (!eventDetails?.isOrganizer) {
        return;
    }
    deleteStatus.hidden = true;
    deleteDialog.showModal();
});

deleteCancel.addEventListener("click", () => deleteDialog.close());
deleteDialog.addEventListener("cancel", event => {
    if (deleteConfirm.disabled) {
        event.preventDefault();
    }
});

find("event-delete-form").addEventListener("submit", async event => {
    event.preventDefault();
    if (!eventDetails?.isOrganizer || deleteConfirm.disabled) {
        return;
    }

    deleteStatus.hidden = true;
    deleteConfirm.disabled = true;
    deleteCancel.disabled = true;
    deleteConfirm.textContent = "Удаляем…";
    let deleted = false;
    try {
        const response = await fetchApi(`/api/events/${encodeURIComponent(eventDetails.id)}`, { method: "DELETE" });
        if (response.ok) {
            window.location.replace("/");
            deleted = true;
            return;
        }
        const messages = {
            401: getSessionErrorMessage(),
            403: "Удалить событие может только организатор.",
            404: "Событие уже удалено или отсутствует.",
        };
        deleteStatus.textContent = messages[response.status] ?? "Не удалось удалить событие. Попробуйте ещё раз.";
        deleteStatus.hidden = false;
    } catch {
        deleteStatus.textContent = "Не удалось подтвердить удаление. Проверьте список событий.";
        deleteStatus.hidden = false;
    } finally {
        if (!deleted) {
            deleteConfirm.disabled = false;
            deleteCancel.disabled = false;
            deleteConfirm.textContent = "Удалить";
        }
    }
});

find("event-menu").addEventListener("click", () => {
    const edit = document.createElement("button");
    edit.className = "event-dialog-action";
    edit.type = "button";
    edit.textContent = "Редактировать событие";
    edit.disabled = true;
    openPanel("Управление событием", edit, paragraph("Редактирование пока недоступно."));
});

document.querySelector(".event-sections").addEventListener("click", ({ target }) => {
    const button = target.closest("[data-section]");
    if (!button) {
        return;
    }
    switch (button.dataset.section) {
        case "budget":
            openPanel("Бюджет",
                paragraph(`Общий бюджет — ${moneyFormat.format(eventDetails.budget)}`),
                paragraph(`Остаток — ${moneyFormat.format(eventDetails.remainingBudget)}`));
            break;
        case "photos": {
            const images = (eventDetails.imageEventUrls ?? []).filter(canDisplayCover).map(url => {
                const image = document.createElement("img");
                image.alt = "Фото события";
                image.loading = "lazy";
                image.referrerPolicy = "no-referrer";
                image.src = url;
                return image;
            });
            openPanel("Фото", ...(images.length ? images : [paragraph("Фото пока не добавлены.")]));
            break;
        }
        default:
            openPanel(button.lastChild.textContent.trim(), paragraph("Раздел пока недоступен."));
    }
});

async function loadEvent() {
    const id = new URLSearchParams(window.location.search).get("id");
    if (!id || !/^[\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12}$/i.test(id)) {
        status.textContent = "Событие не выбрано. Вернитесь к списку.";
        return;
    }

    try {
        const response = await fetchApi(`/api/events/${id}`, { cache: "no-store" });
        if (!response.ok) {
            const messages = {
                401: getSessionErrorMessage(),
                403: "У вас нет доступа к этому событию.",
                404: "Событие не найдено.",
            };
            status.textContent = messages[response.status] ?? "Не удалось загрузить событие.";
            return;
        }
        renderEvent(await response.json());
    } catch {
        status.textContent = "Не удалось загрузить событие. Обновите страницу.";
    }
}

void loadEvent();
