namespace Qotoba.Workbench.Model;

public enum ProjectState { Open, Coarse }

public enum ItemState { Coarse, Planned, Planning, Building, Review, Blocked, Paused, Done }

public enum StepState { Todo, Current, Done, Blocked }

public enum ReportKind { FTR, BUG, PER }

public enum Author { Me, Claude, Machine }

public enum ImplKind { Decision, Proof, Dependency, Precondition }

public enum ImplStatus { Open, Scheduled, Asked, Settled, Proved, Dropped }

public enum EdgeType { Needs, Related, Duplicate }

public enum CardType { Question, Packet, Plan, Stuck, Info, Proposals }

public enum CardState { Open, Answered, Approved, Deferred, Dismissed, Preview }

public enum QuestionState { Open, Answered, NotAsked }

public enum AssumptionState { Unreviewed, Fine, Flipped }

public enum FoundState { InTray, Task, Later, Dropped, Promoted }

public enum SessionState { Working, Waiting, Ended }

public enum RunResult { Pass, Fail, Warn }

public enum MachineKind { Machine, Cloud }

public enum TokenKind { Machine, Session, Ui, Connector }

public enum CommentState { Open, Handled, Stale, Removed }

public enum SeverityLevel { Blocking, Serious, Minor, Cosmetic }

public enum Actor { Owner, Claude, Machine, Host, System }
