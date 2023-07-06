// -----------------------------------------------------------------------
//  <copyright file="StartupMethod.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Cluster.Sharding.Scaling.Config;

public enum StartupMethod
{
    SeedNodes,
    ConfigDiscovery,
    KubernetesDiscovery
}