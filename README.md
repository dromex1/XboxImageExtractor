# Xbox Modding Suite (Image Extractor & Game Downloader) 🎮

Created by **Dromex** (Instagram: [@dromex__](https://www.instagram.com/dromex__/))

A modern, high-performance, All-In-One C# (.NET 8 / WinForms) utility designed for the ultimate Xbox 360 and Classic Xbox modding experience. This open-source repository completely supersedes legacy tools like "Xbox Image Browser v2.9", offering a beautiful UI, asynchronous I/O, and integrated web scraping features.

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

### 3. Games On Demand (ISO2GOD)
*   **Automated Conversion**: Seamlessly transform your ISO images into Microsoft's Games on Demand (GOD) format for immediate dashboard booting.
*   **CON Header Generation**: Natively writes STFS headers mapped to Title IDs.

### 4. USB Softmod Creator (RGH)
*   **FAT32 Formatter**: Safely format USB drives to FAT32 with 32KB clusters, optimal for Xbox 360 RGH compatibility.
*   **One-Click DashLaunch Installation**: Automatically clones and prepares essential tools onto your flash drive for instant console deployment.

## 🚀 Performance Comparison vs Legacy Tools
| Feature | Xbox Image Extractor (2026) | Xbox Image Browser v2.9 |
| :--- | :--- | :--- |
| **Technology** | `async/await` Multithreaded | Single-Threaded Sync |
| **Parsing Logic**| Handled natively (Absolute Offsets) | Hangs / Freezes |
| **Downloading**  | Built-In (Vimm's Lair API) | None |

## 📦 Installation & Usage
1. Download the latest `Release` or clone the repository.
2. Build via Visual Studio 2022 or run `dotnet run` in the project directory or just download release build.
3. Click `File -> Open ISO Image...` or navigate the intuitive Tab UI at the top.
4. Right-click any directory to extract it to HDD (`Extract...`) or burn it directly.

## 📄 License
This project is licensed under the MIT License.
