﻿namespace ISADotNet.QueryModel

open ISADotNet
open System.Text.Json.Serialization
open System.Text.Json
open System.IO

open System.Collections.Generic
open System.Collections


type QProcessSequence (sheets : QSheet list) =

    member this.Sheets = sheets

    new (processSequence : Process list) =
        let updateNodes (sheets : QSheet list) =
            let mapping = 
                sheets
                |> List.collect (fun s -> 
                    s.Rows
                    |> List.collect (fun r -> [r.Input, r.InputType.Value; r.Output, r.OutputType.Value])
                )
                |> List.groupBy fst
                |> List.map (fun (name,vs) -> name, vs |> List.map snd |> IOType.reduce)
                |> Map.ofList
            let updateRow row = 
                {row with 
                    InputType = Some mapping.[row.Input]
                    OutputType = Some mapping.[row.Output]
                }
            sheets
            |> List.map (fun sheet ->
                {sheet with Rows = sheet.Rows |> List.map updateRow}
            )
        let sheets = 
            processSequence
            |> List.groupBy (fun x -> 
                if x.ExecutesProtocol.IsSome && x.ExecutesProtocol.Value.Name.IsSome then
                    x.ExecutesProtocol.Value.Name.Value 
                else
                    // Data Stewards use '_' as seperator to distinguish between protocol template types.
                    // Exmp. 1SPL01_plants, in these cases we need to find the last '_' char and remove from that index.
                    let lastUnderScoreIndex = x.Name.Value.LastIndexOf '_'
                    x.Name.Value.Remove lastUnderScoreIndex
            )
            |> List.map (fun (name,processes) -> QSheet.fromProcesses name processes)
            |> updateNodes
        QProcessSequence(sheets)

    static member fromAssay (assay : Assay) =
        
        QProcessSequence(assay.ProcessSequence |> Option.defaultValue [])
      

    member this.Protocol (i : int, ?EntityName) =
        let sheet = 
            this.Sheets 
            |> List.tryItem i
        match sheet with
        | Some s -> s
        | None -> failwith $"""{EntityName |> Option.defaultValue "ProcessSequence"} does not contain sheet with index {i} """

    member this.Protocol (sheetName, ?EntityName) =
        let sheet = 
            this.Sheets 
            |> List.tryFind (fun sheet -> sheet.SheetName = sheetName)
        match sheet with
        | Some s -> s
        | None -> failwith $"""{EntityName |> Option.defaultValue "ProcessSequence"} does not contain sheet with name "{sheetName}" """

    member this.Protocols = this.Sheets

    member this.ProtocolCount =
        this.Sheets 
        |> List.length

    member this.ProtocolNames =
        this.Sheets 
        |> List.map (fun sheet -> sheet.SheetName)
       
    interface IEnumerable<QSheet> with
        member this.GetEnumerator() = (Seq.ofList this.Sheets).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() = (this :> IEnumerable<QSheet>).GetEnumerator() :> IEnumerator


    static member getNodes (ps : #QProcessSequence) =
        ps.Protocols 
        |> List.collect (fun p -> p.Rows |> List.collect (fun r -> [r.Input;r.Output]))
        |> List.distinct     

    static member getSubTreeOf (node : string) (ps : #QProcessSequence) =
        let rec collectForwardNodes nodes =
            let newNodes = 
                ps.Sheets
                |> List.collect (fun sheet ->
                    sheet.Rows 
                    |> List.choose (fun r -> if List.contains r.Input nodes then Some r.Output else None)
                )
                |> List.append nodes 
                |> List.distinct
                
            if newNodes = nodes then nodes
            else collectForwardNodes newNodes

        let collectBackwardNodes nodes =
            let newNodes = 
                ps.Sheets
                |> List.collect (fun sheet ->
                    sheet.Rows 
                    |> List.choose (fun r -> if List.contains r.Output nodes then Some r.Input else None)
                )
                |> List.append nodes 
                |> List.distinct
                       
            if newNodes = nodes then nodes
            else collectForwardNodes newNodes

        let forwardNodes = collectForwardNodes [node]
        let backwardNodes = collectBackwardNodes [node]

        ps.Sheets
        |> List.map (fun sheet ->
            {sheet 
                with Rows = 
                        sheet.Rows
                        |> List.filter (fun r ->
                            List.contains r.Input forwardNodes 
                            || (List.contains r.Output backwardNodes)

                        )

            }
        )
        |> QProcessSequence

    /// Returns the initial inputs final outputs of the assay, to which no processPoints
    static member getRootInputs (ps : #QProcessSequence) =
        let inputs = ps.Protocols |> List.collect (fun p -> p.Rows |> List.map (fun r -> r.Input))
        let outputs =  ps.Protocols |> List.collect (fun p -> p.Rows |> List.map (fun r -> r.Output)) |> Set.ofList
        inputs
        |> List.filter (fun i -> outputs.Contains i |> not)

    /// Returns the final outputs of the assay, which point to no further nodes
    static member getFinalOutputs (ps : #QProcessSequence) =
        let inputs = ps.Protocols |> List.collect (fun p -> p.Rows |> List.map (fun r -> r.Input)) |> Set.ofList
        let outputs =  ps.Protocols |> List.collect (fun p -> p.Rows |> List.map (fun r -> r.Output))
        outputs
        |> List.filter (fun i -> inputs.Contains i |> not)

    static member getNodesBy (predicate : QueryModel.IOType -> bool) (ps : #QProcessSequence) =
        ps.Protocols 
        |> List.collect (fun p -> 
            p.Rows 
            |> List.collect (fun r -> 
                [
                    if predicate r.InputType.Value then r.Input; 
                    if predicate r.OutputType.Value then  r.Output
                ])
        )
        |> List.distinct 

    static member getRootInputsBy (predicate : QueryModel.IOType -> bool) (ps : #QProcessSequence) =
        let mappings = 
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.map (fun r -> r.Input, r.Output)
                |> List.distinct
            ) 
            |> List.groupBy fst 
            |> List.map (fun (out,ins) -> out, ins |> List.map snd)
            |> Map.ofList

        let typeMappings =
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.collect (fun r -> [r.Input, r.InputType; r.Output, r.OutputType])
            ) 
            |> Map.ofList       

        let predicate (entity : string) =
            match typeMappings.[entity] with
            | Some t -> predicate t
            | None -> false

        let rec loop (searchEntities : string list) (foundEntities : string list) = 
            if searchEntities.IsEmpty then foundEntities |> List.distinct
            else
                let targs = searchEntities |> List.filter predicate
                let nonTargs = searchEntities |> List.filter (predicate >> not)
                let nextSearchEntities = nonTargs |> List.collect (fun en -> Map.tryFind en mappings |> Option.defaultValue [])
                loop nextSearchEntities targs

        loop (QProcessSequence.getRootInputs ps) []

    static member getFinalOutputsBy (predicate : QueryModel.IOType -> bool) (ps : #QProcessSequence) =
        let mappings = 
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.map (fun r -> r.Output, r.Input )
                |> List.distinct
            ) 
            |> List.groupBy fst 
            |> List.map (fun (out,ins) -> out, ins |> List.map snd)
            |> Map.ofList

        let typeMappings =
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.collect (fun r -> [r.Input, r.InputType; r.Output, r.OutputType])
            ) 
            |> Map.ofList       

        let predicate (entity : string) =
            match typeMappings.[entity] with
            | Some t -> predicate t
            | None -> false

        let rec loop (searchEntities : string list) (foundEntities : string list) = 
            if searchEntities.IsEmpty then foundEntities |> List.distinct
            else
                let targs = searchEntities |> List.filter predicate
                let nonTargs = searchEntities |> List.filter (predicate >> not)
                let nextSearchEntities = nonTargs |> List.collect (fun en -> Map.tryFind en mappings |> Option.defaultValue [])
                loop nextSearchEntities targs

        loop (QProcessSequence.getFinalOutputs ps) []

    static member getNodesOfBy (predicate : QueryModel.IOType -> bool) (sample : string) (ps : #QProcessSequence) =
        QProcessSequence.getSubTreeOf sample ps
        |> QProcessSequence.getNodesBy predicate

    /// Returns the initial inputs final outputs of the assay, to which no processPoints
    static member getRootInputsOfBy (predicate : QueryModel.IOType -> bool) (sample : string) (ps : #QProcessSequence) =
        QProcessSequence.getSubTreeOf sample ps
        |> QProcessSequence.getRootInputsBy predicate

    /// Returns the final outputs of the assay, which point to no further nodes
    static member getFinalOutputsOfBy (predicate : QueryModel.IOType -> bool) (sample : string) (ps : #QProcessSequence) =
        QProcessSequence.getSubTreeOf sample ps
        |> QProcessSequence.getFinalOutputsBy predicate
       
     /// Returns the initial inputs final outputs of the assay, to which no processPoints
    static member getPreviousValuesOf (ps : #QProcessSequence) (sample : string) =
        let mappings = 
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.map (fun r -> r.Output,r)
                |> List.distinct
            ) 

            |> Map.ofList
        let rec loop values lastState state = 
            if lastState = state then values 
            else
                let newState,newValues = 
                    state 
                    |> List.map (fun s -> 
                        mappings.TryFind s 
                        |> Option.map (fun r -> r.Input,r.Values)
                        |> Option.defaultValue (s,[])
                    )
                    |> List.unzip
                    |> fun (s,vs) -> s, vs |> List.concat
                loop (newValues@values) state newState
        loop [] [] [sample]  
        |> ValueCollection

    /// Returns the initial inputs final outputs of the assay, to which no processPoints
    static member getSucceedingValuesOf (ps : #QProcessSequence) (sample : string) =
        let mappings = 
            ps.Protocols 
            |> List.collect (fun p -> 
                p.Rows 
                |> List.map (fun r -> r.Input,r)
                |> List.distinct
            ) 

            |> Map.ofList
        let rec loop values lastState state = 
            if lastState = state then values 
            else
                let newState,newValues = 
                    state 
                    |> List.map (fun s -> 
                        mappings.TryFind s 
                        |> Option.map (fun r -> r.Output,r.Values)
                        |> Option.defaultValue (s,[])
                    )
                    |> List.unzip
                    |> fun (s,vs) -> s, vs |> List.concat
                loop (values@newValues) state newState
        loop [] [] [sample]
        |> ValueCollection


    member this.Nearest = 
        this.Sheets
        |> List.collect (fun sheet -> sheet.Values |> Seq.toList)
        |> IOValueCollection
   
    member this.SinkNearest = 
        this.Sheets
        |> List.collect (fun sheet -> 
            sheet.Rows
            |> List.collect (fun r ->               
                
                QProcessSequence.getRootInputsOfBy (fun _ -> true) r.Input this
                |> List.distinct
                |> List.collect (fun inp -> 
                    r.Values
                    |> List.map (fun v -> 
                        KeyValuePair((inp,r.Output),v)
                    )
                )
            )
        )
        |> IOValueCollection

    member this.SourceNearest = 
        this.Sheets
        |> List.collect (fun sheet -> 
            sheet.Rows
            |> List.collect (fun r ->               
                
                QProcessSequence.getFinalOutputsOfBy (fun _ -> true) r.Output this 
                |> List.distinct
                |> List.collect (fun out -> 
                    r.Values
                    |> List.map (fun v -> 
                        KeyValuePair((r.Input,out),v)
                    )
                )
            )
        )
        |> IOValueCollection

    member this.Global =
        this.Sheets
        |> List.collect (fun sheet -> 
            sheet.Rows
            |> List.collect (fun r ->  
                let outs = QProcessSequence.getFinalOutputsOfBy (fun _ -> true) r.Output this |> List.distinct
                let inps = QProcessSequence.getRootInputsOfBy (fun _ -> true) r.Input this |> List.distinct
                outs
                |> List.collect (fun out -> 
                    inps
                    |> List.collect (fun inp ->
                        r.Values
                        |> List.map (fun v -> 
                            KeyValuePair((inp,out),v)
                        )
                    )
                )
            )
        )
        |> IOValueCollection

    member this.Nodes() =
        QProcessSequence.getNodes(this)

    member this.FirstNodes() = 
        QProcessSequence.getRootInputs(this)

    member this.LastNodes() = 
        QProcessSequence.getFinalOutputs(this)

    member this.FirstNodesOf(node) = 
        QProcessSequence.getRootInputsOfBy (fun _ -> true) node this

    member this.LastNodesOf(node) = 
        QProcessSequence.getFinalOutputsOfBy (fun _ -> true) node this

    member this.Samples() =
        QProcessSequence.getNodesBy (fun (io : IOType) -> io.isSample) this

    member this.SamplesOf(node) =
        QProcessSequence.getNodesOfBy (fun (io : IOType) -> io.isSample) node this

    member this.FirstSamples() = 
        QProcessSequence.getRootInputsBy (fun (io : IOType) -> io.isSample) this

    member this.LastSamples() = 
        QProcessSequence.getFinalOutputsBy (fun (io : IOType) -> io.isSample) this

    member this.FirstSamplesOf(node) = 
        QProcessSequence.getRootInputsOfBy (fun (io : IOType) -> io.isSample) node this

    member this.LastSamplesOf(node) = 
        QProcessSequence.getFinalOutputsOfBy (fun (io : IOType) -> io.isSample) node this

    member this.Sources() =
        QProcessSequence.getNodesBy (fun (io : IOType) -> io.isSource) this

    member this.SourcesOf(node) =
        QProcessSequence.getNodesOfBy (fun (io : IOType) -> io.isSource) node this

    member this.Data() =
        QProcessSequence.getNodesBy (fun (io : IOType) -> io.isData) this

    member this.DataOf(node) =
        QProcessSequence.getNodesOfBy (fun (io : IOType) -> io.isData) node this

    member this.FirstData() = 
        QProcessSequence.getRootInputsBy (fun (io : IOType) -> io.isData) this

    member this.LastData() = 
        QProcessSequence.getFinalOutputsBy (fun (io : IOType) -> io.isData) this

    member this.FirstDataOf(node) = 
        QProcessSequence.getRootInputsOfBy (fun (io : IOType) -> io.isData) node this

    member this.LastDataOf(node) = 
        QProcessSequence.getFinalOutputsOfBy (fun (io : IOType) -> io.isData) node this

    member this.RawData() =
        QProcessSequence.getNodesBy (fun (io : IOType) -> io.isRawData) this

    member this.RawDataOf(node) =
        QProcessSequence.getNodesOfBy (fun (io : IOType) -> io.isRawData) node this

    member this.FirstRawData() = 
        QProcessSequence.getRootInputsBy (fun (io : IOType) -> io.isRawData) this

    member this.LastRawData() = 
        QProcessSequence.getFinalOutputsBy (fun (io : IOType) -> io.isRawData) this
    
    member this.FirstRawDataOf(node) = 
        QProcessSequence.getRootInputsOfBy (fun (io : IOType) -> io.isRawData) node this

    member this.LastRawDataOf(node) = 
        QProcessSequence.getFinalOutputsOfBy (fun (io : IOType) -> io.isRawData) node this

    member this.ProcessedData() =
        QProcessSequence.getNodesBy (fun (io : IOType) -> io.isProcessedData) this

    member this.ProcessedDataOf(node) =
        QProcessSequence.getNodesOfBy (fun (io : IOType) -> io.isProcessedData) node this

    member this.FirstProcessedData() = 
        QProcessSequence.getRootInputsBy (fun (io : IOType) -> io.isProcessedData) this

    member this.LastProcessedData() = 
        QProcessSequence.getFinalOutputsBy (fun (io : IOType) -> io.isProcessedData) this

    member this.FirstProcessedDataOf(node) = 
        QProcessSequence.getRootInputsOfBy (fun (io : IOType) -> io.isProcessedData) node this

    member this.LastProcessedDataOf(node) = 
        QProcessSequence.getFinalOutputsOfBy (fun (io : IOType) -> io.isProcessedData) node this

    member this.Values() = 
        this.Sheets
        |> List.collect (fun s -> s.Values.Values().Values)
        |> ValueCollection

    member this.Values(ontology : OntologyAnnotation ) = 
        this.Sheets
        |> List.collect (fun s -> s.Values.Values().Filter(ontology).Values)
        |> ValueCollection

    member this.Values(name : string ) = 
        this.Sheets
        |> List.collect (fun s -> s.Values.Values().Filter(name).Values)
        |> ValueCollection

    member this.Factors() =
        this.Values().Factors()

    member this.Parameters() =
        this.Values().Parameters()

    member this.Characteristics() =
        this.Values().Characteristics()

    member this.ValuesOf(node) =
        (QProcessSequence.getPreviousValuesOf this node).Values @ (QProcessSequence.getSucceedingValuesOf this node).Values
        |> ValueCollection

    member this.PreviousValuesOf(node) =
        QProcessSequence.getPreviousValuesOf this node

    member this.SucceedingValuesOf(node) =
        QProcessSequence.getSucceedingValuesOf this node

    member this.CharacteristicsOf(node) =
        this.ValuesOf(node).Characteristics()

    member this.PreviousCharacteristicsOf(node) =
        this.PreviousValuesOf(node).Characteristics()

    member this.SucceedingCharacteristicsOf(node) =
        this.SucceedingValuesOf(node).Characteristics()

    member this.ParametersOf(node) =
        this.ValuesOf(node).Parameters()

    member this.PreviousParametersOf(node) =
        this.PreviousValuesOf(node).Parameters()

    member this.SucceedingParametersOf(node) =
        this.SucceedingValuesOf(node).Parameters()

    member this.FactorsOf(node) =
        this.ValuesOf(node).Factors()

    member this.PreviousFactorsOf(node) =
        this.PreviousValuesOf(node).Factors()

    member this.SucceedingFactorsOf(node) =
        this.SucceedingValuesOf(node).Factors()

    member this.Contains(ontology : OntologyAnnotation) = 
        this.Values().Contains ontology

    member this.Contains(name : string) = 
        this.Values().Contains name

    //static member toString (rwa : QAssay) =  JsonSerializer.Serialize<QAssay>(rwa,JsonExtensions.options)

    //static member toFile (path : string) (rwa:QAssay) = 
    //    File.WriteAllText(path,QAssay.toString rwa)

    //static member fromString (s:string) = 
    //    JsonSerializer.Deserialize<QAssay>(s,JsonExtensions.options)

    //static member fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> QAssay.fromString