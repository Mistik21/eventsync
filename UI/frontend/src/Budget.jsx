import { useEffect, useState } from "react";
import { message, profileUrl, refresh, useResource, write } from "./api";
import { ActionDialog, Back, invalid, Modal, money, ResourceStatus } from "./ui";
import { getInitials } from "./current-user.js";
import "../../wwwroot/css/budget.css";

function CostForm({ cost, close, save }) {
    return <ActionDialog className="budget-cost-dialog" title={cost ? "Редактирование траты" : "Новая запись"} close={close} submitText={cost ? "Сохранить" : "Создать"} fallback="Не удалось сохранить трату." submit={values => {
        const title = values.get("title").trim();
        const amount = Number(values.get("amount"));
        if (!title || !Number.isInteger(amount) || amount < (cost ? 0 : 1)) throw invalid("Проверьте название и сумму траты.");
        return save(cost ? { amount } : { title, amount });
    }}>
        <div className="budget-cost-field">
            <label htmlFor="cost-title">На что потратили</label>
            <input id="cost-title" name="title" required readOnly={Boolean(cost)} defaultValue={cost?.title} maxLength={120} placeholder="Введите текст…" />
        </div>
        <AmountField id="cost-amount" label="Сумма" min={cost ? 0 : 1} max={2147483647} amount={cost?.amount} />
    </ActionDialog>;
}

function AmountField({ id, label, min, max, amount }) {
    return <div className="budget-cost-field">
        <label htmlFor={id}>{label}</label>
        <div className="budget-amount-field">
            <input id={id} name="amount" type="number" inputMode="numeric" min={min} max={max} step={1} defaultValue={amount} placeholder="0" required />
            <span aria-hidden="true">₽</span>
        </div>
    </div>;
}

