﻿// -----------------------------------------------------------------------
//  <copyright file="AkkaBootstrap.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Sockets;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding.Scaling.Config;
using Akka.Configuration;
using Akka.Discovery.Config.Hosting;
using Akka.Discovery.KubernetesApi;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using ClusterOptions = Akka.Cluster.Sharding.Scaling.Config.ClusterOptions;

namespace Akka.Cluster.Sharding.Scaling;

public static class AkkaBootstrap
{
    public static AkkaConfigurationBuilder BootstrapNetwork(
        this AkkaConfigurationBuilder builder,
        IConfiguration configuration, 
        string role)
    {
        #region Environment variable setup
        
        var options = GetEnvironmentVariables(configuration);
        var remoteOptions = new RemoteOptions
        {
            HostName = "0.0.0.0",
            Port = 5213, 
        };
        var clusterOptions = new Akka.Cluster.Hosting.ClusterOptions
        {
            MinimumNumberOfMembers = 4,
            SeedNodes = new[] { "akka.tcp://shopping-cart@localhost:5213" },
            Roles = new[] { role }
        };
        var pbmOptions = new PetabridgeCmdOptions
        {
            Host = "0.0.0.0",
            Port = options.PbmPort
        };
        var managementOptions = new AkkaManagementOptions
        {
            HostName = options.Ip ?? Dns.GetHostName(),
            Port = options.Discovery.ManagementPort,
        };
        
        var bootstrapOptions = new ClusterBootstrapOptions
        {
            ContactPointDiscovery =
            {
                ServiceName = options.Discovery.ServiceName, 
                PortName = options.Discovery.PortName, 
                StableMargin = TimeSpan.FromSeconds(5), 
                ContactWithAllContactPoints = true
            }
        };
        
        // Clear seed nodes if we're using Config or Kubernetes Discovery
        if (options.StartupMethod is StartupMethod.ConfigDiscovery or StartupMethod.KubernetesDiscovery )
        {
            clusterOptions.SeedNodes = null;
            options.Seeds = null;
        }
        
        // Setup remoting
        // Reads environment variable CLUSTER__PORT
        if (options.Port is not null)
        {
            Console.WriteLine($"From environment: PORT: {options.Port}");
            remoteOptions.Port = options.Port;
        }
        else
        {
            Console.WriteLine($"From environment: PORT: NULL. Using tcp port: {remoteOptions.Port}");
        }
        
        // Reads environment variable CLUSTER__IP
        if (options.Ip is not null)
        {
            var ip = options.Ip.Trim();
            remoteOptions.PublicHostName = ip;
            Console.WriteLine($"From environment: IP: {ip}");
        }
        else if (options.IsDocker)
        {
            var host = Dns.GetHostName();
            Console.WriteLine($"From environment: IP NULL, running in docker container, defaulting to: {host}");
            remoteOptions.PublicHostName = host.ToHocon();
        }
        else
        {
            Console.WriteLine("From environment: IP NULL, not running in docker container, defaulting to: localhost");
            remoteOptions.PublicHostName = "localhost";
        }
        
        if (options.Seeds is not null)
        {
            var seeds = string.Join(",", options.Seeds.Select(s => s.ToHocon()));
            clusterOptions.SeedNodes = options.Seeds;
            Console.WriteLine($"From environment: SEEDS: [{seeds}]");
        }
        else
        {
            Console.WriteLine($"From environment: SEEDS: NULL, using seeds: [{string.Join(", ", clusterOptions.SeedNodes ?? new []{ "" })}]");
        }
        
        #endregion
        
        switch (options.StartupMethod)
        {
            case StartupMethod.SeedNodes:
                // No need to setup seed based cluster
                Console.WriteLine("From environment: Forming cluster using seed nodes");
                return builder
                    .AddHocon(configuration.GetSection("Akka"), HoconAddMode.Prepend)
                    .WithRemoting(remoteOptions)
                    .WithClustering(clusterOptions)
                    .AddPetabridgeCmd(pbmOptions, cmd =>
                    {
                        // enable cluster management commands
                        cmd.RegisterCommandPalette(ClusterCommands.Instance);
                        cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    });
                
            case StartupMethod.ConfigDiscovery:
                Console.WriteLine("From environment: Forming cluster using Akka.Discovery.Config");
                
                if (options.Discovery.ConfigEndpoints is null)
                    throw new ConfigurationException(
                        "Cluster start up is set to configuration discovery but discovery endpoints is null");
                
                var endpoints = string.Join(",", options.Discovery.ConfigEndpoints.Select(s => s.ToHocon()));
                Console.WriteLine($"From environment: Using config based discovery endpoints: [{endpoints}]");
                
                builder.WithConfigDiscovery(new ConfigServiceDiscoveryOptions
                {
                    Services = new List<Service>
                    {
                        new()
                        {
                            Name = options.Discovery.ServiceName,
                            Endpoints = options.Discovery.ConfigEndpoints.ToArray()
                        }
                    }
                });
                break;
                
            case StartupMethod.KubernetesDiscovery:
                Console.WriteLine("From environment: Forming cluster using Akka.Discovery.KubernetesApi");

                var hostName = Dns.GetHostName();
                var hostIp = Dns.GetHostAddresses(hostName, AddressFamily.InterNetwork).First().ToString();
                
                remoteOptions.HostName = hostIp;
                remoteOptions.PublicHostName = hostIp;
                managementOptions.HostName = string.Empty;
                bootstrapOptions.ContactPointDiscovery.RequiredContactPointsNr = 3;
                builder
                    .WithKubernetesDiscovery(opt =>
                    {
                        opt.PodNamespace = options.Discovery.ServiceName;
                        opt.PodLabelSelector = options.Discovery.LabelSelector;
                    })
                    .AddHocon(KubernetesDiscovery.DefaultConfiguration(), HoconAddMode.Append);
                break;
                
            default:
                throw new ConfigurationException($"From environment: Unknown startup method: {options.StartupMethod}");
        }
        
        builder
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions)
            .WithAkkaManagement(managementOptions)
            // Not explicitly setting the liveness provider. The Akka.Remote port
            // is usually an effective-enough tool for this.
            .WithHealthCheck(opt =>
            {
                opt.Readiness.Transport = HealthCheckTransport.Tcp;
                opt.Readiness.TcpPort = options.ReadinessPort;
            })
            // Add Akka.Management.Cluster.Bootstrap support
            .WithClusterBootstrap(bootstrapOptions, autoStart: true)
            .AddPetabridgeCmd(pbmOptions, cmd =>
            {
                // enable cluster management commands
                cmd.RegisterCommandPalette(ClusterCommands.Instance);
                cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
            });
        
        return builder;
    }
    
    private static ClusterOptions GetEnvironmentVariables(IConfiguration configuration)
    {
        var section = configuration.GetSection("Cluster");
        if(!section.GetChildren().Any())
        {
            Console.WriteLine("Skipping environment variable bootstrap. No 'CLUSTER' section found");
            return new ClusterOptions();
        }
        
        var options = section.Get<ClusterOptions>();
        if (options is null)
        {
            Console.WriteLine($"Skipping environment variable bootstrap. Could not bind IConfiguration to '{nameof(ClusterOptions)}'");
            return new ClusterOptions();
        }
        
        return options;
    }
}