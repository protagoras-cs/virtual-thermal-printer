using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace VirtualThermalPrinterDesktop;

public partial class Form1 : Form
{
    private readonly AppSettings _settings;
    private Process? _serverProcess;

    public Form1()
    {
        _settings = AppSettings.Load();
        InitializeComponent();

        refreshButton.Click += (_, _) => RefreshPage();
        settingsButton.Click += (_, _) => OpenSettings();
        Load += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => StopServer();
    }

    private async Task InitializeAsync()
    {
        if (!EnsureUsablePorts())
        {
            statusLabel.Text = "Startup cancelled";
            return;
        }

        if (!TryStartServer())
        {
            statusLabel.Text = "Node server unavailable";
            ShowFallbackHtml("The local printer server could not be started.\n\nMake sure Node.js is installed and the project files are available.");
            return;
        }

        if (!await WaitForServerAsync())
        {
            statusLabel.Text = "Server did not respond";
            ShowFallbackHtml($"The page at {_settings.PrinterUrl} did not respond in time.\n\nCheck the server logs or change the HTTP port from Settings.");
            return;
        }

        RefreshPage();
    }

    private bool EnsureUsablePorts()
    {
        while (PortInUse(_settings.HttpPort) || PortInUse(_settings.SocketPort) || _settings.HttpPort == _settings.SocketPort)
        {
            using var dialog = new SettingsForm(_settings, "One or more ports are already in use. Choose alternate ports.");
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            _settings.HttpPort = dialog.HttpPort;
            _settings.SocketPort = dialog.SocketPort;
            _settings.Save();
        }

        return true;
    }

    private bool TryStartServer()
    {
        var bundledServer = Path.Combine(AppContext.BaseDirectory, "virtual-thermal-printer-windows.exe");
        ProcessStartInfo startInfo;

        if (File.Exists(bundledServer))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = bundledServer,
                Arguments = $"--http {_settings.HttpPort} --socket {_settings.SocketPort}",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }
        else
        {
            var nodeExecutable = ResolveNodeExecutable();
            var projectRoot = FindProjectRoot();
            if (projectRoot is null)
            {
                ShowFallbackHtml("The bundled server or project source files could not be found.\n\nExtract the complete release ZIP, or run the app from the project folder.");
                return false;
            }

            var packageDir = Path.Combine(projectRoot, "packages", "virtual-thermal-printer");
            startInfo = new ProcessStartInfo
            {
                FileName = nodeExecutable,
                Arguments = $"--import tsx/esm --conditions=source main.ts --http {_settings.HttpPort} --socket {_settings.SocketPort}",
                WorkingDirectory = packageDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }

        _serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _serverProcess.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.WriteLine(args.Data);
            }
        };
        _serverProcess.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.Error.WriteLine(args.Data);
            }
        };

        try
        {
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
            statusLabel.Text = $"Starting server on {_settings.PrinterUrl}";
            return true;
        }
        catch (Exception ex)
        {
            ShowFallbackHtml($"The server process failed to start.\n\n{ex.Message}");
            return false;
        }
    }

    private static string ResolveNodeExecutable()
    {
        var localNode = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
        if (File.Exists(localNode))
        {
            return localNode;
        }

        return "node";
    }

    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "packages", "virtual-thermal-printer");
            if (Directory.Exists(candidate))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static bool PortInUse(int port)
    {
        var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        return listeners.Any(listener => listener.Port == port);
    }

    private async Task<bool> WaitForServerAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                var response = await client.GetAsync(_settings.PrinterUrl + "health");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Retry until the Node server comes online.
            }

            await Task.Delay(250);
        }

        return false;
    }

    private void RefreshPage()
    {
        var url = _settings.PrinterUrl;
        statusLabel.Text = $"Loaded {url}";

        try
        {
            if (webView.CoreWebView2 is null)
            {
                _ = webView.EnsureCoreWebView2Async();
            }

            webView.Source = new Uri(url);
        }
        catch
        {
            ShowFallbackHtml($"Unable to open the printer page at {url}.\n\nPlease start the server or adjust the configured port in Settings.");
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings, "Update the local server ports.");
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var newHttpPort = dialog.HttpPort;
            var newSocketPort = dialog.SocketPort;

            if (newHttpPort == newSocketPort)
            {
                MessageBox.Show(this, "HTTP and socket ports must be different.", "Port conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.HttpPort = newHttpPort;
            _settings.SocketPort = newSocketPort;
            _settings.Save();

            StopServer();
            if (TryStartServer())
            {
                _ = WaitForServerAsync();
            }

            RefreshPage();
        }
    }

    private void StopServer()
    {
        if (_serverProcess is null || _serverProcess.HasExited)
        {
            return;
        }

        try
        {
            _serverProcess.Kill(entireProcessTree: true);
            _serverProcess.WaitForExit(4000);
        }
        catch
        {
            // Ignore cleanup failures when the process has already exited.
        }
    }

    private void ShowFallbackHtml(string message)
    {
        var html = new StringBuilder();
        html.Append("<html><head><meta charset='utf-8'><style>");
        html.Append("body{font-family:Segoe UI,sans-serif;background:#121826;color:#e5e7eb;margin:0;padding:24px;} ");
        html.Append(".panel{max-width:960px;margin:24px auto;background:#1f2937;border:1px solid #374151;border-radius:16px;padding:24px;box-shadow:0 12px 28px rgba(0,0,0,.35);} ");
        html.Append("h1{margin-top:0;font-size:26px;} ");
        html.Append(".box{white-space:pre-wrap;line-height:1.6;background:#0f172a;padding:18px;border-radius:12px;color:#dbeafe;overflow:auto;max-height:70vh;} ");
        html.Append("</style></head><body><div class='panel'><h1>Virtual Thermal Printer</h1><div class='box'>");
        html.Append(System.Security.SecurityElement.Escape(message));
        html.Append("</div></div></body></html>");

        if (webView.IsHandleCreated)
        {
            webView.NavigateToString(html.ToString());
        }
    }
}
