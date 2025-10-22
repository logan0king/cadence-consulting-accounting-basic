#!/bin/bash

# Deployment script for Cadence Consulting Accounting Basic
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=""
NAMESPACE=""
IMAGE_TAG=""
DRY_RUN=false
SKIP_TESTS=false

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
usage() {
    echo "Usage: $0 [OPTIONS] ENVIRONMENT"
    echo ""
    echo "ENVIRONMENT:"
    echo "  dev       Deploy to development environment"
    echo "  staging   Deploy to staging environment"
    echo "  prod      Deploy to production environment"
    echo ""
    echo "OPTIONS:"
    echo "  -t, --tag TAG        Docker image tag (default: latest)"
    echo "  -n, --namespace NS   Kubernetes namespace (default: based on environment)"
    echo "  -d, --dry-run        Show what would be deployed without actually deploying"
    echo "  -s, --skip-tests     Skip running tests before deployment"
    echo "  -h, --help           Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 dev"
    echo "  $0 staging --tag v1.2.3"
    echo "  $0 prod --dry-run"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        dev|staging|prod)
            ENVIRONMENT="$1"
            shift
            ;;
        -t|--tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        -n|--namespace)
            NAMESPACE="$2"
            shift 2
            ;;
        -d|--dry-run)
            DRY_RUN=true
            shift
            ;;
        -s|--skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Validate environment
if [[ -z "$ENVIRONMENT" ]]; then
    print_error "Environment is required"
    usage
    exit 1
fi

# Set default values based on environment
case $ENVIRONMENT in
    dev)
        NAMESPACE=${NAMESPACE:-"development"}
        IMAGE_TAG=${IMAGE_TAG:-"dev"}
        ;;
    staging)
        NAMESPACE=${NAMESPACE:-"staging"}
        IMAGE_TAG=${IMAGE_TAG:-"staging"}
        ;;
    prod)
        NAMESPACE=${NAMESPACE:-"production"}
        IMAGE_TAG=${IMAGE_TAG:-"latest"}
        ;;
esac

print_status "Starting deployment to $ENVIRONMENT environment"
print_status "Namespace: $NAMESPACE"
print_status "Image tag: $IMAGE_TAG"
print_status "Dry run: $DRY_RUN"

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if kubectl is installed
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl is not installed or not in PATH"
        exit 1
    fi
    
    # Check if docker is installed
    if ! command -v docker &> /dev/null; then
        print_error "docker is not installed or not in PATH"
        exit 1
    fi
    
    # Check if helm is installed (for production)
    if [[ "$ENVIRONMENT" == "prod" ]] && ! command -v helm &> /dev/null; then
        print_warning "helm is not installed. Some features may not work in production."
    fi
    
    print_status "Prerequisites check passed"
}

# Run tests
run_tests() {
    if [[ "$SKIP_TESTS" == "true" ]]; then
        print_warning "Skipping tests as requested"
        return
    fi
    
    print_status "Running tests..."
    
    # Run unit tests
    if [[ -f "package.json" ]]; then
        npm test
    fi
    
    # Run security scans
    if command -v trivy &> /dev/null; then
        print_status "Running Trivy security scan..."
        trivy image --config security/trivy.yaml ghcr.io/cadence-consulting/accounting-basic:$IMAGE_TAG
    fi
    
    print_status "Tests completed successfully"
}

# Build and push Docker image
build_and_push_image() {
    print_status "Building and pushing Docker image..."
    
    # Build image
    docker build -t ghcr.io/cadence-consulting/accounting-basic:$IMAGE_TAG .
    
    # Push image
    docker push ghcr.io/cadence-consulting/accounting-basic:$IMAGE_TAG
    
    print_status "Docker image built and pushed successfully"
}

# Deploy to Kubernetes
deploy_to_kubernetes() {
    print_status "Deploying to Kubernetes..."
    
    # Create namespace if it doesn't exist
    kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
    
    # Update image tag in deployment files
    if [[ -f "infrastructure/k8s/$ENVIRONMENT/deployment.yaml" ]]; then
        # Create a temporary deployment file with updated image tag
        sed "s|ghcr.io/cadence-consulting/accounting-basic:.*|ghcr.io/cadence-consulting/accounting-basic:$IMAGE_TAG|g" \
            "infrastructure/k8s/$ENVIRONMENT/deployment.yaml" > "/tmp/deployment-$ENVIRONMENT.yaml"
    fi
    
    # Apply Kubernetes manifests
    if [[ "$DRY_RUN" == "true" ]]; then
        print_status "Dry run - showing what would be deployed:"
        kubectl apply -f "infrastructure/k8s/$ENVIRONMENT/" --dry-run=client -o yaml
    else
        # Apply all manifests
        kubectl apply -f "infrastructure/k8s/$ENVIRONMENT/"
        
        # Wait for deployment to be ready
        print_status "Waiting for deployment to be ready..."
        kubectl rollout status deployment/cadence-accounting -n $NAMESPACE --timeout=300s
        
        # Show deployment status
        kubectl get pods -n $NAMESPACE -l app=cadence-accounting
    fi
    
    print_status "Kubernetes deployment completed"
}

# Run post-deployment checks
post_deployment_checks() {
    if [[ "$DRY_RUN" == "true" ]]; then
        return
    fi
    
    print_status "Running post-deployment checks..."
    
    # Check if pods are running
    READY_PODS=$(kubectl get pods -n $NAMESPACE -l app=cadence-accounting --field-selector=status.phase=Running --no-headers | wc -l)
    TOTAL_PODS=$(kubectl get pods -n $NAMESPACE -l app=cadence-accounting --no-headers | wc -l)
    
    if [[ $READY_PODS -eq $TOTAL_PODS ]] && [[ $TOTAL_PODS -gt 0 ]]; then
        print_status "All pods are running successfully"
    else
        print_error "Some pods are not running. Check with: kubectl get pods -n $NAMESPACE"
        exit 1
    fi
    
    # Check service endpoints
    kubectl get endpoints -n $NAMESPACE cadence-accounting-service
    
    print_status "Post-deployment checks completed"
}

# Cleanup function
cleanup() {
    if [[ -f "/tmp/deployment-$ENVIRONMENT.yaml" ]]; then
        rm -f "/tmp/deployment-$ENVIRONMENT.yaml"
    fi
}

# Set trap for cleanup
trap cleanup EXIT

# Main deployment flow
main() {
    print_status "Starting deployment process..."
    
    check_prerequisites
    run_tests
    build_and_push_image
    deploy_to_kubernetes
    post_deployment_checks
    
    print_status "Deployment to $ENVIRONMENT completed successfully!"
}

# Run main function
main

