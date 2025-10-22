# Production Environment Configuration
environment = "production"
aws_region  = "us-east-1"

# VPC Configuration
vpc_cidr = "10.2.0.0/16"
private_subnet_cidrs = ["10.2.1.0/24", "10.2.2.0/24", "10.2.3.0/24"]
public_subnet_cidrs  = ["10.2.101.0/24", "10.2.102.0/24", "10.2.103.0/24"]

# EKS Configuration
kubernetes_version = "1.28"
node_instance_types = ["t3.large", "t3.xlarge"]
node_group_min_size    = 3
node_group_max_size    = 20
node_group_desired_size = 5

# Database Configuration
db_instance_class    = "db.r5.large"
db_allocated_storage = 100
db_name             = "cadence_accounting_prod"
db_username         = "postgres"
# db_password should be set via environment variable or terraform.tfvars

# Redis Configuration
redis_node_type        = "cache.r5.large"
redis_num_cache_nodes  = 3
# redis_auth_token should be set via environment variable or terraform.tfvars

# Tags
tags = {
  Environment = "production"
  Project     = "cadence-accounting"
  Owner       = "platform-team"
  CostCenter  = "engineering"
  Compliance  = "required"
}

