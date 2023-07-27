﻿namespace ISA

module ArcTablesAux =

    ///// 👀 Please do not remove this here until i copied it to Swate ~Kevin F
    //let getNextAutoGeneratedTableName (existingNames: seq<string>) =
    //    let findNextNumber (numbers: int list) =
    //        let rec findNext current = function
    //            | [] -> current + 1
    //            | x::xs when x = current + 1 -> findNext x xs
    //            | _ -> current + 1

    //        match numbers with
    //        | [] -> 0
    //        | x::xs -> findNext x xs
    //    let existingNumbers = existingNames |> Seq.choose (fun x -> match x with | Regex.ActivePatterns.AutoGeneratedTableName n -> Some n| _ -> None) 
    //    let nextNumber =
    //        if Seq.isEmpty existingNumbers then
    //            1
    //        else
    //            existingNumbers
    //            |> Seq.sort
    //            |> List.ofSeq
    //            |> findNextNumber
    //    ArcTable.init($"New Table {nextNumber}")

    let indexByTableName (name: string) (tables: ResizeArray<ArcTable>) =
        match Seq.tryFindIndex (fun t -> t.Name = name) tables with
        | Some index -> index
        | None -> failwith $"Unable to find table with name '{name}'!"

    module SanityChecks =
        
        let validateSheetIndex (index: int) (allowAppend: bool) (sheets: ResizeArray<ArcTable>) =
            let eval x y = if allowAppend then x > y else x >= y
            if index < 0 then failwith "Cannot insert ArcTable at index < 0."
            if eval index sheets.Count then failwith $"Specified index is out of range! Assay contains only {sheets.Count} tables."

        let validateNamesUnique (names:seq<string>) =
            let isDistinct = (Seq.length names) = (Seq.distinct names |> Seq.length)
            if not isDistinct then 
                failwith "Cannot add multiple tables with the same name! Table names inside one assay must be unqiue"

        let validateNewNameUnique (newName:string) (existingNames:seq<string>) =
            match Seq.tryFindIndex (fun x -> x = newName) existingNames with
            | Some i ->
                failwith $"Cannot create table with name {newName}, as table names must be unique and table at index {i} has the same name."
            | None ->
                ()

        let validateNewNamesUnique (newNames:seq<string>) (existingNames:seq<string>) =
            validateNamesUnique newNames
            let setNew = Set.ofSeq newNames
            let setOld = Set.ofSeq existingNames
            let same = Set.intersect setNew setOld
            if not same.IsEmpty then
                failwith $"Cannot create tables with the names {same}, as table names must be unique."

