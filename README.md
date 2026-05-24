# Xbox Modding Suite (Image Extractor & Game Downloader) 🎮

Created by **Dromex** (Instagram: [@dromex__](https://www.instagram.com/dromex__/))

A modern, high-performance, All-In-One C# (.NET 8 / WinForms) utility designed for the ultimate Xbox 360 and Classic Xbox modding experience. This open-source repository completely supersedes legacy tools like "Xbox Image Browser v2.9", offering a beautiful UI, asynchronous I/O, and integrated web scraping features.

## 📥 Download

**[⬇ Download v1.2.0 (Windows x64)](https://github.com/dromex1/XboxImageExtractor/releases/download/v1.2.0/XboxImageExtractor-v1.2-win-x64.zip)**

> Self-contained .NET 8 executable — no installation required. Just extract and run.

## ✨ Features

### 1. Xbox 360 (XGD2/XGD3) & Classic Xbox (XISO)
*   **Asynchronous I/O Engine**: 100% UI responsiveness. Uses large 4MB buffers and async streams to extract files blazing fast compared to older tools.
*   **Protection Against GDFX Corruption**: Recalculates absolute offsets against data partition bases to prevent infinity loops commonly experienced on packed XGD3 discs.
*   **Raw Optical Drive Ripping**: Insert your original Xbox 360 discs directly into your PC's DVD-ROM (bypasses Windows filesystem limits).
*   **Built-in Disc Burner**: Integrated Windows `IMAPI2` COM API. Automatically burns extracted directories to DVD/CD in UDF format ready for RGH/JTAG consoles!

### 2. Live Game Downloader
*   **Instant Vimm's Lair Integration**: Built-in search engine queries the Vimm's Lair database in real-time, pulling from all Xbox and Xbox 360 vaults.
*   **Live Search**: Zero-lag debounced search bar updates results instantly.
*   **Background Fetching**: Automatically fetches and deduplicates lists of available games for offline browsing.
*   **Pause / Resume / Cancel**: Full download control with a dedicated notification panel.

### 3. My Downloads (NEW in v1.2.0)
*   **Smart Tracking**: Only games downloaded through the app are displayed — no scanning of your entire Downloads folder.
*   **One-Click Extract & Load**: Each downloaded game has an inline "Extract & Load ISO" button that unpacks the `.7z`/`.zip` archive, auto-detects Xbox Classic vs Xbox 360 from the ISO sector magic bytes, and routes it directly to the correct extractor tab.
*   **Auto Folder Creation**: When extracting to a bare USB drive root (e.g. `D:\`), the app automatically creates a folder named after the game to keep your pendrive organized.

### 4. Games On Demand (ISO2GOD)
*   **Automated Conversion**: Seamlessly transform your ISO images into Microsoft's Games on Demand (GOD) format for immediate dashboard booting.
*   **CON Header Generation**: Natively writes STFS headers mapped to Title IDs.

### 5. USB Softmod Creator (RGH)
*   **FAT32 Formatter**: Safely format USB drives to FAT32 with 32KB clusters, optimal for Xbox 360 RGH compatibility.
*   **One-Click DashLaunch Installation**: Automatically clones and prepares essential tools onto your flash drive for instant console deployment.

## 🚀 Performance Comparison vs Legacy Tools
| Feature | Xbox Image Extractor (2026) | Xbox Image Browser v2.9 |
| :--- | :--- | :--- |
| **Technology** | `async/await` Multithreaded | Single-Threaded Sync |
| **Parsing Logic**| Handled natively (Absolute Offsets) | Hangs / Freezes |
| **Downloading**  | Built-In (Vimm's Lair API) | None |
| **Download Manager** | Tracked per-app w/ Extract & Load | None |
| **USB Export** | Auto-creates game folder | Manual only |

## 📦 Installation & Usage
1. Download the [latest release](https://github.com/dromex1/XboxImageExtractor/releases/download/v1.2.0/XboxImageExtractor-v1.2-win-x64.zip).
2. Extract the `.zip` and run `XboxImageExtractor.exe` — no installation needed.
3. Click `File -> Open ISO Image...` or navigate the intuitive Tab UI at the top.
4. Use `Game Downloader` to find and download games, then switch to `My Downloads` to unpack and extract them to your USB drive.

## 📄 License
This project is licensed under the MIT License.
