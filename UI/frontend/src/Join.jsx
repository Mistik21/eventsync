import { useEffect, useState } from "react";
import { eventApi, eventsUrl, expire, message, profileUrl, readJson, refresh, write } from "./api";
import { eventIdPattern, eventPath, useNavigation } from "./navigation";
import { Back, Cover, dateTime, Loading, useAddress } from "./ui";
import "../../wwwroot/css/join-event.css";

export default function Join() {
    const { navigate } = useNavigation();
    const [code, setCode] = useState("");
    const [result, setResult] = useState(null);
    const [joining, setJoining] = useState(false);
    const [joinError, setJoinError] = useState("");
    const id = code.trim();
    const valid = eventIdPattern.test(id);
    const invitation = result?.id === id ? result.invitation : null;
    const error = result?.id === id ? result.error : null;
    const address = useAddress(invitation?.eventLocation);

    useEffect(() => {
        if (!valid) return;
        const controller = new AbortController();
        const timer = setTimeout(async () => {
            try {
                const invitation = await readJson(`${eventApi(id)}/invitation`, { signal: controller.signal });
                if (!controller.signal.aborted) setResult({ id, invitation });
            } catch (error) {
                if (!controller.signal.aborted) setResult({ id, error });
            }
        }, 250);
        return () => { clearTimeout(timer); controller.abort(); };
    }, [id, valid]);

    async function join(event) {
        event.preventDefault();
        if (joining || !invitation) return;
        setJoining(true);
        setJoinError("");
        try {
            if (!invitation.isParticipant) {
                await write(`${eventApi(id)}/join`, "POST");
                expire(profileUrl);
                void refresh(eventsUrl, true);
            }
            void refresh(eventApi(id), true);
            navigate(eventPath(id), { replace: true });
        } catch (error) {
            if ([403, 404, 409].includes(error.status)) {
                const updated = await readJson(`${eventApi(id)}/invitation`).catch(() => null);
                if (updated?.isParticipant) {
                    expire(eventsUrl);
                    expire(profileUrl);
                    navigate(eventPath(id), { replace: true });
                    return;
                }
            }
            setJoinError(message(error, "Не удалось присоединиться к событию."));
        } finally {
            setJoining(false);
        }
    }

    return <main className="join-screen">
        <header className="join-header"><Back to="/" label="К списку событий" /><div><h1>Присоединиться</h1><p>По приглашению друзей</p></div></header>
        <form className="join-form" onSubmit={join}>
            <label htmlFor="join-code">Код приглашения</label>
            <div className="join-code-field">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true"><path d="M10 13a4 4 0 0 0 6 0l4-4a4 4 0 0 0-6-6l-2 2m2 6a4 4 0 0 0-6 0l-4 4a4 4 0 0 0 6 6l2-2"/></svg>
                <input id="join-code" autoComplete="off" autoCapitalize="off" spellCheck={false} value={code} readOnly={joining} onChange={e => { setResult(null); setJoinError(""); setCode(e.target.value); }} placeholder="Вставьте код события" />
            </div>
            {valid && !error && !invitation && <Loading className="join-status">Ищем приглашение…</Loading>}
            {id && !valid && <p className="join-status" data-state="error" role="status">Введите полный код приглашения.</p>}
            {error && <p className="join-status" data-state="error" role="status">{error.status === 409 ? "Сбор уже открыт, присоединиться нельзя." : error.status === 404 ? "Приглашение не найдено. Проверьте код." : message(error, "Не удалось проверить приглашение.")}</p>}
            {invitation && <article className="join-preview">
                <Cover className="join-cover" src={invitation.mainImageUrl} />
                <span className="join-role">{invitation.isParticipant ? "Вы уже участник" : "Вы приглашены"}</span>
                <h2>{invitation.title}</h2><time dateTime={invitation.eventStartTime}>{dateTime(invitation.eventStartTime)}</time>
                {invitation.organizerName && <p>{invitation.organizerName} · Организатор</p>}
                <p className="join-location"><span aria-hidden="true">⌖</span> {address} · {invitation.participantsCount} участ.</p>
                {joinError && <p className="join-status" data-state="error" role="alert">{joinError}</p>}
                <button type="submit" disabled={joining}>{joining ? "Присоединяемся…" : invitation.isParticipant ? "Открыть событие" : "Присоединиться"}</button>
            </article>}
        </form>
    </main>;
}