open ArcTablesAux
open ArcTableAux
/// This type only includes mutable options and only static members, the MUST be referenced and used in all record types implementing `ResizeArray<ArcTable>`
type ArcTables(thisTables:ResizeArray<ArcTable>) = 

    inherit ResizeArray<ArcTable>(thisTables)

    member this.TableCount 
        with get() = thisTables.Count

    member this.TableNames 
        with get() = 
            [for s in thisTables do yield s.Name]

    member this.Tables = 
        thisTables

    // - Table API - //
    // remark should this return ArcTable?
    member this.AddTable(table:ArcTable, ?index: int) = 
        let index = defaultArg index this.TableCount
        SanityChecks.validateSheetIndex index true thisTables
        SanityChecks.validateNewNameUnique table.Name this.TableNames
        thisTables.Insert(index, table)

    // - Table API - //
    member this.AddTables(tables:seq<ArcTable>, ?index: int) = 
        let index = defaultArg index this.TableCount
        SanityChecks.validateSheetIndex index true thisTables
        SanityChecks.validateNewNamesUnique (tables |> Seq.map (fun x -> x.Name)) this.TableNames
        thisTables.InsertRange(index, tables)

    // - Table API - //
    member this.InitTable(tableName:string, ?index: int) = 
        let index = defaultArg index this.TableCount
        let table = ArcTable.init(tableName)
        SanityChecks.validateSheetIndex index true thisTables
        SanityChecks.validateNewNameUnique table.Name this.TableNames
        thisTables.Insert(index, table)

    // - Table API - //
    member this.InitTables(tableNames:seq<string>, ?index: int) = 
        let index = defaultArg index this.TableCount
        let tables = tableNames |> Seq.map (fun x -> ArcTable.init(x))
        SanityChecks.validateSheetIndex index true thisTables
        SanityChecks.validateNewNamesUnique (tables |> Seq.map (fun x -> x.Name)) this.TableNames
        thisTables.InsertRange(index, tables)

    // - Table API - //
    member this.GetTableAt(index:int) : ArcTable =
        SanityChecks.validateSheetIndex index false thisTables
        thisTables.[index]

    // - Table API - //
    member this.GetTable(name: string) : ArcTable =
        indexByTableName name thisTables
        |> this.GetTableAt

    // - Table API - //
    member this.UpdateTableAt(index:int, table:ArcTable) =
        SanityChecks.validateSheetIndex index false thisTables
        SanityChecks.validateNewNameUnique table.Name this.TableNames
        thisTables.[index] <- table

    // - Table API - //
    member this.UpdateTable(name: string, table:ArcTable) : unit =
        (indexByTableName name thisTables, table)
        |> this.UpdateTableAt


    // - Table API - //
    member this.RemoveTableAt(index:int) : unit =
        SanityChecks.validateSheetIndex index false thisTables
        thisTables.RemoveAt(index)

    // - Table API - //
    member this.RemoveTable(name: string) : unit =
        indexByTableName name thisTables
        |> this.RemoveTableAt


    // - Table API - //
    // Remark: This must stay `ArcTable -> unit` so name cannot be changed here.
    member this.MapTableAt(index: int, updateFun: ArcTable -> unit) =
        SanityChecks.validateSheetIndex index false thisTables
        let table = thisTables.[index]
        updateFun table

    // - Table API - //
    member this.MapTable(name: string, updateFun: ArcTable -> unit) : unit =
        (indexByTableName name thisTables, updateFun)
        |> this.MapTableAt

    // - Table API - //
    member this.RenameTableAt(index: int, newName: string) : unit =
        SanityChecks.validateSheetIndex index false thisTables
        SanityChecks.validateNewNameUnique newName this.TableNames
        let table = this.GetTableAt index
        let renamed = {table with Name = newName} 
        this.UpdateTableAt(index, renamed)

    // - Table API - //
    member this.RenameTable(name: string, newName: string) : unit =
        (indexByTableName name thisTables, newName)
        |> this.RenameTableAt

    // - Column CRUD API - //
    member this.AddColumnAt(tableIndex:int, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) = 
        this.MapTableAt(tableIndex, fun table ->
            table.AddColumn(header, ?cells=cells, ?index=columnIndex, ?forceReplace=forceReplace)
        )

    // - Column CRUD API - //
    member this.AddColumn(tableName: string, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) =
        indexByTableName tableName thisTables
        |> fun i -> this.AddColumnAt(i, header, ?cells=cells, ?columnIndex=columnIndex, ?forceReplace=forceReplace)

    // - Column CRUD API - //
    member this.RemoveColumnAt(tableIndex: int, columnIndex: int) =
        this.MapTableAt(tableIndex, fun table ->
            table.RemoveColumn(columnIndex)
        )

    // - Column CRUD API - //
    member this.RemoveColumn(tableName: string, columnIndex: int) : unit =
        (indexByTableName tableName thisTables, columnIndex)
        |> this.RemoveColumnAt

    // - Column CRUD API - //
    member this.UpdateColumnAt(tableIndex: int, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        this.MapTableAt(tableIndex, fun table ->
            table.UpdateColumn(columnIndex, header, ?cells=cells)
        )

    // - Column CRUD API - //
    member this.UpdateColumn(tableName: string, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        indexByTableName tableName thisTables
        |> fun tableIndex -> this.UpdateColumnAt(tableIndex, columnIndex, header, ?cells=cells)

    // - Column CRUD API - //
    member this.GetColumnAt(tableIndex: int, columnIndex: int) =
        let table = this.GetTableAt(tableIndex)
        table.GetColumn(columnIndex)

    // - Column CRUD API - //
    member this.GetColumn(tableName: string, columnIndex: int) =
        (indexByTableName tableName thisTables, columnIndex)
        |> this.GetColumnAt

    // - Row CRUD API - //
    member this.AddRowAt(tableIndex:int, ?cells: CompositeCell [], ?rowIndex: int) = 
        this.MapTableAt(tableIndex, fun table ->
            table.AddRow(?cells=cells, ?index=rowIndex)
        )

    // - Row CRUD API - //
    member this.AddRow(tableName: string, ?cells: CompositeCell [], ?rowIndex: int) =
        indexByTableName tableName thisTables
        |> fun i -> this.AddRowAt(i, ?cells=cells, ?rowIndex=rowIndex)

    // - Row CRUD API - //
    member this.RemoveRowAt(tableIndex: int, rowIndex: int) =
        this.MapTableAt(tableIndex, fun table ->
            table.RemoveRow(rowIndex)
        )

    // - Row CRUD API - //
    member this.RemoveRow(tableName: string, rowIndex: int) : unit =
        (indexByTableName tableName thisTables, rowIndex)
        |> this.RemoveRowAt

    // - Row CRUD API - //
    member this.UpdateRowAt(tableIndex: int, rowIndex: int, cells: CompositeCell []) =
        this.MapTableAt(tableIndex, fun table ->
            table.UpdateRow(rowIndex, cells)
        )

    // - Row CRUD API - //
    member this.UpdateRow(tableName: string, rowIndex: int, cells: CompositeCell []) =
        (indexByTableName tableName thisTables, rowIndex, cells)
        |> this.UpdateRowAt

    // - Row CRUD API - //
    member this.GetRowAt(tableIndex: int, rowIndex: int) =
        let table = this.GetTableAt(tableIndex)
        table.GetRow(rowIndex)

    // - Row CRUD API - //
    member this.GetRow(tableName: string, rowIndex: int) =
        (indexByTableName tableName thisTables, rowIndex)
        |> this.GetRowAt

    /// Return a list of all the processes in all the tables.
    member this.GetProcesses() : Process list = 
        this.Tables
        |> Seq.toList
        |> List.collect (fun t -> t.GetProcesses())

    /// Create a collection of tables from a list of processes.
    ///
    /// For this, the processes are grouped by nameroot ("nameroot_1", "nameroot_2" ...) or exectued protocol if no name exists
    ///
    /// Then each group is converted to a table with this nameroot as sheetname
    static member fromProcesses (ps : Process list) : ArcTables = 
        ps
        |> ProcessParsing.groupProcesses
        |> List.map (fun (name,ps) ->
            ps
            |> List.collect (fun p -> ProcessParsing.processToRows p)
            |> fun rows -> ProcessParsing.alignByHeaders rows
            |> fun (headers, rows) -> ArcTable.create(name,headers,rows)
        )
        |> ResizeArray
        |> ArcTables