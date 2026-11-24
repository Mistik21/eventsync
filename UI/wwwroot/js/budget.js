import { fetchCurrentUser, getInitials } from "./current-user.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";

const find = id => document.getElementById(id);
const eventId = window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/budget\/?$/i)?.[1];
const api = `/api/events/${eventId}`;
const collectionNotStarted = 0;
const collectionOpen = 1;
const money = new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 });
const closed = find("budget-closed");
const active = find("budget-active");
const openButton = find("budget-open");
const status = find("budget-status");
const retry = find("budget-retry");
const footer = find("budget-footer");
const addCostButton = find("budget-add-cost");
const supplemental = find("budget-supplemental");
const calculateDebtButton = find("budget-calculate-debt");
const payDialog = find("budget-pay-dialog");
const payForm = find("budget-pay-form");
const payAmount = find("budget-pay-amount");
const payError = find("budget-pay-error");
const savePayButton = find("budget-save-pay");
const closePayButton = find("budget-close-pay");
const costsTitle = find("budget-costs-title");
const costDetails = find("budget-cost-details");
const editCostButton = find("budget-edit-cost");
const deleteCostButton = find("budget-delete-cost");
const costDialog = find("budget-cost-dialog");
const costForm = find("budget-cost-form");
const costFormTitle = find("budget-cost-dialog-title");
const costName = find("budget-cost-name");
const costAmount = find("budget-cost-amount");
const costError = find("budget-cost-error");
const saveCostButton = find("budget-save-cost");
const closeCostButton = find("budget-close-cost");
const deleteDialog = find("budget-delete-dialog");
const deleteError = find("budget-delete-error");
const cancelDeleteButton = find("budget-cancel-delete");
const confirmDeleteButton = find("budget-confirm-delete");
let eventDetails;
let budgetDetails;
let selectedCost;
let pendingLoad;
let startTimer;
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

async function sendBudgetAction(action, method, body) {
    const options = { method };
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

function timeUntilStart() {
    return Date.parse(eventDetails?.eventStartTime) - Date.now();
}

function canManageOpenBudget() {
    return !active.hidden && eventDetails?.isOrganizer && eventDetails.collectionStatus === collectionOpen;
}

function canEditCosts(untilStart = timeUntilStart()) {
    return canManageOpenBudget() && untilStart > 0;
}

function canCalculateDebt(untilStart = timeUntilStart()) {
    return canManageOpenBudget() && untilStart <= 0 && budgetDetails?.remainingBudget < 0
        && Object.keys(budgetDetails.debts ?? {}).length > 0;
}

function updateBudgetActions() {
    window.clearTimeout(startTimer);
    const untilStart = timeUntilStart();
    const editable = canEditCosts(untilStart);
    const canCalculate = canCalculateDebt(untilStart);
    addCostButton.hidden = !editable;
    find("budget-edit-cost-actions").hidden = !editable;
    supplemental.hidden = !canCalculate;
    footer.hidden = !editable && !canCalculate;

    if (canCalculate) {
        find("budget-shortfall").textContent = money.format(-budgetDetails.remainingBudget);
    }
    if (editable) {
        startTimer = window.setTimeout(updateBudgetActions, Math.min(untilStart, 2_147_483_647));
    } else if (untilStart <= 0) {
        const message = "Событие уже началось. Траты больше нельзя менять.";
        if (costDialog.open) {
            saveCostButton.disabled = true;
            showDialogError(costError, message);
        }
        if (deleteDialog.open) {
            confirmDeleteButton.disabled = true;
            showDialogError(deleteError, message);
        }
    }
}

function renderParticipant(participant, currentUserId) {
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
        payButton.addEventListener("click", () => openPayForm(participant.debt));
    } else if (participant.debt > 0) {
        badge.textContent = "Не внесено";
        badge.dataset.state = "due";
    } else if (participant.debt === 0) {
        badge.textContent = "✓ Внесено";
        badge.dataset.state = "paid";
    } else {
        badge.textContent = "Нет данных";
    }
    return row;
}

function renderCost(cost) {
    const row = document.createElement("li");
    const entry = document.createElement(eventDetails.isOrganizer ? "button" : "div");
    entry.className = "budget-cost-entry";
    if (eventDetails.isOrganizer) {
        entry.type = "button";
        entry.setAttribute("aria-haspopup", "dialog");
        entry.addEventListener("click", () => openCostDetails(cost));
    }
    const title = document.createElement("span");
    const amount = document.createElement("strong");
    title.textContent = cost.title;
    amount.textContent = money.format(cost.amount);
    entry.append(title, amount);
    row.append(entry);
    return row;
}

