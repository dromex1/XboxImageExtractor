# Xbox Modding Suite (Image Extractor & Game Downloader) 🎮

Stworzone przez **Dromex** (Instagram: [@dromex__](https://www.instagram.com/dromex__/))

Nowoczesne, wysokowydajne narzędzie All-In-One w języku C# (.NET 8 / WinForms) zaprojektowane z myślą o modyfikowaniu konsol Xbox 360 oraz Classic Xbox. To otwartoźródłowe repozytorium całkowicie zastępuje starsze narzędzia (jak "Xbox Image Browser v2.9"), oferując piękny interfejs, asynchroniczne operacje I/O oraz zintegrowane pobieranie gier.

## ✨ Funkcje

### 1. Xbox 360 (XGD2/XGD3) & Classic Xbox (XISO)
*   **Asynchroniczny silnik I/O**: Interfejs nigdy się nie zacina. Zużywa duże, 4MB bufory i asynchroniczne strumienie, aby wyodrębniać pliki niesamowicie szybko w porównaniu do starszych narzędzi.
*   **Ochrona przed uszkodzeniem GDFX**: Przelicza bezwzględne offsety względem bazy partycji danych, aby zapobiec nieskończonym pętlom, które często występują na zmodyfikowanych płytach XGD3.
*   **Kopiowanie bezpośrednio z napędu DVD**: Włóż oryginalną płytę Xbox 360 bezpośrednio do napędu DVD w komputerze (pomija limity systemu Windows).
*   **Wbudowana Nagrywarka**: Zintegrowane Windowsowe API `IMAPI2` COM. Automatycznie nagrywa rozpakowane foldery na płyty DVD/CD w formacie UDF gotowym pod konsole RGH/JTAG!

### 2. Live Game Downloader
*   **Błyskawiczna Integracja z Vimm's Lair**: Wbudowana wyszukiwarka komunikuje się z bazą danych Vimm's Lair w czasie rzeczywistym, pobierając gry ze zbiorów Xbox i Xbox 360.
*   **Live Search**: Wyszukiwarka na żywo typu "zero-lag" natychmiast aktualizuje wyniki bez zacinania aplikacji.
*   **Pobieranie w Tle**: Automatycznie pobiera i deduplikuje listę dostępnych gier dla przeglądania offline.

### 3. Games On Demand (ISO2GOD)
*   **Automatyczna Konwersja**: Płynnie przerabiaj obrazy ISO do natywnego formatu Microsoftu Games on Demand (GOD), aby bootować je prosto z dashboardu.
*   **Generowanie Nagłówków CON**: Natywnie operuje na nagłówkach STFS i przypisuje je do Title ID.

### 4. USB Softmod Creator (RGH)
*   **FAT32 Formatter**: Bezpiecznie formatuje dyski USB na FAT32 (rozmiar klastra 32KB), optymalne dla RGH.
*   **Instalacja DashLaunch (1 kliknięcie)**: Automatycznie klonuje i przygotowuje na pendrive wszystkie najważniejsze pliki do instalacji RGH na Twojej konsoli.

## 🚀 Porównanie do starszych narzędzi
| Funkcja | Xbox Image Extractor (2026) | Xbox Image Browser v2.9 |
| :--- | :--- | :--- |
| **Technologia** | `async/await` Multithread | Jednowątkowa |
| **Logika Parsowania**| Natywnie analizowana struktura | Zawiesza się / Przycina |
| **Pobieranie gier**  | Wbudowane (Vimm's Lair API) | Brak |

## 📦 Instalacja
1. Pobierz plik z najnowszego `Release` lub sklonuj repozytorium.
2. Skompiluj za pomocą Visual Studio 2022 lub użyj `dotnet run`.
3. Kliknij `File -> Open ISO Image...` lub użyj zakładek w intuicyjnym widoku.
4. Kliknij Prawym Przyciskiem na katalog, aby go wypakować (`Extract...`) lub bezpośrednio nagrać na płytę!

## 📄 Licencja
Projekt udostępniony na licencji MIT.
