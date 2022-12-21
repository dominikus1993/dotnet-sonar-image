#r "nuget: Newtonsoft.Json"
#r "nuget: FSharp.Text.Docker"
#r "nuget: Octokit"

open FSharp.Text.Docker.Builders
open System.IO
open Octokit
open FSharp.Control.TaskBuilder

let sonarScannerRuntime = "net5.0"
let sdk = "6.0.400"
let runtime = "6.0.8"
let openJdk = "11"
let nodeJsVersion = "16"
let dockerVersion = "5:20.10.10~3-0~debian-bullseye"
let containerD = "1.4.11-1"
let sonnarScannerRepositoryId = 34444711

let getSonarScannerRelease() =
    task {
        let client = GitHubClient(ProductHeaderValue("SonarScanner"))
        let! releases = client.Repository.Release.GetAll(sonnarScannerRepositoryId)
        let latest = releases[0]
        let binnary = latest.Assets |> Seq.filter(fun asset -> asset.Name.Contains(sonarScannerRuntime)) |> Seq.head
        return binnary
    }

let envVars = [
    ("DOTNETCORE_SDK", sdk)
    ("DOTNETCORE_RUNTIME", runtime)
    ("NETAPP_VERSION", "net5.0")
    ("DOCKER_VERSION", "5:20.10.10~3-0~debian-bullseye")
    ("CONTAINERD_VERSION", "1.4.11-1")
    ("OPENJDK_VERSION", openJdk)
    ("NODEJS_VERSION", "16")
]

let installPackages = $"""apt-get update \
    && apt-get dist-upgrade -y \
    && apt-get install -y apt-transport-https ca-certificates curl gnupg-agent software-properties-common"""

let installNodeJs = $"""wget https://deb.nodesource.com/setup_{nodeJsVersion}.x \
    && bash setup_{nodeJsVersion}.x \
    && apt-get install -y nodejs"""

let installContainerD = $"""curl -fsSL https://download.docker.com/linux/debian/gpg | apt-key add - \
    && apt-key fingerprint 0EBFCD88 \
    && add-apt-repository \
        "deb [arch=amd64] https://download.docker.com/linux/debian \
        $(lsb_release -cs) \
        stable" \
    && apt-get update \
    && apt-get install -y \
        docker-ce={dockerVersion} \
        docker-ce-cli={dockerVersion} \
        containerd.io={containerD}
"""

let installDotnet5 = $"""wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-5.0"""

let installSonarScannerCmd (release: ReleaseAsset) = 
    $"""apt-get install -y unzip \
        && wget {release.BrowserDownloadUrl} \
        && unzip {release.Name} -d /sonar-scanner \
        && rm {release.Name} \
        && chmod +x -R /sonar-scanner"""

let installSonarScanner() =
    task {
        let! binary = getSonarScannerRelease()
        return binary |> installSonarScannerCmd
    }

let runCleanUp = $"""apt-get -q autoremove \
    && apt-get -q clean -y \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/*.bin"""

let installSonarScannerCommand = 
    installSonarScanner().Result

let dockerSpecBuilder = dockerfile {
    from $"mcr.microsoft.com/dotnet/sdk:{sdk}"
    env_vars envVars
    run installPackages
    run "mkdir -p /usr/share/man/man1mkdir -p /usr/share/man/man1"
    run $"apt-get install -y openjdk-{openJdk}-jre"
    run installNodeJs
    run installContainerD
    run installDotnet5
    run installSonarScannerCommand
    run runCleanUp
}

let dockerFile = dockerSpecBuilder.Build()

printfn "%A" dockerFile

File.WriteAllText("Dockerfile", dockerFile)
