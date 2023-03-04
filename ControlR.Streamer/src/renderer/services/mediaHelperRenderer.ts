export async function setMediaStreams(sourceId: string, peerConnection: RTCPeerConnection) {
    console.log("Getting stream for media source ID: ", sourceId);

    const constraints = getDefaultConstraints();
    constraints.video.mandatory.chromeMediaSourceId = sourceId;

    let stream: MediaStream;

    try {
        stream = await navigator.mediaDevices.getUserMedia(constraints as MediaStreamConstraints);
        setTrack(stream, peerConnection);
    }
    catch (ex) {
        console.warn(ex);
        console.log("Failed to get media with audio constraints.  Dropping audio.");
        delete constraints.audio;
        stream = await navigator.mediaDevices.getUserMedia(constraints as MediaStreamConstraints);
        setTrack(stream, peerConnection);
    }
}

function getDefaultConstraints() {
    return {
        video: {
            mandatory: {
                chromeMediaSource: 'desktop',
                chromeMediaSourceId: ''
            }
        },
        audio: {
            mandatory: {
                chromeMediaSource: 'desktop',
                chromeMediaSourceId: ''
            }
        }
    };    
}

function setTrack(stream: MediaStream, peerConnection: RTCPeerConnection) {
    stream.getTracks().forEach(track => {
        const existingSenders = peerConnection.getSenders();
        const existingTracks = existingSenders.filter(x => x.track.kind == track.kind);
        if (existingTracks && existingTracks.length > 0) {
            existingTracks.forEach(x => {
                console.log("Replacing existing track: ", x);
                x.replaceTrack(track);
            });
        }
        else {
            console.log("Adding track: ", track);
            peerConnection.addTrack(track, stream);
        }
    });
}