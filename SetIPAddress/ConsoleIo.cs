using System.Runtime.InteropServices;
using System.Text;

namespace SetIPAddress;

/// <summary>
/// 控制台输出与提权检测。刻意不使用 System.Text.Encoding.CodePages：
/// 2008 R2 中文控制台代码页为 936，若直接写字节会乱码；
/// 当 stdout 是控制台时走 WriteConsoleW（UTF-16），彻底绕开代码页。
/// </summary>
internal static class ConsoleIo {
    private const int StdOutputHandle = -11;
    private const int StdErrorHandle = -12;

    private static readonly IntPtr OutputHandle = GetStdHandle(StdOutputHandle);
    private static readonly IntPtr ErrorHandle = GetStdHandle(StdErrorHandle);
    private static readonly bool OutputIsConsole = IsConsoleHandle(OutputHandle);
    private static readonly bool ErrorIsConsole = IsConsoleHandle(ErrorHandle);
    private static readonly Stream OutputStream = Console.OpenStandardOutput();
    private static readonly Stream ErrorStream = Console.OpenStandardError();
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static void Write(string text) => WriteTo(OutputHandle, OutputIsConsole, OutputStream, text);

    public static void WriteLine(string text = "") => Write(text + Environment.NewLine);

    public static void ErrorLine(string text) => WriteTo(ErrorHandle, ErrorIsConsole, ErrorStream, text + Environment.NewLine);

    private static bool IsConsoleHandle(IntPtr handle) =>
        handle != IntPtr.Zero && handle != new IntPtr(-1) && GetConsoleMode(handle, out _);

    private static void WriteTo(IntPtr handle, bool isConsole, Stream stream, string text) {
        if (text.Length == 0) {
            return;
        }

        if (isConsole) {
            WriteConsoleW(handle, text, text.Length, out _, IntPtr.Zero);
            return;
        }

        byte[] buffer = Utf8.GetBytes(text);
        stream.Write(buffer, 0, buffer.Length);
        stream.Flush();
    }

    /// <summary>通过 TokenElevation 判断当前进程是否已提权（Vista 及以上可用）。</summary>
    public static bool IsElevated() {
        const uint TokenQuery = 0x0008;
        const int TokenElevationClass = 20;

        if (!OpenProcessToken(GetCurrentProcess(), TokenQuery, out IntPtr token)) {
            return false;
        }

        try {
            IntPtr buffer = Marshal.AllocHGlobal(sizeof(int));
            try {
                if (!GetTokenInformation(token, TokenElevationClass, buffer, sizeof(int), out _)) {
                    return false;
                }

                return Marshal.ReadInt32(buffer) != 0;
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        } finally {
            CloseHandle(token);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteConsoleW(
        IntPtr hConsoleOutput,
        string lpBuffer,
        int nNumberOfCharsToWrite,
        out int lpNumberOfCharsWritten,
        IntPtr lpReserved);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
