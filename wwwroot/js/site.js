// Site-wide JavaScript

// Initialize tooltips (if using Bootstrap 5)
document.addEventListener('DOMContentLoaded', function () {
  // Initialize Bootstrap tooltips
  const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
  const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
  
  // Initialize Bootstrap popovers
  const popoverTriggerList = document.querySelectorAll('[data-bs-toggle="popover"]');
  const popoverList = [...popoverTriggerList].map(popoverTriggerEl => new bootstrap.Popover(popoverTriggerEl));
});

// Global utility functions
window.showAlert = function(message, type = 'info') {
  const alertDiv = document.createElement('div');
  alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
  alertDiv.role = 'alert';
  alertDiv.innerHTML = `
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
  `;
  
  const container = document.querySelector('.container-fluid') || document.body;
  container.insertBefore(alertDiv, container.firstChild);
  
  // Auto-dismiss after 5 seconds
  setTimeout(() => {
    alertDiv.remove();
  }, 5000);
};

// Format currency
window.formatCurrency = function(amount) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD'
  }).format(amount);
};

// Format date
window.formatDate = function(date) {
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(new Date(date));
};