function renderOpenBudget(budget, participants, currentUserId) {
    budgetDetails = budget;
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
    contributionList.replaceChildren(...contributors.map(user => renderParticipant(user, currentUserId)));
    contributionList.hidden = !contributors.length;
    find("budget-no-contributions").hidden = !contributionList.hidden;

    const costs = find("budget-costs");
    costs.replaceChildren(...budget.costs.map(renderCost));
    costs.hidden = !budget.costs.length;
    find("budget-no-costs").hidden = !costs.hidden;
    costsTitle.textContent = budget.costs.length ? `Траты · ${budget.costs.length}` : "Траты";

    active.hidden = false;
    updateBudgetActions();
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
        const isClosed = details.collectionStatus !== collectionOpen;
        closed.hidden = !isClosed;
        openButton.hidden = details.collectionStatus !== collectionNotStarted || !details.isOrganizer;
        if (isClosed) {
            active.hidden = true;
            budgetDetails = undefined;
            updateBudgetActions();
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
        budgetDetails = undefined;
        updateBudgetActions();
        if (!quiet || eventDetails?.collectionStatus !== collectionNotStarted) {
            showRequestError(error, "Не удалось загрузить бюджет.");
        }
    }
}

function loadBudget({ quiet = false } = {}) {
    pendingLoad ??= readBudget(quiet).finally(() => { pendingLoad = undefined; });
    return pendingLoad;
}

function openPayForm(debt) {
    if (saving || active.hidden || payDialog.open || eventDetails?.collectionStatus !== collectionOpen) {
        return;
    }

    payAmount.max = String(debt);
    payAmount.value = String(debt);
    showDialogError(payError, "");
    payDialog.showModal();
}

function showDialogError(field, message) {
    field.textContent = message;
    field.hidden = !message;
}

function costRequestError(error, fallback) {
    if (error.cause === 409 && timeUntilStart() <= 0) {
        return "Событие уже началось. Траты больше нельзя менять.";
    }
    return error.cause === 404 ? "Трата не найдена." : requestErrorMessage(error, fallback);
}

function openCostDetails(cost) {
    if (saving || costDetails.open) {
        return;
    }
    selectedCost = cost;
    find("budget-cost-details-title").textContent = cost.title;
    find("budget-cost-details-amount").textContent = money.format(cost.amount);
    costDetails.showModal();
}

function openCostForm(cost = null) {
    if (saving || !canEditCosts() || costDialog.open) {
        return;
    }
    selectedCost = cost;
    costForm.reset();
    costFormTitle.textContent = cost ? "Редактирование траты" : "Новая запись";
    costName.value = cost?.title ?? "";
    costName.readOnly = Boolean(cost);
    costAmount.value = cost?.amount ?? "";
    costAmount.min = cost ? "0" : "1";
    saveCostButton.textContent = cost ? "Сохранить" : "Создать";
    showDialogError(costError, "");
    if (cost) {
        costDetails.close();
    }
    costDialog.showModal();
    if (cost) {
        costAmount.focus();
    }
}

function closeCostForm() {
    costDialog.close();
    if (selectedCost) {
        costDetails.showModal();
        (canEditCosts() ? editCostButton : find("budget-close-details")).focus();
    }
}

function openDeleteConfirmation() {
    if (saving || !costDetails.open || !selectedCost || !canEditCosts()) {
        return;
    }
    find("budget-delete-title").textContent = `Вы уверены, что хотите удалить позицию: ${selectedCost.title}?`;
    showDialogError(deleteError, "");
    costDetails.close();
    deleteDialog.showModal();
}

function cancelDeleteConfirmation() {
    if (saving) {
        return;
    }
    deleteDialog.close();
    costDetails.showModal();
    (canEditCosts() ? deleteCostButton : find("budget-close-details")).focus();
}

async function refreshCostsAfterSave(dialog) {
    dialog.close();
    await loadBudget();
    costsTitle.focus();
}

async function refreshBudgetAfterConflict(message) {
    budgetDetails = undefined;
    updateBudgetActions();
    await loadBudget();
    if (status.hidden) {
        showStatus(message, { error: true });
    }
}

calculateDebtButton.addEventListener("click", async () => {
    if (saving || !canCalculateDebt()) {
        return;
    }

    saving = true;
    calculateDebtButton.disabled = true;
    calculateDebtButton.textContent = "Рассчитываем…";
    showStatus("");
    try {
        await pendingLoad;
        if (!canCalculateDebt()) {
            return;
        }
        await sendBudgetAction("collection/supplemental", "POST");
        budgetDetails = undefined;
        updateBudgetActions();
        await loadBudget();
        if (status.hidden) {
            showStatus("Доплата начислена.");
            find("budget-contributions-title").focus();
        }
    } catch (error) {
        if (error.cause === 409) {
            await refreshBudgetAfterConflict("Не удалось начислить доплату. Проверьте обновлённый бюджет.");
        } else {
            showRequestError(error, "Не удалось начислить доплату.", false);
        }
    } finally {
        saving = false;
        calculateDebtButton.disabled = false;
        calculateDebtButton.textContent = "Рассчитать долг";
    }
});

