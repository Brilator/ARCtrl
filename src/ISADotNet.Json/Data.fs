namespace ISADotNet.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ISADotNet
open System.IO
open GEncode

module DataFile = 

    let encoder (options : ConverterOptions) (value : obj) = 
        match value with
        | :? DataFile as DataFile.RawDataFile -> 
            Encode.string "Raw Data File"
        | :? DataFile as DataFile.DerivedDataFile  -> 
            Encode.string "Derived Data File"
        | :? DataFile as DataFile.ImageFile  -> 
            Encode.string "Image File"
        | _ -> Encode.nil

    let decoder (options : ConverterOptions) : Decoder<DataFile> =
        fun s json ->
            match Decode.string s json with
            | Ok "Raw Data File" -> 
                Ok DataFile.RawDataFile
            | Ok "Derived Data File" -> 
                Ok DataFile.DerivedDataFile
            | Ok "Image File" -> 
                Ok DataFile.ImageFile
            | Ok s -> Error (DecoderError($"Could not parse {s}.", ErrorReason.BadPrimitive(s,Encode.nil)))
            | Error e -> Error e


module Data = 
    
    let genID (d:Data) = 
        match d.ID with
        | Some id -> URI.toString id
        | None -> match d.Name with
                  | Some n -> n
                  | None -> "#EmptyData"
    
    let rec encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> Data |> genID)
                else tryInclude "@id" GEncode.string (oa |> tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.string "Data"
            tryInclude "name" GEncode.string (oa |> tryGetPropertyValue "Name")
            tryInclude "type" (DataFile.encoder options) (oa |> tryGetPropertyValue "DataType")
            tryInclude "comments" (Comment.encoder options) (oa |> tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let rec decoder (options : ConverterOptions) : Decoder<Data> =
        
        fun s json -> 
            if GDecode.hasUnknownFields ["@id";"name";"type";"comments"] json then 
                Error (DecoderError("Unknown fields in Data", ErrorReason.BadPrimitive(s,Encode.nil)))
            else

                Decode.object (fun get ->
                    {
                        ID = get.Optional.Field "@id" GDecode.uri
                        Name = get.Optional.Field "name" Decode.string
                        DataType = get.Optional.Field "type" (DataFile.decoder options)
                        Comments = get.Optional.Field "comments" (Decode.list (Comment.decoder options))
                    }
            
                )  s json

    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (m:Data) = 
        encoder (ConverterOptions()) m
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (d:Data) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) d
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (m:Data) = 
    //    File.WriteAllText(path,toString m)


module Source = 
    
    let genID (s:Source) = 
        match s.ID with
        | Some id -> URI.toString id
        | None -> match s.Name with
                  | Some n -> "#Source_" + n.Replace(" ","_")
                  | None -> "#EmptySource"
    
    let rec encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> Source |> genID)
                else tryInclude "@id" GEncode.string (oa |> tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.string "Source"
            tryInclude "name" GEncode.string (oa |> tryGetPropertyValue "Name")
            tryInclude "characteristics" (MaterialAttributeValue.encoder options) (oa |> tryGetPropertyValue "Characteristics")        ]
        |> GEncode.choose
        |> Encode.object

    let rec decoder (options : ConverterOptions) : Decoder<Source> =
        fun s json -> 
        if GDecode.hasUnknownFields ["@id";"name";"characteristics"] json then 
                Error (DecoderError("Unknown fields in Source", ErrorReason.BadPrimitive(s,Encode.nil)))
            else
            Decode.object (fun get ->
            
                    {
                        ID = get.Optional.Field "@id" GDecode.uri
                        Name = get.Optional.Field "name" Decode.string
                        Characteristics = get.Optional.Field "characteristics" (Decode.list (MaterialAttributeValue.decoder options))
                    } 
            
            ) s json

    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (m:Source) = 
        encoder (ConverterOptions()) m
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (s:Source) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) s
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (m:Source) = 
    //    File.WriteAllText(path,toString m)

module Sample = 
    
    let genID (s:Sample) = 
        match s.ID with
        | Some id -> URI.toString id
        | None -> match s.Name with
                  | Some n -> "#Sample_" + n.Replace(" ","_")
                  | None -> "#EmptySample"
    
    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.string (oa :?> Sample |> genID)
                else tryInclude "@id" GEncode.string (oa |> tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.string "Sample"
            tryInclude "name" GEncode.string (oa |> tryGetPropertyValue "Name")
            tryInclude "characteristics" (MaterialAttributeValue.encoder options) (oa |> tryGetPropertyValue "Characteristics")
            tryInclude "factorValues" (FactorValue.encoder options) (oa |> tryGetPropertyValue "FactorValues")
            tryInclude "derivesFrom" (Source.encoder options) (oa |> tryGetPropertyValue "DerivesFrom")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Sample> =
        fun s json -> 
            if GDecode.hasUnknownFields ["@id";"name";"characteristics";"factorValues";"derivesFrom"] json then 
                Error (DecoderError("Unknown fields in Sample", ErrorReason.BadPrimitive(s,Encode.nil)))
            else
                Decode.object (fun get ->
                    {
                        ID = get.Optional.Field "@id" GDecode.uri
                        Name = get.Optional.Field "name" Decode.string
                        Characteristics = get.Optional.Field "characteristics" (Decode.list (MaterialAttributeValue.decoder options))
                        FactorValues = get.Optional.Field "factorValues" (Decode.list (FactorValue.decoder options))
                        DerivesFrom = get.Optional.Field "derivesFrom" (Decode.list (Source.decoder options))
                    }
            
                ) s json

    let fromString (s:string) = 
        GDecode.fromString (decoder (ConverterOptions())) s

    let toString (m:Sample) = 
        encoder (ConverterOptions()) m
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (s:Sample) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) s
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (m:Sample) = 
    //    File.WriteAllText(path,toString m)