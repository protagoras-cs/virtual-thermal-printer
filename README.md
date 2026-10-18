# Virtual Thermal Printer

A web-based virtual thermal printer that simulates a real ESC/POS and Epson EPOS
printer. This tool is designed for developers who need to test point-of-sale
(POS) applications without a physical printer.

## Features

- **Epson EPOS Support:** Receives and processes Epson EPOS commands via an HTTP endpoint.
- **ESC/POS Support:** Receives and processes raw ESC/POS commands via a TCP socket.
- **Web-Based Rendering:** Renders the print output in a real-time web interface.

## Screenshots

Print jobs rendered in the web interface, laid out like on an 80 mm printer
(48 columns of Font A):

| Receipt | Formatting |
|---|---|
| <img src="docs/screenshots/receipt.png" alt="A sample café receipt with a centered header, item and price columns, a double-height total, a QR code and a reversed thank-you line" width="400"> | <img src="docs/screenshots/formatting.png" alt="Bold, underlined, reversed, double-width and double-height text, Font B, alignment, tab stops, line wrapping and an EAN-13 barcode" width="400"> |

## Packages

| Package | Description |
|---|---|
| [`escpos-decoder`](packages/escpos-decoder) | Transforms a stream of raw ESC/POS bytes into typed TypeScript blocks |
| [`virtual-thermal-printer`](packages/virtual-thermal-printer) | HTTP + WebSocket + TCP server with browser UI |

## Local Development

Requires [Node.js](https://nodejs.org/) 22+.

```bash
git clone https://github.com/FilipChalupa/virtual-thermal-printer.git
cd virtual-thermal-printer
npm install
npm run dev
```

The server starts at `http://localhost:8555` and listens for raw ESC/POS on TCP port `9101`.

Both ports can be overridden:

```bash
./virtual-thermal-printer --http 8555 --socket 9101
```

## Windows Desktop App

The desktop companion provides an embedded, scrollable print view, starts and
stops the local printer server automatically, and prompts for alternative ports
when the configured ports are already in use. The default ports are HTTP `8555`
and TCP `9101`.

For a release, download `VirtualThermalPrinterDesktop-win-x64.zip`, extract the
entire archive, and run `VirtualThermalPrinterDesktop.exe`. Keep the bundled
`virtual-thermal-printer-windows.exe` beside it; the desktop app starts this
server executable. Microsoft Edge WebView2 Runtime must be available on Windows.

To run from source, install Node.js 22+ and the .NET 8 SDK, then run `npm ci`,
`npm run build`, and `dotnet run --project desktop-app/VirtualThermalPrinterDesktop.csproj`
from the repository root. The source-mode desktop app uses the local Node.js
installation and the repository's installed npm dependencies.

## Sending Print Commands

> [!WARNING]
> The public server is shared among all users. Any print jobs you send will be
> visible to anyone currently viewing the page.

### Epson EPOS (HTTP)

> [!WARNING]
> The public server only supports Epson EPOS commands via HTTP. Raw ESC/POS
> commands over TCP are **not** supported on the public server and must be used
> with a locally running instance.

```bash
curl -X POST \
  -H "Content-Type: text/xml" \
  --data @packages/virtual-thermal-printer/fixtures/request1.xml \
  http://localhost:8555/cgi-bin/epos/service.cgi
```

### ESC/POS (TCP Socket)

Connect to port `9101` and send raw ESC/POS bytes, e.g. via `netcat`:

```bash
echo -e "\x1b\x40Hello\x0a" | nc localhost 9101
```

## Docker

```bash
npm run docker:build
npm run docker:start
```

Or manually:

```bash
docker build -t virtual-thermal-printer .
docker run --rm -p 8555:8555 -p 9101:9101 virtual-thermal-printer
```

The web UI is available at `http://localhost:8555` and the TCP socket on port `9101`.

## Releasing

1. Bump the patch version, update the lockfile, and create a commit and tag:
   ```bash
   npm run version:bump
   npm run version:bump -- --part=minor
   npm run version:bump -- --part=major
   ```
2. Push the branch and the new tag to your fork (replace `<fork-remote>` with its remote name):
   ```bash
   git push <fork-remote> main
   git push <fork-remote> v0.1.1
   ```

Pushing a `v*` tag triggers GitHub Actions to build Linux, macOS, and Windows
server binaries, publish the Windows desktop app bundle, and create a GitHub
Release. The desktop ZIP contains the app and its bundled server executable.

## Scripts

| Command | Description |
|---|---|
| `npm run dev` | Start dev server with hot reload on port 8555 |
| `npm run build` | Build `escpos-decoder` and the frontend |
| `npm test` | Run all tests across all packages |
| `npm run docker:build` | Build Docker image |
| `npm run docker:start` | Run Docker container |
| `npm run version:bump` | Bump patch version, commit and tag |

## License

[MIT](LICENSE)
