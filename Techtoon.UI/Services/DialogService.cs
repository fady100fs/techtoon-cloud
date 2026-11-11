using System;
using System.Collections.Generic;

namespace Techtoon.UI.Services
{
    /// <summary>خدمة عرض الرسائل — قابلة للاستبدال في الاختبارات (لا تحجب الاختبار بنافذة).</summary>
    public interface IDialogService
    {
        void ShowInfo(string message, string title);
        void ShowWarning(string message, string title);
        void ShowError(string message, string title);
    }

    /// <summary>التنفيذ الافتراضي — MessageBox حقيقي.</summary>
    public class WpfDialogService : IDialogService
    {
        public void ShowInfo(string message, string title) =>
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

        public void ShowWarning(string message, string title) =>
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

        public void ShowError(string message, string title) =>
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    /// <summary>خدمة تسجيل بدل عرض النوافذ — تُستخدم في الاختبارات.</summary>
    public class RecordingDialogService : IDialogService
    {
        public List<string> Messages { get; } = new();
        public string LastKind { get; private set; } = "";

        public void ShowInfo(string message, string title) { LastKind = "info"; Messages.Add(title + ": " + message); }
        public void ShowWarning(string message, string title) { LastKind = "warning"; Messages.Add(title + ": " + message); }
        public void ShowError(string message, string title) { LastKind = "error"; Messages.Add(title + ": " + message); }
    }
}
