open System
open Argu
open LibGit2Sharp
open System.Text.Json.Serialization

type Project =
    { [<JsonPropertyName("path_with_namespace")>]
      Path: string
      [<JsonPropertyName("ssh_url_to_repo")>]
      Ssh: string
      [<JsonPropertyName("http_url_to_repo")>]
      Url: string }

type CloneMode = 
    | Http
    | Ssh

let (|?) = defaultArg

let inline (</>) path1 path2 = IO.Path.Combine(path1, path2)

// # doc: https://docs.gitlab.com/ee/api/groups.html#list-a-groups-s-subgroups
let download token (url: string) =
    let client = new Net.WebClient()
    client.Headers.Set("PRIVATE-TOKEN", token)
    client.DownloadString url

let deserialize (json: string) =
    Text.Json.JsonSerializer.Deserialize<seq<Project>> json

// ?private_token={token} ???
let inline buildUrl (root: string) (group: string) =
    $"https://{root}/api/v4/groups/{group}/projects?include_subgroups=true&simple=true&per_page=100&page=1"

let stringToSpan (s: string) =
    System.ReadOnlySpan<char>(s.ToCharArray())

// https://stackoverflow.com/q/30299671/1248177
let matchWildcard pattern path =
    IO.Enumeration.FileSystemName.MatchesSimpleExpression(stringToSpan pattern, stringToSpan path)

let shouldStay (includePattern: string Option) (excludePattern: string Option) path =
    (includePattern.IsNone || matchWildcard includePattern.Value path) &&
    (excludePattern.IsNone || not (matchWildcard excludePattern.Value path))  

let filterByPattern (includePattern: string Option) (excludePattern: string Option) (paths: Project seq) :Project seq =
    paths |> Seq.filter (fun { Path = path } -> path |> shouldStay includePattern excludePattern)

let getUrl cloneMode root (token: string) project =
    if cloneMode = Ssh
    then project.Ssh
    else project.Url.Replace(root, $"oauth2:{token}@{root}")

let cloneOrganisation root group directoryPath token printMode cloneMode includePattern excludePattern =
    let projects =
        buildUrl root group
        |> download token
        |> deserialize
        |> filterByPattern includePattern excludePattern

    if printMode then
        printfn "%s" directoryPath
        projects |> Seq.iter(fun {Path = path} -> directoryPath </> path |> printfn "%s") 
    else
        IO.Directory.CreateDirectory directoryPath |> ignore
        projects |> Seq.iter (fun project -> 
            Repository.Clone(getUrl cloneMode root token project, directoryPath </> project.Path) |> printfn "%s"
        )

type Cmd =
    | [<Mandatory; AltCommandLine("-b")>] BaseAddress of string
    | [<Mandatory; AltCommandLine("-g")>] Group of string
    | [<Mandatory; AltCommandLine("-o")>] Output of string
    | [<Mandatory; AltCommandLine("-t")>] Token of string
    | [<AltCommandLine("-i")>] Include of string
    | [<AltCommandLine("-e")>] Exclude of string
    | [<AltCommandLine("-c")>] CloneMode of CloneMode
    | [<AltCommandLine("-p")>] PrintOnly

    interface Argu.IArgParserTemplate with
        member this.Usage =
            match this with
            | BaseAddress _ -> "specify your gitlab instance base address"
            | Group _ -> "specify the group name or id to clone recursively"
            | Output _ -> "specify the output folder to clone to"
            | Token _ -> "specify your access token"
            | Include _ -> "exclude all repositories but include matching"
            | Exclude _ -> "include all repositories but exclude matching"
            | CloneMode _ -> "specify to clone through http or ssh"
            | PrintOnly -> "print theorical path without actually cloning"

let help = """Kamino                                GitLab Organisation Cloner
----------------------------------------------------------------
EXAMPLE: kamino -b my-gitlab.com -o C:\Development\Git\ -g 42 -t xT0K3Nx4CC355x
"""

[<EntryPoint>]
let main argv =
    let equalsDotNet name =
        String.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase)

    let processName =
        let isDotNet =
            Diagnostics
                .Process
                .GetCurrentProcess()
                .MainModule
                .FileName
            |> IO.Path.GetFileNameWithoutExtension
            |> equalsDotNet

        if isDotNet then
            "dotnet kamino"
        else
            "kamino"

    let parser =
        ArgumentParser<Cmd>(programName = processName)

    try
        let cmd = parser.ParseCommandLine(argv)

        let baseAddress = cmd.GetResult BaseAddress
        let group = cmd.GetResult Group
        let output = cmd.GetResult Output
        let token = cmd.GetResult Token
        let includePattern = cmd.TryGetResult Include
        let excludePattern = cmd.TryGetResult Exclude
        let cloneMode = cmd.TryGetResult CloneMode |? Http
        let printMode = cmd.Contains PrintOnly

        cloneOrganisation baseAddress group output token printMode cloneMode includePattern excludePattern
        0
    with :? Argu.ArguParseException ->
        printfn $"%s{help}"
        printfn $"%s{parser.PrintUsage()}"
        1
