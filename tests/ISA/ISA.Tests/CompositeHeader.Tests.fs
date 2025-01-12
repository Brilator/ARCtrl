﻿module CompositeHeader.Tests

open ARCtrl.ISA

open TestingUtils
open Fable.Core

let private tests_iotype = 
    testList "IOType" [
        testCase "asInput" (fun () ->
            Expect.equal IOType.Source.asInput "Input [Source Name]" "Source"
            Expect.equal IOType.Sample.asInput "Input [Sample Name]" "Sample"
            Expect.equal IOType.RawDataFile.asInput "Input [Raw Data File]" "Raw Data File"
            Expect.equal IOType.DerivedDataFile.asInput "Input [Derived Data File]" "Derived Data File"
            Expect.equal IOType.ImageFile.asInput "Input [Image File]" "Image File"
            Expect.equal IOType.Material.asInput "Input [Material]" "Material"
            Expect.equal (IOType.FreeText "Test").asInput "Input [Test]" "FreeText Test"
        )
        testCase "asOutput" (fun () ->
            Expect.equal IOType.Source.asOutput "Output [Source Name]" "Source"
            Expect.equal IOType.Sample.asOutput "Output [Sample Name]" "Sample"
            Expect.equal IOType.RawDataFile.asOutput "Output [Raw Data File]" "Raw Data File"
            Expect.equal IOType.DerivedDataFile.asOutput "Output [Derived Data File]" "Derived Data File"      
            Expect.equal IOType.ImageFile.asOutput "Output [Image File]" "Image File"
            Expect.equal IOType.Material.asOutput "Output [Material]" "Material"
            Expect.equal (IOType.FreeText "Test").asOutput "Output [Test]" "FreeText Test"
        )
        // This test ensures that new IOTypes are also added to `All` static member.
        testCase "Cases" (fun () -> 
            let caseInfos = IOType.Cases
            Expect.hasLength IOType.All (caseInfos.Length-1) "Expect one less than all because we do not want to track `FreeText` case."
        )
        testCase "getUIToolTip" <| fun _ ->
            let cases = IOType.Cases |> Array.map snd
            for case in cases do
                let actual = IOType.getUITooltip(U2.Case2 case)
                Expect.isTrue (actual.Length > 0) $"{case}"
        testCase "GetUIToolTip" <| fun _ ->
            let iotype = IOType.Material
            let actual = iotype.GetUITooltip()
            Expect.isTrue (actual.Length > 0) ""
    ]

let private tests_jsHelper = testList "jsHelper" [
    testCase "jsGetColumnMetaType" <| fun _ ->
        let cases = CompositeHeader.Cases
        for case in cases do
            let tag = fst case
            let code = CompositeHeader.jsGetColumnMetaType tag
            let validCode = [|0;1;2;3|] |> Array.contains code
            Expect.isTrue validCode $"Code ({code}) for tag ({tag}) is invalid"
]

