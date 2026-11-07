namespace Qotoba.Workbench.Model;

public enum ProjectState { Open, Coarse }

public enum ItemState { Coarse, Planned, Planning, Building, Review, Blocked, Paused, Done }

public enum StepState { Todo, Current, Done, Blocked }

public enum ReportKind { FTR, BUG, PER }

public enum Author { Me, Claude, Machine }

/// <summary>Typed links between items and FPBs, in the words the Quanta registers used.</summary>
public enum EdgeType { Needs, Related, Duplicate, SplitFrom, ReplacedBy, CarriedBy, FoundIn }

/// <summary>What reaches the inbox: a custom page with its own state, a stuck session, a deviation that must be reported. Info is the quiet "for you to know" list.</summary>
public enum CardType { Page, Stuck, Deviation, Info }

public enum CardState { Open, Answered, Approved, Deferred, Dismissed, Preview }

public enum AssumptionState { Unreviewed, Fine, Flipped }

public enum SessionState { Working, Waiting, Ended }

public enum RunResult { Pass, Fail, Warn }

public enum MachineKind { Machine, Cloud }

public enum TokenKind { Machine, Session, Ui, Connector }

public enum CommentState { Open, Handled, Stale, Removed }

public enum SeverityLevel { Blocking, Serious, Minor, Cosmetic }

public enum Actor { Owner, Claude, Machine, Host, System }
