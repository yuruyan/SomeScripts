using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MessageTools {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <remarks>
        /// Command line arguments:
        /// - icon=value: Set the icon of the message box. Possible values are:
        ///   - Information: An information symbol
        ///   - Warning: A warning symbol
        ///   - Error: An error symbol
        /// - title=value: Set the title of the message box.
        /// - message=value: Set the message of the message box.
        /// </remarks>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            GetArgs(args, out MessageBoxIcon icon, out string title, out string message);
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }

        /// <summary>
        /// Get command line arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <param name="icon">Icon of the message box</param>
        /// <param name="title">Title of the message box</param>
        /// <param name="message">Message of the message box</param>
        static void GetArgs(string[] args, out MessageBoxIcon icon, out string title, out string message) {
            icon = MessageBoxIcon.Information;
            title = "Infomation";
            message = "Information";
            foreach (string arg in args) {
                var match = Regex.Match(arg, "^icon=(.*)", RegexOptions.IgnoreCase);
                if (match.Success && Enum.TryParse(match.Groups[1].Value, out icon)) {
                    continue;
                }
                match = Regex.Match(arg, "^title=(.*)", RegexOptions.IgnoreCase);
                if (match.Success) {
                    title = match.Groups[1].Value;
                    continue;
                }
                match = Regex.Match(arg, "^message=(.*)", RegexOptions.IgnoreCase);
                if (match.Success) {
                    message = match.Groups[1].Value;
                    continue;
                }
            }
        }
    }
}
