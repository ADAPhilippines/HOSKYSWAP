class Helper
{
    public static HexToAscii = (str: string) => Buffer.from(str, 'hex').toString();
}

export default Helper