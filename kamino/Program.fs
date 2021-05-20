open System
open Argu
open LibGit2Sharp
open System.Text.Json.Serialization

// # doc: https://docs.gitlab.com/ee/api/groups.html#list-a-groups-s-subgroups

type Project =
    { [<JsonPropertyName("path_with_namespace")>]
      Path: string
      [<JsonPropertyName("http_url_to_repo")>]
      Url: string }

let clone url path printMode =
    if printMode then
        printfn "%s" path
    else
        Repository.Clone(url, path) |> ignore
        printfn "%s" path

let inline (</>) path1 path2 = IO.Path.Combine(path1, path2)

let download token (url: string) =
    let client = new Net.WebClient()
    client.Headers.Set("PRIVATE-TOKEN", token)
    client.DownloadString url

let deserialize (json: string) =
    Text.Json.JsonSerializer.Deserialize<seq<Project>> json

// ?private_token={token} ???
let inline buildUrl url group =
    $"https://{url}/api/v4/groups/{group}/projects?include_subgroups=true&simple=true&per_page=100&page=1"

let insertToken baseAddress token (url: string) =
    url.Replace(baseAddress, $"oauth2:{token}@{baseAddress}")

let stringToSpan (s: string) =
    System.ReadOnlySpan<char>(s.ToCharArray())

// https://stackoverflow.com/q/30299671/1248177
let matchWildcard pattern text =
    IO.Enumeration.FileSystemName.MatchesSimpleExpression(stringToSpan pattern, stringToSpan text)

let filterByPattern includePattern excludePattern (projects: seq<Project>) =
    projects
    |> Seq.filter
        (fun { Path = path } ->
            let shouldKeep =
                match includePattern with
                | Some pattern -> matchWildcard pattern path
                | None -> true

            if not shouldKeep then
                false
            else
                match excludePattern with
                | Some pattern -> matchWildcard pattern path |> not
                | None -> true)

let cloneOrganisation baseAddress group path token printMode includePattern excludePattern =
    let clone project =
        let url =
            insertToken baseAddress token project.Url

        let target = path </> project.Path
        clone url target printMode

    if printMode then
        printfn "%s" "PrintMode: printonly"
    else
        IO.Directory.CreateDirectory path |> ignore

    buildUrl baseAddress group
    |> download token
    |> deserialize
    |> filterByPattern includePattern excludePattern
    |> Seq.iter clone

type Cmd =
    | [<Mandatory; AltCommandLine("-b")>] BaseAddress of string
    | [<Mandatory; AltCommandLine("-g")>] Group of string
    | [<Mandatory; AltCommandLine("-o")>] Output of string
    | [<Mandatory; AltCommandLine("-t")>] Token of string
    | [<AltCommandLine("-i")>] Include of string
    | [<AltCommandLine("-e")>] Exclude of string
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
        let printMode = cmd.Contains PrintOnly

        cloneOrganisation baseAddress group output token printMode includePattern excludePattern
        0
    with :? Argu.ArguParseException ->
        printfn $"%s{help}"
        printfn $"%s{parser.PrintUsage()}"
        1
