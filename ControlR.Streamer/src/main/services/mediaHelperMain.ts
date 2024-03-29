import { app, desktopCapturer, DesktopCapturerSource, Display, screen } from "electron"
import { DisplayDto } from "../../shared/signalrDtos/displayDto";
import { movePointer } from "./inputSimulator";
import { writeLog } from "./logger";

export async function getDisplays() : Promise<DisplayDto[]> {
    try {
        writeLog("Getting screen sources.");
        const sources = await getCaptureSources()
        writeLog("Found screen sources: ", "Info", sources);
        
        if (sources.some(x => !x.display_id)) {
            app.exit(1);
        }

        const primaryDisplay = screen.getPrimaryDisplay();

        writeLog("Found primary display: ", "Info", primaryDisplay);

        const displays = screen.getAllDisplays();

        const screens = sources.map(x => {
            const display = displays.find(d => `${d.id}` == x.display_id);
            return {
                displayId: x.display_id,
                id: x.id,
                name: x.name,
                mediaId: x.id,
                isPrimary: display.id == primaryDisplay.id,
                left: display.bounds.x,
                top: display.bounds.y,
                width: display.bounds.width,
                height: display.bounds.height,
                label: display.label
            } as DisplayDto
        });
    
        writeLog("Merging results with Electron displays.  Result: ", "Info", screens);
        
        return screens;
    }
    catch (exception) {
        writeLog("Error while getting screens.", "Error", JSON.stringify(exception));
    }
}

function getCaptureSources() : Promise<DesktopCapturerSource[]> {

    return new Promise<DesktopCapturerSource[]>(async (resolve, reject) => {
        for (var i = 0; i < 5; i++) {

            const sources = await desktopCapturer.getSources({ types: ['screen'] });

            if (sources.every(x => !!x.display_id)) {
                resolve(sources);
                return;
            }
    
            writeLog("Desktop capture sources are missing display IDs.  Attempting to wake up the screen.");
            await movePointer(1, 1);
            await movePointer(0, 0);
            await new Promise(resolve => setTimeout(resolve, 200));
        }
        reject("Failed to find desktop capture sources.");
    })
}