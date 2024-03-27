using System.Text.Json;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam.Events;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Octokit.Webhooks.Models.PullRequestReviewEvent;
using Orleans.Runtime;
using Orleans.Streams;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Reviewer : AiAgent<DeveloperState>, IReviewApps
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly Kernel _kernel;
    private readonly ILogger<Reviewer> _logger;

    public Reviewer([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperState>> state, Kernel kernel, ILogger<Reviewer> logger)
    : base(state)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async override Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.ReviewRequested):
                {
                    var intent = await ExtractIntent(item.Message);
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.IntentExtracted),
                        Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "parentNumber", item.Data["parentNumber"]  },
                            { "intent", intent}
                        },
                        Message = intent
                    });
                }
                break;
            case nameof(GithubFlowEventType.ReviewCodeContextAdded):
                {
                    var review = await ReviewCode(item.Data["context"]);
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.CodeReviewCompleted),
                        Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "review", review }
                        },
                        Message = review
                    });
                }

                break;
            case nameof(GithubFlowEventType.PullRequestDeltaCreated):
                {
                    var deltas = JsonSerializer.Deserialize<IEnumerable<FilePatchResponse>>(item.Data["deltas"]);
                    var review = await ReviewPullRequest(deltas, item.Message);
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.PullRequestReviewCompleted),
                        Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "review", review }
                        },
                        Message = review
                    });
                }
                break;

            default:
                break;
        }
    }

    public async Task<string> ReviewCode(string ask)
    {
        try
        {
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask), ["code"] = ask };
            return await CallFunction(ReviewerSkills.Review, context, _kernel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            throw;
        }
    }

    public async Task<string> ReviewPullRequest(IEnumerable<FilePatchResponse> deltas, string input)
    {
        try
        {
            var items = deltas.Select(del => $"======= File content ======= \n {del.Content} \n ======= Patch ======= \n {del.Patch}");
            var code = string.Join("\n", items);
            var context = new KernelArguments { ["input"] = input, ["code"] = code };
            return await CallFunction(ReviewerSkills.ReviewPullRequest, context, _kernel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            throw;
        }
    }

    public async Task<string> ExtractIntent(string ask)
    {
        try
        {
            var context = new KernelArguments { ["error"] = AppendChatHistory(ask) };
            return await CallFunction(ReviewerSkills.ExtractIntent, context, _kernel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            throw;
        }
    }
}

[GenerateSerializer]
public class DeveloperState
{
    [Id(0)]
    public string Understanding { get; set; }
}

public interface IReviewApps
{
    public Task<string> ReviewCode(string ask);
}
