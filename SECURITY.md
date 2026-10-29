# Security Policy

Enigma.FileExplorer is a desktop file explorer that lists, opens, copies, moves, renames, trashes and deletes files on
your behalf, and searches inside them. A defect in it could let a crafted file or folder name crash or hang the
explorer, show you something other than what is on disk, or make an operation touch files you did not choose — the
wrong target of a move, a delete, or an overwrite. Vulnerability reports are taken seriously and handled with
priority.

## Supported versions

Security fixes are provided for the latest released version. Enigma.FileExplorer follows
[Semantic Versioning](https://semver.org/), and users are encouraged to stay current with the newest release.

| Version | Supported          |
|---------|--------------------|
| 0.2.x   | :white_check_mark: |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**
Public disclosure before a fix is available puts every user at risk.

Instead, use **GitHub's private vulnerability reporting**:

1. Go to the repository's **Security** tab.
2. Select **Report a vulnerability** to open a private advisory.
3. Include as much detail as you can — the affected version, the component involved, a description of the issue,
   and, where possible, a minimal reproduction and its impact.

This keeps the report private between you and the maintainers while it is triaged and fixed.

## What to expect

- Your report will be acknowledged and triaged as promptly as possible.
- The issue will be investigated and, once confirmed, a fix prepared and released.
- Coordinated disclosure is preferred: please allow a reasonable period for a fix to ship before any public
  discussion of the vulnerability.
- Your contribution will be credited in the resulting advisory unless you ask to remain anonymous.

## Scope

The following are in scope:

- the file operations: create, rename, copy, move, paste from other applications, move to the trash and delete
  permanently, including how conflicts and failures are resolved and which entries an operation targets;
- the name and content search, and the patterns typed into it;
- the way the explorer opens a file with its application, and what it hands that application;
- drag and drop to and from other applications, and the clipboard exchange;
- the way it reads and writes its settings file;
- the error dialog it shows when a failure is not handled elsewhere: what that dialog shows and what *Copy details*
  puts on the clipboard;
- the Linux install, uninstall and publish scripts in `packaging/linux`, and the Windows publish scripts in
  `packaging/windows`: what they write into your home directory, what they remove, and the desktop entry they
  generate.

The explorer makes no network connection and stores no credentials. Issues rooted in Avalonia, SkiaSharp, the
operating system's trash or shell, or the .NET runtime should also be reported to those projects.
