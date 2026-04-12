# How to Run (Event Management Portal)

## Prerequisites
- Windows laptop
- .NET 8 SDK installed
- Internet connection (for Supabase DB + CDN assets)

## 1. Open Project Folder
Open a terminal in the project root:

D:\VS Codes\DBMS Project

## 2. Run the App
Use:

```powershell
dotnet run
```

If successful, you should see lines like:
- `Database connection warmed up.`
- `Now listening on: http://localhost:5190`
- `Application started. Press Ctrl+C to shut down.`

## 3. Open the Frontend
Keep the terminal running, then open:

http://localhost:5190

You can also Ctrl+Click the URL from the VS Code terminal.

## 4. Stop the App
In the same terminal, press:

Ctrl+C

## Quick Checks
If the page does not open:
- Make sure `dotnet run` is still running (terminal should not return to prompt).
- Re-run `dotnet run` and open `http://localhost:5190` again.
- Ensure no firewall or port conflict blocks localhost:5190.

## Verify .NET Installation
```powershell
dotnet --version
```
You should have a .NET 8.x SDK available for this project.
