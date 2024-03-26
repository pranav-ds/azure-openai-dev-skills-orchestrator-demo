using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam;
using Microsoft.AI.DevTeam.Events;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Models;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;
public sealed class GithubWebHookProcessor : WebhookEventProcessor
{
    private readonly ILogger<GithubWebHookProcessor> _logger;
    private readonly IClusterClient _client;

    public GithubWebHookProcessor(ILogger<GithubWebHookProcessor> logger,
    IClusterClient client)
    {
        _logger = logger;
        _client = client;
    }
    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        try
        {
            _logger.LogInformation("Processing issue event");
            var org = issuesEvent.Repository.Owner.Login;
            var repo = issuesEvent.Repository.Name;
            var issueNumber = issuesEvent.Issue.Number;
            var input = issuesEvent.Issue.Body;
            // Assumes the label follows the following convention: Skill.Function example: PM.Readme
            // Also, we've introduced the Parent label, that ties the sub-issue with the parent issue
            var labels = issuesEvent.Issue.Labels
                                    .Select(l => l.Name.Split('.'))
                                    .Where(parts => parts.Length == 2)
                                    .ToDictionary(parts => parts[0], parts => parts[1]);
            var skillName = labels.Keys.Where(k => k != "Parent").FirstOrDefault();
            long? parentNumber = labels.ContainsKey("Parent") ? long.Parse(labels["Parent"]) : null;

            var suffix = $"{org}-{repo}";
            if (issuesEvent.Action == IssuesAction.Opened)
            {
                _logger.LogInformation("Processing HandleNewAsk");
                await HandleNewAsk(issueNumber, parentNumber, skillName, labels[skillName], suffix, input, org, repo);
            }
            else if (issuesEvent.Action == IssuesAction.Closed && issuesEvent.Issue.User.Type.Value == UserType.Bot)
            {
                _logger.LogInformation("Processing HandleClosingIssue");
                await HandleClosingIssue(issueNumber, parentNumber, skillName, labels[skillName], suffix, org, repo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing issue event");
            throw;
        }
    }

    protected override async Task ProcessIssueCommentWebhookAsync(
       WebhookHeaders headers,
       IssueCommentEvent issueCommentEvent,
       IssueCommentAction action)
    {
        try
        {
            _logger.LogInformation("Processing issue comment event");
            var org = issueCommentEvent.Repository.Owner.Login;
            var repo = issueCommentEvent.Repository.Name;
            var issueNumber = issueCommentEvent.Issue.Number;
            var input = issueCommentEvent.Comment.Body;
            // Assumes the label follows the following convention: Skill.Function example: PM.Readme
            var labels = issueCommentEvent.Issue.Labels
                                    .Select(l => l.Name.Split('.'))
                                    .Where(parts => parts.Length == 2)
                                    .ToDictionary(parts => parts[0], parts => parts[1]);
            var skillName = labels.Keys.Where(k => k != "Parent").FirstOrDefault();
            long? parentNumber = labels.ContainsKey("Parent") ? long.Parse(labels["Parent"]) : null;
            var suffix = $"{org}-{repo}";
            // we only respond to non-bot comments
            if (issueCommentEvent.Sender.Type.Value != UserType.Bot)
            {
                await HandleNewAsk(issueNumber, parentNumber, skillName, labels[skillName], suffix, input, org, repo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing issue comment event");
            throw;
        }

    }

    private async Task HandleClosingIssue(long issueNumber, long? parentNumber, string skillName, string functionName, string suffix, string org, string repo)
    {
        /*var streamProvider = _client.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.MainNamespace, suffix+issueNumber.ToString());
        var stream = streamProvider.GetStream<Event>(streamId);
        var eventType = (skillName, functionName) switch
            {
                ("PM","Readme") => nameof(GithubFlowEventType.ReadmeChainClosed),
                ("DevLead","Plan") => nameof(GithubFlowEventType.DevPlanChainClosed),
                ("Developer","Implement") => nameof(GithubFlowEventType.CodeChainClosed),
                _ => nameof(GithubFlowEventType.NewAsk)
            };
        var data = new Dictionary<string, string>
        {
            { "org", org },
            { "repo", repo },
            { "issueNumber", issueNumber.ToString() },
            { "parentNumber", parentNumber?.ToString()}
        };

        await stream.OnNextAsync(new Event
        {
            Type = eventType,
            Data = data
        }); */
    }

    private async Task HandleNewAsk(long issueNumber, long? parentNumber, string skillName, string functionName, string suffix, string input, string org, string repo)
    {
        try
        {
            _logger.LogInformation("Handling new ask");
            var streamProvider = _client.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.MainNamespace, suffix + issueNumber.ToString());
            var stream = streamProvider.GetStream<Event>(streamId);

            var eventType = (skillName, functionName) switch
            {
                ("code-reviewer", "repo") => nameof(GithubFlowEventType.ReviewRequested),
                _ => nameof(GithubFlowEventType.ReviewRequested)
            };
            var data = new Dictionary<string, string>
            {
                { "org", org },
                { "repo", repo },
                { "issueNumber", issueNumber.ToString() },
                { "parentNumber", parentNumber?.ToString()}
            };
            await stream.OnNextAsync(new Event
            {
                Type = eventType,
                Message = input,
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling new ask");
            throw;
        }
    }
}

