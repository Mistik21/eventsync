import { useState } from "react";
import { profileUrl, refresh, useResource, write } from "./api";
import { ActionDialog, Back, dateTime, invalid, Modal, ResourceStatus } from "./ui";
import "../../wwwroot/css/activities.css";

const unlimited = 2_147_483_647;
const startOf = item => item.activityStartTime ?? item["\u0430ctivityStartTime"];
const endOf = item => item.activityEndTime ?? item["\u0430ctivityEndTime"];
const dayFormat = new Intl.DateTimeFormat("ru-RU", { day: "numeric", month: "long", year: "numeric" });
const timeFormat = new Intl.DateTimeFormat("ru-RU", { hour: "2-digit", minute: "2-digit" });
const local = timestamp => {
    const date = new Date(timestamp ?? NaN);
    return Number.isFinite(date.getTime()) ? new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16) : "";
};

function ActivityForm({ event, item, save, close }) {
    const [limited, setLimited] = useState(Boolean(item?.maxParticipants && item.maxParticipants < unlimited));
    const [limit, setLimit] = useState(String(limited ? item.maxParticipants : 10));
    const earliestStart = local(Math.ceil(Date.parse(event.eventStartTime) / 60_000) * 60_000);
    const [start, setStart] = useState(local(startOf(item ?? {})) || earliestStart);

    return <ActionDialog title={item ? "Редактировать активность" : "Новая активность"} className="activities-dialog" close={close} fallback="Не удалось сохранить активность." submit={values => {
        const title = values.get("title").trim();
        const startTime = new Date(start);
        const endTime = new Date(values.get("end"));
        if (!title) throw invalid("Введите название активности.");
        if (!Number.isFinite(startTime.getTime()) || !Number.isFinite(endTime.getTime())) throw invalid("Укажите начало и окончание активности.");
        const activityStartTime = item && start === local(startOf(item)) ? startOf(item) : startTime.toISOString();
        const activityEndTime = item && values.get("end") === local(endOf(item)) ? endOf(item) : endTime.toISOString();
        if (Date.parse(activityEndTime) <= Date.parse(activityStartTime)) throw invalid("Окончание должно быть позже начала.");
        if (Date.parse(activityStartTime) < Date.parse(event.eventStartTime)) throw invalid("Активность должна начинаться не раньше события.");
        return save({ title, description: values.get("description").trim(), activityStartTime, activityEndTime, maxParticipants: limited ? Number(limit) : null });
    }}>
        <label htmlFor="activity-title">Название</label><input id="activity-title" name="title" required maxLength={120} defaultValue={item?.title} />
        <label htmlFor="activity-start">Начало</label><input id="activity-start" type="datetime-local" min={item ? undefined : earliestStart} required value={start} onChange={e => setStart(e.target.value)} />
        <label htmlFor="activity-end">Окончание</label><input id="activity-end" type="datetime-local" name="end" required min={start} defaultValue={local(endOf(item ?? {}))} />
        <label><input type="checkbox" checked={limited} onChange={e => setLimited(e.target.checked)} /> Ограничить количество участников</label>
        {limited && <div className="activity-stepper">
            <button type="button" aria-label="Уменьшить лимит" onClick={() => setLimit(String(Math.max(1, Number(limit) - 1)))}>−</button>
            <input type="number" aria-label="Лимит участников" min="1" max="2147483646" required value={limit} onChange={e => setLimit(e.target.value)} />
            <button type="button" aria-label="Увеличить лимит" onClick={() => setLimit(String(Math.min(unlimited - 1, Number(limit) + 1)))}>+</button>
        </div>}
        <label htmlFor="activity-description">Описание</label><textarea id="activity-description" name="description" rows={3} maxLength={1000} defaultValue={item?.description} />
    </ActionDialog>;
}

function groupActivities(activities) {
    const groups = new Map();
    const ordered = [...activities].sort((a, b) => Date.parse(startOf(a)) - Date.parse(startOf(b)));
    for (const activity of ordered) {
        const date = new Date(startOf(activity) ?? NaN);
        const key = Number.isFinite(date.getTime()) ? dayFormat.format(date) : "Дата не указана";
        if (!groups.has(key)) groups.set(key, []);
        groups.get(key).push(activity);
    }
    return groups;
}

