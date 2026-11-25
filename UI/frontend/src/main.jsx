import { Component, Suspense, lazy, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { eventApi, eventsUrl, profileUrl, refresh } from "./api";
import { Home } from "./Home";
import { Link, NavigationProvider, useNavigation } from "./navigation";
import { Loading } from "./ui";
import "../../wwwroot/css/style.css";

const loadEvent = () => import("./Event");
const loadProfile = () => import("./Profile");
const Event = lazy(loadEvent);
const Profile = lazy(loadProfile);
const EventForm = lazy(() => import("./EventForm"));
const Join = lazy(() => import("./Join"));

if (window.eventSyncEvents) void refresh(eventsUrl);

class ScreenBoundary extends Component {
    state = { failed: false };
    static getDerivedStateFromError() { return { failed: true }; }
    render() {
        return this.state.failed ? <main className="events-screen">
            <p role="alert">Не удалось открыть экран.</p>
            <button type="button" onClick={() => location.reload()}>Обновить страницу</button>
        </main> : this.props.children;
    }
}

function App() {
    const { route } = useNavigation();
    const home = route.page === "home";
    const [visitedHome, setVisitedHome] = useState(home);
    useEffect(() => {
        if (home) setVisitedHome(true);
        if (route.page === "event" || route.page === "edit") void refresh(eventApi(route.id));
        if (route.page === "profile") void refresh(profileUrl);
        document.title = `${{ home: "Мои события", profile: "Профиль", create: "Новое событие", join: "Приглашение", edit: "Редактирование", event: "Событие" }[route.page] ?? "Страница не найдена"} · EventSync`;
    }, [home, route.page, route.id]);

    function prepareEvent(id) {
        void loadEvent().catch(() => {});
        void refresh(eventApi(id));
    }

    function prepareProfile() {
        void loadProfile().catch(() => {});
        void refresh(profileUrl);
    }

    let page;
    switch (route.page) {
        case "event": page = <Event key={route.id} id={route.id} tab={route.tab} />; break;
        case "profile": page = <Profile />; break;
        case "create": page = <EventForm />; break;
        case "edit": page = <EventForm key={route.id} id={route.id} />; break;
        case "join": page = <Join />; break;
        case "home": break;
        default: page = <main className="events-screen"><p>Страница не найдена.</p><Link to="/">Мои события</Link></main>;
    }

    return <>
        {(home || visitedHome) && <Home active={home} prepareEvent={prepareEvent} prepareProfile={prepareProfile} />}
        {page && <ScreenBoundary key={`${route.page}:${route.id ?? ""}`}>
            <Suspense fallback={<main className="events-screen"><Loading /></main>}>{page}</Suspense>
        </ScreenBoundary>}
    </>;
}

createRoot(document.getElementById("root")).render(<NavigationProvider><App /></NavigationProvider>);
