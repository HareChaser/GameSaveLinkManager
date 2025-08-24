# Game Save Link Manager

A Windows desktop utility (WinForms) that keeps your PC and handhelds (e.g., Steam Deck) in sync by **linking local game save folders to a cloud folder (OneDrive or any cloud drive)** using Windows **directory junctions**. Ideal for visual novels and non-Steam titles that don’t support Steam Cloud.

![app_ss](https://github.com/user-attachments/assets/95d828c2-18e7-4974-9910-0a41e9eafc67)

---

## Getting started

### Requirements
- Windows 10/11  
- OneDrive (or your cloud drive of choice) installed & signed in  
- .NET (WinForms). Build from source with Visual Studio 2022  

### Get the package
- **From Releases**: Download the latest zip, extract, and run `GameSaveLinkManager.exe`.  
- **From Source**: Open the solution in Visual Studio → Build → Start.  

### First run
1. Prepare your `mappings.txt` (an empty txt is fine ).
2. Launch the app, click **Browse...** to select the file.
3. Review rows; **Add**/**Remove** as you like.
4. Click **Link Folder** button to create link for all games (if junction folder already exists, it will be skipped.).

---

## Mapping file format: `mappings.txt`

- Comments: any line starting with `#` is ignored  
- Empty lines are ignored  
- **Pipe-separated** fields: `Alias|TargetPath|LocalSavePath`

### Fields
- **Alias**: Your friendly game name (used in UI, defaults, and logs).
- **TargetPath**: Relative to `%OneDrive%` (no leading backslash) or absolute path.
- **LocalSavePath**: Absolute path to the local save folder

### Example
```
# games.txt managed by Game Save Link Manager
Senmomo|C:\Users\<UserName>\OneDrive\sdh-game-sync\home\deck\Desktop\Non-Steam\senmomo\UserData|E:\Games\千の刃濤、桃花染の皇姫\UserData
```

---

### Other Buttons

- **Load**: Load mapping file.
- **Save**: Save changes to mapping file.
- **OneDrive**: Open OneDrive path of selected row.
- **Local**: Open local folder path of selected row.
- **Add Row**: Add a new row.
- **Remove**: Remove selected row.

---

## License
- Apache License 2.0