export default function Activities({ event, path, url }) {
    const activitiesUrl = `${url}/activities`;
    const resource = useResource(activitiesUrl);
    const viewer = useResource(event.isOrganizer ? null : profileUrl).data?.id ?? (event.isOrganizer ? event.organizerId : null);
    const [panel, setPanel] = useState(null);
    const close = () => setPanel(null);
    const creator = item => Boolean(viewer && viewer === (item.creatorId ?? item.creator));
    const joined = item => (item.participants ?? []).some(person => person.id === viewer);
    const full = item => item.maxParticipants > 0 && (item.participants?.length ?? 0) >= item.maxParticipants;
    const groups = groupActivities(resource.data ?? []);

    async function change(suffix, method, data) {
        try { await write(`${activitiesUrl}${suffix}`, method, data); }
        catch (error) {
            if (error.status === 409) void refresh(activitiesUrl, true);
            throw error;
        }
        await refresh(activitiesUrl, true);
    }

    function membership(item) {
        if (!viewer || creator(item)) return null;
        const leaving = joined(item);
        return <button type="button" className="activity-card-membership" data-joined={leaving} disabled={!leaving && full(item)} onClick={() => setPanel({ type: "membership", item, leaving })}>
            {leaving ? "Вы записаны · Отказаться" : full(item) ? "Мест нет" : "Записаться"}
        </button>;
    }

    return <main className="activities-screen">
        <header className="activities-header"><Back className="activities-back" to={path} /><h1>Активности</h1></header>
        <ResourceStatus resource={resource} url={activitiesUrl} className="activities-status" loading="Загружаем активности…" fallback="Не удалось загрузить активности." />
        {resource.data && (groups.size ? <>
            <div className="activities-list">{Array.from(groups, ([day, activities]) => <section className="activity-day" key={day}>
                <header><h2>{day}</h2><span className="activity-day-count">{activities.length}</span></header>
                <ul>{activities.map(item => <li className="activity-card" key={item.id}>
                    <button type="button" className="activity-card-open" onClick={() => setPanel({ type: "details", item })}>
                        <span className="activity-card-time">{[startOf(item), endOf(item)].filter(value => Number.isFinite(Date.parse(value))).map(value => timeFormat.format(new Date(value))).join("–") || "Время не указано"}</span>
                        <strong className="activity-card-title">{item.title}</strong>
                        {item.description && <span className="activity-card-description">{item.description}</span>}
                        <span className="activity-card-seats" data-full={full(item)}>{item.maxParticipants > 0 && item.maxParticipants < unlimited ? `Занято ${item.participants?.length ?? 0} из ${item.maxParticipants}` : "Без лимита"}</span>
                        {creator(item) && <span className="activity-card-role">Вы автор</span>}
                    </button>{membership(item)}
                </li>)}</ul>
            </section>)}</div>
            <button type="button" className="activities-add activities-add-floating" aria-label="Добавить активность" onClick={() => setPanel({ type: "form" })}>＋</button>
        </> : <section className="activities-empty"><div className="activities-empty-circle" aria-hidden="true" />
            <h2>Активностей пока нет</h2><p>Добавьте первую активность события.</p>
            <button type="button" className="activities-add" onClick={() => setPanel({ type: "form" })}>Добавить активность</button>
        </section>)}
        {panel?.type === "form" && <ActivityForm event={event} item={panel.item} close={close} save={data => change(panel.item ? `/${panel.item.id}` : "", panel.item ? "PATCH" : "POST", data)} />}
        {panel?.type === "details" && <Modal className="activities-dialog" title={panel.item.title} onClose={close}>
            <header><h2>{panel.item.title}</h2><button type="button" className="items-close" aria-label="Закрыть" onClick={close}>×</button></header>
            <dl className="activity-detail-times"><dt>Начало</dt><dd>{dateTime(startOf(panel.item))}</dd><dt>Окончание</dt><dd>{dateTime(endOf(panel.item))}</dd></dl>
            {panel.item.description && <p className="activity-description">{panel.item.description}</p>}
            <section><h3>Участники</h3>{panel.item.participants?.length ? <ul className="activity-participants">{panel.item.participants.map(person => <li key={person.id}>{person.name}{viewer === person.id ? " · Вы" : ""}</li>)}</ul> : <p>Пока никто не записался.</p>}</section>
            <footer className="activity-actions">{creator(panel.item) ? <>
                <button type="button" onClick={() => setPanel({ ...panel, type: "form" })}>Редактировать</button>
                <button type="button" onClick={() => setPanel({ ...panel, type: "delete" })}>Удалить</button>
            </> : membership(panel.item)}</footer>
        </Modal>}
        {panel?.type === "delete" && <ActionDialog title={`Удалить «${panel.item.title}»?`} className="activities-dialog" close={close} submitText="Удалить" fallback="Не удалось удалить активность." submit={() => change(`/${panel.item.id}`, "DELETE")} />}
        {panel?.type === "membership" && <ActionDialog title={panel.leaving ? "Отказаться от участия?" : "Записаться на активность?"} className="activities-dialog" close={close} submitText={panel.leaving ? "Отказаться" : "Записаться"} fallback="Не удалось изменить участие." submit={() => change(`/${panel.item.id}/${panel.leaving ? "leave" : "join"}`, "POST")} />}
    </main>;
}
