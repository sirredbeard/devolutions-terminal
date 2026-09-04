namespace Devolutions.Terminal.Shell;

/// <summary>
/// Result of resolving which UI toolkit owns this process.
/// </summary>
/// <param name="Kind">Frontend to start.</param>
/// <param name="Forced">True when <c>--shell=</c> or <c>DT_SHELL</c> forced the choice.</param>
/// <param name="Desktop">Raw desktop session hint used for auto selection, if any.</param>
/// <param name="Reason">Short human reason for logs and diagnostics.</param>
public sealed record ShellSelection(
    ShellKind Kind,
    bool Forced,
    string? Desktop,
    string Reason);
