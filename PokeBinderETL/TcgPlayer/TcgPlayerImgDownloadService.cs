using PokeBinder.ETL.Db.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeBinder.ETL.TcgPlayer;

public record DownloadedImage(int TcgPlayerId, string FilePath, bool attemptHasFailed = false);

public class TcgPlayerImgDownloadService
{
    private readonly IHttpClientFactory _factory;
    private readonly HttpClient _client;
    public const string W200 = "_200w.jpg";
    public const string W1000 = "_in_1000x1000.jpg";

    public TcgPlayerImgDownloadService(IHttpClientFactory httpClientFactory)
    {
        _factory = httpClientFactory;
        _client = _factory.CreateClient("TcgPlayerCDN");
        //https://tcgplayer-cdn.tcgplayer.com/product/633036_in_1000x1000.jpg
        //https://tcgplayer-cdn.tcgplayer.com/product/633036_200w.jpg
    }

    public async Task<List<DownloadedImage>> DownloadImagesFromTcgPlayerIdList(ConcurrentBag<CardSimpleModel> cardList, string directorySet)
    {
        var errors = new ConcurrentBag<string>();
        var downloadResult = new ConcurrentBag<DownloadedImage>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };

        await Parallel.ForEachAsync(cardList, options, async (simpleCard, ct) =>
        {
            var result = await DownloadImageFromTcgPlayer(simpleCard, directorySet, errors);
            downloadResult.Add(result);
        });

        var logPath = Path.Combine(directorySet, "download_errors.log");
        await File.AppendAllLinesAsync(logPath, errors.Count > 0 ? errors : new List<string>() { "No errors detected in the set." });

        return downloadResult.ToList();
    }

    public async Task<DownloadedImage> DownloadImageFromTcgPlayer(CardSimpleModel simpleCard, string directorySet, ConcurrentBag<string> errors)
    {
        try
        {
            var filePath = await DownloadImage(simpleCard.TcgPlayerId, directorySet, false);
            return new DownloadedImage(simpleCard.TcgPlayerId, filePath);
        }
        catch(HttpRequestException)
        {
        }

        try
        {
            var filePath = await DownloadImage(simpleCard.TcgPlayerId, directorySet, true);
            return new DownloadedImage(simpleCard.TcgPlayerId, filePath);
        }
        catch (HttpRequestException e) {
            errors.Add($"{simpleCard.TcgPlayerId},{e.Message}");
            return new DownloadedImage(simpleCard.TcgPlayerId, string.Empty, attemptHasFailed: true);
        }
    }

    private async Task<string> DownloadImage(int tcgPlayerId, string directorySet, bool isFallback)
    {
        var imageSizeTermination = isFallback ? W200 : W1000;

        string cDriveRoot = @"C:\";
        Directory.SetCurrentDirectory(cDriveRoot);
        Directory.CreateDirectory(directorySet);

        using Stream downloadStreamSmallImg = await _client.GetStreamAsync($"product/{tcgPlayerId}{imageSizeTermination}");

        var filePath = $"/{directorySet}/{tcgPlayerId}{imageSizeTermination}";

        // Create a FileStream to write the data to the destination file
        using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

        // Copy the download stream to the file stream
        await downloadStreamSmallImg.CopyToAsync(fileStream);

        return $"/{directorySet}/{tcgPlayerId}{imageSizeTermination}";
    }
}
