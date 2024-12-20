import { app } from "electron";
import { parseBoolean } from "../../shared/helpers";

const sessionId = app.commandLine.getSwitchValue("session-id");
const viewerConnectionId = app.commandLine.getSwitchValue("viewer-id");
const authorizedKey = app.commandLine.getSwitchValue("authorized-key");
const viewerName = app.commandLine.getSwitchValue("viewer-name") ?? "";
const notifyUser =
  parseBoolean(app.commandLine.getSwitchValue("notify-user")) || false;
const isDev = app.commandLine.hasSwitch("dev");

const serverUri =
  app.commandLine.getSwitchValue("server-uri").replace(/\/$/, "") ||
  "https://webrtc.controlr.app";

const websocketUri = serverUri.replace("http", "ws");

interface AppState {
  authorizedKey: string;
  isUnattended: boolean;
  viewerConnectionId: string;
  isDev: boolean;
  sessionId: string;
  serverUri: string;
  websocketUri: string;
  viewerName: string;
  notifyUser: boolean;
}

export default {
  authorizedKey: authorizedKey,
  isDev: isDev,
  sessionId: sessionId,
  isUnattended: !!sessionId,
  viewerConnectionId: viewerConnectionId,
  serverUri: serverUri,
  websocketUri: websocketUri,
  notifyUser: notifyUser,
  viewerName: viewerName,
} as AppState;
