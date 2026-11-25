import { fetchApi } from "./session.js";

const cacheLifetime = 30_000;
const collectionOpen = 1;
const sections = ["items", "activities", "budget"];
const storageKey = section => `eventsync:event-preload:${section}`;
const telegramUserId = () => window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? null;

export function takePreloadedEventSection(eventId, section) {
    try {
        const key = storageKey(section);
        const entry = JSON.parse(window.sessionStorage.getItem(key));
        if (entry?.eventId !== eventId.toLowerCase() || entry.telegramUserId !== telegramUserId()) {
            return undefined;
        }

        window.sessionStorage.removeItem(key);
        return entry.expiresAt > Date.now() ? entry.data : undefined;
    } catch {
        return undefined;
    }
}

async function readJson(url) {
    const response = await fetchApi(url, { cache: "no-store" });
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }
    return response.json();
}

export async function preloadEventSections(event) {
    let storage;
    try {
        storage = window.sessionStorage;
        for (const section of sections) {
            storage.removeItem(storageKey(section));
        }
    } catch {
        return;
    }

    const { id, eventStartTime, isOrganizer, organizerId, budget, collectionStatus } = event;
    const details = { eventStartTime, isOrganizer, organizerId, budget, collectionStatus };
    const entry = {
        eventId: id.toLowerCase(),
        telegramUserId: telegramUserId(),
        expiresAt: Date.now() + cacheLifetime,
    };
    const api = `/api/events/${encodeURIComponent(id)}`;
    const viewer = isOrganizer
        ? Promise.resolve({ id: organizerId })
        : readJson("/api/users/me").then(user => ({ id: user.id }));
    let participants;
    const readParticipants = () => participants ??= readJson(`${api}/participants`);

    async function prepare(section, read) {
        try {
            const data = await read();
            if (entry.expiresAt > Date.now()) {
                storage.setItem(storageKey(section), JSON.stringify({ ...entry, data }));
            }
        } catch {
            // Each section can load normally if its prefetch fails.
        }
    }

    await Promise.all([
        prepare("items", async () => {
            const items = await readJson(`${api}/items`);
            if (!items.some(item => item.responsibleID ?? item.responsibleId)) {
                return { items, participants: [], viewer: null };
            }

            const [people, user] = await Promise.all([readParticipants(), viewer]);
            return { items, participants: people, viewer: user };
        }),
        prepare("activities", async () => {
            const [activities, user] = await Promise.all([readJson(`${api}/activities`), viewer]);
            return { event: details, activities, viewer: user };
        }),
        prepare("budget", async () => {
            if (collectionStatus !== collectionOpen) {
                return { event: details };
            }

            const [budget, people, user] = await Promise.all([readJson(`${api}/budget`), readParticipants(), viewer]);
            return { event: details, budget, participants: people, viewer: user };
        }),
    ]);
}
