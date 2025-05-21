MyCompany.OnionApp.sln
│
├── src/
│   ├── MyCompany.API/                    # API Layer (UI)
│   │   ├── Controllers/
│   │   └── Program.cs / appsettings.json
│   │
│   ├── MyCompany.Application/            # Application Layer (use cases)
│   │   ├── Interfaces/                   # Application service contracts
│   │   ├── Services/                     # Business logic
│   │   ├── DTOs/                         # Request/response models
│   │   └── Validators/                   # FluentValidation (if app-specific)
│   │
│   ├── Core/                              # Reusable shared abstractions
│   │   ├── Web/
│   │   │   ├── Middlewares/               # Middleware (e.g., error handling)
│   │   │   └── Validators/                # Validation logic
│   │   └── Services/                      # Shared service logic
│   │
│   ├── MyCompany.Domain/                 # Domain Layer
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   └── Interfaces/                   # Repository & service contracts
│   │
│   └── MyCompany.Infrastructure/         # Infrastructure Layer
│       ├── Persistence/                  # EF Core DbContext and config
│       ├── Repositories/                 # Repository implementations
│       └── Mappings/                     # AutoMapper profiles
│
└── tests/
    ├── MyCompany.Application.Tests/
    └── MyCompany.API.Tests/