let private tests_compositeHeader =
    testList "CompositeHeader" [
        testCase "Cases" <| fun _ ->
            let count = CompositeHeader.Cases.Length
            Expect.equal count 14 "count"
        testCase "getExplanation" <| fun _ ->
            let cases = CompositeHeader.Cases |> Array.map snd
            for case in cases do
                let actual = CompositeHeader.getUITooltip(U2.Case2 case)
                Expect.isTrue (actual.Length > 0) $"{case}"
        testCase "GetExplanation" <| fun _ ->
            let header = CompositeHeader.Component OntologyAnnotation.empty
            let actual = header.GetUITooltip()
            Expect.isTrue (actual.Length > 0) ""
        testList "ToString()" [
            testCase "Characteristic" (fun () -> 
                let header = CompositeHeader.Characteristic <| OntologyAnnotation.fromString("species", "MS", "MS:0000042")
                let actual = header.ToString()
                let expected = "Characteristic [species]"
                Expect.equal actual expected ""
            )
            testCase "Parameter" (fun () -> 
                let header = CompositeHeader.Parameter <| OntologyAnnotation.fromString("centrifugation time", "MS", "MS:0000042")
                let actual = header.ToString()
                let expected = "Parameter [centrifugation time]"
                Expect.equal actual expected ""
            )
            testCase "Factor" (fun () -> 
                let header = CompositeHeader.Factor <| OntologyAnnotation.fromString("growth temperature", "MS", "MS:0000042")
                let actual = header.ToString()
                let expected = "Factor [growth temperature]"
                Expect.equal actual expected ""
            )
            testCase "Component" (fun () -> 
                let header = CompositeHeader.Component <| OntologyAnnotation.fromString("instrument model", "MS", "MS:0000042")
                let actual = header.ToString()
                let expected = "Component [instrument model]"
                Expect.equal actual expected ""
            )
            testCase "ProtocolType" (fun () -> 
                let header = CompositeHeader.ProtocolType
                let actual = header.ToString()
                let expected = "Protocol Type"
                Expect.equal actual expected ""
            )
            testCase "Input Source" (fun () -> 
                let header = CompositeHeader.Input IOType.Source
                let actual = header.ToString()
                let expected = "Input [Source Name]"
                Expect.equal actual expected ""
            )
            testCase "Output ImageFile" (fun () -> 
                let header = CompositeHeader.Input IOType.ImageFile
                let actual = header.ToString()
                let expected = "Input [Image File]"
                Expect.equal actual expected ""
            )
        ]
        testList "OfHeaderString" [
            testList "TermColumns" [
                testCase "Characteristic" (fun () ->
                    let headerString = "Characteristic [species]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Characteristic <| OntologyAnnotation.fromString("species")
                    Expect.equal actual expected ""
                )
                testCase "Characteristics" (fun () ->
                    let headerString = "Characteristics [species]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Characteristic <| OntologyAnnotation.fromString("species")
                    Expect.equal actual expected ""
                )
                testCase "Characteristics Value" (fun () ->
                    let headerString = "Characteristics Value [species]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Characteristic <| OntologyAnnotation.fromString("species")
                    Expect.equal actual expected ""
                )
                testCase "Parameter" (fun () ->
                    let headerString = "Parameter [centrifugation time]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Parameter <| OntologyAnnotation.fromString("centrifugation time")
                    Expect.equal actual expected ""
                )
                testCase "Parameter Value" (fun () ->
                    let headerString = "Parameter Value [centrifugation time]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Parameter <| OntologyAnnotation.fromString("centrifugation time")
                    Expect.equal actual expected ""
                )
                testCase "Factor" (fun () ->
                    let headerString = "Factor [temperature]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Factor <| OntologyAnnotation.fromString("temperature")
                    Expect.equal actual expected ""
                )
                testCase "Factor Value" (fun () ->
                    let headerString = "Factor Value [temperature]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Factor <| OntologyAnnotation.fromString("temperature")
                    Expect.equal actual expected ""
                )
                testCase "Component" (fun () ->
                    let headerString = "Component [temperature]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Component <| OntologyAnnotation.fromString("temperature")
                    Expect.equal actual expected ""
                )
                testCase "Parameter [Concentration of [nutrient] before start of the experiment]" <| fun _ ->
                    let headerString = "Parameter [Concentration of [nutrient] before start of the experiment]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Parameter <| OntologyAnnotation.fromString("Concentration of [nutrient] before start of the experiment")
                    Expect.equal actual expected ""
            ]
            testList "IoColumns" [
                testCase "Input [Source]" (fun () ->
                    let headerString = "Input [Source]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Input IOType.Source
                    Expect.equal actual expected ""
                )
                testCase "Input [Source Name]" (fun () ->
                    let headerString = "Input [Source Name]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Input IOType.Source
                    Expect.equal actual expected ""
                )
                testCase "Output [Source Name]" (fun () ->
                    let headerString = "Output [Source Name]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Output IOType.Source
                    Expect.equal actual expected ""
                )
                testCase "Input loop" (fun () ->
                    let testForLoop (io:IOType) =      
                        let headerString = io.asInput
                        let actual = CompositeHeader.OfHeaderString headerString
                        let expected = CompositeHeader.Input io
                        Expect.equal actual expected $"""Faulty "to string -> back to IOType" loop for '{io}'"""
                    IOType.All
                    |> Array.iter testForLoop
                )
                testCase "Output loop" (fun () ->
                    let testForLoop (io:IOType) =      
                        let headerString = io.asOutput
                        let actual = CompositeHeader.OfHeaderString headerString
                        let expected = CompositeHeader.Output io
                        Expect.equal actual expected $"""Faulty "to string -> back to IOType" loop for '{io}'"""
                    IOType.All
                    |> Array.iter testForLoop
                )
                testCase "Input FreeText" (fun () ->
                    let headerString = "Input [Anything]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Input (IOType.FreeText "Anything")
                    Expect.equal actual expected ""
                )
                testCase "Input loop FreeText" (fun () ->
                    let headerString = (IOType.FreeText "Anything").asInput
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Input (IOType.FreeText "Anything")
                    Expect.equal actual expected ""
                )
                testCase "Output FreeText" (fun () ->
                    let headerString = "Output [Anything]"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Output (IOType.FreeText "Anything")
                    Expect.equal actual expected ""
                )
                testCase "Output loop FreeText" (fun () ->
                    let headerString = (IOType.FreeText "Anything").asOutput
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Output (IOType.FreeText "Anything")
                    Expect.equal actual expected ""
                )
            ]
            testList "FeaturedColumns" [
                testCase "ProtocolType" (fun () ->
                    let headerString = "Protocol Type"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.ProtocolType
                    Expect.equal actual expected ""
                )
            ]
            testList "SingleColumns" [
                testCase "ProtocolREF" (fun () ->
                    let headerString = "Protocol REF"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.ProtocolREF
                    Expect.equal actual expected ""
                )
                testCase "ProtocolDescription" (fun () ->
                    let headerString = "Protocol Description"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.ProtocolDescription
                    Expect.equal actual expected ""
                )
                testCase "ProtocolUri" (fun () ->
                    let headerString = "Protocol Uri"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.ProtocolUri
                    Expect.equal actual expected ""
                )
                testCase "ProtocolVersion" (fun () ->
                    let headerString = "Protocol Version"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.ProtocolVersion
                    Expect.equal actual expected ""
                )
                testCase "Performer" (fun () ->
                    let headerString = "Performer"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Performer
                    Expect.equal actual expected ""
                )
                testCase "Date" (fun () ->
                    let headerString = "Date"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.Date
                    Expect.equal actual expected ""
                )
            ]
            testList "FreeText" [
                testCase "Anyone important header" (fun () ->
                    let headerString = "Anyone important header"
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.FreeText "Anyone important header"
                    Expect.equal actual expected ""
                )
                testCase "Anyone important header-loop" (fun () ->
                    let headerString = (CompositeHeader.FreeText "Anyone important header").ToString()
                    let actual = CompositeHeader.OfHeaderString headerString
                    let expected = CompositeHeader.FreeText "Anyone important header"
                    Expect.equal actual expected ""
                )
            ]
        ]
    ]

