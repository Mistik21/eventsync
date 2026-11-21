import { fetchCurrentUser, getInitials } from "./current-user.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";

const find = id => document.getElementById(id);
const eventId = window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/budget\/?$/i)?.[1];
const api = `/api/events/${eventId}`;
const collectionNotStarted = 0;
const money = new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 });
const closed = find("budget-closed");
const active = find("budget-active");
const openButton = find("budget-open");
const status = find("budget-status");
const retry = find("budget-retry");
const addCostButton = find("budget-add-cost");
const costDialog = find("budget-cost-dialog");
const costForm = find("budget-cost-form");
const costName = find("budget-cost-name");
const costAmount = find("budget-cost-amount");
const costError = find("budget-cost-error");
const saveCostButton = find("budget-save-cost");
const closeCostButton = find("budget-close-cost");
let eventDetails;
let pendingLoad;
let opening = false;
let saving = false;

window.Telegram?.WebApp?.expand();

function showStatus(message, { error = false, canRetry = false } = {}) {
    status.textContent = message;
    status.dataset.error = String(error);
    status.hidden = !message;
    retry.hidden = !canRetry;
}

function requireSuccess(response) {
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`, { cause: response.status });
    }
    return response;
}

async function getJson(url) {
    return requireSuccess(await fetchApi(url, { cache: "no-store" })).json();
}

async function postBudgetAction(action, body) {
    const options = { method: "POST" };
    if (body !== undefined) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
    }
    requireSuccess(await fetchApi(`${api}/${action}`, options));
}

function requestErrorMessage(error, fallback) {
    const messages = {
        401: getSessionErrorMessage(),
        403: "У вас нет доступа к этому бюджету.",
        404: "Событие не найдено.",
    };
    return messages[error.cause] ?? fallback;
}

function showRequestError(error, fallback, canRetry = true) {
    showStatus(requestErrorMessage(error, fallback), { error: true, canRetry: canRetry && error.cause !== 401 });
}

function renderParticipant(participant, currentUserId, depositRequired) {
    const row = find("budget-row-template").content.firstElementChild.cloneNode(true);
    const isCurrentUser = participant.id === currentUserId;
    row.querySelector(".budget-avatar").textContent = getInitials(participant.name);
    row.querySelector("strong").textContent = isCurrentUser ? "Вы" : participant.name;
    row.querySelector("small").textContent = participant.debt === null ? "—" : money.format(participant.debt);
    const badge = row.querySelector(".budget-badge");
    if (isCurrentUser && participant.debt > 0) {
        badge.hidden = true;
        const payButton = row.querySelector(".budget-pay");
        payButton.hidden = false;
        payButton.addEventListener("click", () => markDebtPaid(payButton));
    } else if (participant.debt > 0) {
        badge.textContent = "Не внесено";
        badge.dataset.state = "due";
    } else if (participant.debt === 0 && depositRequired) {
        badge.textContent = "✓ Внесено";
        badge.dataset.state = "paid";
    } else {
        badge.textContent = participant.debt === 0 ? "Без взноса" : "Нет данных";
    }
    return row;
}

function renderCost(cost) {
    const row = document.createElement("li");
    const title = document.createElement("span");
    const amount = document.createElement("strong");
    title.textContent = cost.title;
    amount.textContent = money.format(cost.amount);
    row.append(title, amount);
    return row;
}

function renderOpenBudget(budget, participants, currentUserId) {
    find("budget-total").textContent = money.format(budget.budget);
    find("budget-collected").textContent = money.format(budget.collectedBudget);
    find("budget-spent").textContent = money.format(budget.spentAmount);
    find("budget-balance").textContent = money.format(budget.collectedBudget - budget.spentAmount);
    const progress = find("budget-progress");
    progress.max = Math.max(budget.budget, 1);
    progress.value = budget.collectedBudget;

    const contributors = participants.map(user => ({ ...user, debt: budget.debts?.[user.id] ?? null }));
    contributors.sort((left, right) => Number(right.id === currentUserId) - Number(left.id === currentUserId));
    const depositRequired = budget.budget >= contributors.length;
    const paid = contributors.filter(user => user.debt === 0).length;
    find("budget-paid-count").textContent = depositRequired && contributors.some(user => user.debt !== null)
        ? `Внесли ${paid} из ${contributors.length}` : "";

    const contributionList = find("budget-contributions");
    contributionList.replaceChildren(...contributors.map(user => renderParticipant(user, currentUserId, depositRequired)));
    contributionList.hidden = !contributors.length;
    find("budget-no-contributions").hidden = !contributionList.hidden;

    const costs = find("budget-costs");
    costs.replaceChildren(...budget.costs.map(renderCost));
    costs.hidden = !budget.costs.length;
    find("budget-no-costs").hidden = !costs.hidden;
    find("budget-costs-title").textContent = budget.costs.length ? `Траты · ${budget.costs.length}` : "Траты";

    active.hidden = false;
    find("budget-footer").hidden = !eventDetails.isOrganizer;
    if (opening) {
        find("budget-total").focus();
    }
}

async function readBudget(quiet) {
    try {
        const details = await getJson(api);
        if (quiet && details.collectionStatus === eventDetails?.collectionStatus) {
            return;
        }

        eventDetails = details;
        const isClosed = details.collectionStatus === collectionNotStarted;
        closed.hidden = !isClosed;
        openButton.hidden = !isClosed || !details.isOrganizer;
        if (isClosed) {
            active.hidden = true;
            find("budget-footer").hidden = true;
            find("budget-planned").textContent = money.format(details.budget);
        } else {
            if (active.hidden) {
                showStatus("Загружаем бюджет…");
            }
            const [budget, participants, viewer] = await Promise.all([
                getJson(`${api}/budget`),
                getJson(`${api}/participants`),
                fetchCurrentUser().then(requireSuccess).then(response => response.json()).catch(() => null),
            ]);
            const currentUserId = viewer?.id ?? (details.isOrganizer ? details.organizerId : null);
            renderOpenBudget(budget, participants, currentUserId);
        }
        showStatus("");
    } catch (error) {
        if (!quiet || eventDetails?.collectionStatus !== collectionNotStarted) {
            showRequestError(error, "Не удалось загрузить бюджет.");
        }
    }
}

function loadBudget({ quiet = false } = {}) {
    pendingLoad ??= readBudget(quiet).finally(() => { pendingLoad = undefined; });
    return pendingLoad;
}

async function markDebtPaid(button) {
    if (saving || active.hidden || button.disabled) {
        return;
    }

    saving = true;
    button.disabled = true;
    button.textContent = "Отмечаем…";
    addCostButton.disabled = true;
    showStatus("");
    let saved = false;
    try {
        await pendingLoad;
        await postBudgetAction("pay-debt");
        saved = true;
        await loadBudget();
        find("budget-contributions-title").focus();
    } catch (error) {
        showRequestError(error, "Не удалось отметить взнос.", false);
    } finally {
        saving = false;
        button.disabled = saved;
        button.textContent = saved ? "✓ Внесено" : "Я скинул";
        addCostButton.disabled = false;
    }
}

function showCostError(message) {
    costError.textContent = message;
    costError.hidden = !message;
}

addCostButton.addEventListener("click", () => {
    if (saving || active.hidden || !eventDetails?.isOrganizer || costDialog.open) {
        return;
    }
    costForm.reset();
    showCostError("");
    costDialog.showModal();
});

closeCostButton.addEventListener("click", () => costDialog.close());
costDialog.addEventListener("cancel", event => {
    if (saveCostButton.disabled) {
        event.preventDefault();
    }
});

costForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (saving || !costDialog.open || active.hidden || !eventDetails?.isOrganizer) {
        return;
    }

    const title = costName.value.trim();
    const amount = costAmount.valueAsNumber;
    if (!title) {
        showCostError("Введите название траты.");
        costName.focus();
        return;
    }
    if (!Number.isInteger(amount) || amount < 1 || amount > Number(costAmount.max)) {
        showCostError("Укажите сумму в целых рублях.");
        costAmount.focus();
        return;
    }

    saving = true;
    saveCostButton.disabled = true;
    saveCostButton.textContent = "Сохраняем…";
    closeCostButton.disabled = true;
    costName.readOnly = true;
    costAmount.readOnly = true;
    showCostError("");
    try {
        await pendingLoad;
        await postBudgetAction("costs", { title, amount });
        costDialog.close();
        await loadBudget();
    } catch (error) {
        showCostError(requestErrorMessage(error, "Не удалось добавить трату."));
    } finally {
        saving = false;
        saveCostButton.disabled = false;
        saveCostButton.textContent = "Создать";
        closeCostButton.disabled = false;
        costName.readOnly = false;
        costAmount.readOnly = false;
    }
});

openButton.addEventListener("click", async () => {
    if (!eventDetails?.isOrganizer || eventDetails.collectionStatus !== collectionNotStarted || opening) {
        return;
    }

    opening = true;
    openButton.disabled = true;
    openButton.textContent = "Открываем…";
    showStatus("");
    try {
        await pendingLoad;
        if (eventDetails.collectionStatus === collectionNotStarted) {
            await postBudgetAction("collection/open");
            closed.hidden = true;
            showStatus("Загружаем бюджет…");
        }
        await loadBudget();
    } catch (error) {
        showRequestError(error, "Не удалось открыть сбор.", false);
    } finally {
        opening = false;
        openButton.disabled = false;
        openButton.textContent = "Открыть";
    }
});

retry.addEventListener("click", () => void loadBudget());

if (eventId) {
    find("budget-back").href = `/html/event.html?id=${eventId}`;
    void loadBudget();
    window.addEventListener("pageshow", event => {
        if (event.persisted) {
            void loadBudget();
        }
    });
    window.setInterval(() => {
        if (!document.hidden && !opening && eventDetails?.collectionStatus === collectionNotStarted) {
            void loadBudget({ quiet: true });
        }
    }, 10000);
} else {
    showStatus("Событие не выбрано. Вернитесь к списку.", { error: true });
}
