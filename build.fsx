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

Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "test/**/bin"
    ++ "test/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Restore" (fun _ ->
    !! "src/**/*.*proj"
    ++ "test/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    ++ "test/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

Target.create "Test" (fun _ ->
  !! "test/**/*.*proj"
  |> Seq.iter (DotNet.test id)
)

// PUBLISH TO NUGET
Target.create "Pack" (fun _ ->
    let release = ReleaseNotes.load "RELEASE_NOTES.md"

    !! "src/**/paket.template"
    |> Seq.iter(fun path ->
      Paket.pack(fun p ->
          { p with
              ToolType = ToolType.CreateCLIToolReference()
              BuildConfig = "Release"
              OutputPath = "bin"
              MinimumFromLockFile = true
              IncludeReferencedProjects = true
              Version = release.AssemblyVersion
              TemplateFile = path
              ReleaseNotes = String.toLines release.Notes })  
    )

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

Target.runOrDefault "All"
