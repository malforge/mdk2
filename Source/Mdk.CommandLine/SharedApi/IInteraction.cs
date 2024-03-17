namespace Mdk.CommandLine.SharedApi;

public enum InteractionType
{
    Normal,
    Script,
    NugetPackageVersionAvailable
}

public interface IInteraction
{
    void Notify(InteractionType type, string? message, params object?[] args);
}