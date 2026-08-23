namespace StackUnderflow.Models;

public class ContentSafety(int violenceSeverity, int hateSeverity, int selfHarmSeverity, int sexualContentSeverity)
{
    public int HateSeverity { get; } = hateSeverity;
    public int SelfHarmSeverity { get; } = selfHarmSeverity;
    public int SexualContentSeverity { get; } = sexualContentSeverity;
    public int ViolenceSeverity { get; } = violenceSeverity;
}