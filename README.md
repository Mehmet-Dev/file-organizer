# FileOrganizer

A small cross-platform CLI tool for organizing files based on their file extensions.

This is a **work in progress**. I'm mainly building it as a personal project to experiment with C#, filesystem handling, CLI applications, and eventually a proper UI.

The program currently sorts files into categories such as `Documents`, `Images`, `Audio`, `Video`, `Archives`, `Code`, and `Installers`.

## Current features

* Organize files based on their extension
* Cross-platform file/path handling
* Dry-run mode
* Confirmation before modifying files
* Verbose output
* Progress animation
* Handling for unknown file extensions
* Execution time tracking
* File collision handling
* JSON-based history
* Undo previous organization operations
* CLI help command

## Usage

Basic usage:

```bash
FileOrganizer <path>
```

For example:

```bash
FileOrganizer ~/Downloads
```

Or, if your Downloads folder has evolved into a digital archaeological site:

```bash
FileOrganizer ~/Downloads
```

The program will create folders such as:

```text
Downloads/
├── Archives/
├── Audio/
├── Code/
├── Documents/
├── Images/
├── Installers/
└── Video/
```

You can also preview what would happen without actually moving anything:

```bash
FileOrganizer ~/Desktop/i_have_no_idea_what_any_of_these_files_are --dry-run
```

## Flags

```text
--help
    Display the help message.

--dry-run
    Preview the organization without moving files.

--loud
    Show information about each file while organizing.

--verbose
    Show additional information about each file.

--organize-unknown
    Move unknown file types into an "Unknown" folder.

--skip-safe
    Skip the confirmation prompt.

--skip-bench
    Disable execution time measurement.

--undo-move
    Select a previous organization and undo it.
```

For the full list of options:

```bash
FileOrganizer --help
```

## Undo

Every organization that actually moves files gets a JSON history file.

This allows the operation to be reverted later:

```bash
FileOrganizer --undo-move
```

The program will show the available histories and let you choose which one to undo.

## Supported file types

Currently, the organizer recognizes common extensions for:

* Documents
* Images
* Audio
* Video
* Archives
* Code
* Installers

The list will probably grow over time.

## What's next?

The project is still very much in development. Some things I plan to work on:

* A simple UI on top of the existing functionality
* More configuration options
* More ways to customize file categories
* More CLI functionality
* More extensive error handling
* Releases once the project is in a more finished state

For now, expect things to change.

## Status

**Work in progress — not a finished release.**

I'm mostly using this project to build something useful while experimenting with C# and filesystem programming.
