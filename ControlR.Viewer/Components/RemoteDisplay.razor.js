class State {
    constructor() {
        this.windowEventHandlers = [];
        this.touchCount = 0;
        this.lastPointerMove = Date.now();
    }

    /** @type {any} */
    componentRef;

    /** @type {string} */
    currentPointerType;

    /** @type {RTCDataChannel} */
    dataChannel;

    /** @type {number] */
    lastPointerMove;

    /** @type {RTCPeerConnection} */
    peerConnection;

    /** @type {number} */
    touchCount;

    /** @type {string} */
    videoId;

    /** @type {HTMLVideoElement} */
    videoElement;

    /** @type {WindowEventHandler[]} */
    windowEventHandlers;
}

class WindowEventHandler {
    /**
     * 
     * @param {keyof WindowEventMap} type
     * @param {EventListener} handler
     */
    constructor(type, handler) {
        this.type = type;
        this.handler = handler;
    }

    /** @type {keyof WindowEventMap} */
    type;

    /** @type {EventListener} */
    handler;
}

/**
 * 
 * @param {string} videoId
 */
export async function dispose(videoId) {
    const state = getState(videoId);

    try {
        if (state.peerConnection) {
            state.peerConnection.close();
        }
    }
    catch { }

    try {
        if (state.dataChannel) {
            state.dataChannel.close();
        }
    }
    catch { }

    state.windowEventHandlers.forEach(x => {
        console.log("Removing event handler: ", x);
        window.removeEventListener(x.type, x.handler);
    })

    delete window[videoId];
}

/**
 * Retains a reference to the video element ID and registers
 * event handlers for the video element.
 * @param {any} componentRef
 * @param {string} videoId
 */
export async function initialize(componentRef, videoId) {
    console.log("Setting video element ID: ", videoId);

    const state = getState(videoId);
    console.log("Got new state: ", state);
    const video = document.getElementById(videoId);

    state.componentRef = componentRef;
    state.videoId = videoId;
    state.videoElement = video;
    state.videoElement.onloadedmetadata = () => {
        state.videoElement.play();
    }

    video.addEventListener("touchstart", ev => {
        state.touchCount = ev.touches.length;
    });

    video.addEventListener("touchend", ev => {
        touchCount = ev.touches.length;
    });
    
    video.addEventListener("pointermove", async ev => {
        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }
        state.currentPointerType = ev.pointerType;

        if (!isDataChannelReady(videoId)) {
            return;
        }
        const now = Date.now();
        if (now - state.lastPointerMove < 50) {
            return;
        }

        state.lastPointerMove = now;

        if (ev.pointerType == "touch") {
            
        }
        else {
            const pointerMoveDto = {
                dtoType: "pointerMove",
                percentX: ev.offsetX / state.videoElement.clientWidth,
                percentY: ev.offsetY / state.videoElement.clientHeight
            };
            
            state.dataChannel.send(JSON.stringify(pointerMoveDto));
        }
    });

    video.addEventListener("pointerdown", async ev => {
        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }
        if (ev.pointerType == "touch") {

        }
        else {
            const mouseButtonDto = {
                dtoType: "mouseButtonEvent",
                percentX: ev.offsetX / state.videoElement.clientWidth,
                percentY: ev.offsetY / state.videoElement.clientHeight,
                isPressed: true,
                button: ev.button
            };
            state.dataChannel.send(JSON.stringify(mouseButtonDto));
        }
    });

    video.addEventListener("pointerup", async ev => {
        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }
        if (ev.pointerType == "touch") {

        }
        else {
            const mouseButtonDto = {
                dtoType: "mouseButtonEvent",
                percentX: ev.offsetX / state.videoElement.clientWidth,
                percentY: ev.offsetY / state.videoElement.clientHeight,
                isPressed: false,
                button: ev.button
            };
            state.dataChannel.send(JSON.stringify(mouseButtonDto));
        }
    });

    video.addEventListener("wheel", ev => {
        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }

        const wheelScrollDto = {
            dtoType: "wheelScrollEvent",
            deltaX: ev.deltaX,
            deltaY: ev.deltaY,
            deltaZ: ev.deltaZ
        };
        state.dataChannel.send(JSON.stringify(wheelScrollDto));
    });

    video.addEventListener("contextmenu", async ev => {
        ev.preventDefault();
    });

    const onKeyDown = (ev) => {
        if (document.querySelector("input:focus") || document.querySelector("textarea:focus")) {
            return;
        }

        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }

        if (!ev.ctrlKey || !ev.shiftKey || ev.key.toLowerCase() != "i") {
            ev.preventDefault();
        }

        const keyPressDto = {
            dtoType: "keyEvent",
            isPressed: true,
            keyCode: ev.code
        };
        state.dataChannel.send(JSON.stringify(keyPressDto));
    };
    window.addEventListener("keydown", onKeyDown);
    state.windowEventHandlers.push(new WindowEventHandler("keydown", onKeyDown));

    const onKeyUp = (ev) => {
        if (document.querySelector("input:focus") || document.querySelector("textarea:focus")) {
            return;
        }

        if (!isDataChannelReady(videoId) || video.classList.contains("minimized")) {
            return;
        }
        ev.preventDefault();
        const keyPressDto = {
            dtoType: "keyEvent",
            isPressed: false,
            keyCode: ev.code
        };
        state.dataChannel.send(JSON.stringify(keyPressDto));
    }
    window.addEventListener("keyup", onKeyUp);
    state.windowEventHandlers.push(new WindowEventHandler("keyup", onKeyUp));

    const onBlur = () => {
        if (!isDataChannelReady(videoId)) {
            return;
        }
        const resetKeysDto = {
            dtoType: "resetKeyboardState"
        };
        state.dataChannel.send(JSON.stringify(resetKeysDto));
    }
    window.addEventListener("blur", onBlur);
    state.windowEventHandlers.push("blur", onBlur);
}

