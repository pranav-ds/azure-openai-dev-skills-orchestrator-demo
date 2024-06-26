
namespace Microsoft.AI.DevTeam;
public static class ReviewerSkills {
    public static string Review = """
        You are a expert code reviewer in C++. You have a really strong sense properly intialization on use of variables.
        Below you'll find the code that I want your opinion on. 
        {{$code}}
        Additional information that might be useful:{{$input}}

        Pls provide your opinion on the code and be as specific as possible with relation to the error.
        """;

    public static string ExtractIntent = """
        You are an expert at parsing C++ error message. You can extract the intent of the code from the error message.
        Below you'll find the error message that I want you to extract the intent from. 
        {{$error}}
        I want you to respond in the following manner:
        {
            "error": "error message",
            "file": "file name",
            "line": "line number"
        }
        Do not output any other text. 
        Do not wrap the JSON in any other text, output the JSON format described above, making sure it's a valid JSON.
        Please.
        """;
    
     public static string ReviewPullRequest = """
        You are a expert code reviewer in C++. You have a really strong sense for good coding practices.
        Below you'll find updates on existing code that I want your opinion on.
        {{$code}}

        Additional information that might be useful:{{$input}}

        Pls provide your opinion on the code and be as specific as possible. If you see specific improvments please also provide code snippets for the updated file.
        """;
}