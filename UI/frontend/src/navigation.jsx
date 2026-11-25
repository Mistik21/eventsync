import { createContext, useContext, useEffect, useLayoutEffect, useRef, useState } from "react";

export const eventIdPattern = /^[\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12}$/i;
export const eventPath = id => `/event/${encodeURIComponent(id)}`;
const Navigation = createContext(null);
const address = () => location.pathname + location.search + location.hash;

export function parseRoute(url) {
    const { pathname, searchParams, hash } = new URL(url, location.origin);
    const path = pathname.replace(/\/$/, "");
    const pages = {
        "": "home", "/html/index.html": "home", "/profile": "profile", "/html/profile.html": "profile",
        "/createevent": "create", "/html/create-event.html": "create", "/joinevent": "join", "/html/join-event.html": "join",
    };
    const page = pages[path.toLowerCase()];
    if (page) return { page };

    const event = path.match(/^\/event\/([^/]+)(?:\/([^/]+))?$/i);
    const legacy = path.match(/^\/html\/(event|activities|items|budget|edit-event|participants|drives)\.html$/i);
    const id = event?.[1] ?? (legacy ? searchParams.get("id") : null);
    if (!id || !eventIdPattern.test(id)) return { page: "missing" };
    const section = (event?.[2] ?? (legacy?.[1] !== "event" ? legacy?.[1] : "") ?? "").toLowerCase();
    const names = { "": "overview", "edit-event": "edit", participant: "information", participants: "information" };
    const tab = section ? names[section] ?? section : hash.slice(1) || "overview";
    return { page: tab === "edit" ? "edit" : "event", id: id.toLowerCase(), tab };
}

export function NavigationProvider({ children }) {
    const [url, setUrl] = useState(address);
    const positions = useRef(new Map());
    const current = useRef(url);

    useEffect(() => {
        history.scrollRestoration = "manual";
        const sync = () => {
            positions.current.set(current.current, window.scrollY);
            setUrl(address());
        };
        window.addEventListener("popstate", sync);
        window.addEventListener("hashchange", sync);
        return () => {
            window.removeEventListener("popstate", sync);
            window.removeEventListener("hashchange", sync);
        };
    }, []);

    useLayoutEffect(() => {
        current.current = url;
        window.scrollTo(0, positions.current.get(url) ?? 0);
    }, [url]);

    function navigate(to, { replace = false, back = false } = {}) {
        if (to === address()) return;
        if (back && history.state?.previous === to) {
            history.back();
            return;
        }
        positions.current.set(url, window.scrollY);
        if (replace) history.replaceState({ previous: history.state?.previous }, "", to);
        else history.pushState({ previous: address() }, "", to);
        setUrl(address());
    }

    return <Navigation.Provider value={{ route: parseRoute(url), navigate }}>{children}</Navigation.Provider>;
}

export function useNavigation() {
    return useContext(Navigation);
}

export function Link({ to, back, intent, children, ...props }) {
    const { navigate } = useNavigation();
    return <a {...props} href={to} onPointerEnter={intent} onFocus={intent} onClick={click => {
        if (click.defaultPrevented || click.button !== 0 || click.metaKey || click.ctrlKey || click.shiftKey || click.altKey) return;
        click.preventDefault();
        navigate(to, { back });
    }}>{children}</a>;
}
