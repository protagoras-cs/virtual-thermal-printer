using Microsoft.Web.WebView2.WinForms;

namespace VirtualThermalPrinterDesktop;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private ToolStrip toolStrip = null!;
    private ToolStripButton refreshButton = null!;
    private ToolStripButton settingsButton = null!;
    private ToolStripLabel statusLabel = null!;
    private WebView2 webView = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(18, 24, 38);
        ClientSize = new Size(1200, 850);
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Virtual Thermal Printer Desktop";

        toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
        };

        refreshButton = new ToolStripButton("Refresh")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = Color.White,
        };

        settingsButton = new ToolStripButton("Settings")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = Color.White,
        };

        statusLabel = new ToolStripLabel("Starting...")
        {
            ForeColor = Color.FromArgb(191, 219, 254),
        };

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            refreshButton,
            settingsButton,
            new ToolStripSeparator(),
            statusLabel,
        });

        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };

        Controls.Add(webView);
        Controls.Add(toolStrip);
        toolStrip.BringToFront();
    }
}
