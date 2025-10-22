# Development Environment Configuration
environment = "dev"
aws_region  = "us-east-1"

# VPC Configuration
vpc_cidr = "10.0.0.0/16"
private_subnet_cidrs = ["10.0.1.0/24", "10.0.2.0/24"]
public_subnet_cidrs  = ["10.0.101.0/24", "10.0.102.0/24"]

# EKS Configuration
kubernetes_version = "1.28"
node_instance_types = ["t3.small"]
node_group_min_size    = 1
node_group_max_size    = 3
node_group_desired_size = 1

# Database Configuration
db_instance_class    = "db.t3.micro"
db_allocated_storage = 20
db_name             = "cadence_accounting_dev"
db_username         = "postgres"
# db_password should be set via environment variable or terraform.tfvars

# Redis Configuration
redis_node_type        = "cache.t3.micro"
redis_num_cache_nodes  = 1
# redis_auth_token should be set via environment variable or terraform.tfvars

# Tags
tags = {
  Environment = "development"
  Project     = "cadence-accounting"
  Owner       = "dev-team"
  CostCenter  = "engineering"
}

