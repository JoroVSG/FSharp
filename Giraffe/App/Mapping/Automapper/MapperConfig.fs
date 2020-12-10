module App.Mapping.Automapper.MapperConfig

open System
open System.Linq.Expressions
open App.DTOs.ApplicationDTO
open AutoMapper
open Domains.Applications.Application

type AutoMapper.IMappingExpression<'TSource, 'TDestination> with
    member this.ForMemberFs<'TMember>
            (destGetter:Expression<Func<'TDestination, 'TMember>>,
             sourceGetter:Action<IMemberConfigurationExpression<'TSource, 'TDestination, 'TMember>>) =
        this.ForMember(destGetter, sourceGetter)

type OptionExpressions =
    static member MapFrom<'source, 'destination, 'sourceMember, 'destinationMember> (e: 'source -> 'sourceMember) =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'destinationMember>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'destinationMember>) -> opts.MapFrom(e))
    static member UseValue<'source, 'destination, 'value> (e: 'value) =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'value>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'value>) -> opts.UseValue(e))
    static member Ignore<'source, 'destination, 'destinationMember> () =
        System.Action<IMemberConfigurationExpression<'source, 'destination, 'destinationMember>> (fun (opts: IMemberConfigurationExpression<'source, 'destination, 'destinationMember>) -> opts.Ignore())
        
let createMapper =
    let configuration =
        MapperConfiguration(fun cfg ->
            cfg.CreateMap<Application, ApplicationDTO>()
                .ForMemberFs(
                    (fun d -> d.Id),
                    (fun opts -> opts.MapFrom(fun s -> s.IdApplication))
                )
                |> ignore
            cfg.CreateMap<ApplicationDTO, Application>()
                .ForMemberFs(
                    (fun d -> d.IdApplication),
                    (fun opts -> opts.MapFrom(fun s -> s.Id))
                )
                |> ignore
            )

    configuration.CreateMapper()