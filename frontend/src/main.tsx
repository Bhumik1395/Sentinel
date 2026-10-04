// frontend/src/main.tsx — full replacement (added HashRouter)
import React from "react";
import ReactDOM from "react-dom/client";
import { HashRouter } from "react-router-dom";
import App from "./App";
import { MockAuthProvider } from "./lib/authContext";

ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <HashRouter>
            <MockAuthProvider as="owner">
                <App />
            </MockAuthProvider>
        </HashRouter>
    </React.StrictMode>
);