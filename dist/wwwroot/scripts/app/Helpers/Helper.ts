import {Buffer} from "Buffer";

class Helper
{
    public static HexToAscii = (str: string) => Buffer.from(str, 'hex').toString();

    public static async Delay(time: number): Promise<void> {
        return new Promise((resolve => {
            setTimeout(() => {
                resolve()
            }, time);
        }));
    }

    public static IsJsonString = (str: string): boolean => {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }
        return true;
    }

    public static HexToBytes = (str: string): Uint8Array => Uint8Array.from(Buffer.from(str, 'hex'));
}

export default Helper