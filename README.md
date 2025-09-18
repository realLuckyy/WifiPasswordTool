# WiFi Password Manager - Professional Edition

A professional Windows application for viewing, managing, and removing saved WiFi passwords and profiles.

## Features

### 🔍 **View WiFi Passwords**
- Display all saved WiFi profiles with their passwords
- Toggle password visibility with secure masking
- Show security type and connection information
- Professional grid-based interface

### 🛡️ **Secure Management**
- Requires administrator privileges for security
- Automatic UAC prompt for elevated permissions
- Safe password handling and display

### 📤 **Export Functionality**
- Export WiFi profiles to CSV or text files
- Timestamped export files
- Organized data format for easy reference

### 🗑️ **Profile Management**
- Delete individual WiFi profiles
- Bulk delete all saved profiles (with confirmation)
- Undo-safe operations with multiple confirmations

### ⌨️ **Keyboard Shortcuts**
- `F5` - Refresh profiles
- `Ctrl+P` - Toggle password visibility
- `Ctrl+E` - Export profiles
- `Delete` - Delete selected profiles
- `Ctrl+Shift+Delete` - Delete all profiles

### 🎨 **Professional Interface**
- Modern, clean design with Material Design colors
- Responsive layout that adapts to window size
- Status bar with operation feedback
- Progress indicators for long operations
- Comprehensive menu system

## System Requirements

- Windows 7/8/8.1/10/11
- .NET Framework 4.7.2 or higher
- Administrator privileges (automatically requested)

## Installation

1. Build the application in Visual Studio or with MSBuild
2. Run the executable as Administrator (UAC prompt will appear)
3. The application will automatically check for required privileges

## Usage

### First Time Setup
1. Launch the application (it will request administrator privileges)
2. Click "Refresh" to load all saved WiFi profiles
3. Use "Show Password" to reveal masked passwords

### Viewing WiFi Information
- All saved WiFi networks are displayed in a table format
- Passwords are masked by default for security
- Click "Show Password" or use `Ctrl+P` to toggle visibility
- Security type and connection information is shown for each network

### Exporting Data
1. Click "Export" or use `Ctrl+E`
2. Choose between CSV or text format
3. Select save location
4. File will be created with timestamp for organization

### Deleting Profiles
**Single Profile:**
1. Select the WiFi profile(s) to delete
2. Click "Delete Selected" or press `Delete`
3. Confirm the deletion

**All Profiles:**
1. Click "Delete All" or use `Ctrl+Shift+Delete`
2. Read the warning carefully
3. Confirm twice (this action cannot be undone)

## Security Considerations

- **Administrator Rights**: Required to access Windows WiFi profile data
- **Password Security**: Passwords are masked by default and only shown when explicitly requested
- **Safe Deletion**: Multiple confirmation prompts prevent accidental data loss
- **No Network Transmission**: All operations are performed locally

## Technical Details

### Architecture
- Built with Windows Forms (.NET Framework 4.7.2)
- Uses Windows `netsh` command-line utility for WiFi operations
- Asynchronous operations to prevent UI freezing
- Professional error handling and user feedback

### Commands Used
- `netsh wlan show profiles` - List WiFi profiles
- `netsh wlan show profile name="[NAME]" key=clear` - Get password
- `netsh wlan delete profile name="[NAME]"` - Delete profile

## Troubleshooting

### "Administrator Rights Required" Message
- Right-click the application and select "Run as administrator"
- Or use the built-in UAC prompt when it appears

### No WiFi Profiles Shown
- Ensure you have saved WiFi networks on your system
- Verify the WiFi adapter is enabled
- Try clicking "Refresh" to reload

### Export Fails
- Check that you have write permissions to the selected folder
- Ensure the destination drive has sufficient space
- Try selecting a different file format

### Profile Deletion Fails
- Verify administrator privileges are active
- Some system-managed profiles may not be deletable
- Check Windows Event Viewer for detailed error information

## Support

This application is designed for educational and administrative purposes. Use responsibly and in accordance with your organization's IT policies.

## License

This software is provided as-is for educational and administrative purposes.
