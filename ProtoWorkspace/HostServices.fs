﻿module ProtoWorkspace.HostServices

open System
open System.Reflection
open System.Composition
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Host
open Microsoft.CodeAnalysis.Host.Mef

// based on - https://gist.github.com/praeclarum/953629b2f80860e54747

type FSharpHostLanguageService (workspace:Workspace) =
    inherit HostLanguageServices()

    override __.Language = Constants.FSharpLanguageName
    override __.WorkspaceServices with get () = workspace.Services
    override __.GetService<'a when 'a :> ILanguageService>() : 'a = Unchecked.defaultof<'a>



type FSharpHostWorkspaceService (workspace:Workspace,baseServices:HostWorkspaceServices) =
    inherit HostWorkspaceServices()

    let languageService = FSharpHostLanguageService workspace

    override __.GetService<'a when 'a :> IWorkspaceService >()  =
        baseServices.GetService<'a>()

    override __.HostServices with get() = workspace.Services.HostServices

    override __.Workspace = workspace

    override __.IsSupported languageName = languageName = Constants.FSharpLanguageName

    override __.SupportedLanguages = seq [Constants.FSharpLanguageName]

    override __.GetLanguageServices _ = languageService :> HostLanguageServices

    override __.FindLanguageServices filter  = base.FindLanguageServices filter


type FSharpHostService () =
    inherit HostServices()
    let baseWorkspace = new AdhocWorkspace()

    override __.CreateWorkspaceServices workspace =
        FSharpHostWorkspaceService(workspace,baseWorkspace.Services) :> HostWorkspaceServices


type IHostServicesProvider =
    abstract Assemblies : Assembly ImmutableArray

[<Export>]
type HostServicesAggregator [<ImportingConstructor>] ([<ImportMany>] hostServicesProviders : seq<IHostServicesProvider>) =
    let builder = ImmutableHashSet.CreateBuilder<Assembly>()

    do
        for asm in MefHostServices.DefaultAssemblies do
            builder.Add asm |> ignore
        for provider in hostServicesProviders do
            for asm in provider.Assemblies do
                builder.Add asm |> ignore

    let assemblies = builder.ToImmutableArray()
    member __.CreateHostServices() = MefHostServices.Create assemblies