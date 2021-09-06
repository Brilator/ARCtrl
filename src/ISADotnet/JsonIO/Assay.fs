namespace ISADotNet.Json

open ISADotNet
open System.Text.Json
open System.IO

module Assay = 

    let fromString (s:string) = 
        JsonSerializer.Deserialize<Assay>(s,JsonExtensions.options)

    let toString (a:Assay) = 
        JsonSerializer.Serialize<Assay>(a,JsonExtensions.options)

    let fromFile (path : string) = 
        File.ReadAllText path 
        |> fromString

    let toFile (path : string) (a:Assay) = 
        File.WriteAllText(path,toString a)