using System.Text;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Helpers;


namespace Microsoft.AI.DevTeam;

public class GithubService : IManageGithub
{
    private readonly GitHubClient _ghClient;
    private readonly ILogger<GithubService> _logger;
    private readonly HttpClient _httpClient;

    public GithubService(GitHubClient ghClient, ILogger<GithubService> logger, HttpClient httpClient)
    {
        _ghClient = ghClient;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task CreateBranch(string org, string repo, string branch)
    {
        try
        {
            var ghRepo = await _ghClient.Repository.Get(org, repo);
            await _ghClient.Git.Reference.CreateBranch(org, repo, branch, ghRepo.DefaultBranch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
             throw;
        }
    }

    public async Task<string> GetMainLanguage(string org, string repo)
    {
        try
        {
            var languages = await _ghClient.Repository.GetAllLanguages(org, repo);
            var mainLanguage = languages.OrderByDescending(l => l.NumberOfBytes).First();
            return mainLanguage.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting main language");
             throw;
        }
    }

    public async Task<int> CreateIssue(string org, string repo, string input, string function, long parentNumber)
    {
        try
        {
            var newIssue = new NewIssue($"{function} chain for #{parentNumber}")
            {
                Body = input,
            };
            newIssue.Labels.Add(function);
            newIssue.Labels.Add($"Parent.{parentNumber}");
            var issue = await _ghClient.Issue.Create(org, repo, newIssue);
            return issue.Number;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue");
             throw;
        }
    }

    public async Task CreatePR(string org, string repo, long number, string branch)
    {
        try
        {
            var ghRepo = await _ghClient.Repository.Get(org, repo);
            await _ghClient.PullRequest.Create(org, repo, new NewPullRequest($"New app #{number}", branch, ghRepo.DefaultBranch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PR");
             throw;
        }
    }

    public async Task PostComment(string org, string repo, long issueNumber, string comment)
    {
        try
        {
            await _ghClient.Issue.Comment.Create(org, repo, (int)issueNumber, comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting comment");
             throw;
        }
    }

     public async Task Post(string org, string repo, long issueNumber, string comment)
    {
        try
        {
            await _ghClient.Issue.Comment.Create(org, repo, (int)issueNumber, comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting comment");
             throw;
        }
    }

    public async Task<IEnumerable<FileResponse>> GetFiles(string org, string repo, string branch, Func<RepositoryContent, bool> filter)
    {
        try
        {
            var items = await _ghClient.Repository.Content.GetAllContentsByRef(org, repo, branch);
            return await CollectFiles(org, repo, branch, items, filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files");
             throw;
        }
    }

    public async Task<IEnumerable<FilePatchResponse>> GetPatches(string org, string repo, int prNumber)
    {
        try
        {
            var files = await _ghClient.PullRequest.Files(org, repo,prNumber);
            var pr = await  _ghClient.PullRequest.Get(org, repo, prNumber);
            var results = new List<FilePatchResponse>();
            foreach (var file in files)
            {
                   var fileContents = await _ghClient.Repository.Content.GetAllContentsByRef(org, repo, file.FileName, pr.Head.Sha);
                   var content = fileContents.Count > 0 ? fileContents.First().Content : "";
                   results.Add(new FilePatchResponse {
                    Content = content,
                    Patch = file.Patch
                   });  
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files");
             throw;
        }
    }

    private async Task<IEnumerable<FileResponse>> CollectFiles(string org, string repo, string branch, IReadOnlyList<RepositoryContent> items, Func<RepositoryContent, bool> filter)
    {
        try
        {
            var result = new List<FileResponse>();
            foreach (var item in items)
            {
                if (item.Type == ContentType.File && filter(item))
                {
                    var content = await _httpClient.GetStringAsync(item.DownloadUrl);
                    result.Add(new FileResponse
                    {
                        Name = item.Name,
                        Content = content
                    });
                }
                else if (item.Type == ContentType.Dir)
                {
                    var subItems = await _ghClient.Repository.Content.GetAllContentsByRef(org, repo, item.Path, branch);
                    result.AddRange(await CollectFiles(org, repo, branch, subItems, filter));
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting files");
             throw;
        }
    }
}

public class FileResponse
{
    public string Name { get; set; }
    public string Content { get; set; }
}

[GenerateSerializer]
public class FilePatchResponse
{
    [Id(0)]
    public string Patch { get; set; }
    [Id(1)]
    public string Content { get; set; }
}
public interface IManageGithub
{
    Task<int> CreateIssue(string org, string repo, string input, string function, long parentNumber);
    Task CreatePR(string org, string repo, long number, string branch);
    Task CreateBranch(string org, string repo, string branch);

    Task PostComment(string org, string repo, long issueNumber, string comment);
    Task<IEnumerable<FileResponse>> GetFiles(string org, string repo, string branch, Func<RepositoryContent, bool> filter);
    Task<string> GetMainLanguage(string org, string repo);
    Task<IEnumerable<FilePatchResponse>> GetPatches(string org, string repo, int prNumber);
}
