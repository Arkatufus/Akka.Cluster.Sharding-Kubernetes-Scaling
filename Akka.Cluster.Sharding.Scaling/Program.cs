// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding.Delivery;
using Akka.Hosting;
using Akka.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Akka.Cluster.Sharding.Scaling;

public static class Program
{
    private const string FrontEndRole = "frontend";
    private const string BackEndRole = "backend";
    
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureHostConfiguration(builder =>
            {
                builder.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
                services.AddAkka("shopping-cart", builder =>
                {
                    var isFrontend = Environment.GetEnvironmentVariable("IS_FRONTEND")?.ToLowerInvariant() == "true";
                    
                    var extractor = new MessageExtractor(10);
                    if (isFrontend)
                        builder
                            .BootstrapNetwork(context.Configuration, FrontEndRole)
                            .WithShardRegionProxy<Customer>(nameof(Customer), BackEndRole, extractor)
                            .AddStartup((system, registry) =>
                            {
                                var cluster = Cluster.Get(system);
                                var shardRegionProxy = registry.Get<Customer>();
                                cluster.RegisterOnMemberUp(() =>
                                {
                                    var producerId = "ProducerId1" + MurmurHash.StringHash(cluster.SelfAddress.ToString());
                        
                                    var shardingProducerController = system.ActorOf(ShardingProducerController.Create<Customer.ICustomerCommand>(
                                        producerId, 
                                        shardRegionProxy, 
                                        Option<Props>.None, 
                                        ShardingProducerController.Settings.Create(system)), "shardingProducerController-1");
                        
                                    var producer = system.ActorOf(Props.Create(() => new Producer()), "msg-producer");
                                    shardingProducerController.Tell(new ShardingProducerController.Start<Customer.ICustomerCommand>(producer));
                                });
                            });
                    else
                        builder
                            .BootstrapNetwork(context.Configuration, BackEndRole)
                            .WithShardRegion<Customer>(
                                nameof(Customer), 
                                (system, _) => e =>
                                    ShardingConsumerController.Create<Customer.ICustomerCommand>( 
                                        c => Props.Create(() => new Customer(e,c)), 
                                        ShardingConsumerController.Settings.Create(system)), 
                                extractor, 
                                new ShardOptions
                                {
                                    Role = BackEndRole, 
                                    PassivateIdleEntityAfter = TimeSpan.FromMinutes(1)
                                });
                });
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
    
}
