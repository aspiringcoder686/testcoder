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


![image](https://github.com/user-attachments/assets/a90d880a-4476-4c07-a7b1-3aa04fb64f8f)

![image](https://github.com/user-attachments/assets/7d295f71-0653-4e86-9472-3f2b837bc4e8)

