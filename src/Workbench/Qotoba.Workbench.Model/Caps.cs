namespace Qotoba.Workbench.Model;

/// <summary>The slots Claude may fill, and how far. Every number here is shown in the UI as a budget.</summary>
public static class Caps
{
    public const int OpenQuestionsPerItem = 2;
    public const int FoundPerSession = 5;
    public const int PlanSteps = 7;
    public const int NoteLines = 2;
    public const int TextLines = 3;
    public const int Fixes = 3;
    public const int PulledCodeLines = 20;
    public const int RunTailLines = 40;
    public const int RunOutputBytes = 64 * 1024;
    public const int QuestionOptionsMin = 2;
    public const int QuestionOptionsMax = 4;
    public const int ProposalsPerSession = 8;
}

/// <summary>A rule of the Workbench was broken. The code names the rule so the UI and tests can name it too.</summary>
public sealed class WorkbenchRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
