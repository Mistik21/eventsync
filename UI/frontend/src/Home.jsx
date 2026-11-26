import { useEffect, useRef, useState } from "react";
import { eventsUrl, message, useResource, write } from "./api";
import { eventPath, Link } from "./navigation";
import { Cover, dateTime, ResourceStatus, useAddress } from "./ui";
import { getInitials } from "./current-user.js";

const groups = { active: "Активные", organizing: "Я организую", archive: "Архив" };
const emptyTitles = { active: "Нет активных событий", organizing: "Нет событий, которые вы организуете", archive: "В архиве пока пусто" };

function EventCard({ event, prepare, active, onAddress }) {
    const card = useRef(null);
    const [visible, setVisible] = useState(false);
    useEffect(() => {
        if (!active || visible || typeof IntersectionObserver === "undefined") return;
        const observer = new IntersectionObserver(entries => {
            if (entries.some(entry => entry.isIntersecting)) setVisible(true);
        });
        observer.observe(card.current);
        return () => observer.disconnect();
    }, [active, visible]);
    const address = useAddress(event.eventLocation, active && visible);
    useEffect(() => {
        if (address !== "Определяем адрес…" && address !== "Место не указано") onAddress(event.id, address);
    }, [address, event.id, onAddress]);

    return <article className="event-card" ref={card}>
        <Link className="event-card-summary" to={eventPath(event.id)} intent={() => prepare(event.id)}>
            <Cover className="event-card-cover" src={event.mainImageUrl} loading="lazy" decoding="async" />
            <div className="event-card-heading">
                <h2 className="event-card-title">{event.title}</h2>
                <span className={`event-card-role${event.isOrganizer ? " event-card-role--organizer" : ""}`}>{event.isOrganizer ? "Вы организатор" : "Вы участник"}</span>
            </div>
            <time className="event-card-date" dateTime={event.eventStartTime}>{dateTime(event.eventStartTime)}</time>
            <div className="event-card-footer">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><path d="M19 10c0 5-7 10-7 10s-7-5-7-10a7 7 0 1 1 14 0Z"/><circle cx="12" cy="10" r="2.5"/></svg>
                <span className="event-card-address">{address}</span>
                <svg className="event-card-chevron" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><path d="m9 6 6 6-6 6"/></svg>
            </div>
        </Link>
    </article>;
}

export function Home({ active, prepareEvent, prepareProfile }) {
    const resource = useResource(eventsUrl, active);
    const [group, setGroup] = useState("active");
    const [search, setSearch] = useState("");
    const [order, setOrder] = useState("ascending");
    const [initials, setInitials] = useState("П");
    const [sendingNotification, setSendingNotification] = useState(false);
    const [notificationStatus, setNotificationStatus] = useState("");
    const addresses = useRef(new Map());
    const filter = useRef(null);
    const events = resource.data ?? [];
    const now = Date.now();
    const upcoming = events.filter(event => Date.parse(event.eventStartTime) >= now);
    const collections = { active: upcoming, organizing: upcoming.filter(event => event.isOrganizer), archive: events.filter(event => Date.parse(event.eventStartTime) < now) };
    const query = search.trim().toLocaleLowerCase("ru");
    const visible = collections[group].filter(event => [event.title, event.eventLocation?.address, addresses.current.get(event.id)]
        .some(value => (value ?? "").toLocaleLowerCase("ru").includes(query)));
    visible.sort((first, second) => (order === "ascending" ? 1 : -1) * (Date.parse(first.eventStartTime) - Date.parse(second.eventStartTime)));

    useEffect(() => {
        void Promise.resolve(window.eventSyncTelegram).then(() => {
            const user = window.Telegram?.WebApp?.initDataUnsafe?.user;
            if (user?.first_name) setInitials(getInitials(user.first_name));
        });
    }, []);

    async function sendTestNotification() {
        setSendingNotification(true);
        setNotificationStatus("");
        try {
            await write("/api/telegram/test-message", "POST");
            setNotificationStatus("Тестовое уведомление отправлено.");
        } catch (error) {
            setNotificationStatus(error.status === 422
                ? "Откройте чат с ботом EventSync, нажмите «Старт» и попробуйте ещё раз."
                : message(error, "Не удалось отправить тестовое уведомление."));
        } finally {
            setSendingNotification(false);
        }
    }

    return <main className="events-screen" hidden={!active}>
        <header className="events-header">
            <span className="events-mark" aria-hidden="true">▣</span>
            <h1>Мои события</h1>
            <Link className="profile-badge" to="/profile" aria-label="Открыть профиль" intent={prepareProfile}>{initials}</Link>
        </header>
        <div className="events-telegram-test">
            <button className="event-group" type="button" disabled={sendingNotification} onClick={sendTestNotification}>
                {sendingNotification ? "Отправляем…" : "Отправить тест в Telegram"}
            </button>
            <p role="status">{notificationStatus}</p>
        </div>
        <ul className="event-groups" aria-label="Группы событий">
            {Object.entries(groups).map(([key, label]) => <li key={key}>
                <button className={`event-group event-group--${key}`} type="button" aria-pressed={group === key} onClick={() => {
                    setGroup(key);
                    setOrder(key === "archive" ? "descending" : "ascending");
                }}>{label}{key !== "archive" && resource.data ? ` · ${collections[key].length}` : ""}</button>
            </li>)}
        </ul>
        <div className="event-search" role="search">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></svg>
            <input type="search" value={search} onChange={e => setSearch(e.target.value)} aria-label="Найти событие" placeholder="Найти событие" />
            {resource.data && <details className="event-filter" ref={filter}>
                <summary className="filter-button" aria-label="Порядок событий"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true"><path d="M4 6h6m4 0h6m-6-2v4M4 12h3m4 0h9m-13-2v4M4 18h9m4 0h3m-7-2v4"/></svg></summary>
                <div className="event-filter-options">
                    <label htmlFor="event-order">По дате начала</label>
                    <select id="event-order" value={order} onChange={e => { setOrder(e.target.value); filter.current.open = false; }}>
                        <option value="ascending">По возрастанию</option><option value="descending">По убыванию</option>
                    </select>
                </div>
            </details>}
        </div>
        <ResourceStatus resource={resource} url={eventsUrl} className="events-status" loading="Загружаем события…" fallback="Не удалось загрузить события." />
        {Boolean(visible.length) && <section className="event-list" aria-label="Мои события">
            {visible.map(event => <EventCard key={event.id} event={event} prepare={prepareEvent} active={active} onAddress={(id, address) => addresses.current.set(id, address)} />)}
        </section>}
        {resource.data && !visible.length && <section className="empty-events">
            <h2>{query ? "Событий не найдено" : events.length ? emptyTitles[group] : "Событий пока нет"}</h2>
            {!query && !events.length && <p>Создайте новое или присоединитесь по коду</p>}
        </section>}
        <div className="event-actions">
            <Link className="event-action event-action--create" to="/createEvent" aria-label="Создать событие"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M12 4v16M4 12h16"/></svg></Link>
            <Link className="event-action event-action--join" to="/joinEvent" aria-label="Присоединиться по коду"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M15 3h5a1 1 0 0 1 1 1v16a1 1 0 0 1-1 1h-5M3 12h12m-5-5 5 5-5 5"/></svg></Link>
        </div>
    </main>;
}
