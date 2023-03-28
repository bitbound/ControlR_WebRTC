import { platform, tmpdir, EOL } from "os";
import { appendFileSync, statSync, readFileSync, writeFileSync, existsSync, mkdirSync } from "fs";
import path from "path";

export function writeLog(message: string, level: LogLevel = "Info", ...args: any[]) {
    try {
        if (level == "Info") {
            console.log(message, args);
        }
        else if (level == "Warning") {
            console.warn(message, args);
        }
        else if (level == "Error"){
            console.error(message, args);
        }

        let logDir = path.join(tmpdir(), "ControlR", "Logs");
        const rootDir = path.parse(logDir).root;
    
        if (platform() == "win32")
        {
            logDir = path.join(rootDir, "ProgramData", "ControlR", "Logs", "ControlR.Streamer");
        }
    
        if (!existsSync(logDir)) {
            mkdirSync(logDir, {recursive: true});
        }

        const date = new Date();
        const year = date.getFullYear();
        const month = (date.getMonth() + 1).toString().padStart(2, "0");
        const day = date.getDate().toString().padStart(2, "0");

        let logPath = path.join(logDir, `Streamer-${year}-${month}-${day}.log`);

        if (existsSync(logPath)) {
            while (statSync(logPath).size > 1000000) {
                let content = readFileSync(logPath, { encoding: "utf8" });
                writeFileSync(logPath, content.substring(content.length / 2));
            }
        }
        var entry = `[${level}]\t[${(new Date()).toLocaleString()}]\t${message}`;

        if (args && args.length > 0) {
            args = args.filter(x => !!x);
            entry += ` ${JSON.stringify(args)}`
        }

        entry += EOL;

        appendFileSync(logPath, entry);
    }
    catch (ex) {
        console.error("Failed to write to log file.", ex);
    }

}