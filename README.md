# NGKF - Next Generation Kill Feed

## What?
.NET 7.0 C# console app that uses [Fortnite API C# wapper](https://github.com/Fortnite-API/csharp-wrapper "Fortnite API C# wapper"), [GameOverlay.Net](https://github.com/michel-pi/GameOverlay.Net "GameOverlay.Net") and [Tesseract C# wrapper](https://github.com/charlesw/tesseract "Tesseract C# wrapper") in order to provide real-time player statistics based on ongoing Fortnite match elimination feed

## How?
As pseudo-code this app
1. Scans Fortnite elimination feed with [Tesseract C# wrapper](https://github.com/charlesw/tesseract "Tesseract C# wrapper")
2. Fetches player stats from [Fortnite API](https://fortnite-api.com/ "Fortnite API")
3. Displays the stats in an overlay based on [GameOverlay.Net](https://github.com/michel-pi/GameOverlay.Net "GameOverlay.Net")

There is a config file named *App.config* (or *NGKF.dll.config* when built) that contains parameters like API Key, your player name, OCR tuning parameters and NGKF overlay parameters. Explore the file to get a better understanding.


## Remarks
In order for the overlay work Fornite must not be in full screen mode. However, windowed full screen is ok.
