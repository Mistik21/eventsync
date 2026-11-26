const encoder = new TextEncoder();

function telegramFailure(status, result) {
    return new Response(null, { status, headers: { "X-EventSync-Telegram-Result": result } });
}

export default {
    async fetch(request, env) {
        if (new URL(request.url).pathname !== "/send") {
            return new Response(null, { status: 404 });
        }
        if (request.method !== "POST") {
            return new Response(null, { status: 405, headers: { Allow: "POST" } });
        }

        const token = env.TELEGRAM_BOT_TOKEN?.trim();
        const secret = env.RELAY_SECRET?.trim();
        if (!token || !secret) {
            return new Response(null, { status: 503 });
        }

        const supplied = encoder.encode(request.headers.get("Authorization") ?? "");
        const expected = encoder.encode(`Bearer ${secret}`);
        if (supplied.length !== expected.length || !crypto.subtle.timingSafeEqual(supplied, expected)) {
            return new Response(null, { status: 401 });
        }

        const payload = await request.json().catch(() => null);
        if (!Number.isSafeInteger(payload?.chatId) || payload.chatId <= 0 ||
            typeof payload.text !== "string" || !payload.text.trim() || payload.text.length > 4096) {
            return new Response(null, { status: 400 });
        }
        if (!/^\d+:[A-Za-z0-9_-]+$/.test(token)) {
            return telegramFailure(502, "invalid-token");
        }

        const signal = AbortSignal.timeout(4000);
        try {
            const response = await fetch(`https://api.telegram.org/bot${token}/sendMessage`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ chat_id: payload.chatId, text: payload.text }),
                redirect: "error",
                signal,
            });
            const result = await response.json().catch(() => null);
            if (signal.aborted) {
                return new Response(null, { status: 504 });
            }
            if (response.ok && result?.ok === true) {
                return new Response(null, { status: 204 });
            }
            const telegramStatus = String(response.status);
            if (result?.ok === false) {
                switch (result.error_code) {
                    case 400:
                    case 403:
                        return telegramFailure(422, telegramStatus);
                    case 429:
                        return telegramFailure(429, telegramStatus);
                }
            }
            return telegramFailure(502, telegramStatus);
        } catch {
            return signal.aborted ? new Response(null, { status: 504 }) : telegramFailure(502, "unreachable");
        }
    },
};
