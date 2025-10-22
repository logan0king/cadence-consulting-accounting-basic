#!/bin/bash

# Backup script for Cadence Consulting Accounting Basic
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BACKUP_DIR="/backups"
S3_BUCKET="cadence-accounting-backups"
RETENTION_DAYS=30

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
    echo "  -d, --database       Backup database only"
    echo "  -f, --files          Backup files only"
    echo "  -a, --all            Backup everything (default)"
    echo "  -s, --s3             Upload to S3"
    echo "  -r, --retention N    Retention days (default: 30)"
    echo "  -h, --help           Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --all --s3"
    echo "  $0 --database --retention 7"
    echo "  $0 --files"
}

# Parse command line arguments
BACKUP_DATABASE=false
BACKUP_FILES=false
BACKUP_ALL=true
UPLOAD_S3=false
RETENTION_DAYS=30

while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--database)
            BACKUP_DATABASE=true
            BACKUP_ALL=false
            shift
            ;;
        -f|--files)
            BACKUP_FILES=true
            BACKUP_ALL=false
            shift
            ;;
        -a|--all)
            BACKUP_ALL=true
            shift
            ;;
        -s|--s3)
            UPLOAD_S3=true
            shift
            ;;
        -r|--retention)
            RETENTION_DAYS="$2"
            shift 2
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

# Set backup flags
if [[ "$BACKUP_ALL" == "true" ]]; then
    BACKUP_DATABASE=true
    BACKUP_FILES=true
fi

# Create backup directory
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_PATH="$BACKUP_DIR/$TIMESTAMP"
mkdir -p "$BACKUP_PATH"

print_status "Starting backup process..."
print_status "Backup path: $BACKUP_PATH"
print_status "Retention days: $RETENTION_DAYS"

# Function to backup database
backup_database() {
    print_status "Backing up database..."
    
    # Get database connection details from environment or config
    DB_HOST=${DB_HOST:-"localhost"}
    DB_PORT=${DB_PORT:-"5432"}
    DB_NAME=${DB_NAME:-"cadence_accounting"}
    DB_USER=${DB_USER:-"postgres"}
    
    # Create database backup
    PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --clean \
        --if-exists \
        --create \
        --format=custom \
        --file="$BACKUP_PATH/database_backup.dump"
    
    # Create SQL dump as well
    PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --verbose \
        --clean \
        --if-exists \
        --create \
        --format=plain \
        --file="$BACKUP_PATH/database_backup.sql"
    
    print_status "Database backup completed"
}

# Function to backup files
backup_files() {
    print_status "Backing up files..."
    
    # Backup application files
    if [[ -d "/app" ]]; then
        tar -czf "$BACKUP_PATH/application_files.tar.gz" -C /app .
    fi
    
    # Backup configuration files
    if [[ -d "/etc/cadence-accounting" ]]; then
        tar -czf "$BACKUP_PATH/config_files.tar.gz" -C /etc cadence-accounting
    fi
    
    # Backup logs
    if [[ -d "/var/log/cadence-accounting" ]]; then
        tar -czf "$BACKUP_PATH/logs.tar.gz" -C /var/log cadence-accounting
    fi
    
    print_status "Files backup completed"
}

# Function to backup Kubernetes resources
backup_kubernetes() {
    print_status "Backing up Kubernetes resources..."
    
    # Backup all resources in the namespace
    kubectl get all -o yaml > "$BACKUP_PATH/kubernetes_resources.yaml"
    
    # Backup secrets (base64 encoded)
    kubectl get secrets -o yaml > "$BACKUP_PATH/kubernetes_secrets.yaml"
    
    # Backup configmaps
    kubectl get configmaps -o yaml > "$BACKUP_PATH/kubernetes_configmaps.yaml"
    
    print_status "Kubernetes resources backup completed"
}

# Function to upload to S3
upload_to_s3() {
    if [[ "$UPLOAD_S3" != "true" ]]; then
        return
    fi
    
    print_status "Uploading backup to S3..."
    
    # Check if AWS CLI is available
    if ! command -v aws &> /dev/null; then
        print_error "AWS CLI is not installed or not in PATH"
        return
    fi
    
    # Upload backup to S3
    aws s3 cp "$BACKUP_PATH" "s3://$S3_BUCKET/backups/$TIMESTAMP/" --recursive
    
    print_status "Backup uploaded to S3 successfully"
}

# Function to cleanup old backups
cleanup_old_backups() {
    print_status "Cleaning up old backups..."
    
    # Clean up local backups
    find "$BACKUP_DIR" -type d -mtime +$RETENTION_DAYS -exec rm -rf {} \; 2>/dev/null || true
    
    # Clean up S3 backups
    if [[ "$UPLOAD_S3" == "true" ]] && command -v aws &> /dev/null; then
        aws s3 ls "s3://$S3_BUCKET/backups/" | while read -r line; do
            createDate=$(echo $line | awk '{print $1" "$2}')
            createDate=$(date -d"$createDate" +%s)
            olderThan=$(date -d"$RETENTION_DAYS days ago" +%s)
            if [[ $createDate -lt $olderThan ]]; then
                fileName=$(echo $line | awk '{print $4}')
                if [[ $fileName != "" ]]; then
                    aws s3 rm "s3://$S3_BUCKET/backups/$fileName" --recursive
                fi
            fi
        done
    fi
    
    print_status "Old backups cleanup completed"
}

# Function to create backup manifest
create_backup_manifest() {
    print_status "Creating backup manifest..."
    
    cat > "$BACKUP_PATH/backup_manifest.json" << EOF
{
    "timestamp": "$TIMESTAMP",
    "backup_date": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "backup_type": "$(if [[ "$BACKUP_ALL" == "true" ]]; then echo "full"; else echo "partial"; fi)",
    "components": {
        "database": $BACKUP_DATABASE,
        "files": $BACKUP_FILES,
        "kubernetes": true
    },
    "retention_days": $RETENTION_DAYS,
    "s3_uploaded": $UPLOAD_S3,
    "backup_size": "$(du -sh "$BACKUP_PATH" | cut -f1)"
}
EOF
    
    print_status "Backup manifest created"
}

# Main backup function
main() {
    print_status "Starting backup process..."
    
    # Create backup directory
    mkdir -p "$BACKUP_PATH"
    
    # Perform backups
    if [[ "$BACKUP_DATABASE" == "true" ]]; then
        backup_database
    fi
    
    if [[ "$BACKUP_FILES" == "true" ]]; then
        backup_files
    fi
    
    # Always backup Kubernetes resources
    backup_kubernetes
    
    # Create backup manifest
    create_backup_manifest
    
    # Upload to S3 if requested
    upload_to_s3
    
    # Cleanup old backups
    cleanup_old_backups
    
    print_status "Backup process completed successfully!"
    print_status "Backup location: $BACKUP_PATH"
    
    if [[ "$UPLOAD_S3" == "true" ]]; then
        print_status "S3 location: s3://$S3_BUCKET/backups/$TIMESTAMP/"
    fi
}

# Run main function
main

