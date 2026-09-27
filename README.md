# RunRemote

VB.NET class library (`Remoting.Common`) for a remote-execution setup. `Server` is a MarshalByRefObject that serves assemblies over .NET Remoting (with SharpZipLib zip payloads), `SponsorshipManager` keeps remoting leases alive, and `Win32` wraps service create/delete PInvoke. Comments say ExecRemote and ServiceOnRemoteMachine consume this library; those hosts are not in this OneDrive folder. Command-line switch parsing lives under CommandLineArguments.

**Source last updated:** 2007-12-25  
**Language:** VB.NET  
**Target:** .NET 2.0  
**Output:** class library

## Solution structure

| Project | Language | Type | Purpose |
|---------|----------|------|---------|
| `Remoting.Common` | VB.NET | class library (.NET 2.0) | Remoting server, lease sponsor, Win32 service helpers, CLI parser |

## How to open

Open `RunRemote.sln` in Visual Studio 2008 or later. The project references `ICSharpCode.SharpZipLib` (HintPath was `C:\Windows\System32\ICSharpCode.SharpZipLib.dll`).

## Requirements

- Visual Studio 2008, .NET Framework 2.0

## Attribution and provenance

Working copy from my Historical Dev folder.

From Dave Robinson's Historical Dev archive (OneDrive folder `RunRemote`). Assembly copyright 2007. SharpZipLib is by Mike Krueger (see `THIRD_PARTY_NOTICES.md`). SponsorshipManager notes MSDN Magazine / thinktecture remoting articles.

## License

MIT License. Copyright (c) 2026 VaderConsulting. SharpZipLib remains under its own terms.
