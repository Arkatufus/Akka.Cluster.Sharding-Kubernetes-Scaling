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
   - Install Kubernetes Dashboard
   - Create an Akka.NET sharded cluster inside the `shopping-cart` namespace
   - Serve Kubernetes Dashboard using `kubectl proxy`
5. In a web browser, navigate to the dashboard at [http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/](http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/)
6. Copy the dashboard web token from the terminal.
   [Web token in terminal](./docs/images/Terminal.png)
7. Paste the dashboard web token shown in the terminal into the dashboard login screen.
   [Dashboard login screen](./docs/images/LoginScreen.png)
8. In the dashboard, switch to `shopping-cart` namespace
9. In the dashboard, click the Pods Workloads
   [Kubernetes dashboard](./docs/images/Dashboard.png)
10. Watch the backend pods being scaled up as CPU consumption rises.

## Stopping The Example

To stop the Kubernetes cluster press `ctrl-c` in the terminal to stop the web proxy and execute:
   ```powershell
   .\destroy-k8s.cmd
   ```
