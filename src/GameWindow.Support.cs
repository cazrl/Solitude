using System.ComponentModel;
using System.Diagnostics;

namespace Solitude;

public sealed partial class GameWindow
{
    private const string DonationUrl = "https://ko-fi.com/flightwire";
    private Action<string> openExternalLink = url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private void OpenDonationPage()
    {
        try { openExternalLink(DonationUrl); }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            notice = "Could not open your browser. You can visit:\n" + DonationUrl;
            OpenDialog(DialogPage.Notice);
        }
    }
}
