import { canDisplayCover } from "./events-list.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";
import { formatCoordinates, loadMapGl, parseCoordinates } from "./two-gis.js";

const find = id => document.getElementById(id);
const dialog = document.querySelector(".event-dialog");
const dialogContent = find("event-dialog-content");
const status = find("event-status");
const dateFormat = new Intl.DateTimeFormat("ru-RU", { dateStyle: "long", timeStyle: "short" });
const moneyFormat = new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 });
const participantFormat = new Intl.PluralRules("ru-RU");
const participantLabels = { one: "участник", few: "участника", many: "участников" };
let eventDetails;

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
        map.once("styleloaderror", () => { container.hidden = true; });
        new mapgl.Marker(map, { coordinates: center, interactive: false });
    } catch {
        container.hidden = true;
    }
}

function renderEvent(event) {
    eventDetails = event;
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
    find("event-address").textContent = event.eventLocation?.address || (point ? formatCoordinates(point) : "Место не указано");
    find("event-menu").hidden = !event.isOrganizer && !event.isParticipant;
    find("event-content").hidden = false;
    status.hidden = true;
    if (point) {
        void renderMap(point);
    }
}

find("event-info").addEventListener("click", () => {
    openPanel("Информация",
        paragraph(find("event-date").textContent),
        paragraph(eventDetails.description || "Описание пока не добавлено"),
        paragraph(find("event-address").textContent),
        paragraph(find("event-participants").textContent));
});

find("event-menu").addEventListener("click", () => {
    const labels = eventDetails.isOrganizer
        ? ["Редактировать событие", "Удалить событие"]
        : ["Покинуть событие"];
    const actions = labels.map(label => {
        const button = document.createElement("button");
        button.className = "event-dialog-action";
        button.type = "button";
        button.textContent = label;
        button.disabled = true;
        return button;
    });
    openPanel(eventDetails.isOrganizer ? "Управление событием" : "Участие в событии",
        ...actions, paragraph("Эти действия пока недоступны."));
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
