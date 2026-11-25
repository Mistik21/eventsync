import { useEffect, useRef, useState } from "react";
import { eventApi, eventsUrl, expire, message, profileUrl, refresh, useResource, write } from "./api";
import { eventPath, useNavigation } from "./navigation";
import { Back, Cover, invalid, Loading, ResourceStatus } from "./ui";
import { formatCoordinates, loadMapGl, lookupAddress, parseCoordinates } from "./two-gis.js";
import "../../wwwroot/css/create-event.css";

function localStart(timestamp) {
    const date = new Date(timestamp ?? NaN);
    if (!Number.isFinite(date.getTime())) return "";
    return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}

async function encodeCover(url) {
    const image = new Image();
    image.src = url;
    await image.decode();
    const canvas = document.createElement("canvas");
    canvas.width = 840;
    canvas.height = 360;
    const context = canvas.getContext("2d");
    const scale = Math.max(canvas.width / image.naturalWidth, canvas.height / image.naturalHeight);
    const width = image.naturalWidth * scale;
    const height = image.naturalHeight * scale;
    context.fillStyle = "white";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(image, (canvas.width - width) / 2, (canvas.height - height) / 2, width, height);
    const result = canvas.toDataURL("image/jpeg", 0.8);
    return result.length <= 220_000 ? result : canvas.toDataURL("image/jpeg", 0.6);
}

function MapPicker({ previous, confirm, unavailable }) {
    const container = useRef(null);
    const [ready, setReady] = useState(false);
    const [point, setPoint] = useState(() => parseCoordinates(previous));
    const failure = useRef(unavailable);
    useEffect(() => {
        const location = parseCoordinates(previous);
        let active = true;
        let map;
        let marker;
        void loadMapGl().then(({ mapgl, apiKey }) => {
            if (!active) return;
            const center = location ? [location.lng, location.lat] : [60.6057, 56.8389];
            map = new mapgl.Map(container.current, { key: apiKey, center, zoom: location ? 15 : 12, lang: "ru" });
            function select(coordinates) {
                const [lng, lat] = coordinates;
                marker?.destroy();
                marker = new mapgl.Marker(map, { coordinates });
                setPoint({ lng, lat });
            }
            if (location) select(center);
            map.on("click", event => select(event.lngLat));
            setReady(true);
        }).catch(() => { if (active) failure.current(); });
        return () => { active = false; marker?.destroy(); map?.destroy(); };
    }, [previous]);

    return <div className="meeting-map-panel" id="meeting-map-panel">
        {!ready && <Loading className="meeting-location-status">Загружаем карту…</Loading>}
        <div className="meeting-map" role="region" aria-label="Выбор места встречи на карте 2ГИС" ref={container} />
        <button className="meeting-map-confirm" type="button" disabled={!ready || !point} onClick={() => confirm(point)}>Подтвердить</button>
    </div>;
}

