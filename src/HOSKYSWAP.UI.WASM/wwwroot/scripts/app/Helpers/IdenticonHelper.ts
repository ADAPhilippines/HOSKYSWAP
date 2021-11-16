import { toSvg } from "jdenticon";

class IdenticonHelper
{
    public static GenerateIdenticon(str: string)
    {
        return "data:image/svg+xml;base64," + btoa(toSvg(str, 100));
    }
}

export default IdenticonHelper;