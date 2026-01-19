# ImageAIRenamer - Project Architecture

## Overview
ImageAIRenamer follows a **Layered Architecture** pattern with clear separation of concerns across multiple functional layers.

## Architecture Layers

### 1. **Presentation Layer** (`Presentation/`)
- Handles user interface and user interactions
- WPF (Windows Presentation Foundation) UI components
- Communicates with Application layer via ViewModels
- **Responsibility**: Display and capture user input

### 2. **Application Layer** (`Application/`)
- **Common**: Shared utilities and helpers for the application
- **ViewModels**: MVVM pattern implementation for Presentation layer
- Acts as mediator between Presentation and Domain layers
- **Responsibility**: Business logic orchestration and workflow management

### 3. **Domain Layer** (`Domain/`)
- Core business entities and interfaces
- **Entities**: Domain models representing business concepts
- **Interfaces**: Contracts that define business operations
- Technology-independent pure business logic
- **Responsibility**: Core business rules and entities

### 4. **Infrastructure Layer** (`Infrastructure/`)
- **Configuration**: Application configuration management
- **DependencyInjection**: IoC (Inversion of Control) container setup
- **Logging**: Logging infrastructure and utilities
- **Services**: External service integrations (API calls, file operations)
- **Responsibility**: Technical implementations and external dependencies

## Key Features

### Testing
- **ImageAIRenamer.Tests/** - Dedicated test project
  - **Unit/** - Unit tests for individual components
  - **Integration/** - Integration tests for system components
  - **Mocks/** - Mock objects for testing

### Configuration
- `appsettings.json` - Application settings
- Environment-specific configurations supported

### Documentation
- `docs/` - Project documentation
- API specifications and guides

## Data Flow

```
User Interaction
       ↓
 [Presentation Layer]
       ↓
 [Application Layer]
       ↓
 [Domain Layer]
       ↓
 [Infrastructure Layer]
       ↓
 External Services / Database / File System
```

## Design Principles

1. **Separation of Concerns**: Each layer has specific responsibilities
2. **Dependency Inversion**: High-level modules don't depend on low-level modules
3. **Single Responsibility**: Components should have one reason to change
4. **Testability**: Layers are designed to be independently testable
5. **MVVM Pattern**: Used in Presentation layer for WPF

## Technology Stack

- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language**: C#
- **Testing**: Unit and Integration test frameworks
- **Logging**: Configurable logging infrastructure
- **Configuration**: Centralized settings management

## Adding New Features

When adding a new feature, follow this workflow:

1. Define domain entities and interfaces in `Domain/`
2. Implement business logic in `Application/` layer
3. Create services in `Infrastructure/Services/`
4. Build UI components in `Presentation/`
5. Add ViewModel logic in `Application/ViewModels/`
6. Write tests in corresponding `ImageAIRenamer.Tests/` folders

## Dependencies

- Lower layers have no dependencies on upper layers
- Upper layers depend on lower layers
- Infrastructure layer is dependency-injected for flexibility
