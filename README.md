# Xbox Image Extractor 🎮

A modern, high-performance C# (.NET 8 / WinForms) tool for browsing, extracting, and burning Xbox 360 ISO images (GDFX/XGD2/XGD3 filesystems). This open-source repository serves as an advanced, asynchronous alternative to the legacy "Xbox Image Browser v2.9".

## ✨ Features

*   **Asynchronous I/O Engine**: 100% UI responsiveness. Uses large 4MB buffers and async streams to extract files blazing fast compared to older tools.
*   **Built-in Disc Burner**: Integrated Windows `IMAPI2` COM API. Automatically burns extracted directories to DVD/CD in UDF format ready for RGH/JTAG consoles!
*   **Raw Optical Drive Ripping**: Can read original Xbox 360 discs directly from your PC's DVD-ROM (via `\\.\D:` bypassing Windows filesystem limits).
*   **Protection Against GDFX Corruption**: Recalculates absolute offsets against data partition bases to prevent infinity loops commonly experienced on packed XGD3 discs.
*   **Modern UI with Real-Time Stats**: Beautiful progress-bars powered by `IProgress<T>`, showing real-time metrics (e.g. `12 / 128 files`, `2 GB / 6 GB`).

## 🚀 Performance Comparison vs Xbox Image Browser v2.9
| Feature | Xbox Image Extractor (2026) | Xbox Image Browser v2.9 |
| :--- | :--- | :--- |
| **Technology** | `async/await` Multithreaded | Single-Threaded Sync |
| **I/O Buffer Limit** | `4 MB` | Very small (KB) |
| **XGD3 Nested Partitions**| Handled natively (Absolute Offsets) | Hangs / Freezes |
| **Disc Burning** | Full UDF/IMAPI2 Support | None |
| **Direct Drive Ripping** | Yes, raw sector scans | Fails on Video partition |

## 📦 Installation & Usage
1. Download the latest `Release` or clone the repository.
2. Build via Visual Studio 2022 or run `dotnet run` in the project directory.
3. Click `File -> Open ISO Image...` or `Open from DVD/CD Drive`.
4. Right-click any directory to extract it to HDD or burn it directly to a CD/DVD.
*(Note: Administrator privileges are required to Rip from physical DVD drives).*

## 📄 License
This project is licensed under the MIT License.
