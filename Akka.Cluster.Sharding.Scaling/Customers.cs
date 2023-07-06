//-----------------------------------------------------------------------
// <copyright file="Customers.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Delivery;

namespace Akka.Cluster.Sharding.Scaling
{
    #region ActorClass

    public class Customer : ReceiveActor
    {
        /// <summary>
        /// Marker interface used for grouping all Customer-entity messages
        /// </summary>
        public interface ICustomerCommand
        {
        }

        public sealed class PurchaseItem : ICustomerCommand
        {
            public readonly string ItemName;

            public PurchaseItem(string itemName)
            {
                ItemName = itemName;
            }
        }

        private readonly List<string> _purchasedItems = new();
        private readonly IActorRef _consumerController; // use to guarantee reliable delivery of messages
        private readonly CancellationTokenSource _cts;
        private readonly string _persistenceId;

        public Customer(string persistenceId, IActorRef consumerController)
        {
            _persistenceId = persistenceId;
            _consumerController = consumerController;
            _cts = new CancellationTokenSource();
            
            Receive<ConsumerController.Delivery<ICustomerCommand>>(purchase =>
            {
                if (purchase.Message is PurchaseItem p)
                {
                    _purchasedItems.Add(p.ItemName);
                    Console.WriteLine(
                        @$"'{persistenceId}' purchased '{p.ItemName}'.
All items: [{string.Join(", ", _purchasedItems)}]
--------------------------");
                }
                else
                {
                    // unsupported message type
                    Unhandled(purchase.Message);
                }
                
                purchase.ConfirmTo.Tell(ConsumerController.Confirmed.Instance);
            });
        }

        protected override void PreStart()
        {
            // signal that we're ready to consume messages
            _consumerController.Tell(new ConsumerController.Start<ICustomerCommand>(Self));
            
            // Consume 10% CPU
#pragma warning disable CS4014
            // deliberately create a detached task
            Cpu.Consume(10, _cts.Token);
#pragma warning restore CS4014
        }

        protected override void PostStop()
        {
            base.PostStop();
            _cts.Cancel();
            Console.WriteLine($"{_persistenceId} actor stopped.");
        }
    }

    #endregion
}