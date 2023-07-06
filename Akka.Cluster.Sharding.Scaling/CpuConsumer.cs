// -----------------------------------------------------------------------
//  <copyright file="CpuConsumer.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Akka.Cluster.Sharding.Scaling;

public static class Cpu
{
    public static async Task Consume(int percentage, CancellationToken token)
    {
        if (percentage < 1 || percentage > 100)
            throw new IndexOutOfRangeException("percentage must be between 1 and 100");
        var watch = new Stopwatch();
        watch.Start();
        while (true)
        {
            // Make the loop go on for "percentage" milliseconds then sleep the 
            // remaining percentage milliseconds. So 40% utilization means work 40ms and sleep 60ms
            if (watch.ElapsedMilliseconds > percentage)
            {
                await Task.Delay(100 - percentage, token);
                watch.Restart();
            }
            if(token.IsCancellationRequested)
                break;
        }
        watch.Stop();
    }

    public static async Task ConsumeAll(int percentage, CancellationToken token)
    {
        await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Consume(percentage, token)));
    }
}

