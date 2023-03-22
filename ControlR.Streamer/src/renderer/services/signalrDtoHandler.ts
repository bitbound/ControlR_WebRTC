import { decode } from "@msgpack/msgpack";
import { SignedPayloadDto } from "../../shared/signalrDtos/signedPayloadDto";
import rtcSession from "./rtcSession";

export async function receiveDto(dto: SignedPayloadDto) {
    const result = await window.mainApi.verifyDto(dto.payload, dto.signature, dto.publicKey, dto.publicKeyPem);
    if (!result) {
        window.mainApi.writeLog("DTO signature failed verification.  Aborting.");
        return;
    }

    const payloadBuffer = Uint8Array.from(atob(dto.payload), c => c.charCodeAt(0));

    switch (dto.dtoType) {
        case "RtcSessionDescription":
            const sessionDescription = decode(payloadBuffer) as RTCSessionDescription;
            await rtcSession.receiveRtcSessionDescription(sessionDescription);
            break;
        case "RtcIceCandidate":
            const iceCandidate = decode(payloadBuffer) as string;
            await rtcSession.receiveIceCandidate(iceCandidate);
            break;
        case "CloseDesktopSession":
            console.log("Received exit request. Shutting down.");
            await window.mainApi.exit();
            break;
        default:
            break;
    }
}