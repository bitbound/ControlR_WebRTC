// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts

import { contextBridge, ipcRenderer } from "electron";
import {  MainApi } from ".";

import { ipcRtmChannels } from "../shared/ipcChannels";

contextBridge.exposeInMainWorld("mainApi", {
    "getServerUri": () => ipcRenderer.invoke(ipcRtmChannels.getServerUri),
    "getSessionId": () => ipcRenderer.invoke(ipcRtmChannels.getSessionId),

    "verifyDto": (base64Payload, base64Signature, publicKey, publicKeyPem) => 
        ipcRenderer.invoke(ipcRtmChannels.verifyDto, base64Payload, base64Signature, publicKey, publicKeyPem),

    "getScreens": () => ipcRenderer.invoke(ipcRtmChannels.getScreens),
    "movePointer": (x, y) => ipcRenderer.invoke(ipcRtmChannels.movePointer, x, y),
    "exit": () => ipcRenderer.invoke(ipcRtmChannels.exit),

    "invokeKeyEvent": (key, isPressed) => 
    ipcRenderer.invoke(ipcRtmChannels.invokeKeyEvent, key, isPressed),

    "invokeMouseButton": (button, isPressed, x, y) => 
        ipcRenderer.invoke(ipcRtmChannels.invokeMouseButtonEvent, button, isPressed, x, y),

    "resetKeyboardState": () => ipcRenderer.invoke(ipcRtmChannels.resetKeyboardState),

    "invokeWheelScroll": (deltaX, deltaY, deltaZ) => 
        ipcRenderer.invoke(ipcRtmChannels.invokeWheelScroll, deltaX, deltaY, deltaZ),

    "writeLog": (message, level, args) => 
        ipcRenderer.invoke(ipcRtmChannels.writeLog, message, level, !args ? null : Array.isArray(args) ? args : [args]),
} as MainApi)