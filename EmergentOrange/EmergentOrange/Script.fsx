open System.Net
open System.IO
open System.Drawing

#r @"..\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll"
open FSharp.Data
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.Linq.dll"
open System.Xml.Linq

// Flickr API key
let apiKey = "YOUR_FLICKR_API_KEY_GOES_HERE"

let pictureUrl farmId serverId (pictureId:int64) secret =
    sprintf "https://farm%i.staticflickr.com/%i/%i_%s.jpg" farmId serverId pictureId secret

let search (something:string) =
    sprintf 
        "https://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=%s&text=%s&format=rest"
        apiKey
        something

let getResponse (req:WebRequest) =
    req.GetResponse () :?> HttpWebResponse

let asText (resp:HttpWebResponse) =
    use stream = resp.GetResponseStream ()
    use reader = new StreamReader (stream)
    reader.ReadToEnd ()

let flickrSearch (searchTerms:string) =
    search searchTerms
    |> WebRequest.Create
    |> getResponse
    |> asText

type SearchResults = XmlProvider<"https://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=YOUR_FLICKR_API_KEY_GOES_HERE&text=cats&format=rest">

let photoUrl (img:SearchResults.Photo) = pictureUrl img.Farm img.Server img.Id img.Secret.Value

let grabImage (imgUrl:string) =   
    let req = imgUrl |> HttpWebRequest.Create
    use res = req.GetResponse () :?> HttpWebResponse
    use str = res.GetResponseStream ()
    str |> Image.FromStream

let resize (img:Image) = new Bitmap(img, Size(100,100)) :> Image
let saveAs (where:string) (img:Image) = img.Save where

let save folder name (img:Image) = 
    let where = folder + name + ".bmp"    
    img.Save where

let folder = @"C:\users\mathias brandewinder\documents\orange\"

// TODO increase diversity by avoiding duplicate authors
let createSample searchTerm =
    searchTerm 
    |> search 
    |> SearchResults.Load
    |> fun imgs ->
        let photos = imgs.Photos.Photos
        photos.[0..8] 
        |> Array.iteri (fun i url -> 
            let name = searchTerm + (string i)
            url |> photoUrl |> grabImage |> resize |> save folder name) 

let averageColor (colors:Color seq) =
    let red = colors |> Seq.averageBy (fun c -> float c.R)
    let green = colors |> Seq.averageBy (fun c -> float c.G)
    let blue = colors |> Seq.averageBy (fun c -> float c.B)
    Color.FromArgb(int red, int green, int blue)

let average (images:Bitmap[]) =
    let width,height = 100,100
    let result = new Bitmap(width,height)
    for col in 0 .. (width - 1) do
        for row in 0 .. (height - 1) do
            let color = 
                [ for img in images -> img.GetPixel(col,row) ]
                |> averageColor
            result.SetPixel(col,row,color)
    result

let searches = [
    "cat"
    "sunny"
    "trumpet"
    "athlete"
    "beefsteak"
    "satanism"
    "torch"
    "cougar"
    "dog"
    "moon"
    "paris"
    "tulip"
    "zoo"
    "mountain"
    "socks"
    "wine"
    "cute"
    "speed" ]

searches |> List.iter createSample

let aggregates () =
    [0..8]
    |> List.iter (fun i ->
        searches
        |> List.map (fun term -> folder + term + (string i) + ".bmp")
        |> List.map Bitmap.FromFile
        |> List.map (fun img -> img :?> Bitmap)
        |> List.toArray
        |> average
        |> fun image -> image.Save (@"C:\users\mathias brandewinder\documents\orange\merged" + (string i) + ".bmp"))

let tile () =
    let width,height = 100,100
    let result = new Bitmap(3 * width, 3 * height)
    for i in 0 .. 8 do
        let image = Bitmap.FromFile (@"C:\users\mathias brandewinder\documents\orange\merged" + (string i) + ".bmp")
        let image = image :?> Bitmap
        let imgrow = i / 3
        let imgcol = i - 3 * imgrow
        for col in 0 .. (width - 1) do
            for row in 0 .. (height - 1) do
                let color = image.GetPixel(col,row)
                result.SetPixel(imgcol * width + col, imgrow * height + row, color)
    result.Save (@"C:\users\mathias brandewinder\documents\orange\final" + ".bmp")