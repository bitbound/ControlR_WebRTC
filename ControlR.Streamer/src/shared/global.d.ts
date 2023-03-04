import { Display } from "electron";

declare interface MediaScreen extends Display {
    mediaId: string;
    displayId: string;
    name: string;
  }
  