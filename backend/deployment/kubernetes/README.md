# Kubernetes Deployment Guide

This directory contains Kubernetes manifests for deploying the Agentic Shopper application to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (v1.25+)
- `kubectl` CLI installed and configured
- NGINX Ingress Controller
- cert-manager (for SSL certificates)
- Access to GitHub Container Registry (GHCR) for images

## Architecture

```
┌─────────────────┐
│   Ingress       │  (NGINX)
│   (TLS/HTTPS)   │
└────────┬────────┘
         │
    ┌────┴─────┐
    │          │
┌───▼──┐   ┌──▼────┐
│Frontend│   │Coordinator│
│(React) │   │  (.NET)   │
└───┬──┘   └──┬────┘
    │          │
    └────┬─────┘
         │
    ┌────┴─────┐
    │          │
┌───▼──┐   ┌──▼────┐
│PostgreSQL│ │ Redis │
│  (DB)    │ │(Cache)│
└─────────┘ └───────┘
```

## Quick Start

### 1. Install NGINX Ingress Controller

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.0/deploy/static/provider/cloud/deploy.yaml
```

### 2. Install cert-manager (for SSL)

```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

### 3. Configure Secrets

**IMPORTANT**: Update `secrets.yaml` with your actual base64-encoded secrets before deploying.

```bash
# Generate JWT secret
JWT_SECRET=$(openssl rand -base64 32)
echo -n "$JWT_SECRET" | base64

# Update secrets.yaml with your actual values
# NEVER commit real secrets to git!
```

For production, consider using:
- **Sealed Secrets**: https://github.com/bitnami-labs/sealed-secrets
- **External Secrets Operator**: https://external-secrets.io/
- **Azure Key Vault Provider**: https://azure.github.io/secrets-store-csi-driver-provider-azure/

### 4. Deploy Application

```bash
# Create namespace and deploy all resources
kubectl apply -f namespace.yaml
kubectl apply -f configmap.yaml
kubectl apply -f secrets.yaml
kubectl apply -f postgresql-deployment.yaml
kubectl apply -f redis-deployment.yaml
kubectl apply -f coordinator-deployment.yaml
kubectl apply -f frontend-deployment.yaml
kubectl apply -f ingress.yaml

# Or use kustomize for single command deployment
kubectl apply -k .
```

### 5. Verify Deployment

```bash
# Check all pods are running
kubectl get pods -n agentic-shopper

# Check services
kubectl get svc -n agentic-shopper

# Check ingress
kubectl get ingress -n agentic-shopper

# View logs
kubectl logs -f deployment/coordinator -n agentic-shopper
kubectl logs -f deployment/frontend -n agentic-shopper
```

### 6. Access Application

```bash
# Get ingress external IP
kubectl get ingress agentic-shopper-ingress -n agentic-shopper

# Update /etc/hosts or DNS
echo "<EXTERNAL-IP> agentic-shopper.example.com api.agentic-shopper.example.com" | sudo tee -a /etc/hosts

# Access application
https://agentic-shopper.example.com
https://api.agentic-shopper.example.com/swagger
```

## Configuration

### Environment-Specific Overlays

For different environments (dev, staging, prod), use Kustomize overlays:

```bash
# Create overlay directory
mkdir -p overlays/production

# Create overlay kustomization
cat > overlays/production/kustomization.yaml <<EOF
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
- ../../base
namespace: agentic-shopper-prod
replicas:
- name: coordinator
  count: 5
- name: frontend
  count: 3
images:
- name: ghcr.io/nileshgule/agentic-shopper-coordinator
  newTag: v1.0.0
EOF

# Deploy production
kubectl apply -k overlays/production
```

### Scaling

```bash
# Manual scaling
kubectl scale deployment coordinator --replicas=5 -n agentic-shopper

# Auto-scaling (HPA already configured)
kubectl get hpa -n agentic-shopper
```

### Resource Limits

Current resource configuration:

