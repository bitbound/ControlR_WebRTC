import { SignalrDtoType } from "./signalrDtoTypes";

export interface SignedPayloadDto {
    payload: string;
    signature: string;
    dtoType: SignalrDtoType;
    publicKey: string;
    publicKeyPem: string;
}