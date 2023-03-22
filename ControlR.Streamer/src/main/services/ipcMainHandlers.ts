import { app, ipcMain, IpcMainInvokeEvent } from "electron";
import { ipcRtmChannels } from "../../shared/ipcChannels";
import appState from "./appState";
import { verify, createPublicKey } from "crypto";
import { getScreens } from "./mediaHelperMain";
import { invokeKeyEvent, invokeMouseButtonEvent, movePointer, resetKeyboardState, scrollWheel } from "./inputSimulator";
import { writeLog } from "./logger";

export async function registerIpcHandlers() {
  ipcMain.handle(ipcRtmChannels.getServerUri, () => appState.serverUri);
  ipcMain.handle(ipcRtmChannels.getSessionId, () => appState.sessionId);
  ipcMain.handle(ipcRtmChannels.verifyDto, verifyDto);
  ipcMain.handle(ipcRtmChannels.getScreens, () => getScreens());
  ipcMain.handle(ipcRtmChannels.movePointer, (_, x, y) => movePointer(x, y));
  ipcMain.handle(ipcRtmChannels.exit, () => app.exit());
  ipcMain.handle(ipcRtmChannels.invokeKeyEvent, (_, key, isPressed) => invokeKeyEvent(key, isPressed));
  ipcMain.handle(ipcRtmChannels.invokeMouseButtonEvent, (_, button, isPressed, x, y) => invokeMouseButtonEvent(button, isPressed, x, y));
  ipcMain.handle(ipcRtmChannels.resetKeyboardState, (_) => resetKeyboardState());
  ipcMain.handle(ipcRtmChannels.invokeWheelScroll, (_, deltaX, deltaY, deltaZ) => scrollWheel(deltaX, deltaY, deltaZ));
  ipcMain.handle(ipcRtmChannels.writeLog, (_, message, level, args) => writeLog(message, level, args));
}

const verifyDto = (
  event: IpcMainInvokeEvent,
  base64Payload: string,
  base64Signature: string,
  publicKey: string,
  publicKeyPem: string
): boolean => {
  console.log("Verifying DTO signature.");

  if (publicKey != appState.authorizedKey) {
    console.error("Public key from DTO does not match the authorized key.");
    return false;
  }

  const payload = Buffer.from(base64Payload, "base64");
  const signature = Buffer.from(base64Signature, "base64");

  const publicKeyObject = createPublicKey({
    key: publicKeyPem,
    type: "pkcs1",
    format: "pem"
  });

  const result = verify("RSA-SHA512", payload, publicKeyObject, signature);

  if (!result) {
    console.error("Public key from DTO does not pass verification!");
    return false;
  }

  console.info("DTO passed signature verification.");

  return true;
};