open ARCtrl.ISA

let tests_ToTerm = testList "ToTerm" [
    let testToTerm (ch: CompositeHeader) =
        testCase (sprintf "%s" <| ch.ToString()) <| fun _ ->
            if ch.IsTermColumn then
                match ch with
                | CompositeHeader.Component oa | CompositeHeader.Factor oa | CompositeHeader.Parameter oa | CompositeHeader.Characteristic oa ->
                    Expect.equal (ch.ToTerm()) oa (sprintf "[TERM] ToTerm does not return correct oa: %A" ch)
                | featuredColumn when ch.IsFeaturedColumn -> 
                    let expected = OntologyAnnotation.fromString(featuredColumn.ToString(), tan=featuredColumn.GetFeaturedColumnAccession)
                    Expect.equal (ch.ToTerm()) expected (sprintf "[FEATURED COLUM] ToTerm does not return correct oa: %A" ch)
                | _ -> failwith "Test"
            else
                let expected = OntologyAnnotation.fromString (ch.ToString())
                Expect.equal (ch.ToTerm()) expected (sprintf "[Others] ToTerm does not return correct oa: %A" ch)
    let allHeaders = 
        [
            CompositeHeader.Component OntologyAnnotation.empty
            CompositeHeader.Characteristic OntologyAnnotation.empty
            CompositeHeader.Factor OntologyAnnotation.empty
            CompositeHeader.Parameter OntologyAnnotation.empty
            CompositeHeader.ProtocolType
            CompositeHeader.ProtocolDescription
            CompositeHeader.ProtocolUri
            CompositeHeader.ProtocolVersion
            CompositeHeader.ProtocolREF
            CompositeHeader.Performer
            CompositeHeader.Date
            CompositeHeader.Input IOType.Source
            CompositeHeader.Output IOType.Sample
            CompositeHeader.FreeText "Hello World"
        ]
        |> List.distinct
    testCase "Ensure all headers listed" <| fun _ ->
        Expect.hasLength allHeaders CompositeHeader.Cases.Length ""
    yield! allHeaders |> List.map testToTerm
]

let main = 
    testList "CompositeHeader" [
        tests_iotype
        tests_jsHelper
        tests_compositeHeader
        tests_ToTerm
    ]