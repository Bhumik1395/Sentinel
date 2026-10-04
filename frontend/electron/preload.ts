import { contextBridge, ipcRenderer } from "electron";

contextBridge.exposeInMainWorld("sentinelAuth", {
    startLogin: () => ipcRenderer.invoke("auth:start-login"),
    onLoginResult: (cb: (tokens: { accessToken: string; refreshToken: string }) => void) =>
        ipcRenderer.on("auth:login-result", (_e, tokens) => cb(tokens)),
});