async function signInWithTelegram() {
    const telegram = window.Telegram?.WebApp;
    telegram?.ready();
    telegram?.expand();

    const initData = telegram?.initData;
    if (!initData) {
        return;
    }

    try {
        const session = await fetch("/api/auth/session", { cache: "no-store" });
        if (session.ok) {
            return;
        }
        if (session.status !== 401) {
            throw new Error("Не удалось проверить сессию.");
        }

        const login = await fetch("/api/auth/telegram", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ initData }),
        });
        if (!login.ok) {
            throw new Error("Не удалось войти через Telegram.");
        }
    } catch {
        const message = document.querySelector(".auth-error");
        message.textContent = "Не удалось войти через Telegram. Откройте приложение ещё раз.";
        message.hidden = false;
    }
}

void signInWithTelegram();
