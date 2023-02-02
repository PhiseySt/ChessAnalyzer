using System.Text.RegularExpressions;

namespace ChessAnalyzer.ChessLib;

public class UciEngineOption
{

    private static readonly Regex RegexOption = new(@"option\sname\s((?:\S+\s)+)type\s(\S+)");
    public enum OptionType { CHECK, SPIN, COMBO, BUTTON, STRING }
    /// <summary>
    /// Name of option
    /// </summary>
    public string? Name { get; internal set; }
    /// <summary>
    /// Option type
    /// </summary>
    public OptionType Type { get; internal set; }

    /// <summary>
    /// Creates an engine option object from an engine's UCI option command
    /// </summary>
    /// <param name="ucicommand">UCI option command, as specified in UCI protocol</param>
    /// <exception cref="ArgumentException">thrown, when option command doesn't fulfill UCI protocol</exception>
    public UciEngineOption(string ucicommand)
    {
        ParseCommand(ucicommand);
    }

    protected virtual void ParseCommand(string ucicommand)
    {
        var m = RegexOption.Match(ucicommand);
        if (!m.Success) throw new ArgumentException("Invalid UciEngineOption Command");
        var groupCollection = m.Groups;
        Name = groupCollection[1].Value.Trim();
        Type = (OptionType) Enum.Parse(typeof(OptionType), groupCollection[2].Value.ToUpperInvariant());
    }

    internal static UciEngineOption CreateOption(string ucicommand)
    {
        if (ucicommand.Contains("type check")) return new UciEngineOptionCheck(ucicommand);
        if (ucicommand.Contains("type spin")) return new UciEngineOptionSpin(ucicommand);
        if (ucicommand.Contains("type string")) return new UciEngineOptionString(ucicommand);
        return new UciEngineOption(ucicommand);
    }
}

/// <summary>
/// Option, which can be represented as checkbox in a GUI
/// </summary>
public class UciEngineOptionCheck : UciEngineOption
{
    /// <summary>
    /// The option's default value
    /// </summary>
    public bool Default { internal set; get; }

    private static readonly Regex RegexOption = new(@"option\sname\s(\S+\s)+type\s(\S+)(?:\sdefault\s(.*))?");


    public UciEngineOptionCheck(string ucicommand) : base(ucicommand) { }

    protected override void ParseCommand(string ucicommand)
    {
        var m = RegexOption.Match(ucicommand);
        if (!m.Success) throw new ArgumentException("Invalid UciEngineOptionCheck Command");
        var groupCollection = m.Groups;
        Name = groupCollection[1].Value.Trim();
        Type = (OptionType)Enum.Parse(typeof(OptionType), groupCollection[2].Value.ToUpperInvariant());
        if (groupCollection.Count > 3 && groupCollection[3].Success)
            Default = bool.Parse(groupCollection[3].Value);
    }
  }

/// <summary>
/// Option for a string field (represented by a text box in a GUI)
/// </summary>
public class UciEngineOptionString : UciEngineOption
{
    /// <summary>
    /// The option's default value
    /// </summary>
    public string? Default { internal set; get; }

    private static readonly Regex RegexOption = new Regex(@"option\sname\s(\S+\s)+type\s(\S+)(?:\sdefault\s(.*))?");

    public UciEngineOptionString(string ucicommand) : base(ucicommand) { }

    protected override void ParseCommand(string ucicommand)
    {
        Match m = RegexOption.Match(ucicommand);
        if (!m.Success) throw new ArgumentException("Invalid UciEngineOptionString Command");
        var groupCollection = m.Groups;
        Name = groupCollection[1].Value.Trim();
        Type = (OptionType)Enum.Parse(typeof(OptionType), groupCollection[2].Value.ToUpperInvariant());
        if (groupCollection.Count > 3 && groupCollection[3].Success)
            Default = groupCollection[3].Value;
    }
 }

/// <summary>
/// An integer engine option
/// </summary>
public class UciEngineOptionSpin : UciEngineOption
{
    /// <summary>
    /// The option's default value
    /// </summary>
    public int Default { internal set; get; }
    /// <summary>
    /// The option's maximum value
    /// </summary>
    public int Max { internal set; get; }
    /// <summary>
    /// The option's minimum value
    /// </summary>
    public int Min { internal set; get; }

    public UciEngineOptionSpin(string ucicommand) : base(ucicommand) { }

    protected override void ParseCommand(string ucicommand)
    {
        base.ParseCommand(ucicommand);

        var token = ucicommand.Split();
        for (var i = 0; i <= token.Length - 1; ++i)
        {
            if (token[i] == "default")
            {
                ++i;
                Default = int.Parse(token[i]);
            }
            else if (token[i] == "max")
            {
                ++i;
                Max = int.Parse(token[i]);
            }
            else if (token[i] == "min")
            {
                ++i;
                Min = int.Parse(token[i]);
            }
        }
    }
}
