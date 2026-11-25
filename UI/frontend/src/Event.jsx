import { lazy, Suspense, useEffect, useRef, useState } from "react";
import { changeCached, eventApi, eventsUrl, expire, message, profileUrl, refresh, useResource, write } from "./api";
import { eventPath, Link, useNavigation } from "./navigation";
import { Back, Cover, dateTime, Loading, Modal, ResourceStatus, useAddress } from "./ui";
import { loadMapGl, parseCoordinates } from "./two-gis.js";
import { getInitials } from "./current-user.js";
import "../../wwwroot/css/event.css";

const loadItems = () => import("./Items");
const loadActivities = () => import("./Activities");
const loadBudget = () => import("./Budget");
const Items = lazy(loadItems);
const Activities = lazy(loadActivities);
const Budget = lazy(loadBudget);
const countFormat = new Intl.PluralRules("ru-RU");
const labels = { one: "участник", few: "участника", many: "участников" };

function EventMap({ coordinates, visible = true }) {
    const host = useRef(null);
    const instance = useRef(null);
    const [unsupported, setUnsupported] = useState(false);
    useEffect(() => {
        const point = parseCoordinates(coordinates);
        if (!point) return;
        let active = true;
        let map;
        void loadMapGl().then(({ mapgl, apiKey }) => {
            if (!active) return;
            const center = [point.lng, point.lat];
            map = new mapgl.Map(host.current, { key: apiKey, center, zoom: 15, lang: "ru", disableZoomOnScroll: true });
            instance.current = map;
            new mapgl.Marker(map, { coordinates: center });
        }).catch(() => { if (active) setUnsupported(true); });
        return () => { active = false; instance.current = null; map?.destroy(); };
    }, [coordinates]);
    useEffect(() => { if (visible) instance.current?.invalidateSize(); }, [visible]);
    return parseCoordinates(coordinates) && <div ref={host} className="event-map" hidden={unsupported} aria-label="Место встречи на карте" />;
}

function Information({ event, url, path }) {
    const participants = useResource(`${url}/participants`);
    const viewer = useResource(event.isOrganizer ? null : profileUrl).data?.id ?? (event.isOrganizer ? event.organizerId : null);
    const address = useAddress(event.eventLocation);
    const start = new Date(event.eventStartTime);
    const calendar = Number.isFinite(start.getTime()) ? Array.from({ length: 7 }, (_, index) => {
        const day = new Date(start);
        const weekday = (day.getDay() + 6) % 7;
        day.setDate(day.getDate() - weekday + index);
        return { day, selected: index === weekday };
    }) : [];

    return <main className="event-screen event-information">
        <header><Back to={path} /><h1>Информация</h1></header>
        <section><h2>Дата</h2><div className="event-info-box">
            <p>{dateTime(event.eventStartTime)}</p>
            {calendar.length > 0 && <div className="event-info-week">{calendar.map(({ day, selected }) => <span key={day.toISOString()} aria-current={selected ? "date" : undefined}>{day.getDate()}</span>)}</div>}
        </div></section>
        <section><h2>Описание</h2><p className="event-info-box event-info-description">{event.description || "Описание пока не добавлено"}</p></section>
        <section><h2>Место встречи</h2><div className="event-info-box">{address}<EventMap coordinates={event.eventLocation?.coordinates} /></div></section>
        <section><h2>Участники</h2>
            <ResourceStatus resource={participants} url={`${url}/participants`} loading="Загружаем участников…" fallback="Не удалось загрузить участников." />
            {participants.data && <ul className="event-info-people">{participants.data.map(person => <li key={person.id}>
                <span aria-hidden="true">{getInitials(person.name)}</span> {person.name}{person.id === viewer ? " · Вы" : ""}{person.id === event.organizerId ? " · Организатор" : ""}
            </li>)}</ul>}
            {participants.data?.length === 0 && <p>Участников пока нет.</p>}
        </section>
    </main>;
}

function Invite({ event, close }) {
    const [status, setStatus] = useState("");
    function share() {
        const url = new URL("https://t.me/share/url");
        url.searchParams.set("url", "https://t.me/EventSync1_bot");
        url.searchParams.set("text", `Приглашение на событие «${event.title}»\nКод приглашения: ${event.id}`);
        if (window.Telegram?.WebApp?.initData && window.Telegram.WebApp.openTelegramLink) window.Telegram.WebApp.openTelegramLink(url.href);
        else window.open(url.href, "_blank", "noopener,noreferrer");
    }
    return <Modal title="Пригласить участников" className="event-dialog event-invite-dialog" onClose={close}>
        <h2>Пригласить участников</h2>
        <div className="event-invite-card">
            <p className="event-invite-code"><code>{event.id}</code></p>
            <div className="event-invite-actions">
                <button type="button" className="event-invite-button event-invite-copy" onClick={async () => {
                    try { await navigator.clipboard.writeText(event.id); setStatus("Код скопирован."); }
                    catch { setStatus("Выделите и скопируйте код вручную."); }
                }}>Копировать код</button>
                <button type="button" className="event-invite-button" onClick={share}>Поделиться</button>
            </div>
            {status && <p role="status">{status}</p>}
        </div>
        <button type="button" onClick={close}>Закрыть</button>
    </Modal>;
}

