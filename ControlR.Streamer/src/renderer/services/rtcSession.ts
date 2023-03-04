import { Point } from "@nut-tree/nut-js";
import { MediaScreen } from "../../shared/global";
import desktopHubConnection from "./desktopHubConnection";
import { setMediaStreams } from "./mediaHelperRenderer";

class RtcSession {
  peerConnection?: RTCPeerConnection;
  dataChannel?: RTCDataChannel;
  currentScreen?: MediaScreen;
  screens: MediaScreen[] = [];

  async receiveRtcSessionDescription(remoteDescription: RTCSessionDescription) {
    console.log("Received session description: ", remoteDescription);

    if (!this.peerConnection) {
      await this.initializePeerConnection();
    }

    console.log("Setting remote description.");
    await this.peerConnection.setRemoteDescription(remoteDescription);

    if (remoteDescription.type == "offer") {
      console.log("Creating answer.");
      await this.peerConnection.setLocalDescription(
        await this.peerConnection.createAnswer()
      );

      console.log("Sending RTC answer: ", this.peerConnection.localDescription);
      await desktopHubConnection.sendRtcSessionDescription(
        this.peerConnection.localDescription
      );
    }
  }

  async receiveIceCandidate(iceCandidateJson?: string): Promise<void> {
    if (!this.peerConnection || !this.peerConnection.remoteDescription) {
      console.log(
        "Received ICE candidate, but initialization hasn't completed.  Retrying in 1 second."
      );
      setTimeout(() => {
        this.receiveIceCandidate(iceCandidateJson);
      }, 1000);
      return;
    }

    if (!iceCandidateJson) {
      console.log("Received null (terminating) ICE candidate");
      await this.peerConnection.addIceCandidate(null);
      return;
    }

    const iceCandidate = JSON.parse(iceCandidateJson);
    console.log("Received ICE candidate: ", iceCandidate);
    await this.peerConnection.addIceCandidate(iceCandidate);
  }

  private async initializePeerConnection(): Promise<void> {
    console.log("Getting ICE servers.");

    const iceServers = await desktopHubConnection.getIceServers();
    console.log("Got ICE servers: ", iceServers);

    this.peerConnection = new RTCPeerConnection({
      iceServers: iceServers,
    });

    this.setConnectionHandlers();

    console.log("Getting screens from main API.");
    this.screens = await window.mainApi.getScreens();
    console.log("Found screens: ", this.screens);

    this.currentScreen = this.screens[0];
    console.log("Getting stream for first screen: ", this.currentScreen);

    console.log("Adding tracks from stream.");
    await setMediaStreams(this.currentScreen.mediaId, this.peerConnection);
  }

  private setConnectionHandlers() {
    console.log("Setting peer connection handlers.");
    this.peerConnection.addEventListener("connectionstatechange", async () => {
      console.log(
        "Connection state changed: ",
        this.peerConnection.connectionState
      );
      switch (this.peerConnection.connectionState) {
        case "closed":
        case "disconnected":
          console.log("Restarting ICE.");
          this.peerConnection.restartIce();
          break;
        case "failed":
          console.log("Connection failed.  Exiting.");
          await window.mainApi.exit();
          break;
        default:
          break;
      }
    });
    this.peerConnection.addEventListener("iceconnectionstatechange", (ev) => {
      console.log(
        "ICE connection state changed: ",
        this.peerConnection.iceConnectionState
      );
    });
    this.peerConnection.addEventListener("icegatheringstatechange", (ev) => {
      console.log(
        "ICE gathering state changed: ",
        this.peerConnection.iceGatheringState
      );
    });
    this.peerConnection.addEventListener("track", (ev) => {
      console.log("Got track: ", ev);
    });
    this.peerConnection.addEventListener("icecandidate", async (ev) => {
      if (!ev.candidate) {
        console.log("End of ICE candidates.");
        return;
      }
      console.log("Sending ICE candidate: ", ev.candidate);
      await desktopHubConnection.sendIceCandidate(
        JSON.stringify(ev.candidate.toJSON())
      );
    });
    this.peerConnection.addEventListener("negotiationneeded", async (ev) => {
      console.log("Negotiation needed. Creating new offer.");
      await this.peerConnection.setLocalDescription(
        await this.peerConnection.createOffer()
      );
      await desktopHubConnection.sendRtcSessionDescription(
        this.peerConnection.localDescription
      );
    });
    this.peerConnection.addEventListener("datachannel", (ev) => {
      this.dataChannel = ev.channel;
      this.dataChannel.addEventListener("close", () => {
        console.log("DataChannel closed.");
      });

      this.dataChannel.addEventListener("error", () => {
        console.log("DataChannel error.");
      });

      this.dataChannel.addEventListener("open", () => {
        console.log("DataChannel opened.");
      });

      this.dataChannel.addEventListener("message", async (ev) => {
        console.log("Got DataChannel message: ", ev.data);

        await this.handleDataChannelMessage(ev.data);
      });
    });
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
          await window.mainApi.invokeKeyEvent(keyDto.keyCode, keyDto.isPressed);
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
      default:
        console.warn("Unhandled DTO type: ", dto.dtoType);
        break;
    }
  }
  private getAbsoluteScreenPoint(percentX: number, percentY: number): Point {
    const screenBounds = this.currentScreen.bounds;
    const x = screenBounds.width * percentX + screenBounds.x;
    const y = screenBounds.height * percentY + screenBounds.y;
    return { x: x, y: y };
  }
  private getAbsoluteY() {}
}

const rtcSession = new RtcSession();

export default rtcSession;
