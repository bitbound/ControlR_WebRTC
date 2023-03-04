import { Component, CSSProperties } from "react";
import { iconImage512x512Base64 } from "../images";
import "./sessionIndicator.tsx.css";

export class SessionIndicator extends Component {
    render() {
       
        return (
            <div style={wrapperCss} className="draggable">
                <div className="text-primary mb-2">
                    Your screen is being viewed
                </div>
                <img
                    src={iconImage512x512Base64}
                    style={logoCss} />
            </div>
        );
    }
}

const wrapperCss = {
    position: "absolute",
    top: "50%",
    left: "50%",
    textAlign: "center",
    transform: "translate(-50%, -50%)",
    width: "100vw",
    overflow: "hidden"
} as CSSProperties;

const logoCss = {
    height: "90px",
    width: "90px",
    animationName: "spinLogo",
    animationIterationCount: "infinite",
    animationDirection: "alternate",
    animationDuration: "3s",
    animationTimeline: "linear"
} as CSSProperties;