export default function Budget({ event, path, url }) {
    const open = event.collectionStatus === 1;
    const budgetUrl = `${url}/budget`;
    const peopleUrl = `${url}/participants`;
    const resource = useResource(open ? budgetUrl : null);
    const people = useResource(open ? peopleUrl : null);
    const viewer = useResource(open && !event.isOrganizer ? profileUrl : null).data?.id ?? (event.isOrganizer ? event.organizerId : null);
    const [panel, setPanel] = useState(null);
    const [opening, setOpening] = useState(false);
    const [error, setError] = useState("");
    const [now, setNow] = useState(Date.now);
    const close = () => setPanel(null);
    const budget = resource.data;
    const debts = budget?.debts ?? {};
    const canEdit = open && event.isOrganizer && Date.parse(event.eventStartTime) > now;
    const supplemental = open && event.isOrganizer && !canEdit && budget?.remainingBudget < 0 && Object.keys(debts).length > 0;

    useEffect(() => {
        const untilStart = Date.parse(event.eventStartTime) - Date.now();
        if (untilStart <= 0 || !Number.isFinite(untilStart)) return;
        const timer = setTimeout(() => setNow(Date.now()), Math.min(untilStart + 10, 2_147_483_647));
        return () => clearTimeout(timer);
    }, [event.eventStartTime, now]);

    useEffect(() => {
        if (event.collectionStatus !== 0) return;
        const timer = setInterval(() => { if (!document.hidden) void refresh(url, true); }, 10_000);
        return () => clearInterval(timer);
    }, [event.collectionStatus, url]);

    async function change(action, method, data) {
        try { await write(`${url}/${action}`, method, data); }
        catch (error) {
            if (error.status === 409) {
                void refresh(url, true);
                if (open) void refresh(budgetUrl, true);
            }
            throw error;
        }
        await Promise.all([refresh(url, true), open ? refresh(budgetUrl, true) : null]);
    }

    async function openCollection() {
        setOpening(true);
        setError("");
        try { await change("collection/open", "POST"); }
        catch (error) { setError(message(error, "Не удалось открыть сбор.")); }
        finally { setOpening(false); }
    }

    const contributors = [...(people.data ?? [])].sort((first, second) => Number(second.id === viewer) - Number(first.id === viewer));
    const paid = contributors.filter(person => debts[person.id] === 0).length;
    return <main className="budget-screen">
        <header className="budget-header"><Back to={path} className="budget-back" /><h1>Бюджет</h1></header>
        {error && <p className="budget-status" data-error="true" role="alert">{error}</p>}
        {!open ? <section className="budget-closed">
            <div className="budget-circle" aria-hidden="true" /><h2>Сбор пока закрыт</h2><p>Планируемый бюджет: {money.format(event.budget)}</p>
            {event.isOrganizer && event.collectionStatus === 0 && <button className="budget-button" type="button" disabled={opening} onClick={() => void openCollection()}>{opening ? "Открываем…" : "Открыть сбор"}</button>}
        </section> : <>
            <ResourceStatus resource={resource} url={budgetUrl} className="budget-status" retryClassName="budget-retry" loading="Загружаем бюджет…" fallback="Не удалось загрузить бюджет." />
            {budget && <div className="budget-active">
                <section className="budget-summary" aria-label="Сводка бюджета"><h2>{money.format(budget.budget)}</h2>
                    <progress value={budget.collectedBudget} max={Math.max(1, budget.budget)} />
                    <dl className="budget-totals">
                        <div><dt>Собрано</dt><dd>{money.format(budget.collectedBudget)}</dd></div>
                        <div><dt>Потрачено</dt><dd>{money.format(budget.spentAmount)}</dd></div>
                        <div><dt>Остаток</dt><dd>{money.format(budget.collectedBudget - budget.spentAmount)}</dd></div>
                    </dl>
                </section>
                <section><div className="budget-section-heading"><h2>Взносы</h2>{contributors.some(person => person.id in debts) && <span>Внесли {paid} из {contributors.length}</span>}</div>
                    <ResourceStatus resource={people} url={peopleUrl} className="budget-status" retryClassName="budget-retry" loading="Загружаем участников…" fallback="Не удалось загрузить взносы." />
                    {people.data && (contributors.length ? <ul className="budget-list budget-people">{contributors.map(person => {
                        const debt = debts[person.id];
                        const mine = person.id === viewer;
                        return <li className="budget-person" key={person.id}>
                            <span className="budget-avatar" aria-hidden="true">{getInitials(person.name)}</span>
                            <span className="budget-person-details"><strong>{mine ? "Вы" : person.name}</strong><small>{debt == null ? "—" : money.format(debt)}</small></span>
                            {mine && debt > 0 ? <button className="budget-button budget-pay" type="button" onClick={() => setPanel({ type: "pay", debt })}>Внести</button> :
                                <span className="budget-badge" data-state={debt === 0 ? "paid" : debt > 0 ? "due" : undefined}>{debt === 0 ? "✓ Внесено" : debt > 0 ? "Не внесено" : "Нет данных"}</span>}
                        </li>;
                    })}</ul> : <p className="budget-empty">Участников пока нет.</p>)}
                </section>
                <section><h2 className="budget-section-heading">Траты {budget.costs.length > 0 && `· ${budget.costs.length}`}</h2>
                    {budget.costs.length ? <ul className="budget-list">{budget.costs.map(cost => <li key={cost.id}>
                        {event.isOrganizer ? <button className="budget-cost-entry" type="button" onClick={() => setPanel({ type: "details", cost })}><span>{cost.title}</span><strong>{money.format(cost.amount)}</strong></button>
                            : <div className="budget-cost-entry"><span>{cost.title}</span><strong>{money.format(cost.amount)}</strong></div>}
                    </li>)}</ul> : <p className="budget-empty">Трат пока нет.</p>}
                </section>
            </div>}
            {budget && (canEdit || supplemental) && <footer className="budget-footer">
                {canEdit && <button className="budget-button" type="button" onClick={() => setPanel({ type: "form" })}>Добавить трату</button>}
                {supplemental && <button className="budget-button" type="button" onClick={() => setPanel({ type: "supplemental" })}>Распределить перерасход · {money.format(-budget.remainingBudget)}</button>}
            </footer>}
        </>}
        {panel?.type === "form" && <CostForm cost={panel.cost} close={close} save={amount => change(panel.cost ? `costs/${panel.cost.id}` : "costs", panel.cost ? "PATCH" : "POST", amount)} />}
        {panel?.type === "details" && <Modal className="budget-cost-dialog" title={panel.cost.title} onClose={close}>
            <header><h2>{panel.cost.title}</h2><button className="budget-close-cost" aria-label="Закрыть" type="button" onClick={close}>×</button></header>
            <dl className="budget-cost-details-field"><dt>Сумма</dt><dd>{money.format(panel.cost.amount)}</dd></dl>
            {canEdit && <footer className="budget-cost-actions">
                <button className="budget-button budget-button-secondary" type="button" onClick={() => setPanel({ ...panel, type: "form" })}>Редактировать</button>
                <button className="budget-button budget-button-danger" type="button" onClick={() => setPanel({ ...panel, type: "delete" })}>Удалить</button>
            </footer>}
        </Modal>}
        {panel?.type === "delete" && <ActionDialog className="budget-cost-dialog" title="Удалить трату?" close={close} danger submitText="Удалить" fallback="Не удалось удалить трату." submit={() => change(`costs/${panel.cost.id}`, "DELETE")} />}
        {panel?.type === "pay" && <ActionDialog className="budget-cost-dialog" title="Внести взнос" close={close} submitText="Отметить взнос" fallback="Не удалось отметить взнос." submit={values => change("pay-debt", "POST", { amount: Number(values.get("amount")) })}>
            <AmountField id="pay-amount" label="Сумма взноса" min={0} max={panel.debt} amount={panel.debt} />
        </ActionDialog>}
        {panel?.type === "supplemental" && <ActionDialog className="budget-cost-dialog" title="Распределить перерасход?" close={close} submitText="Начислить доплату" fallback="Не удалось начислить доплату." submit={() => change("collection/supplemental", "POST")} />}
    </main>;
}
