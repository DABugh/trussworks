Note: The original instructions that I wrote got overwritten... This is my best (quick) attempt to recreate it. I apologize if I missed a few steps. If there is a problem, please do not hesitate to contact me.
Dainen Bugh
Dainen.Bugh@gmail.com
(630)926-8785

# Instructions to Set Up Build Environment in Linux

## Download VSCode
1. Navigate to https://code.visualstudio.com/Download
2. Download .deb installation file
3. Open .deb file in Discover to install Visual Studio Code

## Download .NET Core
1. Open https://www.microsoft.com/net/learn/get-started/linux/ in a web browser
2. Follow the instructions for step 1 to register the Microsoft key and install the .NET SDK

## Install C# Extension
1. Open VSCode
2. ctrl+shift+x to open Extensions Marketplace
3. Search for "C#"
4. Click on C# extension published by Microsoft
5. Click "Install"
6. After installation has finished, "Install" button changes to "Reload." Click it.

## Clone Repository
1. ctrl+shift+p, then type "git clone"
2. Enter repository URL: http://github.com/DABugh/trussworks
3. Select location to save cloned repository
4. [Open Repository] on prompt

## Add CsvHelper package
1. ctrl+` to open integrated terminal
2. Execute command: "dotnet add package CsvHelper --version 7.1.1"

## Build and Execute
1. ctrl+` to open integrated terminal
2. Execute command: "dotnet clean"
3. Execute command: "dotnet run"
4. Enter input. ctrl+d to signal EOF.
5. To switch input or output to a file, uncomment and edit appropriate line in Program.cs.
