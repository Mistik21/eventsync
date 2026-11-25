import { useState } from "react";
import { message, profileUrl, refresh, useResource, write } from "./api";
import { ActionDialog, Back, invalid, Modal, ResourceStatus, webLink } from "./ui";
import "../../wwwroot/css/items.css";

const responsibleId = item => item.responsibleID ?? item.responsibleId;

function ItemForm({ item, save, close }) {
    const [count, setCount] = useState(String(item?.count ?? 1));
    return <ActionDialog title={item ? "Редактировать предмет" : "Новый предмет"} className="items-dialog" close={close} fallback="Не удалось сохранить предмет." submit={values => {
        const title = values.get("title").trim();
        const url = values.get("urlShop").trim();
        if (!title) throw invalid("Введите название предмета.");
        if (url && !webLink(url)) throw invalid("Введите ссылку, которая начинается с http:// или https://.");
        return save({ title, count: Number(count), description: values.get("description").trim(), urlShop: url || null });
    }}>
        <label htmlFor="item-title">Название</label><input id="item-title" name="title" maxLength={120} required defaultValue={item?.title} />
        <label htmlFor="item-url">Ссылка на магазин</label><input id="item-url" name="urlShop" type="url" defaultValue={item?.urlShop ?? ""} />
        <label htmlFor="item-count">Количество</label>
        <div className="items-stepper">
            <button type="button" aria-label="Уменьшить количество" disabled={Number(count) <= 1} onClick={() => setCount(String(Math.max(1, Number(count) - 1)))}>−</button>
            <input id="item-count" type="number" min="1" max="2147483647" step="1" required value={count} onChange={e => setCount(e.target.value)} />
            <button type="button" aria-label="Увеличить количество" onClick={() => setCount(String(Math.min(2147483647, Number(count) + 1)))}>+</button>
        </div>
        <label htmlFor="item-description">Заметки</label><textarea id="item-description" name="description" maxLength={500} rows={3} defaultValue={item?.description} />
    </ActionDialog>;
}

export default function Items({ event, url, path }) {
    const itemsUrl = `${url}/items`;
    const resource = useResource(itemsUrl);
    const items = resource.data ?? [];
    const taken = items.filter(responsibleId).length;
    const participants = useResource(taken ? `${url}/participants` : null).data ?? [];
    const viewer = useResource(taken && !event.isOrganizer ? profileUrl : null).data?.id ?? (event.isOrganizer ? event.organizerId : null);
    const [panel, setPanel] = useState(null);
    const [assignment, setAssignment] = useState(null);
    const [error, setError] = useState("");
    const close = () => setPanel(null);
    const owner = item => participants.find(person => person.id === responsibleId(item))?.name ?? "Участник события";

    async function change(suffix, method, body) {
        try { await write(`${itemsUrl}${suffix}`, method, body); }
        catch (error) {
            if (error.status === 409) void refresh(itemsUrl, true);
            throw error;
        }
        await refresh(itemsUrl, true);
    }

    async function assign(item) {
        const action = responsibleId(item) ? "unassign" : "assign";
        setAssignment(item.id);
        setError("");
        try { await change(`/${item.id}/${action}`, "POST"); }
        catch (error) { setError(message(error, "Не удалось обновить назначение.")); }
        finally { setAssignment(null); }
    }

    return <main className="items-screen">
        <header className="items-header"><Back to={path} className="items-back" /><h1>Чек-лист</h1></header>
        <ResourceStatus resource={resource} url={itemsUrl} className="items-status" retryClassName="items-retry" loading="Загружаем чек-лист…" fallback="Не удалось загрузить чек-лист." />
        {error && <p role="alert" className="items-status" data-error="true">{error}</p>}
        {resource.data && (items.length ? <>
            <section className="items-progress" aria-label="Прогресс чек-листа">
                <div className="items-progress-heading"><strong>Взято {taken} из {items.length}</strong><span>{Math.round(taken / items.length * 100)}%</span></div>
                <progress value={taken} max={items.length} />
            </section>
            <ul className="items-list">{items.map(item => {
                const responsible = responsibleId(item);
                const mine = Boolean(responsible && responsible === viewer);
                return <li className="item-card" data-assigned={Boolean(responsible)} key={item.id}>
                    <button type="button" className="item-open" onClick={() => setPanel({ type: "details", item })}>
                        <span className="item-icon" aria-hidden="true" /><span className="item-summary"><strong>{item.title}</strong><small>{item.count} шт.</small></span>
                        {responsible && !mine && <span className="item-owner">✓ Берёт {owner(item)}</span>}
                    </button>
                    {(!responsible || mine) && <button type="button" className="item-action" disabled={Boolean(assignment)} data-action={mine ? "unassign" : "assign"} onClick={() => void assign(item)}>{mine ? "Не принесу" : "Взять"}</button>}
                </li>;
            })}</ul>
            <button type="button" className="items-add" aria-label="Добавить предмет" onClick={() => setPanel({ type: "form" })}>＋</button>
        </> : <section className="items-empty">
            <div className="items-empty-circle" aria-hidden="true" /><h2>Чек-лист пока пуст</h2><p>Добавьте то, что нужно взять с собой.</p>
            <button type="button" className="items-primary" onClick={() => setPanel({ type: "form" })}>Добавить предмет</button>
        </section>)}
        {panel?.type === "form" && <ItemForm item={panel.item} close={close} save={data => change(panel.item ? `/${panel.item.id}` : "", panel.item ? "PATCH" : "POST", data)} />}
        {panel?.type === "details" && <Modal className="items-dialog" title={panel.item.title} onClose={close}>
            <header><h2>{panel.item.title}</h2><button className="items-close" type="button" aria-label="Закрыть" onClick={close}>×</button></header>
            <dl className="item-details"><dt>Количество</dt><dd>{panel.item.count} шт.</dd><dt>Ответственный</dt><dd>{responsibleId(panel.item) ? responsibleId(panel.item) === viewer ? "Вы" : owner(panel.item) : "Свободно"}</dd>
                {panel.item.description && <><dt>Заметки</dt><dd>{panel.item.description}</dd></>}
                {webLink(panel.item.urlShop) && <><dt>Магазин</dt><dd><a target="_blank" rel="noreferrer" href={webLink(panel.item.urlShop)}>Открыть ссылку</a></dd></>}
            </dl>
            <footer className="item-details-actions">
                <button className="item-details-button" type="button" onClick={() => setPanel({ ...panel, type: "form" })}>Редактировать</button>
                <button className="item-details-button item-delete" type="button" onClick={() => setPanel({ ...panel, type: "delete" })}>Удалить</button>
            </footer>
        </Modal>}
        {panel?.type === "delete" && <ActionDialog title={`Удалить «${panel.item.title}»?`} className="items-dialog item-delete-dialog" close={close} danger submitText="Удалить" fallback="Не удалось удалить предмет." submit={() => change(`/${panel.item.id}`, "DELETE")} />}
    </main>;
}
