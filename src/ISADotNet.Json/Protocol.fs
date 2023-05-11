namespace ISADotNet.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ISADotNet
open System.IO
open GEncode
module ProtocolParameter =
    
    let genID (pp:ProtocolParameter) = 
        match pp.ID with
            | Some id -> URI.toString id
            | None -> "#EmptyProtocolParameter"

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> ProtocolParameter |> genID)
                else tryInclude "@id" GEncode.string (oa |> tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.string "ProtocolParameter"
            tryInclude "parameterName" (OntologyAnnotation.encoder options) (oa |> tryGetPropertyValue "ParameterName")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<ProtocolParameter> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                ParameterName = get.Optional.Field "parameterName" (OntologyAnnotation.decoder options)
            }
        )

    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (p:ProtocolParameter) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:ProtocolParameter) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:ProtocolParameter) = 
    //    File.WriteAllText(path,toString p)

module Component =
    
    let genID (c:Component) = 
        match c.ComponentName with
            | Some cn -> "#Component_" + cn
            | None -> "#EmptyComponent"

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> Component |> genID)
            if options.IncludeType then "@type", GEncode.string "Component"
            tryInclude "componentName" GEncode.string (oa |> tryGetPropertyValue "ComponentName")
            tryInclude "componentType" (OntologyAnnotation.encoder options) (oa |> tryGetPropertyValue "ComponentType")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Component> =
        fun s json ->           
            Decode.object (fun get ->
                {
                    ComponentName = get.Optional.Field "componentName" GDecode.uri
                    ComponentValue = None
                    ComponentUnit = None
                    ComponentType = get.Optional.Field "componentType" (OntologyAnnotation.decoder options)
                }
            ) s json
            |> Result.map (fun c ->
                let v, unit =  
                    match c.ComponentName with
                    | Some c -> Component.decomposeName c |> fun (a,b) -> Some a,b
                    | Option.None -> Option.None, Option.None
                {c with ComponentValue = v; ComponentUnit = unit}    
            )


    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (p:Component) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:Component) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Component) = 
    //    File.WriteAllText(path,toString p)

module Protocol =   
    
    let genID (p:Protocol) = 
        match p.ID with
            | Some id -> URI.toString id
            | None -> match p.Uri with
                        | Some u -> URI.toString u
                        | None -> match p.Name with
                                    | Some n -> "#Protocol_" + n
                                    | None -> "#EmptyComponent" 

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> Protocol |> genID)
                else tryInclude "@id" GEncode.string (oa |> tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.string "Protocol"
            tryInclude "name" GEncode.string (oa |> tryGetPropertyValue "Name")
            tryInclude "protocolType" (OntologyAnnotation.encoder options) (oa |> tryGetPropertyValue "ProtocolType")
            tryInclude "description" GEncode.string (oa |> tryGetPropertyValue "Description")
            tryInclude "uri" GEncode.string (oa |> tryGetPropertyValue "Uri")
            tryInclude "version" GEncode.string (oa |> tryGetPropertyValue "Version")
            tryInclude "parameters" (ProtocolParameter.encoder options) (oa |> tryGetPropertyValue "Parameters")
            tryInclude "components" (Component.encoder options) (oa |> tryGetPropertyValue "Components")
            tryInclude "comments" (Comment.encoder options) (oa |> tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Protocol> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                Name = get.Optional.Field "name" Decode.string
                ProtocolType = get.Optional.Field "protocolType" (OntologyAnnotation.decoder options)
                Description = get.Optional.Field "description" Decode.string
                Uri = get.Optional.Field "uri" GDecode.uri
                Version = get.Optional.Field "version" Decode.string
                Parameters = get.Optional.Field "parameters" (Decode.list (ProtocolParameter.decoder options))
                Components = get.Optional.Field "components" (Decode.list (Component.decoder options))
                Comments = get.Optional.Field "comments" (Decode.list (Comment.decoder options))
            }
        )

    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (p:Protocol) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:Protocol) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Protocol) = 
    //    File.WriteAllText(path,toString p)
