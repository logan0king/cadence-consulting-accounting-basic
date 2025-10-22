# Staging Environment Configuration
environment = "staging"
aws_region  = "us-east-1"

# VPC Configuration
vpc_cidr = "10.1.0.0/16"
private_subnet_cidrs = ["10.1.1.0/24", "10.1.2.0/24"]
public_subnet_cidrs  = ["10.1.101.0/24", "10.1.102.0/24"]

# EKS Configuration
kubernetes_version = "1.28"
node_instance_types = ["t3.medium"]
node_group_min_size    = 2
node_group_max_size    = 5
node_group_desired_size = 2

# Database Configuration
db_instance_class    = "db.t3.small"
db_allocated_storage = 50
db_name             = "cadence_accounting_staging"
db_username         = "postgres"
# db_password should be set via environment variable or terraform.tfvars

# Redis Configuration
redis_node_type        = "cache.t3.small"
redis_num_cache_nodes  = 2
# redis_auth_token should be set via environment variable or terraform.tfvars

# Tags
tags = {
  Environment = "staging"
  Project     = "cadence-accounting"
  Owner       = "dev-team"
  CostCenter  = "engineering"
}

