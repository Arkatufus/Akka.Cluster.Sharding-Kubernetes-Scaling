# Horizontal Pod Auto Scaling With Akka.NET Cluster Sharding

This repository is a clone of [Akka.NET](https://github.com/akkadotnet/akka.net) [shopping cart example](https://github.com/akkadotnet/akka.net/tree/dev/src/examples/Cluster/ClusterSharding/ShoppingCart), modifying it to use [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting/) and [Akka.Discovery.KubernetesApi](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/kubernetes/Akka.Discovery.KubernetesApi) and designed as a showcase of how Akka.NET cluster pods can be auto scaled.

- The sharding entity actor in this example has been modified to generate synthetic CPU load when it is initialized.
- The Kubernetes YAML configuration file was designed to auto scale the cluster from 3 replicas to a maximum of 10 replicas when the metric server detects a spike of average pod CPU consumption over 80% (800m)

# How To Use The Example

## Running The Example

1. Make sure that you have Docker Desktop installed on your computer
2. Make sure that Docker Desktop is running using WSL Linux containers
3. Make sure that Docker Desktop Kubernetes feature is enabled
4. In a Windows PowerShell terminal, navigate to the project directory and execute
   ```powershell
   .\start-k8s.cmd
   ```
   This will: 
   - Compile the project, 
   - Build the docker image
   - Install Kubernetes metrics server
   - Create an Akka.NET sharded cluster inside the `shopping-cart` namespace

## Confirming Auto-scaling

1. The `frontend-0` pod will spawn 30 actors inside the `backend` stateful set cluster, creating a CPU spike on all 3 `backend` pods.
2. Execute
   ```powershell
   kubectl top pod -n shopping-cart
   ```
   and check that backend pod CPU metrics spikes to above 800m
   ```
   PS C:\> kubectl top pod -n shopping-cart
   NAME         CPU(cores)   MEMORY(bytes)
   backend-0    1001m        86Mi
   backend-1    975m         84Mi
   backend-2    962m         83Mi
   frontend-0   63m          99Mi   
   ```
3. The pod auto scaler will scale the `backend` cluster after a few seconds
   ```powershell
   PS C:\> kubectl top pod -n shopping-cart
   NAME         CPU(cores)   MEMORY(bytes)
   backend-0    21m          86Mi
   backend-1    17m          87Mi
   backend-2    18m          86Mi
   backend-3    18m          87Mi
   backend-4    17m          83Mi
   backend-5    16m          88Mi
   backend-6    17m          82Mi
   backend-7    16m          91Mi
   frontend-0   35m          99Mi
   ```
4. Shut down the `frontend-0` pod by scaling the `frontend` stateful set to 0. Execute:
   ```powershell
   kubectl scale --replicas=0 statefulset/frontend -n shopping-cart
   ```
5. Confirm that the `frontend-0` pod has been shut down by executing `kubectl top pod -n shopping-cart`
   ```powershell
   PS C:\> kubectl top pod -n shopping-cart
   NAME        CPU(cores)   MEMORY(bytes)
   backend-0   21m          87Mi
   backend-1   21m          88Mi
   backend-2   17m          84Mi
   backend-3   20m          88Mi
   backend-4   20m          84Mi
   backend-5   26m          89Mi
   backend-6   19m          83Mi
   backend-7   17m          92Mi
   ```
   Note that `frontend-0` pod is now missing from the list
6. Wait 5-10 minutes and check the cluster status again by executing `kubectl top pod -n shopping-cart`
   ```powershell
   PS C:\> kubectl top pod -n shopping-cart
   NAME        CPU(cores)   MEMORY(bytes)
   backend-0   21m          87Mi
   backend-1   21m          88Mi
   backend-2   17m          84Mi
   ```
   Note that the cluster has been automatically scaled down

## Stopping The Example

To stop the Kubernetes cluster, execute:
   ```powershell
   .\destroy-k8s.cmd
   ```
