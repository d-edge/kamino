open System
open Argu
open Argu.ArguAttributes
open LibGit2Sharp

// # doc: https://docs.gitlab.com/ee/api/groups.html#list-a-groups-s-subgroups

type Project =
    { path_with_namespace: string
      http_url_to_repo: string }

let clone url path =
    Repository.Clone(url, path) |> ignore
    printfn "%s" path

let inline (</>) path1 path2 = IO.Path.Combine(path1, path2)

let download token (url: string) =
    let client = new Net.WebClient()
    client.Headers.Set("PRIVATE-TOKEN", token)
    client.DownloadString url

let deserialize (json: string) =
    Text.Json.JsonSerializer.Deserialize<seq<Project>> json

let inline buildUrl url group =
    $"https://{url}/api/v4/groups/{group}/projects?include_subgroups=true&per_page=100&page=1"

let insertToken baseAddress token (url: string) =
    url.Replace(baseAddress, $"oauth2:{token}@{baseAddress}")

let cloneOrganisation baseAddress group path token =
    let clone project =
        let url = insertToken baseAddress token project.http_url_to_repo
        let target = path </> project.path_with_namespace
        clone url target

    IO.Directory.CreateDirectory path |> ignore

    buildUrl baseAddress group
    |> download token
    |> deserialize
    |> Seq.iter clone

type Cmd =
    | [<Mandatory; AltCommandLine("-b")>] BaseAddress of string
    | [<Mandatory; AltCommandLine("-g")>] Group of string
    | [<Mandatory; AltCommandLine("-o")>] Output of string
    | [<Mandatory; AltCommandLine("-t")>] Token of string
    
    interface Argu.IArgParserTemplate with
        member this.Usage =
            match this with
            | BaseAddress _ -> "specify your gitlab instance base address"
            | Group _ -> "specify the group name or id to clone recursively"
            | Output _ -> "specify the output folder to clone to" 
            | Token _ -> "specify your access token"

let help = """Kamino                                GitLab Organisation Cloner
----------------------------------------------------------
Usage: kamino -b my-gitlab.com -o C:\Development\Git -g 42 -t "xT0K3Nx4CC355x"
"""

[<EntryPoint>]
let main argv =
    let processName = 
        let name = IO.Path.GetFileNameWithoutExtension (Diagnostics.Process.GetCurrentProcess().MainModule.FileName )
        if String.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase) then
            "dotnet kamino"
        else
            "kamino"

    let parser = ArgumentParser<Cmd>(programName = processName)
    try
        let cmd = parser.ParseCommandLine(argv)

        let baseAddress = cmd.GetResult BaseAddress
        let group = cmd.GetResult Group
        let output = cmd.GetResult Output
        let token = cmd.GetResult Token

        cloneOrganisation baseAddress group output token
        0
    with
    | :? Argu.ArguParseException ->
        printfn $"%s{help}"
        printfn $"%s{parser.PrintUsage()}"
        1
