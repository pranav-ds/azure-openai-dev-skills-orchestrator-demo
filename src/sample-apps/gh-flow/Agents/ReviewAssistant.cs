using System.Text.Json;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam.Events;
using Orleans.Streams;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class ReviewAssistant : Agent
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly IGetContext _contextGetter;

    public ReviewAssistant(IGetContext contextGetter)
    {
        _contextGetter = contextGetter;
    }

    public override async Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.IntentExtracted):
            {
                var intent = JsonSerializer.Deserialize<ErrorDetail>(item.Data["intent"]);
                var context = await GetContext(intent);
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                {
                    Type = nameof(GithubFlowEventType.ReviewCodeContextAdded),
                    Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "parentNumber", item.Data["parentNumber"]  },
                            { "context", context }
                        }
                });
            }
                break;
            default:
                break;
        }
    }

    public async Task<string> GetContext(ErrorDetail intent)
    {
        return await _contextGetter.GetContext(intent);
    }
}

