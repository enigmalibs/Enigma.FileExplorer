using System.Collections.Generic;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// What a drop will do, worked out while hovering and again at the drop.
/// </summary>
/// <param name="Sources">The paths dropped.</param>
/// <param name="Target">The directory they land in.</param>
/// <param name="Action">Copy, move, or ask.</param>
internal sealed record DropPlan(IReadOnlyList<string> Sources, string Target, DropAction Action);
