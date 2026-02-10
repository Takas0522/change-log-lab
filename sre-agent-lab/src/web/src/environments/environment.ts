export const environment = {
  production: false,
  apiBaseUrl: '',
  appInsights: {
    // CI builds inject the real value from GitHub Actions secrets.
    // Local development uses environment.local.ts (see angular.json fileReplacements).
    connectionString: ''
  }
};
