namespace CityGuessBot.Arcgis

open System.Net
open System.Net.Http

module Geometry =
    type Location = {
        X: double
        Y: double
    }
    
    type Geometry = {
        geometryType: string;
        geometry: Location;
    }
    
    type DistanceResult = {
        distance: float
    }
    
    let distance (l1: Location) (l2: Location) =
        task {
           try
               let j1 = Newtonsoft.Json.JsonConvert.SerializeObject {geometryType="esriGeometryPoint"; geometry=l1}
               let j2 = Newtonsoft.Json.JsonConvert.SerializeObject {geometryType="esriGeometryPoint"; geometry=l2}
               let url = sprintf "https://tasks.arcgisonline.com/arcgis/rest/services/Geometry/GeometryServer/distance?sr=4326&geometry1=%s&geometry2=%s&geodesic=true&distanceUnit=9001&f=json" j1 j2
               use client = new HttpClient()
               let! response = client.GetStringAsync url
               let d = Newtonsoft.Json.JsonConvert.DeserializeObject<DistanceResult> response
               return Some(d.distance)
           with
           | _ -> return None
        }
    
    
module TimeZoneService =
    
    type Attribute = {
        ZONE: double
    }
    type Feature = {
        Attributes: Attribute
        Geometry: Geometry.Location
    }
    type Result = {
        Features: Feature List
    }
    
    let query (p:Geometry.Location) =
        task {
            use client = new HttpClient()
            let url = sprintf "https://services.arcgis.com/P3ePLMYs2RVChkJx/ArcGIS/rest/services/World_Time_Zones/FeatureServer/0/query?where=1%%3D1&objectIds=&time=&geometry=%f%%2C%f&geometryType=esriGeometryPoint&inSR=4326&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&returnGeodetic=false&outFields=ZONE&returnGeometry=false&returnCentroid=false&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=json&token=" p.X p.Y
            let! response = client.GetStringAsync (url)
            let results = Newtonsoft.Json.JsonConvert.DeserializeObject<Result> (response)
            return results.Features |> List.tryHead
        }

module FeatureService =
    
    
    type Attribute = {
        CITY_NAME: string
    }
    type Feature = {
        Attributes: Attribute
        Geometry: Geometry.Location
    }
    type Result = {
        Features: Feature list
    }
    let query (oid: int) =
        task {
            use client = new HttpClient()
            let url = sprintf "https://services.arcgis.com/P3ePLMYs2RVChkJx/ArcGIS/rest/services/World_Cities/FeatureServer/0/query?where=&objectIds=%i&time=&geometry=&geometryType=esriGeometryEnvelope&inSR=&spatialRel=esriSpatialRelIntersects&resultType=none&distance=0.0&units=esriSRUnit_Meter&returnGeodetic=false&outFields=CITY_NAME&returnGeometry=true&featureEncoding=esriDefault&multipatchOption=xyFootprint&maxAllowableOffset=&geometryPrecision=&outSR=4326&datumTransformation=&applyVCSProjection=false&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnExtentOnly=false&returnQueryGeometry=false&returnDistinctValues=false&cacheHint=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&returnZ=false&returnM=false&returnExceededLimitFeatures=true&quantizationParameters=&sqlFormat=none&f=pjson&token=" oid
            let! response = client.GetStringAsync (url)
            let results = Newtonsoft.Json.JsonConvert.DeserializeObject<Result> (response)
            return results.Features |> List.tryHead
        }

module Geocode =
    
    type Candidate = {
        Address: string;
        Score: float
        Location: Geometry.Location
    }
    
    type GeocodeResults = {
        Candidates: Candidate list;
    }
    
    let geocode (name: string) (token:string) =
        task {
            use client = new HttpClient()
            let url = sprintf "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?singleline=%s&f=json&token=%s" name token
            let! response = client.GetStringAsync (url)
            let results = Newtonsoft.Json.JsonConvert.DeserializeObject<GeocodeResults> (response)
            return results.Candidates |> List.tryHead
        }
        
    let candidateToString (c:Candidate): string=
        // TODO: make a link to a map that zooms to this point
        sprintf "%s (at %f, %f) https://moraveclabsllc.maps.arcgis.com/apps/instant/basic/index.html?appid=5bde2195c1444f4997542e81f89857f3&center=%f,%f&level=12" c.Address c.Location.Y c.Location.X c.Location.X c.Location.Y
