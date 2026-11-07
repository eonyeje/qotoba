using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Tests;

/// <summary>The few shape checks left in code. None of them refuses the owner.</summary>
public class RulesTests
{
    [Fact]
    public void NoteOfThreeLinesIsRefused()
    {
        Rules.NoteFits("one\ntwo");
        var ex = Assert.Throws<WorkbenchRuleException>(() => Rules.NoteFits("one\ntwo\nthree"));
        Assert.Equal("note.cap", ex.Code);
    }

    [Fact]
    public void StepsNeedAnEstimateEach()
    {
        Rules.StepsHaveEstimates([new(1, "a", 10, StepState.Todo, null, null, null)]);
        Assert.Equal("steps.empty", Assert.Throws<WorkbenchRuleException>(() => Rules.StepsHaveEstimates([])).Code);
        Assert.Equal("steps.estimate", Assert.Throws<WorkbenchRuleException>(() => Rules.StepsHaveEstimates([new(1, "a", 0, StepState.Todo, null, null, null)])).Code);
    }

    [Fact]
    public void OpeningBesideAnOpenSiblingNamesTheSiblingInsteadOfRefusing()
    {
        var a = new Item { Id = "WB-1", ProjectId = "wb", Title = "Login screen", IsOpen = true };
        var b = new Item { Id = "WB-2", ProjectId = "wb", Title = "Rendering" };
        Assert.Same(a, Rules.OpenClash(b, [a, b]));
        Assert.Null(Rules.OpenClash(b, [b]));
    }

    [Fact]
    public void ADoneNodeDoesNotReopen()
    {
        var d = new Item { Id = "WB-1", ProjectId = "wb", Title = "Done", State = ItemState.Done };
        Assert.Equal("open.done", Assert.Throws<WorkbenchRuleException>(() => Rules.OpenClash(d, [d])).Code);
    }

    [Fact]
    public void OnlyTheOwnerCreatesAProject()
    {
        Rules.OnlyOwnerCreatesProjects(Actor.Owner);
        Assert.Equal("structure.owner", Assert.Throws<WorkbenchRuleException>(() => Rules.OnlyOwnerCreatesProjects(Actor.Claude)).Code);
    }

    [Fact]
    public void ReportStatesAreTheEightRegisterWords()
    {
        Assert.Equal("Won't do", ReportStates.Normalise("wont do"));
        Assert.Equal("Verify first", ReportStates.Normalise("verify first"));
        Assert.Equal("In progress", ReportStates.Normalise("in-progress"));
        Assert.Null(ReportStates.Normalise("sort of done"));
        Assert.True(ReportStates.IsClosed("Exists"));
        Assert.False(ReportStates.IsClosed("Designed"));
    }
}
