import { HubConnection, HubConnectionBuilder, JsonHubProtocol, LogLevel } from "@microsoft/signalr";
import { SignedPayloadDto } from "../../shared/signalrDtos/signedPayloadDto";
import { receiveDto } from "./signalrDtoHandler";

class DesktopHubConnection {
    connection?: HubConnection;
    serverUri?: string;
    sessionId?: string;

    async connect(): Promise<void> {
        this.serverUri = await window.mainApi.getServerUri();
        this.sessionId = await window.mainApi.getSessionId();

        console.log("Starting SignalR connection.");
        console.log("ServerUri: ", this.serverUri);
        console.log("Session ID: ", this.sessionId);

        if (this.connection) {
            await this.connection.stop();
        }

        this.connection = new HubConnectionBuilder()
            .withUrl(`${this.serverUri}/hubs/desktop`)
            .withHubProtocol(new JsonHubProtocol())
            .configureLogging(LogLevel.Information)
            .build();

        this.setHandlers();

        await this.connection.start();

        if (this.sessionId) {
            await this.connection.invoke("setConnectionIdForSession", this.sessionId);
        }
    }

    async getIceServers() : Promise<RTCIceServer[]> {
        return await this.connection.invoke("getIceServers");
    }

    async sendIceCandidate(candidateJson: string) : Promise<void> {
        await this.connection.invoke("sendIceCandidate", this.sessionId, candidateJson);
    }

    async sendRtcSessionDescription(sessionDescription: RTCSessionDescription) {
        await this.connection.invoke("sendRtcSessionDescription", this.sessionId, sessionDescription);
    }

    private setHandlers() {
        this.connection.onclose((err) => {
            console.log("Connection closed: ", err);
        });

        this.connection.on(
            "receiveDto",
            async (dto: SignedPayloadDto) => {
                await receiveDto(dto);
            }
        );
    }
}

const desktopHubConnection = new DesktopHubConnection();

export default desktopHubConnection;