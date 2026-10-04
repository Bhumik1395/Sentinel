// frontend/src/lib/useLoadState.ts
// Shared by every list/detail page (Overview, Alerts, Incidents, ...).
// One state machine, one place to fix if the loading/empty/error contract
// ever changes, instead of five components drifting apart.
import { useCallback, useEffect, useState } from "react";

export type LoadState<T> =
    | { status: "loading" }
    | { status: "error"; message: string }
    | { status: "empty" }
    | { status: "ready"; data: T };

export function useLoadState<T>(
    fetcher: () => Promise<T>,
    isEmpty: (data: T) => boolean,
    deps: unknown[] = []
) {
    const [state, setState] = useState<LoadState<T>>({ status: "loading" });

    const load = useCallback(() => {
        setState({ status: "loading" });
        fetcher()
            .then((data) => setState(isEmpty(data) ? { status: "empty" } : { status: "ready", data }))
            .catch((e) => setState({ status: "error", message: e instanceof Error ? e.message : "Unknown error" }));
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);

    useEffect(load, [load]);

    return { state, reload: load };
}