# Infrastructure Layer
This layer is responsible to implement the Contracts (Interfaces/Adapters) defined within the application layer to the Secondary Actors. Infrastructure Layer supports other layer by implementing the abstractions and integrations to 3rd-party library and systems.

Infrastructure layer contains most of your application’s dependencies on external resources such as file systems, web services, third party APIs, and so on. The implementation of services should be based on interfaces defined within the application layer.

If you have a very large project with many dependencies, it may make sense to have multiple Infrastructure projects, but for most projects one Infrastructure project with folders works fine.

Infrastructure.Persistence
- Infrastructure.Persistence.MySQL
- Infrastructure.Persistence.MongoDB
Infrastructure.Identity
- 
Infrastructure layer contains:
Identity Services
File Storage Services
Queue Storage Services
Message Bus Services
Payment Services
Third-party Services
Notifications
- Email Service
- Sms Service