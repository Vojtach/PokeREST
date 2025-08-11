# PokeREST: A Pokémon Management API

Welcome to PokeREST, a comprehensive RESTful API for managing Pokémon trainers and their collections. This API provides a robust set of endpoints for creating, retrieving, updating, and deleting trainers and Pokémon, with advanced features like searching, pagination, and sorting.

## Features

*   **Trainer Management:** Create, retrieve, update, and delete trainers.
*   **Pokémon Management:** Assign existing Pokémon to trainers.
*   **Advanced Search:** Filter Pokémon and trainers based on various criteria (name, type, level, etc.).
*   **Pagination and Sorting:** Efficiently browse through large datasets of Pokémon and trainers.
*   **Detailed Responses:** Get detailed information about trainers, including their Pokémon, and vice-versa.
*   **Validation:** Input validation to ensure data integrity.
*   **Docker Support:** Containerize the application for easy deployment.
*   **Comprehensive Testing:** Includes unit and integration tests to ensure reliability.

## Technologies Used

*   **.NET 9:** The core framework for building the API.
*   **ASP.NET Core:** For building high-performance, cross-platform web APIs.
*   **Entity Framework Core:** As the Object-Relational Mapper (ORM) for database interactions.
*   **PostgreSQL:** The relational database for storing data.
*   **Docker:** For containerizing the application.
*   **xUnit:** For writing and running tests.
*   **Swagger (OpenAPI):** For API documentation and testing.

## Setup and Installation

### Prerequisites

*   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)
*   A PostgreSQL instance (either local or on a server)


## Database:

## 1. PostgreSQL Setup
Ensure you have a PostgreSQL database set up and running. You can use a local instance and manage with pgadmin.

### 2. Run seeding scripts on your PostgreSQL instance (Scripts are provided separate, as this is a case study and to not leak any company provided resources)
create_pokemon_db.sql - Creates the PostgreSQL database

pokemon_database_complete.sql - Creates tables and populates with sample Pokémon data

## API:

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/PokeREST.git
cd PokeREST
```

### 2. Configure Environment Variables

Create a `.env` file in the root directory of the project and add the following environment variable:

```
DB_CONNECTION_STRING=Host=your_host;Port=your_port;Database=your_db;Username=your_username;Password=your_password
```

Replace the placeholder values with your PostgreSQL connection details.

### 3. Build and Run the Application

#### Using .NET CLI

```bash
dotnet build
dotnet run
```

The API will be available at `http://localhost:5273`.

#### Using Docker

Build the Docker image:

```bash
docker build -t pokerest .
```

Run the Docker container:

```bash
docker run -p 8080:8080 -p 8081:8081 --env-file .env pokerest
```

The API will be available at `http://localhost:8080` (HTTP) and `https://localhost:8081` (HTTPS).

## API Endpoints

The API is documented using Swagger. You can access the Swagger UI at `http://localhost:5273/swagger` when running the application.

### Trainers

*   `POST /api/trainers`: Create a new trainer.
*   `GET /api/trainers`: Get all trainers.
*   `GET /api/trainers/{id}`: Get a specific trainer by ID.
*   `PATCH /api/trainers/{id}`: Update a trainer's information.
*   `DELETE /api/trainers/{id}`: Delete a trainer.
*   `GET /api/trainers/paginated`: Get paginated trainers with filtering and sorting.

### Pokémon

*   `POST /api/trainers/{trainerId}/pokemon`: Add an existing Pokémon to a trainer.
*   `GET /api/pokemon`: Get all Pokémon with optional filtering.
*   `GET /api/pokemon/paginated`: Get paginated Pokémon with advanced filtering and sorting.

For detailed information about the request and response formats, please refer to the Swagger documentation.

## Database Schema

The database consists of the following tables:

*   **Trainers:** Stores information about Pokémon trainers.
*   **Pokemon:** Stores information about individual Pokémon.
*   **Elements:** Stores Pokémon types (e.g., Fire, Water, Grass).
*   **Moves:** Stores Pokémon moves.
*   **Pokemon_Moves:** A join table for the many-to-many relationship between Pokémon and Moves.

## Testing

The project includes comprehensive unit and integration tests to ensure reliability and maintainability.

### Running Tests

To run all tests:
```bash
dotnet test
```

To run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

- **Controllers Tests:** Test API endpoints, request validation, and response formatting.
- **Services Tests:** Test business logic and data access.
- **DTOs Tests:** Test data transfer object validation.
- **Helpers Tests:** Test utility functions like fuzzy matching.

## API Usage Examples

### Creating a Trainer

```bash
curl -X POST "http://localhost:5273/api/trainers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Ash Ketchum",
    "region": "Kanto",
    "age": 10
  }'
```

### Searching Pokémon

```bash
# Search by name (supports fuzzy matching)
curl "http://localhost:5273/api/pokemon?name=pikachu"

# Search by type and level range
curl "http://localhost:5273/api/pokemon?type=electric&minLevel=5&maxLevel=50"

# Search for unowned Pokémon
curl "http://localhost:5273/api/pokemon?hasOwner=false"
```

### Pagination and Sorting

```bash
# Get paginated trainers sorted by wins
curl "http://localhost:5273/api/trainers/paginated?pageSize=10&sortBy=wins&sortOrder=desc"

# Get paginated Pokémon with filters
curl "http://localhost:5273/api/pokemon/paginated?type=fire&sortBy=level&sortOrder=asc"
```

## Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "data": {
    // Response data here
  }
}
```

For paginated responses:

```json
{
  "success": true,
  "message": "Found 25 items (Page 1 of 3)",
  "statusCode": 200,
  "data": {
    "items": [...],
    "totalItems": 25,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3
  }
}
```

## Features in Detail

### Fuzzy Search
The API includes intelligent fuzzy search capabilities that can handle:
- Typos in Pokémon names
- Partial matches
- Case-insensitive searches

### Advanced Filtering
- **Pokémon:** Filter by name, type, level range, owner, and ownership status
- **Trainers:** Filter by name, region, age range, wins, losses, and Pokémon count

### Validation
- Input validation using Data Annotations
- Custom business logic validation
- Consistent error responses

### Error Handling
Robust error handling with meaningful error messages:
- 400 Bad Request: Invalid input data
- 404 Not Found: Resource not found
- 500 Internal Server Error: Server-side errors

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|---------|
| `DB_CONNECTION_STRING` | PostgreSQL connection string | Yes |

### Database Migration

To create and apply database migrations:

```bash
# Create a new migration
dotnet ef migrations add InitialCreate

# Apply migrations to database
dotnet ef database update
```

## Deployment

### Docker Compose

For easy deployment with PostgreSQL, you can use Docker Compose:

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - DB_CONNECTION_STRING=Host=db;Port=5432;Database=pokerest;Username=postgres;Password=password
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: pokerest
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

### Production Considerations

- Use environment-specific configuration files
- Implement proper logging
- Add authentication and authorization
- Implement rate limiting
- Use HTTPS in production
- Configure CORS appropriately
- Monitor performance and health

## Acknowledgments

- Built with .NET 9 and ASP.NET Core
- Uses Entity Framework Core for data access
- PostgreSQL as the database
- xUnit for testing
- Swagger/OpenAPI for documentation
