module CityGuessBot.Program

open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open CityGuessBot.Arcgis
open DSharpPlus
open DSharpPlus.Entities
open DSharpPlus.SlashCommands
open Microsoft.Extensions.Configuration

let appConfig =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory ())
        .AddJsonFile("appsettings.json", true, true)
        .Build()
        
// Get today's date as yyyyMMdd and convert to an integer
// Use that integer to generate a Random() object and
// Get an integer between 1 and 2500 (an OID for the city layer)
// This way we create a deterministic method for computing the city
// that we are guessing

let cityOidList = [ 2; 3; 14; 15; 40; 42; 47; 69; 85; 86; 88; 96; 109; 118; 125; 144; 146; 148; 157; 158; 160; 162; 165; 169; 178; 186; 187; 207; 208; 215; 216; 226; 235; 237; 238; 241; 244; 250; 255; 256; 259; 260; 261; 276; 329; 353; 365; 407; 436; 450; 451; 457; 459; 478; 492; 496; 505; 509; 514; 524; 525; 538; 572; 600; 608; 637; 649; 665; 677; 683; 704; 708; 715; 718; 724; 729; 750; 779; 781; 795; 796; 807; 820; 822; 824; 832; 836; 842; 848; 852; 856; 861; 865; 886; 911; 919; 964; 966; 971; 973; 985; 1008; 1011; 1018; 1030; 1031; 1032; 1035; 1036; 1037; 1040; 1041; 1044; 1045; 1047; 1048; 1065; 1072; 1077; 1078; 1082; 1085; 1086; 1087; 1094; 1098; 1101; 1109; 1111; 1112; 1115; 1120; 1121; 1122; 1124; 1141; 1143; 1152; 1166; 1176; 1179; 1210; 1212; 1214; 1243; 1252; 1258; 1267; 1274; 1277; 1293; 1300; 1309; 1318; 1332; 1336; 1337; 1338; 1339; 1340; 1341; 1342; 1343; 1344; 1345; 1346; 1347; 1351; 1352; 1353; 1354; 1355; 1357; 1358; 1359; 1360; 1361; 1362; 1363; 1364; 1365; 1366; 1367; 1368; 1369; 1370; 1371; 1372; 1373; 1374; 1377; 1379; 1380; 1381; 1383; 1388; 1390; 1391; 1392; 1393; 1400; 1405; 1407; 1408; 1414; 1419; 1440; 1442; 1455; 1457; 1460; 1461; 1462; 1463; 1464; 1465; 1466; 1469; 1470; 1473; 1479; 1481; 1482; 1486; 1494; 1508; 1517; 1522; 1525; 1532; 1579; 1580; 1609; 1611; 1615; 1643; 1658; 1665; 1672; 1673; 1690; 1694; 1737; 1752; 1846; 1848; 1850; 1860; 1871; 1874; 1875; 1883; 1885; 1887; 1888; 1893; 1910; 1921; 1937; 1957; 1973; 2029; 2042; 2082; 2124; 2144; 2145; 2159; 2160; 2164; 2171; 2181; 2191; 2195; 2204; 2214; 2216; 2225; 2226; 2230; 2241; 2246; 2249; 2254; 2256; 2257; 2260; 2266; 2270; 2278; 2297; 2305; 2312; 2332; 2342; 2344; 2348; 2353; 2364; 2382; 2384; 2394; 2397; 2427; 2435; 2436; 2438; 2446; 2457; 2462; 2469; 2482; 2487; 2496; 2501; 2502; 2504; 2530]

let getCityId (d: DateTime): int =
    let s = d.ToString("yyyyMMdd")
    let is, i = Int32.TryParse (s)
    let seed =
        match is with
        | false -> 0
        | true -> i
    let r = Random(seed)
    cityOidList[r.Next(0, List.length cityOidList)]
    
let getCityIdToday () =
    getCityId DateTime.UtcNow
    
let getInfomapticUrl (oid: int): string =
    sprintf "https://app.infomaptic.com/api/report/4fd0db9f47b640e1ad1268abe0eaf343/view?key=%i&refreshCache=false&type=png" oid
    
let getInfomapticImage (oid: int) =
    task {
        use client = new HttpClient()
        let url = getInfomapticUrl oid
        let! response = client.GetAsync (url)
        let! img = response.Content.ReadAsStreamAsync ()
        return img
    }
    
let getCityFeature (oid:int) =
    task {
        return! FeatureService.query oid
    }
    
