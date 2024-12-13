using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services
{
    public class ThreadingService
    {
        private const int MaxEntries = 10_000_000;
        private const int ThresholdForEvenNumbers = 2_500_000;

        private readonly ILogger<ThreadingService> _logger;
        private readonly DataContext _context;

        private readonly List<int> _globalNumbers = new();
        private readonly object _lock = new();

        private bool _stopThreads;

        public ThreadingService(ILogger<ThreadingService> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Async methods
        public async Task<int> GetOddNumbersAsync() => await Task.FromResult(_globalNumbers.Count(n => n % 2 != 0));
        public async Task<int> GetEvenNumbersAsync() => await Task.FromResult(_globalNumbers.Count(n => n % 2 == 0));
        public async Task<int> GetTotalNumbersAsync() => await Task.FromResult(_globalNumbers.Count);
        public async Task<List<int>> GetAllNumbersAsync() => await Task.FromResult(_globalNumbers);

        // New method to get the count of prime numbers asynchronously
        public async Task<int> GetPrimeNumbersAsync() => await Task.FromResult(_globalNumbers.Count(n => IsPrime(Math.Abs(n))));

        //public async Task StartAsync()
        //{
        //    _stopThreads = false;

        //    // Start asynchronous tasks
        //    var oddTask = AddOddNumbersAsync();
        //    var primeTask = CalculateAndNegatePrimeNumbersAsync();
        //    var evenTask = StartEvenNumbersAsync();

        //    while (!_stopThreads)
        //    {
        //        lock (_lock)
        //        {
        //            if (_globalNumbers.Count >= ThresholdForEvenNumbers && !evenTask.IsCompleted)
        //            {
        //                // Start even number generation if required
        //                evenTask = StartEvenNumbersAsync();
        //            }

        //            if (_globalNumbers.Count >= MaxEntries)
        //            {
        //                _stopThreads = true;
        //            }
        //        }

        //        await Task.Delay(100); // Prevent CPU overutilization
        //    }

        //    // Wait for all tasks to complete
        //    await Task.WhenAll(oddTask, primeTask, evenTask);

        //    lock (_lock)
        //    {
        //        _globalNumbers.Sort();
        //    }

        //    _logger.LogInformation("Processing completed. Total: {Total}, Odd: {Odd}, Even: {Even}",
        //        await GetTotalNumbersAsync(), await GetOddNumbersAsync(), await GetEvenNumbersAsync());
        //}

        public async Task StartAsync()
        {
            _stopThreads = false;

            var oddTask = AddOddNumbersAsync();
            var primeTask = CalculateAndNegatePrimeNumbersAsync();
            var evenTask = StartEvenNumbersAsync();

            var timeout = Task.Delay(TimeSpan.FromMinutes(5)); // Set a timeout

            while (!_stopThreads)
            {
                lock (_lock)
                {
                    if (_globalNumbers.Count >= ThresholdForEvenNumbers && !evenTask.IsCompleted)
                    {
                        evenTask = StartEvenNumbersAsync();
                    }

                    if (_globalNumbers.Count >= MaxEntries)
                    {
                        _stopThreads = true;
                    }
                }

                await Task.Delay(10);

                // Exit loop if timeout occurs
                if (timeout.IsCompleted)
                {
                    _stopThreads = true;
                    _logger.LogWarning("Processing stopped due to timeout.");
                }
            }

            await Task.WhenAll(oddTask, primeTask, evenTask);

            lock (_lock)
            {
                _globalNumbers.Sort();
            }

            _logger.LogInformation("Processing completed. Total: {Total}, Odd: {Odd}, Even: {Even}",
                await GetTotalNumbersAsync(), await GetOddNumbersAsync(), await GetEvenNumbersAsync());
        }

        private async Task AddOddNumbersAsync()
        {
            var random = new Random();

            while (!_stopThreads)
            {
                lock (_lock)
                {
                    if (_globalNumbers.Count >= MaxEntries) return;

                    var number = random.Next(1, int.MaxValue);
                    if (number % 2 != 0)
                    {
                        _globalNumbers.Add(number);
                    }
                }

                await Task.Delay(10); // Small delay to prevent tight loop
            }
        }

        private async Task CalculateAndNegatePrimeNumbersAsync()
        {
            var number = 2;

            while (!_stopThreads)
            {
                lock (_lock)
                {
                    if (_globalNumbers.Count >= MaxEntries) return;

                    if (IsPrime(number))
                    {
                        _globalNumbers.Add(-number);
                    }

                    number++;
                }

                await Task.Delay(10); // Small delay to prevent tight loop
            }
        }

        private async Task StartEvenNumbersAsync()
        {
            var random = new Random();

            while (!_stopThreads)
            {
                lock (_lock)
                {
                    if (_globalNumbers.Count >= MaxEntries) return;

                    var number = random.Next(1, int.MaxValue);
                    if (number % 2 == 0)
                    {
                        _globalNumbers.Add(number);
                    }
                }

                await Task.Delay(10); // Small delay to prevent tight loop
            }
        }

        private static bool IsPrime(int number)
        {
            if (number < 2) return false;
            for (var i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0) return false;
            }

            return true;
        }

        public async Task SaveAsync()
        {
            try
            {
                var numbersToSave = _globalNumbers.Select(n => new Number
                {
                    Value = n,
                    IsPrime = IsPrime(Math.Abs(n))
                }).ToList();

                await _context.Numbers.AddRangeAsync(numbersToSave);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Data saved successfully to SQLite.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving data to SQLite.");
                throw;
            }
        }
    }
}
