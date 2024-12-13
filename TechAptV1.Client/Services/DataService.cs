using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services;

public sealed class DataService
{
    private readonly ILogger<DataService> _logger;
    private readonly DataContext _context;

    public DataService(ILogger<DataService> logger, DataContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Saves a list of numbers to the database.
    /// </summary>
    /// <param name="dataList">The list of numbers to save.</param>
    public async Task SaveAsync(List<Number> dataList)
    {
        try
        {
            _logger.LogInformation("Starting to save data to SQLite.");

            if (dataList == null || !dataList.Any())
            {
                _logger.LogWarning("The data list is empty. Nothing to save.");
                return;
            }

            await _context.Numbers.AddRangeAsync(dataList);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Data saved successfully to SQLite.");
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update failed while saving data.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while saving data.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all numbers from the database.
    /// </summary>
    /// <returns>A list of numbers stored in the database.</returns>
    public async Task<List<Number>> GetAllAsync(int? limit = null)
    {
        try
        {
            _logger.LogInformation("Retrieving numbers from SQLite.");

            IQueryable<Number> query = _context.Numbers.AsNoTracking();

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var numbers = await query.ToListAsync();

            _logger.LogInformation("Successfully retrieved {Count} numbers from SQLite.", numbers.Count);
            return numbers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving data.");
            throw;
        }
    }

    /// <summary>
    /// Exports the list of numbers to an XML file and saves it to disk.
    /// </summary>
    /// <param name="filePath">The full path to save the XML file.</param>
    /// <returns>The file path of the saved XML.</returns>
    public async Task<string> ExportToXmlAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Starting XML export process.");

            var numbers = await GetAllAsync();

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }

            // Serialize to XML and save to file
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                var serializer = new XmlSerializer(typeof(List<Number>));
                serializer.Serialize(fileStream, numbers);
            }

            _logger.LogInformation("Data successfully exported to XML at {FilePath}.", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while exporting to XML.");
            throw;
        }
    }

    /// <summary>
    /// Exports the list of numbers to a binary file.
    /// </summary>
    /// <returns>Serialized binary data as a byte array.</returns>
    //public async Task<byte[]> ExportToBinaryAsync()
    //{
    //    try
    //    {
    //        _logger.LogInformation("Starting binary export process.");

    //        var numbers = await GetAllAsync();

    //        using var memoryStream = new MemoryStream();
    //        using var binaryWriter = new BinaryWriter(memoryStream);

    //        foreach (var number in numbers)
    //        {
    //            binaryWriter.Write(number.Id);
    //            binaryWriter.Write(number.Value);
    //            binaryWriter.Write(number.IsPrime);
    //        }

    //        _logger.LogInformation("Data successfully exported to binary.");
    //        return memoryStream.ToArray();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error occurred while exporting to binary.");
    //        throw;
    //    }
    //}

    public async Task ExportToBinaryAsync(string filePath)
    {
        try
        {
            var numbers = await GetAllAsync();

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var binaryWriter = new BinaryWriter(fileStream);

            foreach (var number in numbers)
            {
                binaryWriter.Write(number.Id);
                binaryWriter.Write(number.Value);
                binaryWriter.Write(number.IsPrime);
            }

            _logger.LogInformation("Data successfully exported to binary file at {FilePath}.", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while exporting to binary file.");
            throw;
        }
    }
}
