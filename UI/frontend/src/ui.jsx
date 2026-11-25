import { useEffect, useRef, useState } from "react";
import { Link } from "./navigation";
import { message, refresh } from "./api";
import { lookupAddress, parseCoordinates } from "./two-gis.js";

export const money = new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 });
const dateFormat = new Intl.DateTimeFormat("ru-RU", { dateStyle: "long", timeStyle: "short" });
export function dateTime(value) {
    const date = new Date(value ?? NaN);
    return Number.isFinite(date.getTime()) ? dateFormat.format(date) : "Дата не указана";
}

export function webLink(value) {
    if (!value) return null;
    try {
        const url = new URL(value);
        return ["https:", "http:"].includes(url.protocol) ? url.href : null;
    } catch {
        return null;
    }
}

export function Cover({ src, ...props }) {
    const [broken, setBroken] = useState(null);
    if (!src || broken === src || (!webLink(src) && !/^data:image\/(jpeg|png|webp|gif|avif);base64,/i.test(src))) return null;
    return <img {...props} src={src} alt={props.alt ?? ""} referrerPolicy="no-referrer" onError={() => setBroken(src)} />;
}

export function Back({ to, className = "event-icon-button", label = "Назад к событию" }) {
    return <Link to={to} back className={className} aria-label={label}>←</Link>;
}

export function Loading({ children = "Загружаем…", className }) {
    return <p role="status" className={className} data-loading-status="">{children}</p>;
}

export function ResourceStatus({ resource, url, loading, fallback, className }) {
    if (resource.error) return <>
        <p role="status" className={className} data-error="true">{message(resource.error, fallback)}</p>
        {![401, 403, 404].includes(resource.error.status) && url && <button type="button" onClick={() => void refresh(url, true)}>Повторить</button>}
    </>;
    if (resource.data === undefined) return <Loading className={className}>{loading}</Loading>;
    return null;
}

export function Modal({ className, title, children, onClose, busy = false }) {
    const dialog = useRef(null);
    useEffect(() => {
        const element = dialog.current;
        element.showModal();
        return () => element.close();
    }, []);

    return <dialog aria-label={title} ref={dialog} className={className} onCancel={event => {
        event.preventDefault();
        if (!busy) onClose();
    }}>{children}</dialog>;
}

export function ActionDialog({ title, className, close, submit, submitText = "Сохранить", fallback, children }) {
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const submitting = useRef(false);

    async function save(event) {
        event.preventDefault();
        if (submitting.current) return;
        submitting.current = true;
        setBusy(true);
        setError("");
        try {
            await submit(new FormData(event.currentTarget));
            close();
        } catch (error) {
            setError(error.validation ?? message(error, fallback));
        } finally {
            submitting.current = false;
            setBusy(false);
        }
    }

    const family = className.split("-")[0];
    const buttonClass = { items: "items-primary", activities: "activities-primary", budget: "budget-button" }[family];
    const fieldsClass = { items: "items-fields", activities: "activity-fields", budget: "budget-fields" }[family];
    return <Modal title={title} className={className} onClose={close} busy={busy}>
        <header><h2>{title}</h2><button type="button" className="items-close" aria-label="Закрыть" disabled={busy} onClick={close}>×</button></header>
        <form onSubmit={save}>
            {children && <fieldset className={fieldsClass} disabled={busy}>{children}</fieldset>}
            <footer>{error && <p role="alert" data-error="true">{error}</p>}
                <button type="submit" className={buttonClass} disabled={busy}>{busy ? "Сохраняем…" : submitText}</button>
            </footer>
        </form>
    </Modal>;
}

export const invalid = validation => Object.assign(new Error(validation), { validation });

const addresses = new Map();
export function useAddress(location, visible = true) {
    const supplied = location?.address?.trim();
    const key = location?.coordinates;
    const point = supplied ? null : parseCoordinates(key);
    const [resolved, setResolved] = useState(null);
    useEffect(() => {
        if (!point || !visible) return;
        let active = true;
        if (!addresses.has(key)) addresses.set(key, lookupAddress(point).catch(() => ""));
        void addresses.get(key).then(address => { if (active) setResolved({ key, address }); });
        return () => { active = false; };
    }, [key, supplied, visible]);

    if (supplied) return supplied;
    if (!point) return "Место не указано";
    return resolved?.key === key ? resolved.address || "Не удалось определить адрес" : "Определяем адрес…";
}
