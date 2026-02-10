import { ErrorHandler, Injectable } from '@angular/core';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApplicationInsightsService {
  private appInsights: ApplicationInsights | null = null;

  constructor() {
    this.init();
  }

  private init(): void {
    const connStr = environment.appInsights?.connectionString;
    if (!connStr) {
      console.warn('Application Insights connection string is not configured.');
      return;
    }

    this.appInsights = new ApplicationInsights({
      config: {
        connectionString: connStr,
        enableAutoRouteTracking: true,
        enableCorsCorrelation: true,
        enableRequestHeaderTracking: true,
        enableResponseHeaderTracking: true
      }
    });

    this.appInsights.loadAppInsights();
  }

  trackEvent(name: string, properties?: Record<string, string>): void {
    this.appInsights?.trackEvent({ name }, properties);
  }

  trackException(error: Error, properties?: Record<string, string>): void {
    this.appInsights?.trackException({ exception: error }, properties);
  }

  trackTrace(message: string, properties?: Record<string, string>): void {
    this.appInsights?.trackTrace({ message }, properties);
  }

  trackPageView(name?: string): void {
    this.appInsights?.trackPageView({ name });
  }
}

@Injectable()
export class ApplicationInsightsErrorHandler implements ErrorHandler {
  constructor(private appInsightsService: ApplicationInsightsService) {}

  handleError(error: unknown): void {
    const err = error instanceof Error ? error : new Error(String(error));
    this.appInsightsService.trackException(err);
    console.error(err);
  }
}
