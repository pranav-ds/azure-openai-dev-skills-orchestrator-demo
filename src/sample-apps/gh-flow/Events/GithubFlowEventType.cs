using Microsoft.AI.Agents.Abstractions;

namespace Microsoft.AI.DevTeam.Events
{
    public enum GithubFlowEventType
    {
        ReviewRequested,
        IntentExtracted,
        ReviewCodeContextAdded,
        CodeReviewCompleted
    }
}