function Fields({ id, event }) {
    const { navigate } = useNavigation();
    const editing = Boolean(id);
    const savedStart = localStart(event?.eventStartTime);
    const [start, setStart] = useState(savedStart);
    const [location, setLocation] = useState(() => ({ address: event?.eventLocation?.address ?? "", coordinates: event?.eventLocation?.coordinates ?? "" }));
    const [mapOpen, setMapOpen] = useState(false);
    const [mapError, setMapError] = useState(false);
    const [preview, setPreview] = useState(null);
    const [removed, setRemoved] = useState(false);
    const [error, setError] = useState("");
    const [saving, setSaving] = useState(false);
    const selection = useRef(null);
    const fileInput = useRef(null);
    const running = useRef(false);

    useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

    function choosePoint(point) {
        const coordinates = formatCoordinates(point);
        setLocation({ address: "", coordinates });
        setMapOpen(false);
        const request = lookupAddress(point).catch(() => "").then(address => ({ address: address || coordinates, coordinates }));
        selection.current = request;
        void request.then(value => {
            if (selection.current === request) setLocation(value);
        });
    }

    async function save(eventSubmit) {
        eventSubmit.preventDefault();
        if (running.current) return;
        const values = new FormData(eventSubmit.currentTarget);
        const title = values.get("title").trim();
        const description = values.get("description").trim();
        const begins = new Date(start);
        if (!title || !description || !Number.isFinite(begins.getTime())) {
            setError("Заполните название, описание и дату события.");
            return;
        }
        if (!location.address.trim() && !location.coordinates) {
            setError("Укажите место встречи.");
            return;
        }

        running.current = true;
        setSaving(true);
        setError("");
        try {
            const [place, mainImageUrl] = await Promise.all([
                selection.current ?? { ...location, address: location.address.trim() },
                preview ? encodeCover(preview).catch(() => { throw invalid("Не удалось подготовить обложку. Выберите другое изображение."); }) : editing && !removed ? null : "",
            ]);
            const eventStartTime = editing && start === savedStart ? event.eventStartTime : begins.toISOString();
            const budget = Number(values.get("budget") || (editing ? event.budget : 0));
            await write(editing ? eventApi(id) : eventsUrl, editing ? "PATCH" : "POST", { title, description, eventStartTime, eventLocation: place, budget, mainImageUrl });
            expire(profileUrl);
            void refresh(eventsUrl, true);
            if (editing) await refresh(eventApi(id), true);
            navigate(editing ? eventPath(id) : "/", { replace: true });
        } catch (error) {
            setError(error.validation ?? message(error, editing ? "Не удалось сохранить изменения." : "Не удалось подтвердить сохранение. Перед повтором проверьте «Мои события»."));
        } finally {
            running.current = false;
            setSaving(false);
        }
    }

    const original = !removed && event?.mainImageUrl;
    const shift = editing ? new Date(start) - new Date(event.eventStartTime) : 0;
    return <form className="create-form" onSubmit={save}>
        <fieldset className="create-fields" disabled={saving}>
            <div className="cover-control">
                <label className="cover-picker" htmlFor="event-cover">
                    <span className="cover-label">{preview || original ? "Изменить обложку" : "Добавить обложку"}</span>
                    {preview ? <img className="cover-preview" src={preview} alt="" /> : <Cover className="cover-preview" src={original} />}
                    <input ref={fileInput} id="event-cover" type="file" accept="image/jpeg,image/png,image/webp" onChange={e => {
                        const file = e.target.files[0];
                        if (!file) return;
                        if (!/^image\/(jpeg|png|webp)$/.test(file.type)) { setError("Выберите изображение JPEG, PNG или WebP."); return; }
                        setPreview(URL.createObjectURL(file));
                        setError("");
                    }} />
                </label>
                {(preview || original) && <button className="cover-remove" type="button" aria-label="Удалить обложку" onClick={() => {
                    fileInput.current.value = "";
                    setPreview(null);
                    setRemoved(true);
                }}>×</button>}
            </div>
            <div className="create-field"><label htmlFor="event-title">Название события *</label><input id="event-title" name="title" required defaultValue={event?.title} /></div>
            <div className="create-field"><label htmlFor="event-description">Описание *</label><textarea id="event-description" name="description" rows={3} required defaultValue={event?.description} /></div>
            <div className="create-field"><label htmlFor="event-start">Дата и время *</label><input id="event-start" type="datetime-local" required value={start} onChange={e => setStart(e.target.value)} />
                {editing && start !== savedStart && Number.isFinite(shift) && shift !== 0 && <small>Активности сдвинутся {shift > 0 ? "вперёд" : "назад"} вместе с событием.</small>}
            </div>
            <div className="create-field meeting-location">
                <label htmlFor="meeting-address">Место встречи *</label>
                <input id="meeting-address" value={location.address} placeholder={location.coordinates ? "Определяем адрес…" : "Введите адрес места встречи"} onChange={e => {
                    selection.current = null;
                    setLocation({ address: e.target.value, coordinates: "" });
                }} />
                <button className="meeting-map-toggle" type="button" aria-expanded={mapOpen} onClick={() => { setMapError(false); setMapOpen(!mapOpen); }}>Выбрать на карте 2ГИС</button>
                {mapError && <p className="meeting-location-status" role="status">Карта недоступна. Укажите адрес вручную.</p>}
                {mapOpen && <MapPicker previous={location.coordinates} confirm={choosePoint} unavailable={() => { setMapError(true); setMapOpen(false); }} />}
            </div>
            <div className="create-field"><label htmlFor="event-budget">Общий бюджет мероприятия</label>
                <input id="event-budget" type="number" name="budget" min="0" max="2147483647" step="1" placeholder="0" defaultValue={event?.budget} />
            </div>
        </fieldset>
        {error && <p className="create-status create-status--error" role="alert">{error}</p>}
        <button className="create-submit" type="submit" disabled={saving}>{saving ? "Сохраняем…" : "Сохранить"}</button>
    </form>;
}

export default function EventForm({ id }) {
    const editing = Boolean(id);
    const url = editing ? eventApi(id) : null;
    const resource = useResource(url);
    const allowed = !editing || resource.data?.isOrganizer && resource.data.collectionStatus === 0;
    return <main className={editing ? "create-screen edit-screen" : "create-screen"}>
        <header className="create-header"><Back className="create-back" to={editing ? eventPath(id) : "/"} label={editing ? "Назад к событию" : "Вернуться к моим событиям"} />
            <h1>{editing ? "Редактировать событие" : "Создать событие"}</h1>
        </header>
        {editing && <ResourceStatus resource={resource} url={url} className="create-status" loading="Загружаем событие…" fallback="Не удалось загрузить событие." />}
        {editing && resource.data && !allowed && <p className="create-status create-status--error" role="alert">{resource.data.isOrganizer ? "Сбор уже открыт. Редактирование недоступно." : "Редактировать событие может только организатор."}</p>}
        {allowed && <Fields id={id} event={resource.data} />}
    </main>;
}