/**
 * 
 * @param {string} candidateJson
 * @param {string} videoId
 */
export async function receiveIceCandidate(candidateJson, videoId) {
    const state = getState(videoId);

    if (!candidateJson) {
        console.log("Received null (terminating) ICE candidate.");
        invokeDotNet("LogInfo", videoId, "Received null (terminating) ICE candidate");
        await this.peerConnection.addIceCandidate(null);
        return;
    }

    invokeDotNet("LogInfo", videoId, "Adding ICE candidate: " + candidateJson);
    const candidate = JSON.parse(candidateJson);
    console.log("Adding ICE candidate: ", candidate);
    state.peerConnection.addIceCandidate(candidate);
}

/**
 * 
 * @param {RTCSessionDescription} sessionDescription
 * @param {string} videoId
 */
export async function receiveRtcSessionDescription(sessionDescription, videoId) {
    const state = getState(videoId);

    console.log("Setting remote description: ", sessionDescription);
    invokeDotNet("LogInfo", videoId, "Setting remote description: " + JSON.stringify(sessionDescription));
    await state.peerConnection.setRemoteDescription(sessionDescription);

    if (sessionDescription.type == "offer") {
        console.log("Creating RTC answer.");
        invokeDotNet("LogInfo", videoId, "Creating RTC answer.");
        await state.peerConnection.setLocalDescription(await state.peerConnection.createAnswer());
        console.log("Sending RTC answer.");
        invokeDotNet("LogInfo", videoId, "Sending RTC answer.");
        await invokeDotNet("SendRtcDescription", videoId, state.peerConnection.localDescription);
    }
}

/**
 * 
 * @param {RTCIceServer[]} iceServers
 * @param {string} videoId
 */
export async function startRtcOffer(iceServers, videoId) {
    const state = getState(videoId);

    console.log("Creating peer connection with ICE servers: ", iceServers);
    invokeDotNet("LogInfo", videoId, "Creating peer connection with ICE servers: " + JSON.stringify(iceServers));

    if (state.peerConnection) {
        state.peerConnection.close();
    }

    const pc = new RTCPeerConnection({
        iceServers: iceServers,
    });

    state.peerConnection = pc;
    setPeerConnectionHandlers(state.peerConnection, videoId);

    state.dataChannel = pc.createDataChannel("input");
    setDataChannelHandlers(state.dataChannel, videoId);
}


/**
 * 
 * @param {string} videoId
 * @returns {State}
 */
function getState(videoId) {
    if (!window[`state-${videoId}`]) {
        window[`state-${videoId}`] = new State();
    }
    return window[`state-${videoId}`];
}

