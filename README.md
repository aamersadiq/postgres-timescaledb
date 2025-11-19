# PostgreSQL TimescaleDB .NET 8 Web API

This project demonstrates how to build a .NET 8 Web API application that leverages PostgreSQL with TimescaleDB extension to handle transaction data. The application showcases various TimescaleDB features including time series data analysis, high volume transaction processing, data retention policies, and TimescaleDB specific optimizations.

## Project Structure

- **TransactionApi.WebApi**: ASP.NET Core Web API project with controllers and API endpoints
- **TransactionApi.Data**: Data access layer with Entity Framework Core, models, and repositories

## Features

- **CRUD Operations for Transactions**: Create, read, update, and delete transaction records
- **Time-Series Data Analysis**: Analyze transaction patterns over time
- **TimescaleDB Integration**: Hypertables, continuous aggregates, and other TimescaleDB features
- **Test Data Generation**: Automatic generation of sample transaction data
- **Swagger Documentation**: Interactive API documentation

## TimescaleDB Features Used

- **Hypertables**: Automatic partitioning of time-series data
- **Continuous Aggregates**: Pre-computed aggregations for faster queries
- **Time Bucketing**: Group data by time intervals
- **Compression Policies**: Efficient storage of historical data

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Getting Started

### 1. Start PostgreSQL with TimescaleDB

```bash
docker-compose up -d
```

### 2. Build and Run the API

```bash
dotnet build
cd TransactionApi.WebApi
dotnet run
```

### 3. Access the API

Open your browser and navigate to:
- Swagger UI: https://localhost:7001/swagger
- API Endpoints: https://localhost:7001/api/transactions

## API Endpoints

### Transactions

- `GET /api/transactions`: List transactions with filtering options
- `GET /api/transactions/{id}`: Get transaction details
- `POST /api/transactions`: Create new transaction
- `PUT /api/transactions/{id}`: Update transaction
- `DELETE /api/transactions/{id}`: Delete transaction

### Analytics

- `GET /api/analytics/daily-summary`: Daily transaction aggregates
- `GET /api/analytics/category-summary`: Category distribution analysis
- `GET /api/analytics/time-comparison`: Compare transactions between time periods

## License

This project is licensed under the MIT License - see the LICENSE file for details.