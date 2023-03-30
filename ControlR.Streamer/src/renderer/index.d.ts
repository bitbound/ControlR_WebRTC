import { SignedPayloadDto } from "src/shared/dtos/signedPayloadDto";
import { MediaScreen } from "src/shared/models/MediaScreen";

declare interface MainApi {
  exit(): Promise<void>;
  verifyDto(base64Payload: string, base64Signature: string, publicKey: string, publicKeyPem: string): Promise<boolean>;
  getServerUri(): Promise<string>;
  getSessionId(): Promise<string>;
  getDisplays(): Promise<MediaScreen[]>;
  movePointer(x: number, y: number): Promise<void>;
  invokeMouseButton(button: number, isPressed: boolean, x: number, y: number): Promise<void>;
  invokeKeyEvent(keyCode: string, isPressed: boolean): Promise<void>;
  resetKeyboardState(): Promise<void>;
  invokeWheelScroll(deltaX: number, deltaY: number, deltaZ: number): Promise<void>;
  writeLog(message: string, level: LogLevel = "Info", ...args: any[]);
}

declare global {
  interface Window {
    mainApi: MainApi
  }
}