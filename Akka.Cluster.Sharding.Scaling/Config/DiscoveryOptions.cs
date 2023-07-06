// -----------------------------------------------------------------------
//  <copyright file="ConfigDiscoveryOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Cluster.Sharding.Scaling.Config;

public sealed class DiscoveryOptions
{
    public string? ServiceName { get; set; } = null;
    public string? PortName { get; set; } = null;
    public int ManagementPort { get; set; } = 8558;
    public List<string>? ConfigEndpoints { get; set; } = null;
    public string LabelSelector { get; set; } = "cluster={0}";
}