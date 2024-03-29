import { Point } from "@nut-tree/nut-js";
import { DisplayDto } from "../../shared/signalrDtos/displayDto";
import streamerHubConnection from "./streamerHubConnection";
import { setMediaStreams } from "./mediaHelperRenderer";

class RtcSession {
  peerConnection?: RTCPeerConnection;
  dataChannel?: RTCDataChannel;
  isMakingOffer: boolean;
  currentScreen?: DisplayDto;
  screens: DisplayDto[] = [];

  async startRtcSession() {
    window.mainApi.writeLog("Getting ICE servers.");
  
      const iceServers = await streamerHubConnection.getIceServers();
      window.mainApi.writeLog("Got ICE servers: ", "Info", iceServers);
  
      this.peerConnection = new RTCPeerConnection({
        iceServers: iceServers,
      });

      this.setConnectionHandlers();
  
      window.mainApi.writeLog("Getting screens from main API.");
      this.screens = await window.mainApi.getDisplays();
      window.mainApi.writeLog("Found screens: ", "Info", this.screens);
  
      this.currentScreen = this.screens[0];
      window.mainApi.writeLog("Getting stream for first screen: ", "Info", this.currentScreen);
  
      window.mainApi.writeLog("Adding tracks from stream.");
      await setMediaStreams(this.currentScreen.mediaId, this.peerConnection);

      window.mainApi.writeLog("Creating data channel.");
      this.setDataChannel(this.peerConnection.createDataChannel("input"));
  }

  async receiveRtcSessionDescription(remoteDescription: RTCSessionDescription) {
    try {
      window.mainApi.writeLog("Received session description: ", "Info", remoteDescription);

      const offerCollision =
        remoteDescription.type === "offer" &&
        (this.isMakingOffer || this.peerConnection.signalingState !== "stable");
  
      if (offerCollision) {
        window.mainApi.writeLog("Ignoring session description due to offer collision.", "Info", remoteDescription);
        return;
      }

      window.mainApi.writeLog("Setting remote description.");
      await this.peerConnection.setRemoteDescription(remoteDescription);
  
      if (remoteDescription.type == "offer") {
        window.mainApi.writeLog("Creating answer.");
        await this.peerConnection.setLocalDescription();
  
        window.mainApi.writeLog("Sending RTC answer: ", "Info", this.peerConnection.localDescription.toJSON());
        await streamerHubConnection.sendRtcSessionDescription(
          this.peerConnection.localDescription.toJSON()
        );
      }
    }
    catch (ex) {
      window.mainApi.writeLog("Error while receiving session description: ", "Error", ex);
    }
  }

  async receiveIceCandidate(iceCandidateJson?: string): Promise<void> {
    try{
      if (!this.peerConnection || !this.peerConnection.remoteDescription) {
        window.mainApi.writeLog(
          "Received ICE candidate, but initialization hasn't completed.  Retrying in 1 second."
        );
        setTimeout(() => {
          this.receiveIceCandidate(iceCandidateJson);
        }, 1000);
        return;
      }
  
      if (!iceCandidateJson) {
        window.mainApi.writeLog("Received null (terminating) ICE candidate");
        await this.peerConnection.addIceCandidate(null);
        return;
      }
  
      const iceCandidate = JSON.parse(iceCandidateJson);
      window.mainApi.writeLog("Received ICE candidate: ", "Info", iceCandidate);
      await this.peerConnection.addIceCandidate(iceCandidate);
    }
    catch (ex) {
      window.mainApi.writeLog("Error while receiving ICE candidate: ", "Error", ex);
    }
  }