function Overview({ event, url, path, visible }) {
    const { navigate } = useNavigation();
    const [panel, setPanel] = useState(null);
    const [deleting, setDeleting] = useState(false);
    const [error, setError] = useState("");
    const address = useAddress(event.eventLocation);
    const count = event.participantsCount;
    const close = () => setPanel(null);
    useEffect(() => { if (!visible) setPanel(null); }, [visible]);

    async function deleteEvent() {
        setDeleting(true);
        setError("");
        try {
            await write(url, "DELETE");
            changeCached(eventsUrl, events => events?.filter(item => item.id !== event.id));
            expire(profileUrl);
            navigate("/", { replace: true });
            changeCached(url, () => undefined);
        } catch (error) {
            setError(message(error, "Не удалось удалить событие."));
        } finally {
            setDeleting(false);
        }
    }

    return <main className="event-screen" hidden={!visible}>
        <header className="event-hero">
            <Cover src={event.mainImageUrl} decoding="async" />
            <div className="event-header-actions"><Back to="/" label="К списку событий" />
                {event.isOrganizer && event.collectionStatus === 0 && <Link className="event-icon-button" to={`${path}/edit`} aria-label="Редактировать событие">⚙</Link>}
            </div>
            <h1>{event.title}</h1>
        </header>
        <section className="event-info">
            <Link to={`${path}#information`} className="event-info-heading">Информация <span aria-hidden="true">›</span></Link>
            <ul className="event-facts">
                <li><span aria-hidden="true">▦</span><time dateTime={event.eventStartTime}>{dateTime(event.eventStartTime)}</time></li>
                <li><span aria-hidden="true">♙</span>{count} {labels[countFormat.select(count)] ?? "участника"}</li>
                <li><span aria-hidden="true">⌖</span>{address}</li>
            </ul>
            <div inert><EventMap coordinates={event.eventLocation?.coordinates} visible={visible} /></div>
        </section>
        <nav className="event-sections" aria-label="Разделы события">
            <Link to={`${path}/activities`}><span aria-hidden="true">✦</span>Активности</Link>
            <Link to={`${path}/drives`}><span aria-hidden="true">↗</span>Поездки</Link>
            <Link to={`${path}/budget`}><span aria-hidden="true">₽</span>Бюджет</Link>
            <Link to={`${path}/items`}><span aria-hidden="true">✓</span>Чек-лист</Link>
            <Link to={`${path}#photos`}><span aria-hidden="true">▧</span>Фото</Link>
        </nav>
        {event.collectionStatus === 0 && <button type="button" className="event-invite-button" onClick={() => setPanel("invite")}>Пригласить участников</button>}
        <button type="button" className="event-membership-action" disabled={!event.isOrganizer} onClick={() => { setError(""); setPanel("delete"); }}>{event.isOrganizer ? "Удалить событие" : "Покинуть событие"}</button>
        {!event.isOrganizer && <p className="event-membership-hint">Действие пока недоступно.</p>}
        {panel === "invite" && <Invite event={event} close={close} />}
        {panel === "delete" && <Modal className="event-dialog" title="Удалить событие?" onClose={close} busy={deleting}>
            <h2>Удалить событие?</h2><p>Событие «{event.title}» будет удалено.</p>
            {error && <p role="alert">{error}</p>}
            <button type="button" disabled={deleting} onClick={close}>Отмена</button>{" "}
            <button type="button" disabled={deleting} onClick={deleteEvent}>{deleting ? "Удаляем…" : "Удалить"}</button>
        </Modal>}
    </main>;
}

export default function Event({ id, tab }) {
    const url = eventApi(id);
    const path = eventPath(id);
    const resource = useResource(url);
    const event = resource.data;
    const [visitedOverview, setVisitedOverview] = useState(tab === "overview");
    useEffect(() => {
        if (!event || tab !== "overview") return;
        setVisitedOverview(true);
        const timer = setTimeout(() => {
            void loadItems().catch(() => {});
            void loadActivities().catch(() => {});
            void loadBudget().catch(() => {});
            void refresh(`${url}/items`).then(items => {
                if (items?.some(item => item.responsibleID ?? item.responsibleId)) {
                    void refresh(`${url}/participants`);
                }
            });
            void refresh(`${url}/activities`);
            if (!event.isOrganizer) void refresh(profileUrl);
            if (event.collectionStatus === 1) {
                void refresh(`${url}/budget`);
                void refresh(`${url}/participants`);
            }
        }, 250);
        return () => clearTimeout(timer);
    }, [event, tab, url]);

    if (!event) return <main className="event-screen event-information">
        <header><Back to={tab === "overview" ? "/" : path} /><h1>Событие</h1></header>
        <ResourceStatus resource={resource} url={url} loading="Загружаем событие…" fallback="Не удалось загрузить событие." />
    </main>;

    let section;
    if (tab === "information") section = <Information event={event} url={url} path={path} />;
    else if (tab === "items") section = <Items event={event} url={url} path={path} />;
    else if (tab === "activities") section = <Activities event={event} url={url} path={path} />;
    else if (tab === "budget") section = <Budget event={event} url={url} path={path} />;
    else if (tab !== "overview") section = <main className="event-screen event-information">
        <header><Back to={path} /><h1>{tab === "photos" ? "Фото" : "Поездки"}</h1></header>
        {tab === "photos" ? (event.imageEventUrls?.length ? event.imageEventUrls.map((src, index) => <Cover key={`${src}:${index}`} src={src} loading="lazy" alt="Фото события" />) : <p>Фото пока не добавлены.</p>) : <p>Раздел пока недоступен.</p>}
    </main>;

    const showOverview = tab === "overview";
    return <>
        {(showOverview || visitedOverview) && <Overview event={event} url={url} path={path} visible={showOverview} />}
        {section && <Suspense fallback={<main className="event-screen event-information">
            <header><Back to={path} /><h1>{event.title}</h1></header><Loading>Загружаем раздел…</Loading>
        </main>}>{section}</Suspense>}
    </>;
}
