namespace VirtualThermalPrinterDesktop;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _httpPort;
    private readonly NumericUpDown _socketPort;
    private readonly Label _messageLabel;

    public int HttpPort => (int)_httpPort.Value;
    public int SocketPort => (int)_socketPort.Value;

    public SettingsForm(AppSettings settings, string? message = null)
    {
        Text = "Printer ports";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 430;
        Height = 250;
        ShowInTaskbar = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

        _messageLabel = new Label
        {
            Text = message ?? "Choose the ports used by the local printer server.",
            ForeColor = Color.FromArgb(31, 41, 55),
            AutoSize = true,
            MaximumSize = new Size(340, 0),
            Anchor = AnchorStyles.Left,
        };

        var httpLabel = new Label
        {
            Text = "HTTP port",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
        };

        _httpPort = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = settings.HttpPort,
            Width = 170,
        };

        var socketLabel = new Label
        {
            Text = "Socket port",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
        };

        _socketPort = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = settings.SocketPort,
            Width = 170,
        };

        var saveButton = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        };

        layout.Controls.Add(_messageLabel, 0, 0);
        layout.SetColumnSpan(_messageLabel, 2);
        layout.Controls.Add(httpLabel, 0, 1);
        layout.Controls.Add(_httpPort, 1, 1);
        layout.Controls.Add(socketLabel, 0, 2);
        layout.Controls.Add(_socketPort, 1, 2);

        var buttonRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };

        buttonRow.Controls.Add(cancelButton);
        buttonRow.Controls.Add(saveButton);
        layout.Controls.Add(buttonRow, 0, 4);
        layout.SetColumnSpan(buttonRow, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        FormClosing += (_, _) =>
        {
            if (DialogResult == DialogResult.OK)
            {
                _httpPort.Value = Math.Clamp((int)_httpPort.Value, 1, 65535);
                _socketPort.Value = Math.Clamp((int)_socketPort.Value, 1, 65535);
            }
        };
    }
}
