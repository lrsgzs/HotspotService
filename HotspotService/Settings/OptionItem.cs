namespace HotspotService.Settings;

public sealed record OptionItem<T>(string Label, T Value)
{
    public override string ToString()
    {
        return Label;
    }
}
