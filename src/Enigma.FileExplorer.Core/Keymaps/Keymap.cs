using System;
using System.Collections.Generic;
using System.Linq;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// A set of key bindings arranged as a trie, so a sequence is resolved one stroke at a time.
/// </summary>
/// <remarks>
/// The keymap is prefix-free: no binding may be a prefix of another. That is what lets a sequence resolve
/// the moment it is complete, with no timeout — vim needs <c>timeoutlen</c> only because its maps may be
/// ambiguous.
/// </remarks>
public sealed class Keymap
{
    /// <summary>
    /// Builds a keymap.
    /// </summary>
    /// <param name="bindings">The bindings.</param>
    /// <exception cref="ArgumentException">A sequence is bound twice, or is a prefix of another.</exception>
    public Keymap(IEnumerable<KeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        Bindings = [.. bindings];

        foreach (KeyBinding binding in Bindings)
        {
            Add(binding);
        }
    }

    /// <summary>
    /// Every binding, in declaration order.
    /// </summary>
    public IReadOnlyList<KeyBinding> Bindings { get; }

    /// <summary>
    /// The root of the trie.
    /// </summary>
    internal Node Root { get; } = new();

    /// <summary>
    /// The bindings of a command, in declaration order: the first one is the command's primary key.
    /// </summary>
    /// <param name="commandId">The command.</param>
    /// <param name="argument">The fixed argument to match, when the command takes one.</param>
    /// <returns>The bindings.</returns>
    public IReadOnlyList<KeyBinding> BindingsFor(string commandId, string? argument = null)
        => [.. Bindings.Where(binding => (binding.CommandId == commandId) && ((argument is null) || (binding.Argument == argument)))];

    private void Add(KeyBinding binding)
    {
        if (binding.Keys.Count == 0)
        {
            throw new ArgumentException($"The binding for '{binding.CommandId}' has no keys.", nameof(binding));
        }

        Node node = Root;

        foreach (KeyStroke stroke in binding.Keys)
        {
            if (node.Binding is { } shorter)
            {
                throw new ArgumentException($"'{shorter.Notation}' ({shorter.CommandId}) is a prefix of '{binding.Notation}' ({binding.CommandId}).", nameof(binding));
            }

            if (!node.Children.TryGetValue(stroke, out Node? child))
            {
                child = new Node();
                node.Children[stroke] = child;
            }

            node = child;
        }

        if (node.Binding is { } existing)
        {
            throw new ArgumentException($"'{binding.Notation}' is bound to both '{existing.CommandId}' and '{binding.CommandId}'.", nameof(binding));
        }

        if (node.Children.Count > 0)
        {
            throw new ArgumentException($"'{binding.Notation}' ({binding.CommandId}) is a prefix of other bindings.", nameof(binding));
        }

        node.Binding = binding;
    }

    /// <summary>
    /// A node of the trie: either a complete binding (a leaf) or a prefix with children.
    /// </summary>
    internal sealed class Node
    {
        /// <summary>
        /// The strokes that continue the sequence.
        /// </summary>
        public Dictionary<KeyStroke, Node> Children { get; } = [];

        /// <summary>
        /// The binding a sequence ending here runs.
        /// </summary>
        public KeyBinding? Binding { get; set; }
    }
}