closePayButton.addEventListener("click", () => payDialog.close());
payDialog.addEventListener("cancel", event => {
    if (saving) {
        event.preventDefault();
    }
});

payForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (saving || !payDialog.open || active.hidden || eventDetails?.collectionStatus !== collectionOpen) {
        return;
    }

    const amount = payAmount.valueAsNumber;
    if (!Number.isInteger(amount) || amount < 0 || amount > Number(payAmount.max)) {
        showDialogError(payError, "Укажите сумму в целых рублях, не превышающую долг.");
        payAmount.focus();
        return;
    }

    saving = true;
    savePayButton.disabled = true;
    savePayButton.textContent = "Отмечаем…";
    closePayButton.disabled = true;
    payAmount.readOnly = true;
    showDialogError(payError, "");
    try {
        await pendingLoad;
        await sendBudgetAction("pay-debt", "POST", { amount });
        payDialog.close();
        await loadBudget();
        find("budget-contributions-title").focus();
    } catch (error) {
        if (error.cause === 409) {
            payDialog.close();
            await refreshBudgetAfterConflict("Взнос не отмечен. Проверьте актуальный остаток долга.");
        } else {
            showDialogError(payError, requestErrorMessage(error, "Не удалось отметить взнос."));
        }
    } finally {
        saving = false;
        savePayButton.disabled = false;
        savePayButton.textContent = "Отметить";
        closePayButton.disabled = false;
        payAmount.readOnly = false;
    }
});

addCostButton.addEventListener("click", () => openCostForm());
find("budget-close-details").addEventListener("click", () => costDetails.close());
editCostButton.addEventListener("click", () => openCostForm(selectedCost));
deleteCostButton.addEventListener("click", openDeleteConfirmation);
cancelDeleteButton.addEventListener("click", cancelDeleteConfirmation);
deleteDialog.addEventListener("cancel", event => {
    event.preventDefault();
    cancelDeleteConfirmation();
});

closeCostButton.addEventListener("click", closeCostForm);
costDialog.addEventListener("cancel", event => {
    if (saving) {
        event.preventDefault();
    } else if (selectedCost) {
        event.preventDefault();
        closeCostForm();
    }
});

costForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (saving || !costDialog.open || !canEditCosts()) {
        return;
    }

    const cost = selectedCost;
    const title = costName.value.trim();
    const amount = costAmount.valueAsNumber;
    if (!cost && !title) {
        showDialogError(costError, "Введите название траты.");
        costName.focus();
        return;
    }
    if (!Number.isInteger(amount) || amount < Number(costAmount.min) || amount > Number(costAmount.max)) {
        showDialogError(costError, "Укажите сумму в целых рублях.");
        costAmount.focus();
        return;
    }

    saving = true;
    saveCostButton.disabled = true;
    saveCostButton.textContent = "Сохраняем…";
    closeCostButton.disabled = true;
    costName.readOnly = true;
    costAmount.readOnly = true;
    showDialogError(costError, "");
    try {
        await pendingLoad;
        if (!canEditCosts()) {
            updateBudgetActions();
            return;
        }
        if (cost) {
            await sendBudgetAction(`costs/${encodeURIComponent(cost.id)}`, "PATCH", { amount });
        } else {
            await sendBudgetAction("costs", "POST", { title, amount });
        }
        await refreshCostsAfterSave(costDialog);
    } catch (error) {
        const message = costRequestError(error, cost ? "Не удалось изменить трату." : "Не удалось добавить трату.");
        showDialogError(costError, message);
    } finally {
        saving = false;
        saveCostButton.disabled = !canEditCosts();
        saveCostButton.textContent = cost ? "Сохранить" : "Создать";
        closeCostButton.disabled = false;
        costName.readOnly = Boolean(cost);
        costAmount.readOnly = false;
    }
});

confirmDeleteButton.addEventListener("click", async () => {
    if (saving || !deleteDialog.open || !canEditCosts()) {
        return;
    }

    const cost = selectedCost;
    saving = true;
    confirmDeleteButton.disabled = true;
    cancelDeleteButton.disabled = true;
    confirmDeleteButton.textContent = "Удаляем…";
    showDialogError(deleteError, "");
    try {
        await pendingLoad;
        if (!canEditCosts()) {
            updateBudgetActions();
            return;
        }
        await sendBudgetAction(`costs/${encodeURIComponent(cost.id)}`, "DELETE");
        await refreshCostsAfterSave(deleteDialog);
    } catch (error) {
        showDialogError(deleteError, costRequestError(error, "Не удалось удалить трату."));
    } finally {
        saving = false;
        confirmDeleteButton.disabled = !canEditCosts();
        cancelDeleteButton.disabled = false;
        confirmDeleteButton.textContent = "Удалить";
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
            await sendBudgetAction("collection/open", "POST");
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
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            updateBudgetActions();
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
