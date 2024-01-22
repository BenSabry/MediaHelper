# Domain Layer
Domain Layer implements the core, use-case independent business logic of the domain/system. By design, this layer is highly abstracted and stable. This layer contains a considerable amount of domain entities and should not depend on external libraries and frameworks. Ideally it should be loosely coupled even to the .NET Framework.

Domain project is core and backbone project. It is the heart and center project of the Clean Architecture design. All other projects should be depended on the Domain project.

This package contains the high level modules which describe the Domain via Aggregate Roots, Entities and Value Objects.

Domain layer contains:

Entities
Aggregates
Value Objects
Domain Events
Enums
Constants