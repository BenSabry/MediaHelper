# Application Layer
Application Layer implements the use cases of the application based on the domain. A use case can be thought as a user interaction on the User Interface (UI). This layer contains all application logic. It is dependent on the domain layer, but has no dependencies on any other layer or project. This layer defines interfaces that are implemented by outside layers.

Application layer contains the application Use Cases which orchestrate the high level business rules. By design the orchestration will depend on abstractions of external services (e.g. Repositories). The package exposes Boundaries Interfaces (in other terms Contracts or Ports or Interfaces) which are used by the User Interface.

For example, if the application need to access a email service, a new interface would be added to application and an implementation would be created within infrastructure.

Application layer contains:

Abstractions/Contracts/Interfaces
Application Services/Handlers
Commands and Queries
Exceptions
Models (DTOs)
Mappers
Validators
Behaviors
Specifications