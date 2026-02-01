# Installing MDK Hub

## First Time Installing? Start Here!

MDK Hub is an open-source development tool for Space Engineers. Because it's not signed with a commercial code-signing certificate, Windows SmartScreen will display a warning when you first download it. **This is normal for open-source software** and doesn't mean the software is unsafe.

### Why Does Windows Show a Warning?

Windows SmartScreen shows warnings for unsigned executables that haven't built up "download reputation" yet. Code-signing certificates cost hundreds of dollars per year, which isn't always practical for free, open-source community tools.

### Is MDK Hub Safe?

Yes! MDK Hub is:
- **Open source** - All code is publicly visible on [GitHub](https://github.com/malware-dev/mdk2)
- **Community maintained** - Built by the Space Engineers modding community
- **Verifiable** - You can check the source code and build it yourself if you prefer

---

## Installation Options

### Option 1: Setup.exe (Recommended)

The Setup.exe installer provides automatic updates and integrates with Windows.

**Installation Steps:**

1. **Download** `Malforge.MdkHub-win-Setup.exe` from the [latest release](https://github.com/malware-dev/mdk2/releases)

2. **Run the installer** - Windows SmartScreen will show a warning:
   ```
   Windows protected your PC
   Microsoft Defender SmartScreen prevented an unrecognized app from starting.
   ```

3. **Click "More info"** in the SmartScreen dialog

4. **Click "Run anyway"** button that appears

5. **Follow the installation wizard** to complete setup

The Hub will automatically check for updates and can update itself in the background.

### Option 2: Portable ZIP

If you prefer not to run the installer, the portable version requires no installation.

**Setup Steps:**

1. **Download** `Malforge.MdkHub-win-Portable.zip` from the [latest release](https://github.com/malware-dev/mdk2/releases)

2. **Extract** the ZIP file to a folder of your choice (e.g., `C:\Tools\MDK Hub`)

3. **Run** `Mdk.Hub.exe` from the extracted folder

**Note:** Windows may still show a SmartScreen warning when you first run the .exe. Follow the same "More info" → "Run anyway" steps above.

---

## Verifying the Download (Optional)

If you want to verify you downloaded the authentic file from GitHub:

1. Download the release from the official [MDK2 GitHub repository](https://github.com/malware-dev/mdk2/releases)
2. Check the SHA256 hash of the downloaded file matches the one shown on the GitHub release page
3. **Windows**: `Get-FileHash -Path "Malforge.MdkHub-win-Setup.exe" -Algorithm SHA256`
4. **The hash should match** the one listed in the GitHub release assets

---

## System Requirements

- **Operating System:** Windows 10/11 (64-bit) or Linux
- **.NET SDK:** .NET 9.0 or later
- **Space Engineers:** Access to Space Engineers binaries (easiest via game installation, but any legal source works)
- **IDE/Editor:** Any C# development environment (Visual Studio, Rider, VS Code, or command line)

---

## After Installation

### First Launch

When you first launch MDK Hub:

1. **On Windows:** If Space Engineers is installed normally, paths are auto-detected
2. **On Linux (or manual setup):** You'll be prompted to configure paths to your Space Engineers binaries, script output, and mod output folders

### Getting Started

1. **Create a new project:**
   - **Via Hub:** Use the Hub's project creation UI (convenient for all IDEs, especially VS Code)
   - **Via IDE:** Visual Studio and Rider can use MDK templates directly via "New Project"
   - **Via CLI:** Use `dotnet new` commands
2. **Choose between Script or Mod** project types
3. **Configure your project** options and output paths
4. **Open in your preferred IDE** - Visual Studio, Rider, VS Code, or any C# editor
5. **Start coding!** MDK handles deployment to Space Engineers

For detailed usage instructions, see the [documentation site](https://malforge.github.io/spaceengineers/mdk2/).

---

## Troubleshooting

### "Windows cannot access the specified device, path, or file"

This means Windows Defender or your antivirus blocked the file. 

**Solution:** 
1. Right-click the file → Properties
2. Check "Unblock" at the bottom of the General tab
3. Click OK and try running again

### SmartScreen Warning Keeps Appearing

If SmartScreen continues blocking after you click "Run anyway":
1. Right-click the .exe → Properties
2. Look for an "Unblock" checkbox at the bottom
3. Check it and click OK
4. Run the installer again

### Need Help?

- **Issues/Bugs:** [GitHub Issues](https://github.com/malware-dev/mdk2/issues)
- **Documentation:** [Official Docs](https://malforge.github.io/spaceengineers/mdk2/)
- **Community:** Join the Space Engineers modding community for support

---

## Building from Source (Advanced)

If you prefer to build MDK Hub yourself:

1. Clone the repository: `git clone https://github.com/malware-dev/mdk2.git`
2. Open `Source/MDK-Complete.sln` in Visual Studio
3. Build the solution
4. Run `Mdk.Hub` project

This allows you to verify the source code and build with your own environment.
