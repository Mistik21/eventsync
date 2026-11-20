import { canDisplayCover } from "./events-list.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";
import { lookupAddress, parseCoordinates } from "./two-gis.js";

const find = id => document.getElementById(id);
const input = find("join-code");
const status = find("join-status");
const preview = find("join-preview");
const submit = find("join-submit");
const cover = find("join-cover");
const dateFormat = new Intl.DateTimeFormat("ru-RU", { dateStyle: "long", timeStyle: "short" });
const plurals = new Intl.PluralRules("ru-RU");
const participantLabels = { one: "участник", few: "участника", many: "участников" };
let invitation;
let lookup;
let lookupTimer;
let joining = false;

window.Telegram?.WebApp?.expand();

function showStatus(text = "", state = "") {
    status.textContent = text;
    status.dataset.state = state;
    status.hidden = !text;
}

function renderPreview(event, signal) {
    invitation = event;
    const hasCover = canDisplayCover(event.mainImageUrl);
    cover.hidden = !hasCover;
    if (hasCover) {
        cover.src = event.mainImageUrl;
    } else {
        cover.removeAttribute("src");
    }

    find("join-title").textContent = event.title;
    const date = find("join-date");
    const start = new Date(event.eventStartTime);
    date.dateTime = event.eventStartTime;
    date.textContent = Number.isNaN(start.getTime()) ? "Дата не указана" : dateFormat.format(start);
    const organizer = find("join-organizer");
    organizer.textContent = event.organizerName ? `${event.organizerName} · Организатор` : "";
    organizer.hidden = !event.organizerName;

    const count = event.participantsCount;
    const label = participantLabels[plurals.select(count)] ?? "участника";
    const address = event.eventLocation?.address?.trim();
    const location = find("join-location");
    location.textContent = `${address || "Адрес не указан"} · ${count} ${label}`;
    const point = address ? null : parseCoordinates(event.eventLocation?.coordinates);
    if (point) {
        void lookupAddress(point).then(resolved => {
            if (resolved && !signal.aborted) {
                location.textContent = `${resolved} · ${count} ${label}`;
            }
        }).catch(() => {});
    }

    find("join-role").textContent = event.isParticipant ? "Вы уже участник" : "Вы приглашены";
    submit.textContent = event.isParticipant ? "Открыть событие" : "Присоединиться";
    preview.hidden = false;
}

async function loadInvitation(id, signal) {
    showStatus("Ищем приглашение…");
    try {
        const response = await fetchApi(`/api/events/${id}/invitation`, { signal, cache: "no-store" });
        if (!response.ok) {
            if (signal.aborted) {
                return;
            }
            const messages = {
                401: getSessionErrorMessage(),
                404: "Приглашение не найдено. Проверьте код.",
                409: "Сбор уже открыт, присоединиться нельзя.",
            };
            showStatus(messages[response.status] ?? "Не удалось проверить приглашение.", "error");
            return;
        }
        const data = await response.json();
        if (!signal.aborted) {
            renderPreview(data, signal);
            showStatus("✓ Приглашение найдено", "found");
        }
    } catch {
        if (!signal.aborted) {
            showStatus("Не удалось проверить приглашение. Попробуйте ещё раз.", "error");
        }
    }
}

function checkCode() {
    clearTimeout(lookupTimer);
    lookup?.abort();
    invitation = undefined;
    preview.hidden = true;
    const id = input.value.trim();
    showStatus();
    if (!id) {
        return;
    }
    if (!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(id)) {
        showStatus("Введите полный код приглашения.", "error");
        return;
    }
    lookup = new AbortController();
    const { signal } = lookup;
    lookupTimer = setTimeout(() => { void loadInvitation(id, signal); }, 250);
}

cover.addEventListener("error", () => { cover.hidden = true; });
input.addEventListener("input", checkCode);
find("join-form").addEventListener("submit", async event => {
    event.preventDefault();
    if (joining) {
        return;
    }
    if (!invitation) {
        checkCode();
        return;
    }

    const id = encodeURIComponent(invitation.id);
    const destination = `/html/event.html?id=${id}`;
    if (invitation.isParticipant) {
        window.location.replace(destination);
        return;
    }

    joining = true;
    input.readOnly = true;
    submit.disabled = true;
    submit.textContent = "Присоединяемся…";
    let joined = false;
    try {
        const response = await fetchApi(`/api/events/${id}/join`, { method: "POST" });
        if (response.ok) {
            window.location.replace(destination);
            joined = true;
            return;
        }
        if (response.status === 401) {
            showStatus(getSessionErrorMessage(), "error");
        } else if (response.status === 403 || response.status === 404 || response.status === 409) {
            await loadInvitation(id, lookup.signal);
            if (invitation.isParticipant) {
                window.location.replace(destination);
                joined = true;
                return;
            }
            if (status.dataset.state !== "error") {
                showStatus("Не удалось присоединиться к событию.", "error");
            } else {
                preview.hidden = true;
                invitation = undefined;
            }
        } else {
            showStatus("Не удалось присоединиться к событию. Попробуйте ещё раз.", "error");
        }
    } catch {
        showStatus("Не удалось подтвердить вступление. Проверьте список событий.", "error");
    } finally {
        if (!joined) {
            joining = false;
            input.readOnly = false;
            submit.disabled = false;
            submit.textContent = "Присоединиться";
        }
    }
});

checkCode();
