namespace StackUnderflow.Models;

/// <summary>
/// Data shown on the home page: the top questions list.
/// </summary>
public class HomeViewModel
{
    public IReadOnlyList<SUThread> Threads { get; set; } = [];
}