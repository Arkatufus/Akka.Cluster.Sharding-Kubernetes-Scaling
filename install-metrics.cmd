@echo off
REM Installs Kubernetes metrics-server

kubectl apply -f "%~dp0/k8s/infrastructure/metrics.yaml"