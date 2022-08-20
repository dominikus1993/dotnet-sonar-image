#r "nuget: Newtonsoft.Json"
#r "nuget: FSharp.Text.Docker"

open FSharp.Text.Docker.Builders
open System.IO

let sdk = "6.0.400"
let runtime = "6.0.8"
let openJdk = "11"
let nodeJsVersion = "16"
let envVars = [
    ("DOTNETCORE_SDK", sdk)
    ("DOTNETCORE_RUNTIME", runtime)
    ("NETAPP_VERSION", "net5.0")
    ("DOCKER_VERSION", "5:20.10.10~3-0~debian-bullseye")
    ("CONTAINERD_VERSION", "1.4.11-1")
    ("OPENJDK_VERSION", openJdk)
    ("NODEJS_VERSION", "16")
]

let installNodeJs = """
RUN wget https://deb.nodesource.com/setup_$NODEJS_VERSION.x \
    && bash setup_$NODEJS_VERSION.x \
    && apt-get install -y nodejs
"""

let dockerSpecBuilder = dockerfile {
    from $"mcr.microsoft.com/dotnet/sdk:{sdk}"
    env_vars envVars
    run "apt-get update"
    run "apt-get dist-upgrade -y"
    run "apt-get install -y apt-transport-https ca-certificates curl gnupg-agent software-properties-common"    
    run "mkdir -p /usr/share/man/man1mkdir -p /usr/share/man/man1"
    run $"apt-get install -y openjdk-{openJdk}-jre"
    run $"wget https://deb.nodesource.com/setup_{nodeJsVersion}.x"
    run $"bash setup_{nodeJsVersion}.x "
    run "apt-get install -y nodejs"
}

let dockerFile = dockerSpecBuilder.Build()

printfn "%A" dockerFile

File.WriteAllText("DockerfileTest", dockerFile)