/**
 * 
 * @param {string} videoId
 * @returns
 */
function isDataChannelReady(videoId) {
    const state = getState(videoId);
    if (!state.dataChannel || state.dataChannel.readyState != "open") {
        return false;
    }

    return true;
}

/**
 * 
 * @param {RTCPeerConnection} peerConnection
 * @param {string} videoId
 */
function setPeerConnectionHandlers(peerConnection, videoId) {
    const state = getState(videoId);

    peerConnection.addEventListener("connectionstatechange", ev => {
        console.log("Connection state changed: ", peerConnection.connectionState);
        invokeDotNet("LogInfo", videoId, "Connection state changed: " + peerConnection.connectionState);
        switch (peerConnection.connectionState) {
            case "closed":
            case "disconnected":
                peerConnection.restartIce();
                invokeDotNet("SetStatusMessage", videoId, "Reconnecting");
                break;
            case "failed":
                invokeDotNet("SetStatusMessage", videoId, "Connection failed");
                break;
            case "connected":
                invokeDotNet("SetStatusMessage", videoId, "");
                break;
            default:
                break;
        }
    });

    peerConnection.addEventListener("iceconnectionstatechange", ev => {
        console.log("ICE connection state changed: ", peerConnection.iceConnectionState);
        invokeDotNet("LogInfo", videoId, "ICE connection state changed: " + peerConnection.iceConnectionState);
    });

    peerConnection.addEventListener("icecandidateerror", ev => {
        console.log("ICE candidate error: ", ev);
        invokeDotNet("LogInfo", videoId, "ICE candidate error: " + JSON.stringify(ev));
    });

    peerConnection.addEventListener("signalingstatechange", ev => {
        console.log("Signaling state changed: ", peerConnection.signalingState);
        invokeDotNet("LogInfo", videoId, "Signaling state changed: " + peerConnection.signalingState);
    });

    peerConnection.addEventListener("track", async ev => {
        console.log("Got tracks: ", ev);
        invokeDotNet("LogInfo", videoId, "Got tracks: " + JSON.stringify(ev));
        await invokeDotNet("SetStatusMessage", videoId, "");
        state.videoElement.srcObject = ev.streams[0];
    });

    peerConnection.addEventListener("negotiationneeded", async ev => {
        console.log("Negotiation needed.");
        invokeDotNet("LogInfo", videoId, "Negotiation needed.");
        await peerConnection.setLocalDescription(await peerConnection.createOffer());
        await invokeDotNet("SendRtcDescription", videoId, peerConnection.localDescription);
    });

    peerConnection.addEventListener("icecandidate", async ev => {
        if (!ev.candidate) {
            console.log("End of ICE candidates.");
            invokeDotNet("LogInfo", videoId, "End of ICE candidates.");
            return;
        }

        console.log("Ice candidate: ", ev.candidate);
        invokeDotNet("LogInfo", videoId, "Ice candidate: " + JSON.stringify(ev.candidate.toJSON()));
        await invokeDotNet("SendIceCandidate", videoId, JSON.stringify(ev.candidate.toJSON()));
    });
}

/**
 * 
 * @param {RTCDataChannel} dataChannel
 * @param {string} videoId
 */
function setDataChannelHandlers(dataChannel, videoId) {
    dataChannel.addEventListener("close", () => {
        console.log("DataChannel closed.");
        invokeDotNet("LogInfo", videoId, "DataChannel closed.");
    });

    dataChannel.addEventListener("error", () => {
        console.log("DataChannel error.");
        invokeDotNet("LogInfo", videoId, "DataChannel error.");
    });

    dataChannel.addEventListener("open", async () => {
        console.log("DataChannel opened");
        invokeDotNet("LogInfo", videoId, "DataChannel opened.");
        await invokeDotNet("SetStatusMessage", videoId, "");
    });

    dataChannel.addEventListener("message", ev => {
        console.log("Got DataChannel message: ", ev.data);
        invokeDotNet("LogInfo", videoId, "Got DataChannel message: " + ev.data);
    });
}

/**
 * @param {string} methodName
 * @param {string} videoId
 * @returns {Promise<any>}
 */
function invokeDotNet(methodName, videoId, ...args) {
    const state = getState(videoId);
    return state.componentRef.invokeMethodAsync(methodName, ...args);
}