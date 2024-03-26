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
                var contents = string.IsNullOrEmpty(item.Message)? "Sorry, I got tired, can you try again please? ": item.Message;
                await PostComment(item.Data["org"], item.Data["repo"], long.Parse(item.Data["issueNumber"]), contents);
                break;
            default:
                break;
        }
    }

    public async Task PostComment(string org, string repo, long issueNumber, string comment)
    {
        await _ghService.PostComment(org, repo, issueNumber, comment);
    }
}