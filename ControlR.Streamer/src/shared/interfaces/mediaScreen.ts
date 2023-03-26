import { Display } from "electron";

export interface MediaScreen extends Display {
    mediaId: string;
    displayId: string;
    name: string;
    isPrimary: boolean;
}
