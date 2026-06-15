using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SYSi.BugTracker;

public partial class ReportPopup : Window
{
    public enum ReportAction
    {
        None,
        Facebook,
        GitHub
    }

    public ReportAction SelectedAction { get; private set; }
        = ReportAction.None;

    private bool _closed { 
        get 
        {
            return SelectedAction != ReportAction.None;
        } 
    }

    public ReportPopup()
    {
        InitializeComponent();
        Deactivated += (_, _) =>
        {
            if (!_closed)
            {
                Close();
            }
        };
    }

    private void Facebook_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = ReportAction.Facebook;

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.facebook.com/MaiXuan.HuynhOR/",
            UseShellExecute = true
        });

        Close();
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = ReportAction.GitHub;

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/maihcx/SYSi/issues",
            UseShellExecute = true
        });

        Close();
    }
}