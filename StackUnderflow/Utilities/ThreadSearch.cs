using StackUnderflow.Models;

namespace StackUnderflow.Utilities;

public sealed class ThreadSearch
{
    public string Text { get; }
    public List<string> Tags { get; }
    public bool RequireAllTags { get; }
    public string Query => string.Join(" ", new[] { Text }.Where(text => text.Length > 0)
        .Concat(Tags.Select(tag => $"#{tag}")));

    public ThreadSearch(string? query, IEnumerable<string>? tags = null, string? removeTag = null, bool requireAllTags = false)
    {
        RequireAllTags = requireAllTags;
        var words = (query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Text = string.Join(" ", words.Where(word => !word.StartsWith('#')));
        Tags = words.Where(word => word.StartsWith('#')).Select(word => word[1..])
            .Concat(tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag) && tag != removeTag)
            .Distinct()
            .ToList();
    }

    public IQueryable<SUThread> Apply(IQueryable<SUThread> threads)
    {
        if (Text.Length > 0)
            threads = threads.Where(thread => thread.Title.Contains(Text) || thread.Content.Contains(Text));
        if (Tags.Count > 0)
        {
            if (RequireAllTags)
            {
                foreach (var name in Tags)
                    threads = threads.Where(thread => thread.ThreadTags.Any(tag => tag.Tag.Name == name));
            }
            else
            {
                threads = threads.Where(thread => thread.ThreadTags.Any(tag => Tags.Contains(tag.Tag.Name)));
            }
        }
        return threads;
    }
}
