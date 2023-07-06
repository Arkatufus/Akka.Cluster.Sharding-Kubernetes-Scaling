//-----------------------------------------------------------------------
// <copyright file="Producers.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster.Sharding.Delivery;
using Akka.Util;

namespace Akka.Cluster.Sharding.Scaling;

#region MessageProducer

/// <summary>
/// Actor is responsible for producing messages
/// </summary>
public sealed class Producer : ReceiveActor, IWithTimers
{
    private static readonly string[] Customers = new[]
    {
        "Yoda", "Obi-Wan", "Darth Vader", "Princess", "Leia", 
        "Luke", "Skywalker", "R2D2", "Han", "Solo", "Chewbacca", "Jabba", 
        "Pepe", "the Frog", "Blue", "Guy", "Alf", "Sad", "Banana", "Noid",
        "Ratchet", "Dorian", "Pavus", "Bella", "Goth", "Alduin", "Steve",
        "Chloe", "Price", "Cayde", "Garrus", "Vakarian", "Isaac", "Clarke",
        "Tom", "Nook", "Niko", "Bellic", "Kassandra", "Guybrush", "Threepwood",
        "Arthur", "Morgan", "Max", "Payne", "Sam", "Fisher", "Shepard", "Jim",
        "McCree", "Jonathan", "Irons", "Nathan", "Drake", "Deckard",
        "Cain", "Gordon", "Freeman", "Samus", "Aran", "Zelda", "Bonnie",
        "MacFarlane", "Rayne", "Sarah", "Kerrigan", "Marcus", "Fenix", "Kratos",
        "Duke", "Nukem", "Cloud", "Strife", "Geralt", "Lara", "Croft",
        "John", "Marston"
    };
    
    private static readonly string [] Items = new[]
    {
        "Yoghurt", "Fruits", "Lightsaber", "Fluffy toy", "Dreamcatcher", 
        "Candies", "Cigars", "Chicken nuggets", "French fries"
    };
    
    private sealed class Produce
    {
        public static readonly Produce Instance = new();
        private Produce() {}
    }

    public ITimerScheduler Timers { get; set; }

    private IActorRef _sendNext = ActorRefs.Nobody;
    private int _burstCount;

    public Producer()
    {
        Idle();
    }

    protected override void PreStart()
    {
        // produce a burst of messages every 5 minutes
        Timers.StartPeriodicTimer("produce", Produce.Instance, TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(5));
    }

    private void Active()
    {
        Receive<Produce>(_ =>
        {
            Console.WriteLine("Bursting");
            Send();
            Become(Burst); // Send the burst
        });

        Receive<ShardingProducerController.RequestNext<Customer.ICustomerCommand>>(next =>
        {
            // no work to do yet, but update SendNext
            _sendNext = next.SendNextTo;
        });
    }

    private void Burst()
    {
        Receive<ShardingProducerController.RequestNext<Customer.ICustomerCommand>>(next =>
        {
            _sendNext = next.SendNextTo;
            Send();
        });
    }

    private void Send()
    {
        if (_burstCount >= 30)
        {
            _burstCount = 0;
            Console.WriteLine("Activating");
            Become(Active);
            return;
        }
        
        var customer = $"{PickRandom(Customers)} {PickRandom(Customers)}";
        var item = PickRandom(Items);
        var msg = new Customer.PurchaseItem(item);
        Console.WriteLine($"{_burstCount}: Sending {item} to {customer}");
        _sendNext.Tell(new ShardingEnvelope(customer, msg));
        _burstCount++;
    }

    /// <summary>
    /// Waiting for demand for messages to come from sharding system
    /// </summary>
    private void Idle()
    {
        Receive<Produce>(_ =>
        {
            // ignore
        });
        
        Receive<ShardingProducerController.RequestNext<Customer.ICustomerCommand>>(next =>
        {
            Console.WriteLine("Activating");
            Become(Active);
            _sendNext = next.SendNextTo;
        });
    }
    
    private static T PickRandom<T>(IReadOnlyList<T> items) => items[ThreadLocalRandom.Current.Next(items.Count)];
}

#endregion