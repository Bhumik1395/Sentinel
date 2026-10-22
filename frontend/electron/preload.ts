import { contextBridge, ipcRenderer } from "electron";

contextBridge.exposeInMainWorld("sentinelAuth", {
    startLogin: () => ipcRenderer.invoke("auth:start-login"),
});
