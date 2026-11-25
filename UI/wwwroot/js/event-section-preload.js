import { fetchApi } from "./session.js";

const collectionOpen = 1;

async function readJson(url) {
    const response = await fetchApi(url, { cache: "no-store" });
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }
    return response.json();
}

export function preloadEventSections(event) {
    const api = `/api/events/${encodeURIComponent(event.id)}`;
    const viewer = event.isOrganizer
        ? Promise.resolve({ id: event.organizerId })
        : readJson("/api/users/me").then(user => ({ id: user.id })).catch(() => null);
    let participants;
    const readParticipants = () => participants ??= readJson(`${api}/participants`);

    const items = readJson(`${api}/items`).then(async items => {
        if (!items.some(item => item.responsibleID ?? item.responsibleId)) {
            return { items, participants: [], viewer: null };
        }
        const [people, user] = await Promise.all([readParticipants(), viewer]);
        return { items, participants: people, viewer: user };
    });

    const activities = Promise.all([readJson(`${api}/activities`), viewer])
        .then(([activities, viewer]) => ({ event, activities, viewer }));

    const budget = event.collectionStatus === collectionOpen
        ? Promise.all([readJson(`${api}/budget`), readParticipants(), viewer])
            .then(([budget, participants, viewer]) => ({ event, budget, participants, viewer }))
        : Promise.resolve({ event });

    return {
        items: items.catch(() => undefined),
        activities: activities.catch(() => undefined),
        budget: budget.catch(() => undefined),
    };
}
