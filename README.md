Following are instructions for setting up the environment to load and run this project on Linux.

# Download VSCode
1. Navigate to https://code.visualstudio.com/Download
2. Download .deb installation file
3. Open .deb file in Discover to install Visual Studio Code

# Install .NET Core framework
1. Open https://www.microsoft.com/net/learn/get-started/linux/ in a web browser
2. Follow the instructions for step 1 to register the Microsoft key and install the .NET SDK

# Install C# extension
1. Open VSCode
2. ctrl+shift+x to open Extensions Marketplace
3. Search for "C#"
4. Click on C# extension published by Microsoft
5. Click "Install"
6. After installation has finished, "Install" button changes to "Reload." Click it.

# Load project from GitHub
1. ctrl+shift+p, then type "git clone"
2. Enter repository URL: http://github.com/DABugh/trussworks
3. Select location to save cloned repository
4. Click [Open Repository] on prompt

# Execute project
1. Open integrated Terminal (ctrl+`, or select from View menu)
2. Type "dotnet run"
3. Enter input
4. Empty line to mark end of input