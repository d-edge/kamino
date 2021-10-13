# ![logo](kamino.svg) Kamino

Clone an organisation from GitLab

## Getting Started

Install kamino as a global dotnet tool

```bash
dotnet tool install kamino -g
```

or update it with

```bash
dotnet tool update kamino
```

or as a dotnet local tool

```bash
dotnet new tool-manifest
dotnet tool install kamino
```

## Quickstart

Run kamino:

```bash
kamino -b my-gitlab.com -o C:\Development\Git\ -g 42 -t xT0K3Nx4CC355x
```

## Usage

You can also get this with `dotnet kamino help`.

```sh
Kamino                                GitLab Organisation Cloner
----------------------------------------------------------------
EXAMPLE: kamino -b my-gitlab.com -o C:\Development\Git\ -g 42 -t xT0K3Nx4CC355x

USAGE: kamino [--help] --baseaddress <string> --group <string> --output <string> --token <string> [--include <string>]
              [--exclude <string>] [--printonly]

OPTIONS:

    --baseaddress, -b <string>
                          specify your gitlab instance base address
    --group, -g <string>  specify the group name or id to clone recursively
    --output, -o <string> specify the output folder to clone to
    --token, -t <string>  specify your access token
    --include, -i <string>
                          exclude all repositories but include matching
    --exclude, -e <string>
                          include all repositories but exclude matching
    --printonly, -p       print theorical path without actually cloning
    --help                display this list of options.
```

## Contributing

Help and feedback is always welcome and pull requests get accepted. 

Here is the contribution flow ([more information on datascholl.io](https://www.dataschool.io/how-to-contribute-on-github/)):

* Open or answer an issue to discuss the changes
*  Fork the project after the change has been formally approved
* Create a [feature branch](https://www.martinfowler.com/bliki/FeatureBranch.html)
* Follow the code convention by examining existing code (mostly Microsoft's guidelines)
* Edit the code with the changes
* Add/modify unit tests as required
* Submit your PR against the main branch

PRs can only be approved and merged when all checks succeed (builds on Windows, MacOs and Linux)

## Alternatives

* [GitLabber](https://github.com/ezbz/gitlabber) - clones or pulls entire groups tree from gitlab
* [Ghorg](https://github.com/gabrie30/ghorg) - clone an entire org/users repositories into one directory
* [Related SO Q&A](https://stackoverflow.com/q/29099456/1248177) - How to clone all projects of a group at once in GitLab?

## License

* [Marble icon](https://thenounproject.com/term/marble/1056965/) is licensed as Creative Commons CCBY by [Monjin Friends](https://thenounproject.com/monjin.friends/) from the [Noun Project](https://thenounproject.com).
* [MIT](./License)
