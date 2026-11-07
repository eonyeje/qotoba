using Qotoba.Workbench.Model;

namespace Qotoba.Workbench.Tests;

/// <summary>The invariants named in the design, one test each, so a change to a cap is a visible decision.</summary>
public class RulesTests
{
    private static QuestionOption Opt(string t, bool rec = false) => new(t, rec, "because", "small", null);

    [Fact]
    public void ThirdOpenQuestionIsRefused()
    {
        Rules.QuestionMayBeAsked(0);
        Rules.QuestionMayBeAsked(1);
        var ex = Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionMayBeAsked(2));
        Assert.Equal("questions.cap", ex.Code);
    }

    [Fact]
    public void QuestionNeedsOneRecommendationAndReasons()
    {
        Rules.QuestionIsClosed([Opt("a", true), Opt("b")]);
        Assert.Equal("questions.options", Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionIsClosed([Opt("a", true)])).Code);
        Assert.Equal("questions.recommendation", Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionIsClosed([Opt("a"), Opt("b")])).Code);
        Assert.Equal("questions.why", Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionIsClosed([Opt("a", true), new QuestionOption("b", false, "", null, null)])).Code);
    }

    [Fact]
    public void QuestionIsBornFromAStep()
    {
        Rules.QuestionIsAnchored("WB-1", 2);
        Assert.Equal("questions.anchor", Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionIsAnchored(null, 2)).Code);
        Assert.Equal("questions.anchor", Assert.Throws<WorkbenchRuleException>(() => Rules.QuestionIsAnchored("WB-1", null)).Code);
    }

    [Fact]
    public void PlanOfEightStepsIsRefused()
    {
        var seven = Enumerable.Range(1, 7).Select(n => new Step(n, "s" + n, 10, StepState.Todo, null, null, null)).ToList();
        Rules.PlanFits(seven);
        var eight = seven.Append(new Step(8, "s8", 10, StepState.Todo, null, null, null)).ToList();
        Assert.Equal("plan.cap", Assert.Throws<WorkbenchRuleException>(() => Rules.PlanFits(eight)).Code);
        Assert.Equal("plan.estimate", Assert.Throws<WorkbenchRuleException>(() => Rules.PlanFits([new Step(1, "s", 0, StepState.Todo, null, null, null)])).Code);
    }

    [Fact]
    public void SixthFoundItemIsRefused()
    {
        Rules.FoundMayBeAdded(4);
        Assert.Equal("found.cap", Assert.Throws<WorkbenchRuleException>(() => Rules.FoundMayBeAdded(5)).Code);
    }

    [Fact]
    public void NoteOfThreeLinesIsRefused()
    {
        Rules.NoteFits("one\ntwo");
        Assert.Equal("note.cap", Assert.Throws<WorkbenchRuleException>(() => Rules.NoteFits("one\ntwo\nthree")).Code);
    }

    [Fact]
    public void ClaudeCannotCreateItem()
    {
        Rules.OnlyOwnerCreatesStructure(Actor.Owner, "task");
        Assert.Equal("structure.owner", Assert.Throws<WorkbenchRuleException>(() => Rules.OnlyOwnerCreatesStructure(Actor.Claude, "task")).Code);
    }

    [Fact]
    public void OneHomeMachine()
    {
        Rules.OneHomeMachine(null, "laptop");
        Rules.OneHomeMachine("laptop", "laptop");
        Assert.Equal("machine.home", Assert.Throws<WorkbenchRuleException>(() => Rules.OneHomeMachine("desk-pc", "laptop")).Code);
    }

    [Fact]
    public void OneOpenNodePerLevelPerLane()
    {
        var a = new Item { Id = "WB-1", ProjectId = "wb", Title = "a", IsOpen = true, LaneKey = "A" };
        var b = new Item { Id = "WB-2", ProjectId = "wb", Title = "b", LaneKey = "A" };
        var c = new Item { Id = "WB-3", ProjectId = "wb", Title = "c", LaneKey = "B" };
        Assert.Equal("open.one", Assert.Throws<WorkbenchRuleException>(() => Rules.MayOpen(b, [a, b, c])).Code);
        Rules.MayOpen(c, [a, b, c]);
        Assert.Equal("open.done", Assert.Throws<WorkbenchRuleException>(() => Rules.MayOpen(new Item { Id = "x", ProjectId = "wb", Title = "x", State = ItemState.Done }, [])).Code);
    }

    [Fact]
    public void ReportNeverWaitsOnOwner()
    {
        Assert.Equal("report.waits", Assert.Throws<WorkbenchRuleException>(() => Rules.ReportNeverWaits(new Card { Id = "q", Headline = "h", Type = CardType.Question, ReportId = "BUG-1" })).Code);
        Rules.ReportNeverWaits(new Card { Id = "q", Headline = "h", Type = CardType.Question, ReportId = "BUG-1", ItemId = "WB-1" });
    }
}
