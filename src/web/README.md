# Web - Todo Application Frontend

Angular-based frontend for the collaborative todo list application.

## Features

- **Authentication**: User registration, login, and device-based logout
- **Todo Management**: Create, read, update, and delete todo lists and items
- **Sharing**: Invite users to collaborate on lists (owner/viewer roles)
- **Real-time Updates**: SignalR integration for live synchronization across devices
- **Responsive UI**: Modern design with Tailwind CSS

## Architecture

Built following Angular best practices:
- Standalone components (Angular v21+)
- Signals for state management
- `inject()` function for dependency injection
- Lazy-loaded routes
- HTTP interceptors for authentication

## Development server

To start a local development server, run:

```bash
npm start
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Prerequisites

The following backend services must be running:
- BFF Service: http://localhost:5000
- Auth Service: http://localhost:5001
- User Service: http://localhost:5002
- Todo Service: http://localhost:5003
- Realtime Service (SignalR): http://localhost:5004

## Configuration

API endpoints are configured in the service files:
- `src/app/services/auth.service.ts`
- `src/app/services/list.service.ts`
- `src/app/services/todo.service.ts`
- `src/app/services/user.service.ts`
- `src/app/services/realtime.service.ts`

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
