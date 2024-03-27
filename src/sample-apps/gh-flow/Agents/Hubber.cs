using System.Text.Json;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam.Events;
using Orleans.Streams;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Hubber : Agent
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly IManageGithub _ghService;

    public Hubber(IManageGithub ghService)
    {
       _ghService = ghService;
    }

    public override async Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.CodeReviewCompleted):
            case nameof(GithubFlowEventType.PullRequestReviewCompleted):
                var contents = string.IsNullOrEmpty(item.Message)? "Sorry, I got tired, can you try again please? ": item.Message;
                await PostComment(item.Data["org"], item.Data["repo"], long.Parse(item.Data["issueNumber"]), contents);
                break;
            case nameof(GithubFlowEventType.PullRequestReviewRequested):
                var prNumber = long.Parse(item.Data["prNumber"]);
                var deltas = await FetchDelta(item.Data["org"], item.Data["repo"], prNumber);
                var jsonDeltas = JsonSerializer.Serialize(deltas);
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                {
                    Type = nameof(GithubFlowEventType.PullRequestDeltaCreated),
                    Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "deltas",jsonDeltas }
                        },
                    Message = item.Message
                });
                break;
            default:
                break;
        }
    }

    public async Task PostComment(string org, string repo, long issueNumber, string comment)
    {
       await _ghService.PostComment(org, repo, issueNumber, comment);
    }

    public async Task<IEnumerable<FilePatchResponse>> FetchDelta(string org, string repo, long prNumber)
    {
       return await _ghService.GetPatches(org, repo, (int)prNumber);
    }
}