  private setConnectionHandlers() {
    window.mainApi.writeLog("Setting peer connection handlers.");
    this.peerConnection.addEventListener("connectionstatechange", async () => {
      window.mainApi.writeLog(
        "Connection state changed: ",
        "Info",
        this.peerConnection.connectionState
      );
      switch (this.peerConnection.connectionState) {
        case "closed":
        case "disconnected":
          window.mainApi.writeLog("Restarting ICE.");
          this.peerConnection.restartIce();
          break;
        case "failed":
          window.mainApi.writeLog("Connection failed.  Exiting.");
          await window.mainApi.exit();
          break;
        default:
          break;
      }
    });
    this.peerConnection.addEventListener("iceconnectionstatechange", (ev) => {
      window.mainApi.writeLog(
        "ICE connection state changed: ",
        "Info",
        this.peerConnection.iceConnectionState
      );
    });
    this.peerConnection.addEventListener("icegatheringstatechange", (ev) => {
      window.mainApi.writeLog(
        "ICE gathering state changed: ",
        "Info",
        this.peerConnection.iceGatheringState
      );
    });
    this.peerConnection.addEventListener("track", (ev) => {
      window.mainApi.writeLog("Got track: ", "Info", ev);
    });
    this.peerConnection.addEventListener("icecandidate", async (ev) => {
      if (!ev.candidate) {
        window.mainApi.writeLog("End of ICE candidates.");
        return;
      }
      window.mainApi.writeLog("Sending ICE candidate: ", "Info", JSON.stringify(ev.candidate));
      await streamerHubConnection.sendIceCandidate(
        JSON.stringify(ev.candidate)
      );
    });
    this.peerConnection.addEventListener("icecandidateerror", async (ev: RTCPeerConnectionIceErrorEvent) => {
      const err = {
        errorCode: ev.errorCode,
        errorText: ev.errorText,
        port: ev.port,
        url: ev.url,
        address: ev.address
    }
      window.mainApi.writeLog("ICE candidate error: ", "Error", err);
    });
    this.peerConnection.addEventListener("negotiationneeded", async () => {
      try {
        this.isMakingOffer = true;
        window.mainApi.writeLog("Negotiation needed. Creating new offer.");
        await this.peerConnection.setLocalDescription();

        window.mainApi.writeLog("Sending RTC offer: ", "Info", this.peerConnection.localDescription.toJSON());
        await streamerHubConnection.sendRtcSessionDescription(
          this.peerConnection.localDescription.toJSON()
        );
      }
      catch (ex) {
        window.mainApi.writeLog("Error during negotiation: ", "Error", ex);
      }
      finally {
        this.isMakingOffer = false;
      }
    });
    this.peerConnection.addEventListener("datachannel", (ev) => {
      this.setDataChannel(ev.channel);
    });
  }

  private getAbsoluteScreenPoint(percentX: number, percentY: number): Point {
    const x = this.currentScreen.width * percentX + this.currentScreen.left;
    const y = this.currentScreen.height * percentY + this.currentScreen.top;
    return { x: x, y: y };
  }


  private async handleDataChannelMessage(data: string) {
    const dto = JSON.parse(data) as BaseDto;
    switch (dto.dtoType) {
      case "pointerMove":
        {
          const moveDto = dto as PointerMoveDto;
          const point = this.getAbsoluteScreenPoint(
            moveDto.percentX,
            moveDto.percentY
          );
          await window.mainApi.movePointer(point.x, point.y);
        }
        break;
      case "keyEvent":
        {
          const keyDto = dto as KeyEventDto;
          await window.mainApi.invokeKeyEvent(keyDto.keyCode, keyDto.isPressed, keyDto.shouldRelease);
        }
        break;
      case "mouseButtonEvent":
        {
          const buttonDto = dto as MouseButtonEventDto;
          const point = this.getAbsoluteScreenPoint(
            buttonDto.percentX,
            buttonDto.percentY
          );
          await window.mainApi.invokeMouseButton(
            buttonDto.button,
            buttonDto.isPressed,
            point.x,
            point.y
          );
        }
        break;
      case "resetKeyboardState":
        {
          await window.mainApi.resetKeyboardState();
        }
        break;
      case "wheelScrollEvent":
        {
            const scrollDto = dto as WheelScrollDto;
            await window.mainApi.invokeWheelScroll(scrollDto.deltaX, scrollDto.deltaY, scrollDto.deltaZ);
        }
        break;
      case "typeText": 
        {
          const typeDto = dto as TypeTextDto;
          await window.mainApi.invokeTypeText(typeDto.text);
        }
        break;
      default:
        console.warn("Unhandled DTO type: ", dto.dtoType);
        break;
    }
  }

  private setDataChannel(channel: RTCDataChannel){
    this.dataChannel = channel;
    this.dataChannel.addEventListener("close", () => {
      window.mainApi.writeLog("DataChannel closed.");
    });

    this.dataChannel.addEventListener("error", () => {
      window.mainApi.writeLog("DataChannel error.");
    });

    this.dataChannel.addEventListener("open", () => {
      window.mainApi.writeLog("DataChannel opened.");
    });

    this.dataChannel.addEventListener("message", async (ev) => {
      await this.handleDataChannelMessage(ev.data);
    });
  }
}

const rtcSession = new RtcSession();

export default rtcSession;
