#!/bin/bash

# Health check script for Cadence Consulting Accounting Basic
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
HEALTH_ENDPOINT="http://localhost:3000/health"
READY_ENDPOINT="http://localhost:3000/ready"
TIMEOUT=30
RETRY_COUNT=3
RETRY_DELAY=5

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
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "OPTIONS:"
    echo "  -h, --host HOST      Health check host (default: localhost)"
    echo "  -p, --port PORT      Health check port (default: 3000)"
    echo "  -t, --timeout SEC    Timeout in seconds (default: 30)"
    echo "  -r, --retries N      Number of retries (default: 3)"
    echo "  -d, --delay SEC      Delay between retries (default: 5)"
    echo "  -v, --verbose        Verbose output"
    echo "  --help               Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0"
    echo "  $0 --host api.example.com --port 443"
    echo "  $0 --timeout 60 --retries 5"
}

# Parse command line arguments
HOST="localhost"
PORT="3000"
TIMEOUT=30
RETRY_COUNT=3
RETRY_DELAY=5
VERBOSE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--host)
            HOST="$2"
            shift 2
            ;;
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        -r|--retries)
            RETRY_COUNT="$2"
            shift 2
            ;;
        -d|--delay)
            RETRY_DELAY="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        --help)
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

# Update endpoints with parsed values
HEALTH_ENDPOINT="http://$HOST:$PORT/health"
READY_ENDPOINT="http://$HOST:$PORT/ready"

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to make HTTP request
make_request() {
    local url="$1"
    local timeout="$2"
    
    if command_exists curl; then
        curl -s -f --max-time "$timeout" "$url" 2>/dev/null
    elif command_exists wget; then
        wget -q -O- --timeout="$timeout" "$url" 2>/dev/null
    else
        print_error "Neither curl nor wget is available"
        return 1
    fi
}

# Function to check health endpoint
check_health() {
    local url="$1"
    local timeout="$2"
    
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking health endpoint: $url"
    fi
    
    local response
    if response=$(make_request "$url" "$timeout"); then
        if [[ "$VERBOSE" == "true" ]]; then
            echo "Response: $response"
        fi
        
        # Check if response contains "healthy" or "ok"
        if echo "$response" | grep -qi "healthy\|ok\|up"; then
            return 0
        else
            return 1
        fi
    else
        return 1
    fi
}

# Function to check readiness endpoint
check_readiness() {
    local url="$1"
    local timeout="$2"
    
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking readiness endpoint: $url"
    fi
    
    local response
    if response=$(make_request "$url" "$timeout"); then
        if [[ "$VERBOSE" == "true" ]]; then
            echo "Response: $response"
        fi
        
        # Check if response contains "ready" or "ok"
        if echo "$response" | grep -qi "ready\|ok\|up"; then
            return 0
        else
            return 1
        fi
    else
        return 1
    fi
}

# Function to check database connectivity
check_database() {
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking database connectivity"
    fi
    
    # Check if database environment variables are set
    if [[ -z "$DB_HOST" ]] || [[ -z "$DB_NAME" ]]; then
        print_warning "Database environment variables not set, skipping database check"
        return 0
    fi
    
    # Try to connect to database
    if command_exists psql; then
        if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "${DB_PORT:-5432}" -U "${DB_USER:-postgres}" -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1; then
            if [[ "$VERBOSE" == "true" ]]; then
                print_status "Database connection successful"
            fi
            return 0
        else
            print_error "Database connection failed"
            return 1
        fi
    else
        print_warning "psql not available, skipping database check"
        return 0
    fi
}

# Function to check Redis connectivity
check_redis() {
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking Redis connectivity"
    fi
    
    # Check if Redis environment variables are set
    if [[ -z "$REDIS_HOST" ]]; then
        print_warning "Redis environment variables not set, skipping Redis check"
        return 0
    fi
    
    # Try to connect to Redis
    if command_exists redis-cli; then
        if redis-cli -h "$REDIS_HOST" -p "${REDIS_PORT:-6379}" ping >/dev/null 2>&1; then
            if [[ "$VERBOSE" == "true" ]]; then
                print_status "Redis connection successful"
            fi
            return 0
        else
            print_error "Redis connection failed"
            return 1
        fi
    else
        print_warning "redis-cli not available, skipping Redis check"
        return 0
    fi
}

# Function to check disk space
check_disk_space() {
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking disk space"
    fi
    
    local usage
    usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    
    if [[ $usage -gt 90 ]]; then
        print_error "Disk usage is critical: ${usage}%"
        return 1
    elif [[ $usage -gt 80 ]]; then
        print_warning "Disk usage is high: ${usage}%"
    else
        if [[ "$VERBOSE" == "true" ]]; then
            print_status "Disk usage is normal: ${usage}%"
        fi
    fi
    
    return 0
}

# Function to check memory usage
check_memory() {
    if [[ "$VERBOSE" == "true" ]]; then
        print_status "Checking memory usage"
    fi
    
    if command_exists free; then
        local usage
        usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
        
        if [[ $usage -gt 90 ]]; then
            print_error "Memory usage is critical: ${usage}%"
            return 1
        elif [[ $usage -gt 80 ]]; then
            print_warning "Memory usage is high: ${usage}%"
        else
            if [[ "$VERBOSE" == "true" ]]; then
                print_status "Memory usage is normal: ${usage}%"
            fi
        fi
    else
        print_warning "free command not available, skipping memory check"
    fi
    
    return 0
}

# Function to perform retry logic
retry_check() {
    local check_function="$1"
    local check_name="$2"
    local url="$3"
    local timeout="$4"
    local retries="$5"
    local delay="$6"
    
    local attempt=1
    while [[ $attempt -le $retries ]]; do
        if [[ "$VERBOSE" == "true" ]]; then
            print_status "Attempt $attempt/$retries: $check_name"
        fi
        
        if "$check_function" "$url" "$timeout"; then
            return 0
        fi
        
        if [[ $attempt -lt $retries ]]; then
            if [[ "$VERBOSE" == "true" ]]; then
                print_warning "Check failed, retrying in $delay seconds..."
            fi
            sleep "$delay"
        fi
        
        ((attempt++))
    done
    
    return 1
}

# Main health check function
main() {
    print_status "Starting health check..."
    print_status "Target: $HOST:$PORT"
    print_status "Timeout: $TIMEOUT seconds"
    print_status "Retries: $RETRY_COUNT"
    
    local exit_code=0
    
    # Check health endpoint
    if ! retry_check check_health "Health endpoint" "$HEALTH_ENDPOINT" "$TIMEOUT" "$RETRY_COUNT" "$RETRY_DELAY"; then
        print_error "Health check failed"
        exit_code=1
    else
        print_status "Health check passed"
    fi
    
    # Check readiness endpoint
    if ! retry_check check_readiness "Readiness endpoint" "$READY_ENDPOINT" "$TIMEOUT" "$RETRY_COUNT" "$RETRY_DELAY"; then
        print_error "Readiness check failed"
        exit_code=1
    else
        print_status "Readiness check passed"
    fi
    
    # Check database connectivity
    if ! check_database; then
        print_error "Database check failed"
        exit_code=1
    fi
    
    # Check Redis connectivity
    if ! check_redis; then
        print_error "Redis check failed"
        exit_code=1
    fi
    
    # Check system resources
    if ! check_disk_space; then
        exit_code=1
    fi
    
    if ! check_memory; then
        exit_code=1
    fi
    
    if [[ $exit_code -eq 0 ]]; then
        print_status "All health checks passed!"
    else
        print_error "Some health checks failed"
    fi
    
    exit $exit_code
}

# Run main function
main

