import { desktopCapturer, screen } from "electron"
import { MediaScreen } from "../../shared/interfaces/mediaScreen";
import { writeLog } from "./logger";

export async function getScreens() : Promise<MediaScreen[]> {
    try {
        writeLog("Getting screen sources.");
        const sources = await desktopCapturer.getSources({ types: ['screen'] });
        writeLog("Got screen sources: ", "Info", sources);
    
        const displays = screen.getAllDisplays();
        const primaryDisplay = screen.getPrimaryDisplay();
    
        const screens = sources.map(x => {
            const display = displays.find(d => `${d.id}` == x.display_id);
            return {
                displayId: x.display_id,
                id: x.id,
                name: x.name,
                mediaId: x.id,
                isPrimary: display.id == primaryDisplay.id,
                ...display
            } as MediaScreen
        });
    
        writeLog("Merging results with Electron displays.  Result: ", "Info", screens);
        
        return screens;
    }
    catch (exception) {
        writeLog("Error while getting screens.", "Error", exception);
    }
}