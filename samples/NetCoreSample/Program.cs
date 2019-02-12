﻿using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NetCoreSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(x => x.AddConsole())
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<HostOptions>(option =>
                    {
                        option.ShutdownTimeout = TimeSpan.FromMilliseconds(5000);
                    });

                    services.AddHangfire(configuration => configuration
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseIgnoredAssemblyVersionTypeResolver()
                        .UseSqlServerStorage(
                            @"Server=.\;Database=Hangfire.Sample;Trusted_Connection=True;", new SqlServerStorageOptions
                            {
                                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                                QueuePollInterval = TimeSpan.FromTicks(1),
                                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(1)
                            }));

                    services.AddHostedService<RecurringJobsService>();
                    services.AddHangfireServer();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class RecurringJobsService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                RecurringJob.AddOrUpdate("seconds", () => Console.WriteLine("Hello, seconds!"), "*/15 * * * * *");
                RecurringJob.AddOrUpdate(() => Console.WriteLine("Hello, world!"), Cron.Minutely);
                RecurringJob.AddOrUpdate("hourly", () => Console.WriteLine("Hello"), "25 15 * * *");
                RecurringJob.AddOrUpdate("neverfires", () => Console.WriteLine("Can only be triggered"), "0 0 31 2 *");

                RecurringJob.AddOrUpdate("Hawaiian", () => Console.WriteLine("Hawaiian"),  "15 08 * * *", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time"));
                RecurringJob.AddOrUpdate("UTC", () => Console.WriteLine("UTC"), "15 18 * * *");
                RecurringJob.AddOrUpdate("Russian", () => Console.WriteLine("Russian"), "15 21 * * *", TimeZoneInfo.Local);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
}