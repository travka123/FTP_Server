using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FTP_Server
{
    public static class Logger
    {
        private static TextBox textBox;
        private static Dispatcher dispatcher;

        public static void SetTextBlock(TextBox tb)
        {
            dispatcher = tb.Dispatcher;
            textBox = tb;
        }

        public static void Log(string message)
        {
            dispatcher.Invoke(() =>
            {
                textBox.AppendText(message + Environment.NewLine);
                textBox.ScrollToEnd();
            });
        }
    }
}
