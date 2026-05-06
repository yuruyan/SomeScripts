using CommonTools.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using TestConnectionNotification;

//// 监听通知激活(点击)
//ToastNotificationManagerCompat.OnActivated += toastArgs => {
//    Environment.Exit(0);
//};

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resources.Help)) {
    return;
}

var Config = ArgumentUtils.GetConfiguration(args);

var target = Config["target"] ?? args[0];
var portArg = Config["port"];
var port = -1;
if (string.IsNullOrEmpty(target)) {
    Console.Error.WriteLine("target is empty");
    return;
}
// 端口参数验证
if (portArg is not null) {
    if (!int.TryParse(portArg, out port)) {
        Console.Error.WriteLine("port is not a number");
        return;
    } else if (port < 1 || port > 65535) {
        Console.Error.WriteLine("port is out of range");
        return;
    }
}

int successCount = 0, failedCount = 0;

while (true) {
    Thread.Sleep(1000);
    var arg = port == -1 ? $"Test-Connection {target} -Quiet" : $"(Test-NetConnection {target} -Port {port}).TcpTestSucceeded";
    var command = $"-Command \"& {{{arg}}}\"";
    var proc = TaskUtils.Try(() => Process.Start(new ProcessStartInfo {
        FileName = "PowerShell.exe",
        Arguments = command,
        RedirectStandardOutput = true,
    }));
    if (proc is null) {
        Console.WriteLine("Start process PowerShell failed");
        continue;
    }
    proc.WaitForExit();
    var output = proc.StandardOutput.ReadToEnd() ?? string.Empty;
    // failed
    if (!output.Trim().Contains("true", StringComparison.InvariantCultureIgnoreCase)) {
        failedCount++;
        Console.WriteLine("Test-Connection timeout " + failedCount);
        successCount = 0;
        continue;
    }
    successCount++;
    // success
    Console.WriteLine($"Test-Connection success {DateTime.Now}");
    new ToastContentBuilder()
        .AddText($"Test-Connection {target} success") // 标题文本
        .Show();
    // 成功连续超过 3 次，退出程序
    if (successCount >= 3) {
        return;
    }
}
