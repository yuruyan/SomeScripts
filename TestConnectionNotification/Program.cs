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
if (string.IsNullOrEmpty(target)) {
    Console.WriteLine("target is empty");
    return;
}
int successCount = 0, failedCount = 0;

while (true) {
    var proc = TaskUtils.Try(() => Process.Start(new ProcessStartInfo {
        FileName = "PowerShell.exe",
        Arguments = $"-Command \"& {{Test-Connection {target} -Quiet}}\"",
        RedirectStandardOutput = true,
    }));
    if (proc is null) {
        Console.WriteLine("Start process PowerShell failed");
        Thread.Sleep(1000);
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
    Thread.Sleep(4000);
}