| Component | Requests | Limits |
|-----------|----------|--------|
| Coordinator | 250m CPU, 512Mi RAM | 1000m CPU, 1Gi RAM |
| Frontend | 100m CPU, 128Mi RAM | 200m CPU, 256Mi RAM |
| PostgreSQL | 250m CPU, 512Mi RAM | 500m CPU, 1Gi RAM |
| Redis | 100m CPU, 128Mi RAM | 200m CPU, 256Mi RAM |

Adjust in deployment YAML files as needed.

## Monitoring

### Health Checks

```bash
# Coordinator health
curl https://api.agentic-shopper.example.com/health

# Coordinator readiness
curl https://api.agentic-shopper.example.com/health/ready
```

### Logs

```bash
# Stream coordinator logs
kubectl logs -f deployment/coordinator -n agentic-shopper

# View last 100 lines
kubectl logs --tail=100 deployment/coordinator -n agentic-shopper

# Logs from all replicas
kubectl logs -l app=coordinator -n agentic-shopper
```

### Metrics (requires Prometheus)

```bash
# Install Prometheus
helm install prometheus prometheus-community/kube-prometheus-stack

# View metrics
kubectl port-forward -n monitoring svc/prometheus-kube-prometheus-prometheus 9090:9090
```

## Backup & Recovery

### PostgreSQL Backup

```bash
# Create backup
kubectl exec -it deployment/postgresql -n agentic-shopper -- \
  pg_dump -U postgres agentic_shopper > backup-$(date +%Y%m%d).sql

# Restore backup
kubectl exec -i deployment/postgresql -n agentic-shopper -- \
  psql -U postgres agentic_shopper < backup-20250117.sql
```

### Persistent Volume Snapshots

```bash
# List PVCs
kubectl get pvc -n agentic-shopper

# Create snapshot (if supported by storage class)
kubectl create -f pvc-snapshot.yaml
```

## Troubleshooting

### Pods Not Starting

```bash
# Describe pod
kubectl describe pod <pod-name> -n agentic-shopper

# Check events
kubectl get events -n agentic-shopper --sort-by='.lastTimestamp'

# Check logs
kubectl logs <pod-name> -n agentic-shopper
```

### Database Connection Issues

```bash
# Test database connectivity
kubectl run -it --rm debug --image=postgres:15-alpine --restart=Never -n agentic-shopper -- \
  psql -h postgresql-service -U postgres -d agentic_shopper
```

### Ingress Not Working

```bash
# Check ingress controller
kubectl get pods -n ingress-nginx

# Check ingress resource
kubectl describe ingress agentic-shopper-ingress -n agentic-shopper

# Check ingress logs
kubectl logs -n ingress-nginx deployment/ingress-nginx-controller
```

## Cleanup

```bash
# Delete all resources in namespace
kubectl delete namespace agentic-shopper

# Or delete individual resources
kubectl delete -k .
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Deploy to Kubernetes
  run: |
    kubectl set image deployment/coordinator \
      coordinator=ghcr.io/nileshgule/agentic-shopper-coordinator:${{ github.sha }} \
      -n agentic-shopper
    kubectl rollout status deployment/coordinator -n agentic-shopper
```

### Rolling Updates

```bash
# Update image
kubectl set image deployment/coordinator \
  coordinator=ghcr.io/nileshgule/agentic-shopper-coordinator:v1.1.0 \
  -n agentic-shopper

# Monitor rollout
kubectl rollout status deployment/coordinator -n agentic-shopper

# Rollback if needed
kubectl rollout undo deployment/coordinator -n agentic-shopper
```

## Security Best Practices

1. **Never commit secrets** - Use external secret management
2. **Use RBAC** - Create service accounts with minimal permissions
3. **Network Policies** - Restrict pod-to-pod communication
4. **Pod Security Standards** - Enable restricted PSS
5. **Image Scanning** - Scan container images for vulnerabilities
6. **TLS Everywhere** - Use HTTPS for all external traffic
7. **Regular Updates** - Keep Kubernetes and images up to date

## Support

For issues or questions:
- GitHub Issues: https://github.com/NileshGule/agentic-shopper/issues
- Documentation: /specs/001-shopping-analyzer/
