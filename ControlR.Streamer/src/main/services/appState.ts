import { app } from "electron";

const sessionId = app.commandLine.getSwitchValue("session-id");
const authorizedKey = app.commandLine.getSwitchValue("authorized-key");
const isDev = app.commandLine.hasSwitch("dev");

const serverUri = !isDev ?
    "https://app.controlr.app" :
    "http://localhost:5120";

    
interface AppState {
    authorizedKey: string;
    isUnattended: boolean;
    isDev: boolean;
    sessionId: string;
    serverUri: string;
}

export default {
    authorizedKey: authorizedKey,
    isDev: isDev,
    isUnattended: !!sessionId,
    sessionId: sessionId,
    serverUri: serverUri
} as AppState;