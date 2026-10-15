namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// What a command is, for people: help lists it under its category, menus show its title.
/// </summary>
/// <param name="Id">The command's id (see <see cref="CommandIds"/>).</param>
/// <param name="Title">A short imperative title, e.g. "Go to parent folder".</param>
/// <param name="Category">The help section it belongs to.</param>
public sealed record CommandDescriptor(string Id, string Title, string Category);
