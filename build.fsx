#r "paket:
  nuget Fake.DotNet.Cli
  nuget Fake.IO.FileSystem
  nuget Fake.Core.Target
  nuget Fake.Core.ReleaseNotes
  nuget Fake.DotNet.Paket //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let rootDir = __SOURCE_DIRECTORY__ 

let sln = rootDir </> "Plough.WebApi.sln"

let srcGlob = rootDir </> "src/**/*.??proj"
let testsGlob = rootDir </> "test/**/*.??proj"


let distDir = rootDir </> "dist"
let distGlob = distDir </> "*.nupkg"

let isRelease (targets : Target list) =
    targets
    |> Seq.map(fun t -> t.Name)
    |> Seq.exists ((=)"Release")

let configuration (targets : Target list) =
    let defaultVal = if isRelease targets then "Release" else "Debug"
    match Environment.environVarOrDefault "CONFIGURATION" defaultVal with
    | "Debug" -> DotNet.BuildConfiguration.Debug
    | "Release" -> DotNet.BuildConfiguration.Release
    | config -> DotNet.BuildConfiguration.Custom config

Target.initEnvironment ()


Target.create "Clean" (fun _ ->
    [distDir]
    |> Shell.cleanDirs

    !! srcGlob
    ++ testsGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp -> System.IO.Path.GetDirectoryName p </> sp ))
    |> Shell.cleanDirs

    [
        rootDir </> "paket-files/paket.restore.cached"
    ]
    |> Seq.iter Shell.rm
)


Target.create "Restore" (fun _ ->
    [sln]
    |> Seq.iter (DotNet.restore id)
)

Target.create "Build" (fun _ ->
    [sln]
    |> Seq.iter (DotNet.build id)
)

Target.create "Test" (fun _ ->
  [sln]
  |> Seq.iter (DotNet.test id)
)

// PUBLISH TO NUGET

Target.create "Pack" (fun ctx ->
    let release = ReleaseNotes.load "RELEASE_NOTES.md"
    let releaseNotes = (String.concat "\n" release.Notes)
    let args =
        [
            sprintf "/p:PackageVersion=%s" release.NugetVersion
            sprintf "/p:PackageReleaseNotes=\"%s\"" releaseNotes
        ]
    DotNet.pack (fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            OutputPath = Some distDir
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln


)
"Build" ==> "Pack"

Target.create "Publish" (fun _ ->
    // target not yet working
    // dotnet nuget push Plough.WebApi.<package>.<version>.nupkg -s https://api.nuget.org/v3/index.json -k <api key>
    Paket.push(fun p ->
        { p with
            WorkingDir = "bin" })
)

Target.create "All" ignore

"Clean"
  ==> "Restore"
  ==> "Build"
  ==> "Test"
  ==> "All"

Target.runOrDefaultWithArguments "All"