let computeDistanceMsg (l1: FeatureService.Feature option) (l2: Geocode.Candidate option) =
    task {
        let! d =
            match l1 with
            | Some a -> match l2 with
                        | Some b -> Geometry.distance a.Geometry b.Location
                        | _ -> Task.FromResult None
            | _ -> Task.FromResult None
            
        // If there was a distance, check out what timezone each point is in
        let! tz1 = match l1 with
                    | Some a -> TimeZoneService.query a.Geometry
                    | _ -> Task.FromResult None
        let! tz2 = match l2 with
                    | Some a -> TimeZoneService.query a.Location
                    | _ -> Task.FromResult None
                    
        // if we got two timezones, check out the "distance" between them
        let tzd = match tz1 with
                    | Some t1 -> match tz2 with
                                    | Some t2 -> Some (Math.Abs (t2.Attributes.ZONE - t1.Attributes.ZONE))
                                    | _ -> None
                    | _ -> None
                    
        // we now have a timezone delta (probably) so do that!
        
        return! match tzd with
                | Some td  when td > 0 && td < 1 -> Task.FromResult (sprintf "So close, only %.1f timezones away!" td)
                | Some td  when td = 1 -> Task.FromResult (sprintf "So close, only %.0f timezone away!" td)
                | Some td  when td > 1 && td < 3 -> Task.FromResult (sprintf "So close, only %.0f timezones away!" td)
                | Some td  when td >= 3 -> Task.FromResult (sprintf ":clock1::clock2::clock3:Not that close: %.0f timezones away..." td)
                | _ -> match d with
                        | Some i -> Task.FromResult ( sprintf ":person_doing_cartwheel: You are in the same timezone! Only %.0f kilometers from the answer" (i/1000.0))
                        | _ -> Task.FromResult ("And I can't compute a distance for that.")
    }
    
let levenshtein word1 word2 =
    let preprocess = fun (str : string) -> str.ToLower().ToCharArray()
    let chars1, chars2 = preprocess word1, preprocess word2
    let m, n = chars1.Length, chars2.Length
    let table : int[,] = Array2D.zeroCreate (m + 1) (n + 1)
    for i in 0..m do
        for j in 0..n do
            match i, j with
            | i, 0 -> table.[i, j] <- i
            | 0, j -> table.[i, j] <- j
            | _, _ ->
                let delete = table.[i-1, j] + 1
                let insert = table.[i, j-1] + 1
                //cost of substitution is 2
                let substitute = 
                    if chars1.[i - 1] = chars2.[j - 1] 
                        then table.[i-1, j-1] //same character
                        else table.[i-1, j-1] + 2
                table.[i, j] <- List.min [delete; insert; substitute]
    //table.[m, n], table //return tuple of the table and distance
    table.[m, n]
    
let compareStringsMsg (s1: string) (s2: string): string option =
    //match String.Compare (s1, s2, true) with
    match levenshtein s1 s2 with
    | t when t < 2 -> Some(":partying_face: :partying_face: You got it! :fireworks: :fireworks:")
    | t when t < 5 -> Some("You are :pinching_hand: close! check your spelling!")
    | t when t < 7 -> Some(":speech_balloon: Close, I think your spelling might be a little off... or maybe the word is just similar!")
    | _ -> None
    
let processGuess (guess: string) =
    task {
        let! g = Geocode.geocode guess appConfig.["ArcgisToken"]
        let oid = getCityIdToday ()

        let m =
            match g with
            | None -> "I am ashamed to say I don't know where that is! :parachute:"
            | Some s -> sprintf ":map: I found %s" (Geocode.candidateToString s)
        let! city = getCityFeature oid
        let! m' =
            match city with
            | None -> Task.FromResult (sprintf "%s%sI couldn't find the answer right now... try later?" m Environment.NewLine)
            | Some s -> match compareStringsMsg s.Attributes.CITY_NAME guess with
                        | Some a -> Task.FromResult a
                        | None -> computeDistanceMsg (Some(s)) g
        return sprintf "%s%s%s" m Environment.NewLine m'
    }
    
type CityBot () =
    inherit  ApplicationCommandModule()
    
    [<SlashCommand ("city", "Get today's city to guess", true)>]
    member this.City (ctx: InteractionContext): Task =
        task {
            //do! ctx.TriggerTypingAsync()
            do! ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            let m = getCityIdToday ()
            let url = getInfomapticUrl m
            let! _ = ctx.Client.SendMessageAsync(ctx.Channel, "Let me find today's city, be right there!")
            //do! ctx.TriggerTypingAsync()
            use! img = getInfomapticImage m
            //let! _ = ctx.Client.SendMessageAsync(ctx.Channel, url)
            let! _ = ctx.EditResponseAsync((new DiscordWebhookBuilder()).WithContent(url))
            return ()
        } :> Task
        
    [<SlashCommand ("guess", "Guess the current city", true)>]
    member this.Guess (ctx: InteractionContext, [<Option("city", "the name of the city to guess")>]guess: string): Task =
        task {
            let city =
                match guess with
                | null -> None
                | "" -> None
                | c -> Some c
                
            let! msg =
                match city with
                | None -> Task.FromResult (":mag: Oh, I didn't see your guess there!")
                | Some a -> processGuess a
            
            //let! _ = ctx.Client.SendMessageAsync(ctx.Channel, msg)
            let! _ = ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, (new DiscordInteractionResponseBuilder()).WithContent(msg))
            return ()
        } :> Task
        
[<EntryPoint>]
let main argv =
    printfn "Starting..."
    let token = appConfig.["DiscordToken"]
    let config = DiscordConfiguration ()
    config.Token <- token
    config.TokenType <- TokenType.Bot
    //config.MinimumLogLevel <- LogLevel.Debug
    
    let discord = new DiscordClient (config)
    
    let commandsConfig = SlashCommandsConfiguration ()

    let commands = discord.UseSlashCommands(commandsConfig)
    //let b =
    commands.RegisterCommands<CityBot>(877970379686182992UL)
    commands.RegisterCommands<CityBot>(709744990879744072UL)
    
    discord.ConnectAsync ()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
    Task.Delay (-1)
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
    1